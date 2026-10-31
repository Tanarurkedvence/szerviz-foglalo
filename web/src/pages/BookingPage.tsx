import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { api, ApiError, errorMessages } from '../api';
import DayPicker from '../components/DayPicker';
import ErrorList from '../components/ErrorList';
import PlateInput from '../components/PlateInput';
import SlotPicker from '../components/SlotPicker';
import { formatDuration, formatHuf, formatPlate, isValidPlate, longDateOf, timeOf, upcomingWorkdays } from '../format';
import type { Booking, ServiceType } from '../types';

const EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;
const PHONE = /^\+?[0-9 ()/-]{6,20}$/;

interface CustomerForm {
  name: string;
  email: string;
  phone: string;
  plate: string;
  note: string;
}

const emptyForm: CustomerForm = { name: '', email: '', phone: '', plate: '', note: '' };

/** Same checks and wording as the API, so the user does not have to wait for a round trip. */
export function validateBooking(serviceId: number | null, slot: string | null, form: CustomerForm): string[] {
  const problems: string[] = [];
  if (serviceId === null) problems.push('Válaszd ki, milyen munkát végezzünk.');
  if (slot === null) problems.push('Válassz időpontot.');
  if (!form.name.trim()) problems.push('A név megadása kötelező.');
  if (!EMAIL.test(form.email.trim())) problems.push('Érvénytelen e-mail cím.');
  if (!PHONE.test(form.phone.trim())) problems.push('Érvénytelen telefonszám.');
  if (!isValidPlate(form.plate)) problems.push('Érvénytelen rendszám (pl. ABC-123 vagy AA AA-123).');
  return problems;
}

