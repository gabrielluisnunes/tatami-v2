import { Injectable } from '@angular/core';

type FaceApiModule = typeof import('@vladmandic/face-api');

@Injectable({ providedIn: 'root' })
export class FaceApiLoaderService {
  private loadPromise: Promise<FaceApiModule> | null = null;
  private api: FaceApiModule | null = null;

  load(): Promise<FaceApiModule> {
    if (!this.loadPromise) {
      this.loadPromise = (async () => {
        const faceapi = await import('@vladmandic/face-api');
        const modelUrl = '/models';
        await Promise.all([
          faceapi.nets.ssdMobilenetv1.loadFromUri(modelUrl),
          faceapi.nets.faceLandmark68Net.loadFromUri(modelUrl),
          faceapi.nets.faceRecognitionNet.loadFromUri(modelUrl),
        ]);
        this.api = faceapi;
        return faceapi;
      })();
    }

    return this.loadPromise;
  }

  async getApi(): Promise<FaceApiModule> {
    if (this.api) {
      return this.api;
    }
    return this.load();
  }
}
