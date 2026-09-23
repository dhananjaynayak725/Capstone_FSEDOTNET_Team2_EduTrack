import { downloadFile, http, toQuery } from './http';
import type {
  AdminDashboard,
  AuthResponse,
  Course,
  CourseFilters,
  CourseInput,
  Enrollment,
  EnrollmentStatus,
  User,
  UserRole,
} from './types';

export const authApi = {
  login: (email: string, password: string) => http.post<AuthResponse>('/api/auth/login', { email, password }),
  register: (fullName: string, email: string, password: string) =>
    http.post<AuthResponse>('/api/auth/register', { fullName, email, password }),
  logout: () => http.post<{ message: string }>('/api/auth/logout'),
  me: () => http.get<User>('/api/auth/me'),
};

export const coursesApi = {
  list: (filters: CourseFilters = {}) => http.get<Course[]>(`/api/courses${toQuery({ ...filters })}`),
  categories: () => http.get<string[]>('/api/courses/categories'),
  get: (id: number) => http.get<Course>(`/api/courses/${id}`),
  create: (input: CourseInput) => http.post<Course>('/api/courses', input),
  update: (id: number, input: CourseInput) => http.put<Course>(`/api/courses/${id}`, input),
  remove: (id: number) => http.delete(`/api/courses/${id}`),
};

export const enrollmentsApi = {
  enroll: (courseId: number) => http.post<Enrollment>('/api/enrollments', { courseId }),
  mine: () => http.get<Enrollment[]>('/api/enrollments/mine'),
  all: (filters: { courseId?: number; status?: EnrollmentStatus } = {}) =>
    http.get<Enrollment[]>(`/api/enrollments${toQuery({ ...filters })}`),
  updateStatus: (id: number, status: EnrollmentStatus, grade?: number | null) =>
    http.put<Enrollment>(`/api/enrollments/${id}/status`, { status, grade: grade ?? null }),
};

export const usersApi = {
  list: (filters: { search?: string; role?: UserRole } = {}) => http.get<User[]>(`/api/users${toQuery({ ...filters })}`),
  update: (id: number, input: { fullName: string; isInstructor: boolean }) => http.put<User>(`/api/users/${id}`, input),
  remove: (id: number) => http.delete(`/api/users/${id}`),
};

export const adminApi = {
  dashboard: () => http.get<AdminDashboard>('/api/admin/dashboard'),
  exportEnrollments: () => downloadFile('/api/admin/reports/enrollments.csv', 'enrollments.csv'),
};
