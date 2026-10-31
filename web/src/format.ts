import type { BookingStatus } from './types';

// ---------- Licence plates (same rules as LicensePlateRules.cs) ----------

/** "abc-123 " -> "ABC123" */
export function normalizePlate(input: string): string {
  return input.replace(/[^\p{L}\p{N}]/gu, '').toUpperCase();
}

const OLD_PLATE = /^[A-Z]{3}[0-9]{3}$/;
const NEW_PLATE = /^[A-Z]{4}[0-9]{3}$/;

export function isValidPlate(input: string): boolean {
  const plate = normalizePlate(input);
  return OLD_PLATE.test(plate) || NEW_PLATE.test(plate);
}

/** "abc123" -> "ABC-123", "aaaa123" -> "AA AA-123"; anything else is just upper-cased. */
export function formatPlate(input: string): string {
  const plate = normalizePlate(input);
  if (OLD_PLATE.test(plate)) return `${plate.slice(0, 3)}-${plate.slice(3)}`;
  if (NEW_PLATE.test(plate)) return `${plate.slice(0, 2)} ${plate.slice(2, 4)}-${plate.slice(4)}`;
  return input.toUpperCase();
}

// ---------- Money and duration ----------

const huf = new Intl.NumberFormat('hu-HU', { style: 'currency', currency: 'HUF', maximumFractionDigits: 0 });

export function formatHuf(amount: number): string {
  return huf.format(amount);
}

export function formatDuration(minutes: number): string {
  const hours = Math.floor(minutes / 60);
  const rest = minutes % 60;
  if (hours === 0) return `${rest} perc`;
  if (rest === 0) return `${hours} óra`;
  return `${hours} óra ${rest} perc`;
}

// ---------- Dates and times ----------
// The API works in local workshop time with strings like "2026-11-03T09:00:00",
// so the UI never converts between time zones.

/** Date -> "2026-11-03" (local date, not UTC). */
export function toIsoDate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** "2026-11-03T09:00:00" -> "09:00" */
export function timeOf(isoDateTime: string): string {
  return isoDateTime.slice(11, 16);
}

/** "2026-11-03" -> local Date at midnight. */
export function parseIsoDate(isoDate: string): Date {
  const [y, m, d] = isoDate.split('-').map(Number);
  return new Date(y, m - 1, d);
}

/** The next `count` weekdays, starting with `from` if it is a weekday. */
export function upcomingWorkdays(from: Date, count: number): string[] {
  const days: string[] = [];
  const cursor = new Date(from.getFullYear(), from.getMonth(), from.getDate());
  while (days.length < count) {
    const weekday = cursor.getDay();
    if (weekday !== 0 && weekday !== 6) days.push(toIsoDate(cursor));
    cursor.setDate(cursor.getDate() + 1);
  }
  return days;
}

const weekdayShort = new Intl.DateTimeFormat('hu-HU', { weekday: 'short' });
const monthDay = new Intl.DateTimeFormat('hu-HU', { month: 'short', day: 'numeric' });
const longDate = new Intl.DateTimeFormat('hu-HU', { year: 'numeric', month: 'long', day: 'numeric', weekday: 'long' });

/** "2026-11-03" -> { weekday: "K", day: "nov. 3." } */
export function dayLabel(isoDate: string): { weekday: string; day: string } {
  const date = parseIsoDate(isoDate);
  return { weekday: weekdayShort.format(date), day: monthDay.format(date) };
}

/** "2026-11-03T09:00:00" -> "2026. november 3., kedd" */
export function longDateOf(isoDateTime: string): string {
  return longDate.format(parseIsoDate(isoDateTime.slice(0, 10)));
}

// ---------- Statuses ----------

export const statusLabels: Record<BookingStatus, string> = {
  Booked: 'Foglalva',
  InProgress: 'Szerelés alatt',
  Completed: 'Kész',
  Cancelled: 'Lemondva',
};
