import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface StudentMeProfile {
  id: string;
  academyId: string;
  userId: string;
  fullName: string;
  email: string;
  paymentDueDay: number | null;
  photoUrl: string | null;
  isProfileComplete: boolean;
}

export interface CompleteStudentProfileRequest {
  paymentDueDay: number;
  photoBase64: string;
  faceDescriptor: number[];
}

@Injectable({ providedIn: 'root' })
export class StudentProfileService {
  constructor(private readonly http: HttpClient) {}

  getMe() {
    return this.http.get<StudentMeProfile>(`${environment.apiUrl}/api/students/me`);
  }

  completeProfile(request: CompleteStudentProfileRequest) {
    return this.http.post<StudentMeProfile>(
      `${environment.apiUrl}/api/students/me/complete-profile`,
      request,
    );
  }
}