export default function BookingPage() {
  const days = useMemo(() => upcomingWorkdays(new Date(), 15), []);

  const [services, setServices] = useState<ServiceType[] | null>(null);
  const [serviceId, setServiceId] = useState<number | null>(null);
  const [day, setDay] = useState(days[0]);
  const [slots, setSlots] = useState<string[]>([]);
  const [slotsLoading, setSlotsLoading] = useState(false);
  const [slotsVersion, setSlotsVersion] = useState(0);
  const [slot, setSlot] = useState<string | null>(null);
  const [form, setForm] = useState<CustomerForm>(emptyForm);
  const [errors, setErrors] = useState<string[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [booked, setBooked] = useState<Booking | null>(null);

  useEffect(() => {
    api.services().then(setServices).catch((e) => setErrors(errorMessages(e)));
  }, []);

  // Reload the free times whenever the service or the day changes (or after a conflict).
  useEffect(() => {
    if (serviceId === null) return;
    let cancelled = false;
    setSlot(null);
    setSlotsLoading(true);
    api
      .availability(serviceId, day)
      .then((result) => {
        if (!cancelled) setSlots(result);
      })
      .catch((e) => {
        if (cancelled) return;
        setSlots([]);
        setErrors(errorMessages(e));
      })
      .finally(() => {
        if (!cancelled) setSlotsLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [serviceId, day, slotsVersion]);

  const service = services?.find((s) => s.id === serviceId) ?? null;
  const update = (field: keyof CustomerForm) => (value: string) => setForm((f) => ({ ...f, [field]: value }));

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const problems = validateBooking(serviceId, slot, form);
    if (problems.length > 0) {
      setErrors(problems);
      return;
    }

    setSubmitting(true);
    setErrors([]);
    try {
      const booking = await api.createBooking({
        customerName: form.name.trim(),
        customerEmail: form.email.trim(),
        customerPhone: form.phone.trim(),
        licensePlate: formatPlate(form.plate),
        serviceTypeId: serviceId!,
        startTime: slot!,
        note: form.note.trim() || undefined,
      });
      setBooked(booking);
    } catch (e) {
      setErrors(errorMessages(e));
      // Someone else took the time in the meantime: refresh the list.
      if (e instanceof ApiError && e.status === 409) setSlotsVersion((v) => v + 1);
    } finally {
      setSubmitting(false);
    }
  }

  function startOver() {
    setBooked(null);
    setServiceId(null);
    setSlot(null);
    setSlots([]);
    setForm(emptyForm);
    setErrors([]);
  }

  if (booked) {
    return <Confirmation booking={booked} onNewBooking={startOver} />;
  }

  return (
    <div className="booking-layout">
      <form id="booking-form" className="sheet" onSubmit={handleSubmit} noValidate aria-labelledby="booking-title">
        <h1 id="booking-title">Foglalj időpontot a szervizbe</h1>
        <p className="lede">
          Válaszd ki a munkát és az időpontot, add meg az adataidat. Online a kezdés előtt 24 óráig ingyen lemondhatod.
        </p>

        <section className="step" aria-labelledby="step-1">
          <h2 id="step-1">
            <span className="step-no" aria-hidden="true">1</span>
            Mit csináljunk?
          </h2>
          {services === null ? (
            <p className="muted">Szolgáltatások betöltése…</p>
          ) : (
            <fieldset className="services">
              <legend className="visually-hidden">Szolgáltatás</legend>
              {services.map((s) => (
                <label key={s.id} className="service">
                  <input
                    type="radio"
                    name="service"
                    value={s.id}
                    checked={serviceId === s.id}
                    onChange={() => setServiceId(s.id)}
                  />
                  <span className="service-name">{s.name}</span>
                  <span className="service-duration">{formatDuration(s.durationMinutes)}</span>
                  <span className="service-price">{formatHuf(s.priceHuf)}</span>
                </label>
              ))}
            </fieldset>
          )}
        </section>

        <section className="step" aria-labelledby="step-2">
          <h2 id="step-2">
            <span className="step-no" aria-hidden="true">2</span>
            Mikor hozod az autót?
          </h2>
          <DayPicker days={days} selected={day} onSelect={setDay} />
          {serviceId === null ? (
            <p className="muted">Előbb válaszd ki a munkát: abból tudjuk, mennyi időre kell az emelő.</p>
          ) : (
            <SlotPicker slots={slots} selected={slot} loading={slotsLoading} onSelect={setSlot} />
          )}
        </section>

        <section className="step" aria-labelledby="step-3">
          <h2 id="step-3">
            <span className="step-no" aria-hidden="true">3</span>
            Kinek a nevére foglaljunk?
          </h2>
          <div className="grid-2">
            <div className="field">
              <label htmlFor="name">Név</label>
              <input id="name" name="name" autoComplete="name" value={form.name} onChange={(e) => update('name')(e.target.value)} />
            </div>
            <div className="field">
              <label htmlFor="phone">Telefonszám</label>
              <input
                id="phone"
                name="phone"
                type="tel"
                autoComplete="tel"
                placeholder="+36 30 123 4567"
                value={form.phone}
                onChange={(e) => update('phone')(e.target.value)}
              />
            </div>
            <div className="field field-wide">
              <label htmlFor="email">E-mail cím</label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                value={form.email}
                onChange={(e) => update('email')(e.target.value)}
              />
            </div>
          </div>
          <PlateInput value={form.plate} onChange={update('plate')} />
          <div className="field">
            <label htmlFor="note">Megjegyzés (nem kötelező)</label>
            <textarea
              id="note"
              name="note"
              rows={3}
              maxLength={500}
              placeholder="Pl. fékezéskor nyikorog a bal első kerék."
              value={form.note}
              onChange={(e) => update('note')(e.target.value)}
            />
          </div>
        </section>
      </form>

      <aside className="ticket" aria-labelledby="ticket-title">
        <h2 id="ticket-title">Munkalap</h2>
        <dl className="ticket-rows">
          <div>
            <dt>Munka</dt>
            <dd>{service ? `${service.name}, ${formatDuration(service.durationMinutes)}` : 'Még nincs kiválasztva'}</dd>
          </div>
          <div>
            <dt>Időpont</dt>
            <dd>{slot ? `${longDateOf(slot)}, ${timeOf(slot)}` : 'Még nincs kiválasztva'}</dd>
          </div>
          <div>
            <dt>Rendszám</dt>
            <dd>{isValidPlate(form.plate) ? formatPlate(form.plate) : '–'}</dd>
          </div>
          <div className="ticket-price">
            <dt>Várható díj</dt>
            <dd>{service ? formatHuf(service.priceHuf) : '–'}</dd>
          </div>
        </dl>
        <ErrorList messages={errors} />
        <button type="submit" form="booking-form" className="btn btn-primary btn-block" disabled={submitting}>
          {submitting ? 'Foglalás folyamatban…' : 'Időpont foglalása'}
        </button>
        <p className="hint">A díjat a munka után, a helyszínen fizeted.</p>
      </aside>
    </div>
  );
}

function Confirmation({ booking, onNewBooking }: { booking: Booking; onNewBooking: () => void }) {
  const headingRef = useRef<HTMLHeadingElement>(null);

  // Move focus to the heading so keyboard and screen-reader users notice the change.
  useEffect(() => {
    headingRef.current?.focus();
  }, []);

  return (
    <section className="sheet confirmation" aria-labelledby="confirm-title">
      <h1 id="confirm-title" ref={headingRef} tabIndex={-1}>
        Lefoglaltuk az időpontot
      </h1>
      <p className="lede">
        Várunk {longDateOf(booking.startTime)}, {timeOf(booking.startTime)}-kor. Ha teheted, érkezz 10 perccel előbb.
      </p>
      <dl className="ticket-rows">
        <div>
          <dt>Foglalásszám</dt>
          <dd className="booking-number" data-testid="booking-number">
            {booking.id}
          </dd>
        </div>
        <div>
          <dt>Munka</dt>
          <dd>{booking.serviceName}</dd>
        </div>
        <div>
          <dt>Időpont</dt>
          <dd>
            {timeOf(booking.startTime)}–{timeOf(booking.endTime)}, {booking.bayNumber}. emelő
          </dd>
        </div>
        <div>
          <dt>Rendszám</dt>
          <dd>{booking.licensePlate}</dd>
        </div>
      </dl>
      <p>
        Ha mégsem jó az időpont, a foglalásszámmal és az e-mail címeddel a Lemondás oldalon mondhatod le, legkésőbb 24
        órával a kezdés előtt.
      </p>
      <button type="button" className="btn" onClick={onNewBooking}>
        Új foglalás
      </button>
    </section>
  );
}
