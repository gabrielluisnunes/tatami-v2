import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { LiveCameraCaptureComponent } from '../../components/live-camera-capture/live-camera-capture.component';
import { StudentProfileService } from '../../../../core/students/student-profile.service';

type WizardStep = 'payment-day' | 'instructions' | 'camera' | 'saving' | 'saved';

@Component({
  selector: 'app-completar-perfil',
  imports: [LiveCameraCaptureComponent],
  templateUrl: './completar-perfil.component.html',
  styleUrl: './completar-perfil.component.scss',
})
export class CompletarPerfilComponent implements OnInit {
  private readonly profileService = inject(StudentProfileService);
  private readonly router = inject(Router);

  readonly days = Array.from({ length: 31 }, (_, i) => i + 1);
  readonly instructions = [
    'Fundo branco ou claro atrás de você',
    'Sem óculos nem brincos',
    'Cabelo preso (se aplicável)',
    'Ambiente bem iluminado, rosto centralizado',
    'Use apenas a câmera — upload de arquivo não é permitido',
  ];

  readonly step = signal<WizardStep>('payment-day');
  readonly paymentDay = signal<number | null>(null);
  readonly firstName = signal('Aluno');
  readonly errorMessage = signal('');
  readonly loading = signal(true);

  private photoBase64: string | null = null;
  private faceDescriptor: number[] | null = null;

  ngOnInit(): void {
    this.profileService.getMe().subscribe({
      next: profile => {
        this.loading.set(false);
        this.firstName.set(profile.fullName.split(' ')[0] || 'Aluno');
        if (profile.isProfileComplete) {
          void this.router.navigateByUrl('/aluno');
          return;
        }

        if (profile.paymentDueDay) {
          this.paymentDay.set(profile.paymentDueDay);
          this.step.set('instructions');
        } else {
          this.step.set('payment-day');
        }
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Não foi possível carregar seu perfil.');
      },
    });
  }

  selectDay(day: number): void {
    this.paymentDay.set(day);
  }

  goToInstructions(): void {
    if (!this.paymentDay()) {
      this.errorMessage.set('Selecione o dia de vencimento.');
      return;
    }
    this.errorMessage.set('');
    this.step.set('instructions');
  }

  goToCamera(): void {
    this.errorMessage.set('');
    this.step.set('camera');
  }

  onCapture(event: { photoBase64: string; faceDescriptor: number[] }): void {
    this.photoBase64 = event.photoBase64;
    this.faceDescriptor = event.faceDescriptor;
  }

  onResetCapture(): void {
    this.photoBase64 = null;
    this.faceDescriptor = null;
    this.errorMessage.set('');
  }

  save(): void {
    const dueDay = this.paymentDay();
    if (!dueDay || !this.photoBase64 || !this.faceDescriptor) {
      this.errorMessage.set('Capture a foto com o rosto detectado para continuar.');
      return;
    }

    this.step.set('saving');
    this.errorMessage.set('');

    this.profileService
      .completeProfile({
        paymentDueDay: dueDay,
        photoBase64: this.photoBase64,
        faceDescriptor: this.faceDescriptor,
      })
      .subscribe({
        next: () => {
          this.step.set('saved');
          setTimeout(() => void this.router.navigateByUrl('/aluno'), 1200);
        },
        error: (error: HttpErrorResponse) => {
          this.step.set('camera');
          this.errorMessage.set(
            error.error?.error ?? 'Não foi possível salvar o perfil.',
          );
        },
      });
  }
}
