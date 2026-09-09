export interface StudentSport {
  sport: string;
  belt?: string | null;
  degree: number;
  beltUpdatedAt?: string;
}

export interface StudentSportInput {
  sport: string;
  belt?: string | null;
  degree: number;
}

export interface Student {
  id: string;
  academyId: string;
  userId: string;
  fullName: string;
  email: string;
  phone?: string | null;
  emergencyPhone?: string | null;
  birthDate?: string | null;
  cep?: string | null;
  address?: string | null;
  neighborhood?: string | null;
  city?: string | null;
  state?: string | null;
  paymentDueDay?: number | null;
  photoUrl?: string | null;
  isActive: boolean;
  isProfileComplete: boolean;
  sports: StudentSport[];
  createdAt: string;
}

export interface EnrollStudentRequest {
  fullName: string;
  email: string;
  phone?: string | null;
  emergencyPhone?: string | null;
  birthDate?: string | null;
  cep?: string | null;
  address?: string | null;
  neighborhood?: string | null;
  city?: string | null;
  state?: string | null;
  sports: StudentSportInput[];
}

export interface UpdateStudentRequest {
  fullName: string;
  phone?: string | null;
  emergencyPhone?: string | null;
  birthDate?: string | null;
  cep?: string | null;
  address?: string | null;
  neighborhood?: string | null;
  city?: string | null;
  state?: string | null;
  sports: StudentSportInput[];
}

export interface EnrollStudentResponse {
  student: Student;
  emailSent: boolean;
  temporaryPassword?: string | null;
}

export const STUDENT_SPORT_OPTIONS = [
  { value: 'jiu-jitsu', label: 'Jiu-Jitsu' },
  { value: 'muay thai', label: 'Muay Thai' },
  { value: 'boxe', label: 'Boxe' },
] as const;

export const BELT_OPTIONS = [
  'branca',
  'azul',
  'roxa',
  'marrom',
  'preta',
] as const;
