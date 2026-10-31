// Mirrors the API's JSON (see src/ServiceBooking.Api/Contracts).

export type BookingStatus = 'Booked' | 'InProgress' | 'Completed' | 'Cancelled';

export interface ServiceType {
  id: number;
  name: string;
  durationMinutes: number;
  priceHuf: number;
}

export interface Booking {
  id: number;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  licensePlate: string;
  serviceTypeId: number;
  serviceName: string;
  /** Local workshop time, e.g. "2026-11-03T09:00:00" */
  startTime: string;
  endTime: string;
  bayNumber: number;
  status: BookingStatus;
  note: string | null;
}

export interface CreateBookingRequest {
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  licensePlate: string;
  serviceTypeId: number;
  startTime: string;
  note?: string;
}
