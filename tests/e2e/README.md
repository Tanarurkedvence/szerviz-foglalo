# End-to-end and API tests

Browser and API tests for Szerviz-foglaló, run against the whole app (ASP.NET Core API serving the built React app).

- **UI end-to-end tests** with Playwright for .NET (C#, NUnit), using the Page Object pattern
- **API tests** in C# (NUnit + HttpClient) and as a **Postman collection**
- **Test documentation:** [test plan](docs/TEST_PLAN.md), [33 test cases](docs/TEST_CASES.md) with the design technique
  behind each, [bug reports](docs/BUG_REPORTS.md)
- **CI on every push:** the `e2e` job builds the app, starts it on the runner and runs all tests, with Playwright traces uploaded for failed tests

## What is covered

| Area | UI tests | API tests | Postman |
|---|---|---|---|
| Booking (happy path, validation, full times, plate formatting) | 7 | 5 | 4 |
| Cancellation (in time, too late, wrong e-mail, no id probing) | 3 | 1 | 2 |
| Workshop board (login, start/finish job, cancelled jobs) | 3 | 2 | 4 |
| Regression for fixed bugs | – | 1 | 1 |

Unit, integration and component tests (83) are in `tests/ServiceBooking.Tests` and `web/`; see the [test plan](docs/TEST_PLAN.md)
for how the levels fit together.

## Project layout

```
ServiceBooking.E2E/
  Pages/          Page Objects: BookingPage, CancelPage, WorkshopPage
  Support/        settings, API client for test data, random test data, base class (tracing)
  Tests/UI/       browser tests
  Tests/Api/      HTTP contract tests
postman/          Postman collection (run it with the Collection Runner or newman)
docs/             test plan, test cases, bug reports
e2e.runsettings   where the app runs, browser settings
```

### Design choices

- **Arrange through the API, act and assert through the UI.** Tests create their bookings with HTTP calls, which is fast
  and does not depend on the booking page working.
- **No hard-coded dates or times.** Each test asks the API for a free time and uses random plates and e-mails, so tests are
  independent and can run against a database that already has data.
- **User-facing locators.** Elements are found by role and label (`GetByRole`, `GetByLabel`), the way a user or screen
  reader finds them; `data-testid` only for the day and time buttons.
- **No sleeps.** Playwright's auto-waiting assertions, plus waiting for the exact network response where a page reloads data.
- **Traces on failure.** A failed test saves a Playwright trace (screenshots, DOM snapshots, network) to
  `playwright-traces/`; open it with `pwsh bin/Debug/net10.0/playwright.ps1 show-trace <file>`.

## Running the tests

Prerequisites: .NET 10 SDK, PowerShell 7 (`pwsh`, only for installing the browser), and the app running.

```bash
# 1. From the repository root: build the React app into the API and start it on http://localhost:5080
cd web && npm install && npm run build:api
cd ../src/ServiceBooking.Api && dotnet run

# 2. In a new terminal, from tests/e2e
dotnet build ServiceBooking.E2E
pwsh ServiceBooking.E2E/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet test ServiceBooking.E2E --settings e2e.runsettings

# Only some tests
dotnet test ServiceBooking.E2E --settings e2e.runsettings --filter "TestCategory=Smoke"
dotnet test ServiceBooking.E2E --settings e2e.runsettings --filter "TestCategory=API"
```

To watch the browser, set `<Headless>false</Headless>` in `e2e.runsettings`.
To test another environment, set `E2E_BASE_URL` and `E2E_ADMIN_KEY` there or as environment variables.

**Postman:** import `postman/szerviz-foglalo.postman_collection.json`, check the `baseUrl` and `adminKey` collection
variables, and run the whole collection with the Collection Runner (the requests depend on each other's results). Or:
`npx newman run postman/szerviz-foglalo.postman_collection.json`.

## CI

The `e2e` job in `.github/workflows/ci.yml` runs on every push: it builds the React app into the API, starts the API on
the runner and runs the UI and API tests against it. The app's own unit and frontend tests run in the other two jobs of
the same workflow.
