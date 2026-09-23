export type EnrollmentStatus = 'Active' | 'Completed' | 'Dropped';

export interface User {
  id: number;
  email: string;
  fullName: string;
  isAdmin: boolean;
  isInstructor: boolean;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface Course {
  id: number;
  title: string;
  description: string;
  category: string;
  instructorId: number;
  instructorName: string;
  createdAt: string;
  enrolledCount: number;
}

export interface CourseInput {
  title: string;
  description: string;
  category: string;
  instructorId: number;
}

export interface Enrollment {
  id: number;
  studentId: number;
  studentName: string;
  studentEmail: string;
  courseId: number;
  courseTitle: string;
  courseCategory: string;
  instructorName: string;
  status: EnrollmentStatus;
  enrolledAt: string;
  grade: number | null;
}

export interface AdminDashboard {
  totalStudents: number;
  totalInstructors: number;
  totalCourses: number;
  totalEnrollments: number;
  activeEnrollments: number;
  completedEnrollments: number;
  droppedEnrollments: number;
  averageGrade: number | null;
  topCourses: { courseId: number; title: string; count: number }[];
  enrollmentTrend: { month: string; count: number }[];
}

export interface CourseFilters {
  search?: string;
  category?: string;
  instructor?: string;
}

export type UserRole = 'student' | 'instructor' | 'admin';
