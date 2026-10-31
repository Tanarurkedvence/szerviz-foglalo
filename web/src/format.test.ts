import { describe, expect, it } from 'vitest';
import { formatDuration, formatHuf, formatPlate, isValidPlate, normalizePlate, timeOf, upcomingWorkdays } from './format';

describe('licence plates', () => {
  it.each(['ABC-123', 'abc123', ' abc 123 ', 'AA AA-123', 'aaaa123'])('accepts %s', (plate) => {
    expect(isValidPlate(plate)).toBe(true);
  });

  it.each(['', 'AB-123', 'ABC-1234', '123-ABC', 'AAAAA-123', 'ÁBC-123'])('rejects "%s"', (plate) => {
    expect(isValidPlate(plate)).toBe(false);
  });

  it('normalises by removing spaces and dashes', () => {
    expect(normalizePlate('aa aa-123')).toBe('AAAA123');
  });

  it('formats old and new plates the official way', () => {
    expect(formatPlate('abc123')).toBe('ABC-123');
    expect(formatPlate('aaaa123')).toBe('AA AA-123');
  });
});

describe('money and duration', () => {
  it('formats forints without decimals', () => {
    expect(formatHuf(18000)).toMatch(/^18\s000\sFt$/);
  });

  it.each([
    [45, '45 perc'],
    [60, '1 óra'],
    [90, '1 óra 30 perc'],
  ])('%i minutes -> %s', (minutes, text) => {
    expect(formatDuration(minutes)).toBe(text);
  });
});

describe('dates', () => {
  it('skips weekends when listing workdays', () => {
    const friday = new Date(2026, 10, 6);
    expect(upcomingWorkdays(friday, 3)).toEqual(['2026-11-06', '2026-11-09', '2026-11-10']);
  });

  it('starts on Monday when called on a Sunday', () => {
    const sunday = new Date(2026, 10, 8);
    expect(upcomingWorkdays(sunday, 1)).toEqual(['2026-11-09']);
  });

  it('reads the time from an API date-time string', () => {
    expect(timeOf('2026-11-03T09:15:00')).toBe('09:15');
  });
});
