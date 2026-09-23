import { ApiError } from '../api/http';

/** The API stores UTC. SQL Server can return dates without a "Z", so add it before parsing. */
function parseUtc(iso: string): Date {
  return new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(iso) ? iso : `${iso}Z`);
}

export function formatDate(iso: string): string {
  return parseUtc(iso).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

/** "2026-09" -> "Sep" */
export function formatMonth(yyyyMm: string): string {
  const [year, month] = yyyyMm.split('-').map(Number);
  return new Date(year, month - 1, 1).toLocaleDateString(undefined, { month: 'short' });
}

export function formatGrade(grade: number | null): string {
  return grade === null ? '-' : `${Number(grade.toFixed(1))}`;
}

// Muted, distinct hues used to colour-code categories consistently across the app.
const CATEGORY_COLORS = ['#1b6b70', '#4453a6', '#85407a', '#66741f', '#2b6f9e', '#4a5a60'];

export function categoryColor(category: string): string {
  let hash = 0;
  for (const char of category) hash = (hash * 31 + char.charCodeAt(0)) >>> 0;
  return CATEGORY_COLORS[hash % CATEGORY_COLORS.length];
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.details;
  if (error instanceof Error) return error.message;
  return 'Something went wrong. Try again.';
}

/** First validation message for a field (API keys are camelCase, e.g. "fullName"). */
export function fieldError(error: unknown, field: string): string | undefined {
  return error instanceof ApiError ? error.fieldErrors[field]?.[0] : undefined;
}
