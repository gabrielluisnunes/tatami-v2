import { Injectable, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export const LAST_ACTIVITY_KEY = 'tatami_last_activity_at';
export const SESSION_EXPIRED_REASON = 'expired';

const ACTIVITY_EVENTS = [
  'mousedown',
  'mousemove',
  'keydown',
  'scroll',
  'touchstart',
  'click',
] as const;

@Injectable({ providedIn: 'root' })
export class IdleSessionService {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly timeoutMs = environment.idleTimeoutMinutes * 60 * 1000;
  private readonly checkEveryMs = 30_000;
  private readonly activityThrottleMs = 1_000;

  private started = false;
  private checkTimer: ReturnType<typeof setInterval> | null = null;
  private lastRecordedAt = 0;
  private readonly onActivity = () => this.recordActivity();
  private readonly onVisibilityChange = () => {
    if (document.visibilityState === 'visible') {
      this.expireIfIdle();
    }
  };
  private readonly onStorage = (event: StorageEvent) => {
    if (event.key === LAST_ACTIVITY_KEY) {
      this.lastRecordedAt = 0;
      this.expireIfIdle();
    }
  };

  constructor() {
    effect(() => {
      if (this.authService.authenticated()) {
        this.start();
        return;
      }

      this.stop();
    });
  }

  initialize(): void {
    if (!this.authService.isAuthenticated()) {
      if (this.authService.getAccessToken()) {
        this.expire(false);
      }
      return;
    }

    if (this.hasIdleExpired()) {
      this.expire(false);
      return;
    }

    this.start();
  }

  hasIdleExpired(): boolean {
    const lastActivity = this.readLastActivity();
    if (lastActivity === null) {
      return false;
    }

    return Date.now() - lastActivity >= this.timeoutMs;
  }

  start(): void {
    if (this.started) {
      return;
    }

    if (!this.authService.isAuthenticated()) {
      return;
    }

    if (this.hasIdleExpired()) {
      this.expire(true);
      return;
    }

    this.started = true;
    this.recordActivity(true);
    this.bindListeners();
    this.checkTimer = setInterval(() => this.expireIfIdle(), this.checkEveryMs);
  }

  stop(): void {
    if (!this.started && !this.checkTimer) {
      this.unbindListeners();
      return;
    }

    this.started = false;
    this.unbindListeners();
    if (this.checkTimer) {
      clearInterval(this.checkTimer);
      this.checkTimer = null;
    }
  }

  private expireIfIdle(): void {
    if (!this.authService.isAuthenticated() || !this.hasIdleExpired()) {
      return;
    }

    this.expire(true);
  }

  private expire(navigate: boolean): void {
    this.stop();
    if (this.authService.authenticated() || this.authService.getAccessToken()) {
      this.authService.logout();
    }
    localStorage.removeItem(LAST_ACTIVITY_KEY);

    if (!navigate) {
      return;
    }

    void this.router.navigate(['/login'], {
      queryParams: { reason: SESSION_EXPIRED_REASON },
    });
  }

  private recordActivity(force = false): void {
    if (!this.authService.isAuthenticated()) {
      return;
    }

    if (document.visibilityState === 'hidden') {
      return;
    }

    const now = Date.now();
    if (!force && now - this.lastRecordedAt < this.activityThrottleMs) {
      return;
    }

    this.lastRecordedAt = now;
    localStorage.setItem(LAST_ACTIVITY_KEY, String(now));
  }

  private readLastActivity(): number | null {
    const raw = localStorage.getItem(LAST_ACTIVITY_KEY);
    if (!raw) {
      return null;
    }

    const value = Number(raw);
    return Number.isFinite(value) ? value : null;
  }

  private bindListeners(): void {
    for (const eventName of ACTIVITY_EVENTS) {
      window.addEventListener(eventName, this.onActivity, { passive: true });
    }

    document.addEventListener('visibilitychange', this.onVisibilityChange);
    window.addEventListener('storage', this.onStorage);
  }

  private unbindListeners(): void {
    for (const eventName of ACTIVITY_EVENTS) {
      window.removeEventListener(eventName, this.onActivity);
    }

    document.removeEventListener('visibilitychange', this.onVisibilityChange);
    window.removeEventListener('storage', this.onStorage);
  }
}
