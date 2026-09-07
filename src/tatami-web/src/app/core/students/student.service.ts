import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  EnrollStudentRequest,
  EnrollStudentResponse,
  Student,
  UpdateStudentRequest,
} from './student.models';

@Injectable({ providedIn: 'root' })
export class StudentService {
  constructor(private readonly http: HttpClient) {}

  list(options?: { search?: string; active?: boolean }) {
    let params = new HttpParams();
    if (options?.search) {
      params = params.set('search', options.search);
    }
    if (options?.active !== undefined) {
      params = params.set('active', String(options.active));
    }

    return this.http.get<Student[]>(`${environment.apiUrl}/api/students`, { params });
  }

  getById(id: string) {
    return this.http.get<Student>(`${environment.apiUrl}/api/students/${id}`);
  }

  enroll(request: EnrollStudentRequest) {
    return this.http.post<EnrollStudentResponse>(
      `${environment.apiUrl}/api/students/enroll`,
      request,
    );
  }

  update(id: string, request: UpdateStudentRequest) {
    return this.http.put<Student>(`${environment.apiUrl}/api/students/${id}`, request);
  }

  deactivate(id: string) {
    return this.http.post<Student>(
      `${environment.apiUrl}/api/students/${id}/deactivate`,
      {},
    );
  }

  activate(id: string) {
    return this.http.post<Student>(
      `${environment.apiUrl}/api/students/${id}/activate`,
      {},
    );
  }
}
