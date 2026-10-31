import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import BookingPage, { validateBooking } from './BookingPage';

const services = [
  { id: 1, name: 'Olajcsere', durationMinutes: 60, priceHuf: 18000 },
  { id: 2, name: 'Fékbetét csere', durationMinutes: 90, priceHuf: 32000 },
];

function json(body: unknown, status = 200) {
  return Promise.resolve(new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }));
}

/** A fake API: answers by URL, and lets a test override the booking response. */
function mockApi(createResponse: () => Promise<Response>) {
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    if (url === '/api/services') return json(services);
    if (url.startsWith('/api/availability')) return json(['2026-11-03T09:00:00', '2026-11-03T13:30:00']);
    if (url === '/api/bookings' && init?.method === 'POST') return createResponse();
    return json({ errors: ['Ismeretlen hívás'] }, 404);
  });
  vi.stubGlobal('fetch', fetchMock);
  return fetchMock;
}

async function fillInBooking() {
  const user = userEvent.setup();
  await user.click(await screen.findByRole('radio', { name: /Olajcsere/ }));
  await user.click(screen.getByTestId('day-2026-11-03'));
  await user.click(await screen.findByRole('button', { name: '09:00' }));
  await user.type(screen.getByLabelText('Név'), 'Teszt Elek');
  await user.type(screen.getByLabelText('Telefonszám'), '+36 30 123 4567');
  await user.type(screen.getByLabelText('E-mail cím'), 'teszt.elek@example.com');
  await user.type(screen.getByLabelText('Rendszám'), 'abc123');
  return user;
}

describe('BookingPage', () => {
  beforeEach(() => {
    // Freeze only the date, so the day strip always starts on Monday 2 November 2026.
    vi.useFakeTimers({ toFake: ['Date'] });
    vi.setSystemTime(new Date(2026, 10, 2, 10, 0, 0));
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('books an appointment and shows the booking number', async () => {
    const fetchMock = mockApi(() =>
      json(
        {
          id: 42,
          customerName: 'Teszt Elek',
          customerEmail: 'teszt.elek@example.com',
          customerPhone: '+36 30 123 4567',
          licensePlate: 'ABC-123',
          serviceTypeId: 1,
          serviceName: 'Olajcsere',
          startTime: '2026-11-03T09:00:00',
          endTime: '2026-11-03T10:00:00',
          bayNumber: 1,
          status: 'Booked',
          note: null,
        },
        201,
      ),
    );
    render(<BookingPage />);

    const user = await fillInBooking();
    await user.click(screen.getByRole('button', { name: 'Időpont foglalása' }));

    expect(await screen.findByRole('heading', { name: 'Lefoglaltuk az időpontot' })).toBeInTheDocument();
    expect(screen.getByTestId('booking-number')).toHaveTextContent('42');

    const [, init] = fetchMock.mock.calls.find(([url]) => url === '/api/bookings')!;
    expect(JSON.parse(String(init!.body))).toMatchObject({
      serviceTypeId: 1,
      startTime: '2026-11-03T09:00:00',
      licensePlate: 'ABC-123',
    });
  });

  it('shows the server message when the time was taken meanwhile', async () => {
    mockApi(() => json({ errors: ['Ez az időpont közben betelt, kérlek válassz másikat.'] }, 409));
    render(<BookingPage />);

    const user = await fillInBooking();
    await user.click(screen.getByRole('button', { name: 'Időpont foglalása' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('közben betelt');
  });

  it('lists every missing field instead of calling the API', async () => {
    const fetchMock = mockApi(() => json({}, 500));
    render(<BookingPage />);
    const user = userEvent.setup();
    await screen.findByRole('radio', { name: /Olajcsere/ });

    await user.click(screen.getByRole('button', { name: 'Időpont foglalása' }));

    const alert = screen.getByRole('alert');
    expect(within(alert).getAllByRole('listitem')).toHaveLength(6);
    expect(fetchMock).not.toHaveBeenCalledWith('/api/bookings', expect.anything());
  });

  it('shows the price on the work order when a service is picked', async () => {
    mockApi(() => json({}, 500));
    render(<BookingPage />);
    const user = userEvent.setup();

    await user.click(await screen.findByRole('radio', { name: /Fékbetét csere/ }));

    const ticket = screen.getByRole('complementary', { name: 'Munkalap' });
    expect(ticket).toHaveTextContent('Fékbetét csere, 1 óra 30 perc');
    expect(ticket).toHaveTextContent(/32\s000\sFt/);
  });
});

describe('validateBooking', () => {
  const okForm = { name: 'Teszt Elek', email: 'a@b.hu', phone: '+36301234567', plate: 'ABC-123', note: '' };

  it('accepts a complete booking', () => {
    expect(validateBooking(1, '2026-11-03T09:00:00', okForm)).toEqual([]);
  });

  it('rejects a bad plate with the same message as the API', () => {
    expect(validateBooking(1, '2026-11-03T09:00:00', { ...okForm, plate: 'AB-12' })).toEqual([
      'Érvénytelen rendszám (pl. ABC-123 vagy AA AA-123).',
    ]);
  });
});
