import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PixQrService {
  async dataUrl(payload: string): Promise<string> {
    const qrcode = await import('qrcode');
    return qrcode.toDataURL(payload, { width: 300, margin: 2, errorCorrectionLevel: 'M' });
  }
}