import { signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import {
  IdleSessionService,
  LAST_ACTIVITY_KEY,
  SESSION_EXPIRED_REASON,
} from './idle-session.service';

describe('IdleSessionService', () => {
  const authenticated = signal(true);
  const logout = jasmine.createSpy('logout').and.callFake(() => {
    authenticated.set(false);
  });
  const navigate = jasmine.createSpy('navigate').and.resolveTo(true);

  let service: IdleSessionService;

  beforeEach(() => {
    authenticated.set(false);
    logout.calls.reset();
    navigate.calls.reset();
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        IdleSessionService,
        {
          provide: AuthService,
          useValue: {
            authenticated,
            isAuthenticated: () => authenticated(),
            getAccessToken: () => (authenticated() ? 'token' : null),
            logout,
          },
        },
        {
          provide: Router,
          useValue: { navigate },
        },
      ],
    });

    service = TestBed.inject(IdleSessionService);
  });

  afterEach(() => {
    service.stop();
    localStorage.clear();
  });

  it('encerra sessão antiga ao inicializar depois do período ocioso', () => {
    authenticated.set(true);
    localStorage.setItem(
      LAST_ACTIVITY_KEY,
      String(Date.now() - 16 * 60 * 1000),
    );

    service.initialize();

    expect(logout).toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('mantém sessão ativa quando há atividade recente', () => {
    authenticated.set(true);
    localStorage.setItem(LAST_ACTIVITY_KEY, String(Date.now()));

    service.initialize();

    expect(logout).not.toHaveBeenCalled();
  });

  it('encerra a sessão e redireciona após inatividade', fakeAsync(() => {
    authenticated.set(true);
    localStorage.setItem(LAST_ACTIVITY_KEY, String(Date.now()));
    service.start();

    tick(15 * 60 * 1000 + 30_000);

    expect(logout).toHaveBeenCalled();
    expect(navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { reason: SESSION_EXPIRED_REASON },
    });
  }));
});
