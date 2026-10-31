# Test plan – Szerviz-foglaló (car service booking)

## 1. What is tested

The Szerviz-foglaló web app (this repository): customers book a time for a car service,
cancel it online, and workshop staff move bookings through their life cycle on a daily board.

**In scope:** booking, free time slots, cancellation, staff board and status changes, the REST API, input validation,
basic accessibility (keyboard, labels) and mobile layout.

**Out of scope:** load and performance testing, security penetration testing, e-mail notifications (not implemented).

## 2. Test levels

| Level | Where | Tool | What it proves |
|---|---|---|---|
| Unit | `tests/ServiceBooking.Tests` | xUnit | Business rules in isolation (time rules, lifts, cancellation, statuses) |
| Integration | `tests/ServiceBooking.Tests/ApiTests.cs` | xUnit + WebApplicationFactory | HTTP layer: routing, JSON, filters, status codes, database |
| Component | `web/src/**/*.test.tsx` | Vitest + Testing Library | React components and client-side validation |
| API (black box) | `tests/e2e`, `Tests/Api` | NUnit + HttpClient, Postman | The running API's contract, as any client sees it |
| End-to-end UI | `tests/e2e`, `Tests/UI` | Playwright (.NET) | Real user flows in a real browser |
| Exploratory | manual | – | Things scripts miss: odd timing, usability, edge cases |

The pyramid is intentional: most checks are fast unit tests; a handful of slow browser tests cover the main journeys.

## 3. Test design techniques

**Equivalence partitioning** – licence plates: valid old format (`ABC-123`), valid new format (`AA AA-123`),
too few/many letters or digits, wrong order, accented letters.

**Boundary value analysis**

| Rule | Just valid | Just invalid |
|---|---|---|
| At least 2 hours ahead (now = 10:00) | 12:00 | 11:45 |
| Must end by 17:00 (60-minute job) | starts 16:00 | starts 16:15 |
| Opening at 08:00 | 08:00 | 07:45 |
| 15-minute grid | :00 | :05, :14 |
| Lifts: back-to-back jobs | 10:00 after a 09:00–10:00 job | 09:15 |
| Free online cancellation | 24 h + before start | 23 h before start |

**Decision table** – customer cancellation:

| Conditions | R1 | R2 | R3 | R4 |
|---|---|---|---|---|
| Booking exists and e-mail matches | N | Y | Y | Y |
| Status is "Booked" | – | N | Y | Y |
| Starts more than 24 h from now | – | – | N | Y |
| **Result** | 404, same message as "no such booking" | 422 "can no longer be cancelled" | 422 "call us" | 200, cancelled, lift freed |

**State transition** – booking status:

```
Booked ──► InProgress ──► Completed
   │
   └─────► Cancelled
```
Valid transitions are tested once each; invalid ones (Booked → Completed, Completed → anything) must return 422.

## 4. Environments and data

| Environment | Address | Used for |
|---|---|---|
| Local | `http://localhost:5080` (API serving the built React app) | Development, all suites |
| CI | `http://localhost:5080` on the GitHub Actions runner | E2E job on every push |
| Demo (Render) | set in `E2E_BASE_URL` | Manual runs against the deployed demo |

Every automated test creates its own data (random plates, unique e-mails) through the API and looks up a free time
first, so tests do not depend on each other or on previous runs and can run against a non-empty database.

## 5. Entry and exit criteria

**Start E2E testing when:** the app's CI is green (unit, integration and component tests pass) and the app starts.

**Done when:** all High priority test cases pass, no open Critical/Major bug without an agreed workaround,
and every fixed bug has a regression test.

## 6. Risks

| Risk | Mitigation |
|---|---|
| Two customers book the last free lift at the same moment (double booking) | Logged as BUG-002, exploratory/load test needed |
| Time-dependent tests are flaky (they depend on "now") | Unit tests use a fake clock; E2E tests ask the API for a free time instead of hard-coding one |
| Free hosting sleeps, first request is slow | CI "wake up" step, 90 s HTTP timeout |
| UI changes break locators | Page Objects; locators by role and label, which change less than CSS |

## 7. Reporting

- All results: the CI workflow in GitHub Actions (one job each for the API, the web app and E2E); on an E2E failure a Playwright trace (screenshots, DOM, network) is uploaded.
- Bugs: [BUG_REPORTS.md](BUG_REPORTS.md). Test cases: [TEST_CASES.md](TEST_CASES.md).
