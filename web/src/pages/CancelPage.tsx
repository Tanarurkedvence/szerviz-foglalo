import { useState, type FormEvent } from 'react';
import { api, errorMessages } from '../api';
import ErrorList from '../components/ErrorList';
import { longDateOf, timeOf } from '../format';
import type { Booking } from '../types';

export default function CancelPage() {
  const [bookingNumber, setBookingNumber] = useState('');
  const [email, setEmail] = useState('');
  const [errors, setErrors] = useState<string[]>([]);
  const [busy, setBusy] = useState(false);
  const [cancelled, setCancelled] = useState<Booking | null>(null);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();

    const problems: string[] = [];
    const id = Number(bookingNumber.trim());
    if (!Number.isInteger(id) || id <= 0) problems.push('Add meg a foglalásszámot (a visszaigazolásban találod).');
    if (!email.includes('@')) problems.push('Érvénytelen e-mail cím.');
    if (problems.length > 0) {
      setErrors(problems);
      return;
    }

    setBusy(true);
    setErrors([]);
    try {
      setCancelled(await api.cancelBooking(id, email.trim()));
    } catch (e) {
      setErrors(errorMessages(e));
    } finally {
      setBusy(false);
    }
  }

  if (cancelled) {
    return (
      <section className="sheet narrow" aria-labelledby="cancelled-title">
        <h1 id="cancelled-title">Lemondtuk a foglalást</h1>
        <p className="lede">
          A {cancelled.id}. számú foglalást ({longDateOf(cancelled.startTime)}, {timeOf(cancelled.startTime)}) töröltük.
          Az időpontot most már más is lefoglalhatja.
        </p>
        <a className="btn" href="#/foglalas">
          Új időpont foglalása
        </a>
      </section>
    );
  }

  return (
    <section className="sheet narrow" aria-labelledby="cancel-title">
      <h1 id="cancel-title">Foglalás lemondása</h1>
      <p className="lede">
        Online a kezdés előtt 24 óráig mondhatod le. Ha ennél közelebb van az időpont, hívj minket telefonon.
      </p>
      <form onSubmit={handleSubmit} noValidate>
        <div className="field">
          <label htmlFor="booking-number">Foglalásszám</label>
          <input
            id="booking-number"
            name="bookingNumber"
            inputMode="numeric"
            autoComplete="off"
            value={bookingNumber}
            onChange={(e) => setBookingNumber(e.target.value)}
          />
        </div>
        <div className="field">
          <label htmlFor="cancel-email">E-mail cím, amellyel foglaltál</label>
          <input
            id="cancel-email"
            name="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
        <ErrorList messages={errors} />
        <button type="submit" className="btn btn-danger" disabled={busy}>
          {busy ? 'Lemondás folyamatban…' : 'Foglalás lemondása'}
        </button>
      </form>
    </section>
  );
}
