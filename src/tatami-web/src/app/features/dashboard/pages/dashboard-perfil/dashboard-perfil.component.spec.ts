import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Academy, PixKeyType } from '../../../../core/academy/academy.models';
import { DashboardPerfilComponent } from './dashboard-perfil.component';

describe('DashboardPerfilComponent PIX', () => {
  let component: DashboardPerfilComponent;
  let http: HttpTestingController;
  const academy: Academy = {
    id: 'academy', name: 'Academia', sport: 'jiu-jitsu', monthlyPrice: 100,
    subscriptionStatus: 'trial', ownerId: 'admin',
    pixKey: 'academia@example.com', pixKeyType: 'email',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    http = TestBed.inject(HttpTestingController);
    component = TestBed.runInInjectionContext(() => new DashboardPerfilComponent());
    component.ngOnInit();
    const request = http.expectOne(req => req.url.endsWith('/api/academies/me'));
    expect(request.request.method).toBe('GET');
    request.flush(academy);
  });

  afterEach(() => http.verify());

  it('carrega a chave e o tipo existentes', () => {
    expect(component.academyForm.getRawValue()).toEqual(jasmine.objectContaining({
      pixKey: academy.pixKey, pixKeyType: academy.pixKeyType,
    }));
  });

  const validKeys: [PixKeyType, string][] = [
    ['celular', '+5511999999999'],
    ['email', 'academia@example.com'],
    ['cpf', '12345678909'],
    ['cnpj', '12345678000195'],
    ['aleatoria', '123e4567-e89b-42d3-a456-426614174000'],
  ];

  for (const [pixKeyType, pixKey] of validKeys) {
    it(`envia chave ${pixKeyType} válida e sem espaços externos`, () => {
      component.academyForm.patchValue({ pixKeyType, pixKey: ` ${pixKey} ` });
      component.saveAcademy();
      const request = http.expectOne(req => req.method === 'PUT' && req.url.endsWith('/api/academies/me'));
      expect(request.request.body).toEqual({
        name: academy.name, sport: academy.sport, monthlyPrice: academy.monthlyPrice,
        pixKey, pixKeyType,
      });
      request.flush({ ...academy, pixKey, pixKeyType });
      expect(component.academy?.pixKey).toBe(pixKey);
      expect(component.academyLoading).toBeFalse();
    });
  }

  const invalidKeys: [PixKeyType | '', string][] = [
    ['', 'academia@example.com'],
    ['email', '   '],
    ['email', 'sem-arroba'],
    ['email', 'a b@example.com'],
    ['email', `${'a'.repeat(250)}@example.com`],
    ['celular', '11999999999'],
    ['celular', '+01234567890'],
    ['cpf', '1234567890'],
    ['cpf', '1234567890a'],
    ['cnpj', '1234567800019'],
    ['aleatoria', 'nao-e-uuid'],
  ];

  for (const [pixKeyType, pixKey] of invalidKeys) {
    it(`bloqueia par PIX inválido: ${pixKeyType}/${pixKey}`, () => {
      component.academyForm.patchValue({ pixKeyType, pixKey });
      expect(component.academyForm.hasError('pix')).toBeTrue();
      component.saveAcademy();
      http.expectNone(req => req.method === 'PUT');
    });
  }

  it('envia null em ambos os campos para remover o PIX', () => {
    component.academyForm.patchValue({ pixKey: '   ', pixKeyType: '' });
    component.saveAcademy();
    const request = http.expectOne(req => req.method === 'PUT');
    expect(request.request.body.pixKey).toBeNull();
    expect(request.request.body.pixKeyType).toBeNull();
    request.flush({ ...academy, pixKey: null, pixKeyType: null });
  });
});