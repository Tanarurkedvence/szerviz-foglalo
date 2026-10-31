import { useEffect, useState, type CSSProperties, type FormEvent } from 'react';
import { api, ApiError, errorMessages } from '../api';
import ErrorList from '../components/ErrorList';
import { statusLabels, timeOf, toIsoDate } from '../format';
import type { Booking, BookingStatus } from '../types';

const STORAGE_KEY = 'szerviz-admin-key';

// sessionStorage can be unavailable (private mode, blocked storage), so never let it crash the page.
function loadKey(): string {
  try {
    return sessionStorage.getItem(STORAGE_KEY) ?? '';
  } catch {
    return '';
  }
}

function saveKey(key: string) {
  try {
    if (key) sessionStorage.setItem(STORAGE_KEY, key);
    else sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // The key then only lives until the page is reloaded.
  }
}

/** What the staff can do next with a booking in a given state. */
const nextSteps: Record<BookingStatus, { status: BookingStatus; label: string; kind: string }[]> = {
  Booked: [
    { status: 'InProgress', label: 'Szerelés indítása', kind: 'btn-primary' },
    { status: 'Cancelled', label: 'Lemondás', kind: 'btn-ghost' },
  ],
  InProgress: [{ status: 'Completed', label: 'Kész', kind: 'btn-primary' }],
  Completed: [],
  Cancelled: [],
};

export default function AdminPage() {
  const [adminKey, setAdminKey] = useState(loadKey);
  const [keyInput, setKeyInput] = useState('');
  const [date, setDate] = useState(() => toIsoDate(new Date()));
  const [bookings, setBookings] = useState<Booking[] | null>(null);
  const [errors, setErrors] = useState<string[]>([]);
  const [busyId, setBusyId] = useState<number | null>(null);
  const [reloadCount, setReloadCount] = useState(0);

  useEffect(() => {
    if (!adminKey) return;
    let cancelled = false;
    setBookings(null);
    api
      .listBookings(date, adminKey)
      .then((result) => {
        if (cancelled) return;
        setBookings(result);
        setErrors([]);
      })
      .catch((e) => {
        if (cancelled) return;
        if (e instanceof ApiError && e.status === 401) {
          saveKey('');
          setAdminKey('');
        }
        setErrors(errorMessages(e));
      });
    return () => {
      cancelled = true;
    };
  }, [adminKey, date, reloadCount]);

  function logIn(event: FormEvent) {
    event.preventDefault();
    const key = keyInput.trim();
    if (!key) {
      setErrors(['Add meg az admin kulcsot.']);
      return;
    }
    saveKey(key);
    setErrors([]);
    setKeyInput('');
    setAdminKey(key);
  }

  function logOut() {
    saveKey('');
    setAdminKey('');
    setBookings(null);
  }

  async function changeStatus(booking: Booking, status: BookingStatus) {
    setBusyId(booking.id);
    try {
      const updated = await api.changeStatus(booking.id, status, adminKey);
      setBookings((list) => list?.map((b) => (b.id === updated.id ? updated : b)) ?? null);
      setErrors([]);
    } catch (e) {
      setErrors(errorMessages(e));
    } finally {
      setBusyId(null);
    }
  }

  if (!adminKey) {
    return (
      <section className="sheet narrow" aria-labelledby="login-title">
        <h1 id="login-title">Belépés a műhely nézetbe</h1>
        <p className="lede">A napi foglalásokat és az emelők beosztását csak a szerviz munkatársai látják.</p>
        <form onSubmit={logIn} noValidate>
          <div className="field">
            <label htmlFor="admin-key">Admin kulcs</label>
            <input
              id="admin-key"
              name="adminKey"
              type="password"
              autoComplete="current-password"
              value={keyInput}
              onChange={(e) => setKeyInput(e.target.value)}
            />
          </div>
          <ErrorList messages={errors} />
          <button type="submit" className="btn btn-primary">
            Belépés
          </button>
          {import.meta.env.DEV && <p className="hint">Fejlesztői gépen a kulcs: dev-admin-key</p>}
        </form>
      </section>
    );
  }

  const bayCount = Math.max(2, ...(bookings ?? []).map((b) => b.bayNumber));
  const bays = Array.from({ length: bayCount }, (_, i) => i + 1);

  return (
    <section aria-labelledby="board-title">
      <div className="board-toolbar">
        <h1 id="board-title">Napi beosztás</h1>
        <div className="toolbar-controls">
          <label htmlFor="board-date">Nap</label>
          <input id="board-date" type="date" value={date} onChange={(e) => e.target.value && setDate(e.target.value)} />
          <button type="button" className="btn" onClick={() => setReloadCount((n) => n + 1)}>
            Frissítés
          </button>
          <button type="button" className="btn btn-ghost" onClick={logOut}>
            Kilépés
          </button>
        </div>
      </div>

      <ErrorList messages={errors} />

      {bookings === null ? (
        <p className="muted">Foglalások betöltése…</p>
      ) : (
        <div className="board" style={{ '--bays': bayCount } as CSSProperties}>
          {bays.map((bay) => {
            const jobs = bookings.filter((b) => b.bayNumber === bay);
            return (
              <section key={bay} className="bay" aria-labelledby={`bay-${bay}`}>
                <h2 id={`bay-${bay}`}>{bay}. emelő</h2>
                {jobs.length === 0 ? (
                  <p className="empty">Erre a napra nincs munka ezen az emelőn.</p>
                ) : (
                  <ol className="jobs">
                    {jobs.map((job) => (
                      <li key={job.id} className={`job job-${job.status.toLowerCase()}`} data-testid={`job-${job.id}`}>
                        <p className="job-time">
                          {timeOf(job.startTime)}–{timeOf(job.endTime)}
                        </p>
                        <div className="job-body">
                          <p className="job-title">
                            <span className="mini-plate">{job.licensePlate}</span>
                            {job.serviceName}
                          </p>
                          <p className="job-customer">
                            {job.customerName}, <a href={`tel:${job.customerPhone}`}>{job.customerPhone}</a>
                          </p>
                          {job.note && <p className="job-note">„{job.note}”</p>}
                          <div className="job-actions">
                            <span className={`status status-${job.status.toLowerCase()}`}>{statusLabels[job.status]}</span>
                            {nextSteps[job.status].map((step) => (
                              <button
                                key={step.status}
                                type="button"
                                className={`btn btn-small ${step.kind}`}
                                disabled={busyId === job.id}
                                onClick={() => changeStatus(job, step.status)}
                              >
                                {step.label}
                              </button>
                            ))}
                          </div>
                        </div>
                      </li>
                    ))}
                  </ol>
                )}
              </section>
            );
          })}
        </div>
      )}
    </section>
  );
}
