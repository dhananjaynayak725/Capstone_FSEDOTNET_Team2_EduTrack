const BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? '';
const TOKEN_KEY = 'edutrack.token';

/** Shape of every error the API returns: { timestamp, path, error, message, errors? } */
interface ApiErrorBody {
  timestamp?: string;
  path?: string;
  error?: string;
  message?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  /** One readable line: field-level details when present, otherwise the message. */
  get details(): string {
    const fields = Object.values(this.fieldErrors).flat();
    return fields.length > 0 ? fields.join(' ') : this.message;
  }
}

export const tokenStore = {
  get: (): string | null => localStorage.getItem(TOKEN_KEY),
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

let unauthorizedHandler: (() => void) | null = null;

/** Called when a signed-in request comes back 401 (expired or revoked token). */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

async function toApiError(response: Response): Promise<ApiError> {
  let body: ApiErrorBody | null = null;
  try {
    body = (await response.json()) as ApiErrorBody;
  } catch {
    body = null;
  }
  const message = body?.message ?? `Request failed (${response.status}).`;
  return new ApiError(response.status, message, body?.errors ?? {});
}

async function send(path: string, method: string, body?: unknown): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = tokenStore.get();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response: Response;
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, 'Cannot reach the server. Check that the API is running and try again.');
  }

  if (!response.ok) {
    if (response.status === 401 && token && !path.startsWith('/api/auth/login')) {
      unauthorizedHandler?.();
    }
    throw await toApiError(response);
  }
  return response;
}

async function request<T>(path: string, method: string, body?: unknown): Promise<T> {
  const response = await send(path, method, body);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const http = {
  get: <T>(path: string) => request<T>(path, 'GET'),
  post: <T>(path: string, body?: unknown) => request<T>(path, 'POST', body ?? {}),
  put: <T>(path: string, body?: unknown) => request<T>(path, 'PUT', body ?? {}),
  delete: (path: string) => request<void>(path, 'DELETE'),
};

/** Downloads a file from an authenticated endpoint (a plain link cannot send the bearer token). */
export async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const response = await send(path, 'GET');
  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition);
  const filename = match ? decodeURIComponent(match[1]) : fallbackName;

  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/** Builds ?a=1&b=2 while skipping empty values. */
export function toQuery(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== '') {
      search.set(key, String(value));
    }
  });
  const text = search.toString();
  return text ? `?${text}` : '';
}
