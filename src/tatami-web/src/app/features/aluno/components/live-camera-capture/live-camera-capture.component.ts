import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
  output,
  signal,
} from '@angular/core';
import { FaceApiLoaderService } from '../../../../core/face/face-api-loader.service';

type CameraStatus =
  | 'idle'
  | 'requesting'
  | 'streaming'
  | 'capturing'
  | 'detecting'
  | 'success'
  | 'no-face'
  | 'camera-denied'
  | 'error'
  | 'models-loading'
  | 'models-error';

@Component({
  selector: 'app-live-camera-capture',
  templateUrl: './live-camera-capture.component.html',
  styleUrl: './live-camera-capture.component.scss',
})
export class LiveCameraCaptureComponent implements OnInit, OnDestroy {
  private readonly faceApiLoader = inject(FaceApiLoaderService);

  readonly captured = output<{ photoBase64: string; faceDescriptor: number[] }>();
  readonly reset = output<void>();

  @ViewChild('video') videoRef?: ElementRef<HTMLVideoElement>;
  @ViewChild('canvas') canvasRef?: ElementRef<HTMLCanvasElement>;

  readonly status = signal<CameraStatus>('idle');
  readonly errorMessage = signal<string | null>(null);
  readonly capturedImage = signal<string | null>(null);

  private stream: MediaStream | null = null;

  async ngOnInit(): Promise<void> {
    this.status.set('models-loading');
    try {
      await this.faceApiLoader.load();
      this.status.set('idle');
    } catch {
      this.status.set('models-error');
      this.errorMessage.set('Não foi possível carregar o reconhecimento facial.');
    }
  }

  ngOnDestroy(): void {
    this.stopStream();
  }

  async startCamera(): Promise<void> {
    this.status.set('requesting');
    this.capturedImage.set(null);
    this.errorMessage.set(null);
    this.stopStream();

    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: {
          facingMode: 'user',
          width: { ideal: 640 },
          height: { ideal: 480 },
        },
      });
      this.stream = stream;
      const video = this.videoRef?.nativeElement;
      if (video) {
        video.srcObject = stream;
        await video.play();
      }
      this.status.set('streaming');
    } catch (err) {
      const name = err && typeof err === 'object' && 'name' in err ? String((err as { name?: string }).name) : '';
      if (name === 'NotAllowedError' || name === 'PermissionDeniedError') {
        this.status.set('camera-denied');
        this.errorMessage.set(
          'Permissão de câmera negada. Habilite o acesso nas configurações do navegador.',
        );
      } else if (name === 'NotFoundError') {
        this.status.set('error');
        this.errorMessage.set('Câmera não encontrada.');
      } else {
        this.status.set('error');
        this.errorMessage.set('Não foi possível acessar a câmera.');
      }
    }
  }

  async capture(): Promise<void> {
    const video = this.videoRef?.nativeElement;
    const canvas = this.canvasRef?.nativeElement;
    if (!video || !canvas || this.status() !== 'streaming') {
      return;
    }

    this.status.set('capturing');
    const SIZE = 600;
    canvas.width = SIZE;
    canvas.height = SIZE;
    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    let sx = 0;
    let sy = 0;
    let sw = video.videoWidth;
    let sh = video.videoHeight;
    if (sw > sh) {
      sx = (sw - sh) / 2;
      sw = sh;
    } else {
      sy = (sh - sw) / 2;
      sh = sw;
    }

    // Un-mirror for correct face matching
    ctx.save();
    ctx.translate(SIZE, 0);
    ctx.scale(-1, 1);
    ctx.drawImage(video, sx, sy, sw, sh, 0, 0, SIZE, SIZE);
    ctx.restore();

    const photoBase64 = canvas.toDataURL('image/jpeg', 0.9);
    this.capturedImage.set(photoBase64);
    this.stopStream();
    this.status.set('detecting');

    try {
      const faceapi = await this.faceApiLoader.getApi();
      const detection = await faceapi
        .detectSingleFace(canvas, new faceapi.SsdMobilenetv1Options({ minConfidence: 0.5 }))
        .withFaceLandmarks()
        .withFaceDescriptor();

      if (!detection) {
        this.status.set('no-face');
        this.errorMessage.set('Nenhum rosto detectado. Centralize o rosto e tente novamente.');
        return;
      }

      const descriptor = Array.from(detection.descriptor) as number[];
      this.status.set('success');
      this.captured.emit({ photoBase64, faceDescriptor: descriptor });
    } catch {
      this.status.set('error');
      this.errorMessage.set('Falha ao analisar o rosto. Tente novamente.');
    }
  }

  retake(): void {
    this.reset.emit();
    this.capturedImage.set(null);
    this.errorMessage.set(null);
    void this.startCamera();
  }

  private stopStream(): void {
    this.stream?.getTracks().forEach(track => track.stop());
    this.stream = null;
  }
}
