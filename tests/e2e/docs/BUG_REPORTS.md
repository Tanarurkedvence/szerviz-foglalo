# Bug reports

Template used for every report:

> **ID / Title** – one line: what is wrong, where.
> **Severity** (impact) / **Priority** (urgency) / **Status**
> **Environment** – version, browser, address
> **Steps to reproduce** – numbered, from a clean start
> **Expected** / **Actual**
> **Evidence** – request/response, screenshot or Playwright trace
> **Notes** – suspected cause, workaround, regression test

---

## BUG-001 – Unknown API address returns the web page with 200 OK

**Severity:** Minor · **Priority:** Medium · **Status:** Fixed

**Environment:** production build (API serving the React app from `wwwroot`), any browser or HTTP client.

**Steps to reproduce**
1. Start the app with the built frontend (`npm run build:api`, then `dotnet run`).
2. Send `GET /api/servicess` (typo on purpose).

**Expected:** `404 Not Found` with a JSON error body, like every other API error.

**Actual:** `200 OK` with `Content-Type: text/html` – the React app's `index.html`.

**Evidence**
```
GET /api/servicess
HTTP/1.1 200 OK
Content-Type: text/html
<!doctype html><html lang="hu">…
```

**Notes:** `MapFallbackToFile("index.html")` answered every address no controller matched, including `/api/…`.
A client calling a mistyped endpoint got HTML and failed later with a confusing JSON parse error, and monitoring saw a
success. Fixed by mapping `/api/{**path}` to a JSON 404 before the fallback.
Regression tests: `Unknown_api_address_returns_json_404` (API suite), `UnknownApiAddress_Returns404AsJson`
(integration), Postman request #11.

---

## BUG-002 – Two simultaneous requests can book the same lift twice

**Severity:** Major · **Priority:** Medium · **Status:** Open – found in code review, not yet reproduced

**Environment:** any; most likely under load or when two customers press "Időpont foglalása" for the last free time
within a few milliseconds.

**Steps to reproduce (proposed)**
1. Make sure only one lift is free at a given time (book the other one).
2. Send two `POST /api/bookings` requests for that time at the same moment
   (e.g. `Task.WhenAll` of two `HttpClient` calls, or k6 with 2 virtual users).

**Expected:** one `201 Created`, one `409 Conflict`.

**Actual (suspected):** both get `201`, both on the same lift.

**Notes:** `BookingService.CreateAsync` reads the overlapping bookings, picks a free lift, then inserts. Between the
read and the insert another request can do the same ("check-then-act" race). Possible fixes: run the check and insert
in a serializable transaction, or a lock around booking creation (enough for a single server instance).
A load test (TC-40) should confirm the bug before fixing.

---

## BUG-003 – Booking page left open overnight still offers yesterday

**Severity:** Trivial · **Priority:** Low · **Status:** Open

**Environment:** any browser, booking page.

**Steps to reproduce**
1. Open the booking page in the evening and leave the tab open.
2. After midnight, choose a service without reloading the page.

**Expected:** the day list starts with today.

**Actual:** the first day is still yesterday; selecting it shows "Erre a napra nincs szabad időpont".

**Notes:** the list of days is calculated once when the page loads (`useMemo` in `BookingPage.tsx`).
Workaround: reload the page. Fix idea: recalculate the days when the tab becomes visible again (`visibilitychange`).
