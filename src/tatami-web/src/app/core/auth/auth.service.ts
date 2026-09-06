import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  AuthUser,
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
} from './auth.models';

const ACCESS_TOKEN_KEY = 'tatami_access_token';
const REFRESH_TOKEN_KEY = 'tatami_refresh_token';
const USER_KEY = 'tatami_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly authenticated = signal(false);
  readonly currentUser = signal<AuthUser | null>(null);

  constructor(private readonly http: HttpClient) {
    this.authenticated.set(!!localStorage.getItem(ACCESS_TOKEN_KEY));
    this.currentUser.set(this.readStoredUser());
  }

  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(tap(response => this.persistSession(response)));
  }

  register(request: RegisterRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, request)
      .pipe(tap(response => this.persistSession(response)));
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.http
      .patch<AuthResponse>(`${environment.apiUrl}/api/auth/profile`, request)
      .pipe(tap(response => this.persistSession(response)));
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.post<{ message: string }>(
      `${environment.apiUrl}/api/auth/change-password`,
      request,
    );
  }

  logout() {
    const refreshToken = localStorage.getItem(REFRESH_TOKEN_KEY);
    if (refreshToken) {
      this.http
        .post(`${environment.apiUrl}/api/auth/logout`, { refreshToken })
        .subscribe({ error: () => undefined });
    }

    this.clearSession();
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  getUser(): AuthUser | null {
    return this.currentUser() ?? this.readStoredUser();
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken();
    return !!token && token.split('.').length === 3;
  }

  needsOnboarding(): boolean {
    const user = this.getUser();
    return !!user && user.role === 'admin' && !user.academyId;
  }

  getRoleHomeRoute(): string {
    const user = this.getUser();
    if (!user) {
      return '/login';
    }

    if (user.role === 'admin') {
      return user.academyId ? '/dashboard' : '/onboarding';
    }

    if (user.role === 'professor') {
      return '/professor';
    }

    if (user.role === 'aluno') {
      return '/aluno';
    }

    return '/login';
  }

  getPostLoginRoute(): string {
    return this.getRoleHomeRoute();
  }

  updateSession(response: AuthResponse): void {
    this.persistSession(response);
  }

  clearSession(): void {
    this.clearSessionInternal();
  }

  private persistSession(response: AuthResponse): void {
    if (!response?.accessToken) {
      throw new Error('Resposta de autenticação inválida.');
    }

    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    this.currentUser.set(response.user);
    this.authenticated.set(true);
  }

  private clearSessionInternal(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.authenticated.set(false);
  }

  private readStoredUser(): AuthUser | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthUser;
    } catch {
      return null;
    }
  }
}
