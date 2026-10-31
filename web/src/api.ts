import type { Booking, BookingStatus, CreateBookingRequest, ServiceType } from './types';

/** An error the UI can show as it is: the API already sends Hungarian messages. */
export class ApiError extends Error {
  readonly status: number;
  readonly messages: string[];

  constructor(status: number, messages: string[]) {
    super(messages.join(' '));
    this.status = status;
    this.messages = messages;
  }
}

/** Messages to show for any error thrown by the api calls. */
export function errorMessages(error: unknown): string[] {
  return error instanceof ApiError ? error.messages : ['Váratlan hiba történt. Frissítsd az oldalt, és próbáld újra.'];
}

async function request<T>(path: string, init: RequestInit = {}, adminKey?: string): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (init.body) headers['Content-Type'] = 'application/json';
  if (adminKey) headers['X-Admin-Key'] = adminKey;

  let response: Response;
  try {
    response = await fetch(path, { ...init, headers });
  } catch {
    throw new ApiError(0, ['Nem érem el a szervert. Ellenőrizd az internetkapcsolatot, és próbáld újra.']);
  }

  if (!response.ok) {
    throw new ApiError(response.status, await readErrors(response));
  }

  return (await response.json()) as T;
}

async function readErrors(response: Response): Promise<string[]> {
  try {
    const body: unknown = await response.json();
    if (body && typeof body === 'object' && 'errors' in body) {
      const errors = (body as { errors: unknown }).errors;
      if (Array.isArray(errors)) return errors.map(String);
      if (errors && typeof errors === 'object') return Object.values(errors).flat().map(String);
    }
  } catch {
    // The body was not JSON; fall back to a generic message below.
  }
  return [fallbackMessage(response.status)];
}

function fallbackMessage(status: number): string {
  if (status === 401) return 'Hiányzó vagy hibás admin kulcs.';
  if (status === 404) return 'A keresett adat nem található.';
  if (status >= 500) return 'Hiba történt a szerveren. Próbáld újra pár perc múlva.';
  return 'A kérést nem sikerült teljesíteni.';
}

export const api = {
  services: () => request<ServiceType[]>('/api/services'),

  availability: (serviceTypeId: number, date: string) =>
    request<string[]>(`/api/availability?serviceTypeId=${serviceTypeId}&date=${date}`),

  createBooking: (body: CreateBookingRequest) =>
    request<Booking>('/api/bookings', { method: 'POST', body: JSON.stringify(body) }),

  cancelBooking: (id: number, email: string) =>
    request<Booking>(`/api/bookings/${id}/cancel`, { method: 'POST', body: JSON.stringify({ email }) }),

  listBookings: (date: string, adminKey: string) =>
    request<Booking[]>(`/api/bookings?date=${date}`, {}, adminKey),

  changeStatus: (id: number, status: BookingStatus, adminKey: string) =>
    request<Booking>(`/api/bookings/${id}/status`, { method: 'POST', body: JSON.stringify({ status }) }, adminKey),
};
