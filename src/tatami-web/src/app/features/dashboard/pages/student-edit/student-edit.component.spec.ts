import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { Student } from '../../../../core/students/student.models';
import { StudentEditComponent } from './student-edit.component';

describe('StudentEditComponent PaymentDueDay', () => {
  let component: StudentEditComponent;
  let http: HttpTestingController;
  const student: Student = {
    id: 'student', academyId: 'academy', userId: 'user',
    fullName: 'Aluno Teste', email: 'aluno@example.com', paymentDueDay: 15,
    isActive: true, isProfileComplete: true, createdAt: '2026-09-01T00:00:00Z',
    sports: [{ sport: 'jiu-jitsu', belt: 'branca', degree: 0 }],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(), provideHttpClientTesting(), provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: student.id }) } } },
      ],
    });
    spyOn(TestBed.inject(Router), 'navigateByUrl').and.resolveTo(true);
    http = TestBed.inject(HttpTestingController);
    component = TestBed.runInInjectionContext(() => new StudentEditComponent());
    component.ngOnInit();
    const request = http.expectOne(req => req.url.endsWith(`/api/students/${student.id}`));
    expect(request.request.method).toBe('GET');
    request.flush(student);
  });

  afterEach(() => http.verify());

  it('carrega o dia de vencimento existente', () => {
    expect(component.form.controls.paymentDueDay.value).toBe(15);
  });

  for (const paymentDueDay of [1, 15, 31, null]) {
    it(`envia dia de vencimento ${paymentDueDay}`, () => {
      component.form.controls.paymentDueDay.setValue(paymentDueDay);
      component.submit();
      const request = http.expectOne(req => req.method === 'PUT' && req.url.endsWith(`/api/students/${student.id}`));
      expect(request.request.body.paymentDueDay).toBe(paymentDueDay);
      expect(request.request.body.fullName).toBe(student.fullName);
      request.flush({ ...student, paymentDueDay });
      expect(component.saving).toBeFalse();
    });
  }

  for (const paymentDueDay of [-1, 0, 32, 1.5]) {
    it(`bloqueia dia de vencimento inválido ${paymentDueDay}`, () => {
      component.form.controls.paymentDueDay.setValue(paymentDueDay);
      expect(component.form.controls.paymentDueDay.invalid).toBeTrue();
      component.submit();
      http.expectNone(req => req.method === 'PUT');
    });
  }
});