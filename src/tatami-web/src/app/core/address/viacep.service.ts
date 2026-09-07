import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface ViaCepLookup {
  cep: string | null;
  logradouro: string | null;
  bairro: string | null;
  localidade: string | null;
  uf: string | null;
}

@Injectable({ providedIn: 'root' })
export class ViaCepService {
  constructor(private readonly http: HttpClient) {}

  lookup(cep: string) {
    const digits = cep.replace(/\D/g, '');
    const params = new HttpParams().set('cep', digits);
    return this.http.get<ViaCepLookup>(`${environment.apiUrl}/api/viacep`, { params });
  }

  formatCep(value: string): string {
    const digits = value.replace(/\D/g, '').slice(0, 8);
    if (digits.length <= 5) {
      return digits;
    }
    return `${digits.slice(0, 5)}-${digits.slice(5)}`;
  }
}
