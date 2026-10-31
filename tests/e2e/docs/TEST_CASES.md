# Test cases

Priority: H = high, M = medium, L = low. "Automated in" names the test that covers the case
(unit/integration tests in `tests/ServiceBooking.Tests`, UI/API tests in `tests/e2e/ServiceBooking.E2E`, Postman in `tests/e2e/postman/`).

## Booking

| ID | Title | Technique | Prio | Steps | Expected | Automated in |
|---|---|---|---|---|---|---|
| TC-01 | Customer books a service | Use case | H | Pick "Olajcsere", a day, a free time, fill in name, phone, e-mail, plate; press "Időpont foglalása" | Confirmation page with a booking number; work order showed service and time before submitting | UI `Customer_can_book_an_oil_change`, Postman #3 |
| TC-02 | Plate comes back in official format | EP | M | Book with plate `abc123` | Response/confirmation shows `ABC-123` | Postman #3, integration `CreateBooking_ValidRequest_Returns201AndTheBooking` |
| TC-03 | Booking exactly 2 hours ahead | BVA | H | Now 10:00, book 12:00 | Accepted | unit `Create_ExactlyTwoHoursAhead_IsAllowed` |
| TC-04 | Booking less than 2 hours ahead | BVA | H | Now 10:00, book 11:45 | 400 "Legalább 2 órával előre kell foglalni." | unit `Create_InvalidTime_ThrowsValidation` |
| TC-05 | Full time is not offered | State | H | Fill both lifts at a time via API; open that day in the UI | The time is missing from the list, other times are shown | UI `Full_time_is_not_offered`, unit `GetAvailableSlots_SkipsTimesWhenBothLiftsAreBusy` |
| TC-06 | Back-to-back jobs do not conflict | BVA | H | Both lifts busy 09:00–10:00, book 10:00 | Accepted on lift 1 | unit `Create_BackToBackBookings_DoNotConflict` |
| TC-07 | Same car twice at overlapping times | Decision | M | Book `ABC-123` at 09:00, then `abc123` at 09:30 | 409, second booking refused | unit `Create_SamePlateAtOverlappingTime_ThrowsConflict` |
| TC-08 | Empty form | EP | M | Press "Időpont foglalása" without filling anything | 6 problems listed at once, nothing sent to the API | UI `Empty_form_lists_every_problem` |
| TC-09 | Server lists every validation error | EP | M | POST with empty name, bad e-mail, phone and plate | 400 with 4 messages | API `Invalid_booking_returns_400_with_every_problem`, Postman #4 |
| TC-10 | Invalid plate while typing | EP | L | Type `AB-12345` | Red frame and hint, field marked `aria-invalid` | UI `Invalid_plate_is_flagged_while_typing` |
| TC-11 | Plate formatted on leaving the field | EP | L | Type `abc123` / `aaaa123`, press Tab | `ABC-123` / `AA AA-123` | UI `Plate_is_formatted_when_leaving_the_field` |
| TC-12 | Weekend | EP | H | POST a Saturday 09:00 booking | 400 "Hétvégén zárva vagyunk." | API `Weekend_booking_is_rejected`, unit |
| TC-13 | 15-minute grid | BVA | M | POST start at :00, :05, :14 | 201, 400, 400 | API `Start_time_must_be_on_the_15_minute_grid` |
| TC-14 | Keyboard only | Accessibility | M | Focus first service, Space, Arrow Down | Selection follows, work order updates | UI `Service_can_be_chosen_with_the_keyboard` |
| TC-15 | Job must end by closing | BVA | H | 60-minute job at 16:00 and at 16:15 | 16:00 accepted, 16:15 refused | unit `Create_EndingExactlyAtClosing_IsAllowed`, `Create_InvalidTime_ThrowsValidation` |
| TC-16 | Phone screen | Usability | M | 390 px wide screen | No sideways scrolling | UI `Page_fits_a_phone_screen` |
| TC-17 | More than 60 days ahead | BVA | L | Book 63 days ahead | 400 | unit `Create_InvalidTime_ThrowsValidation` |

## Cancellation

| ID | Title | Technique | Prio | Steps | Expected | Automated in |
|---|---|---|---|---|---|---|
| TC-20 | Customer cancels in time | Decision R4 | H | Booking 3+ workdays ahead; enter booking number and e-mail | "Lemondtuk a foglalást" | UI `Customer_can_cancel_their_booking`, unit |
| TC-21 | Cancel within 24 hours | Decision R3 | H | Booking less than 24 h ahead; try to cancel | 422 "…csak telefonon lehet lemondani." | UI `Booking_within_24_hours_cannot_be_cancelled_online`, unit |
| TC-22 | Someone else's e-mail | Security | H | Valid booking number, other e-mail | "Nincs ilyen foglalás ezzel az e-mail címmel." | UI `Wrong_email_cannot_cancel`, Postman #9 |
| TC-23 | Cannot probe booking numbers | Security | M | Cancel a real id with a wrong e-mail, and a non-existent id | Identical 404 responses | API `Cancel_does_not_reveal_whether_a_booking_exists` |
| TC-24 | Cancelling frees the lift | State | H | Fill both lifts, cancel one, book again | New booking gets the freed lift | unit `Cancel_MoreThan24HoursBefore_CancelsAndFreesTheLift` |
| TC-25 | Started job cannot be cancelled online | Decision R2 | M | Staff starts the job, customer tries to cancel | 422 | Postman #10 |

## Workshop (staff)

| ID | Title | Technique | Prio | Steps | Expected | Automated in |
|---|---|---|---|---|---|---|
| TC-30 | Wrong admin key | Security | H | Log in with a wrong key | Error shown, board stays hidden; API returns 401 | UI `Wrong_admin_key_is_rejected`, API `Admin_endpoints_need_the_right_key` |
| TC-31 | Start a job | State | H | Booking on the board, press "Szerelés indítása" | Status "Szerelés alatt" | UI `Mechanic_can_start_and_finish_a_job` |
| TC-32 | Finish a job | State | H | Job in progress, press "Kész" | Status "Kész", no more buttons | UI `Mechanic_can_start_and_finish_a_job` |
| TC-33 | Cancelled booking on the board | State | M | Customer cancels, staff opens the day | Card shows "Lemondva", plate struck through | UI `Cancelled_booking_shows_as_cancelled` |
| TC-34 | Cannot skip "in progress" | State (invalid) | M | Booked → Completed | 422 | API `Status_cannot_skip_from_booked_to_completed`, Postman #7, unit |
| TC-35 | Unknown API address | Regression (BUG-001) | M | GET `/api/nincs-ilyen` | 404 with JSON body | API `Unknown_api_address_returns_json_404`, Postman #11 |

## Exploratory charters (manual)

| ID | Charter | Notes |
|---|---|---|
| TC-40 | Two people book the last free lift at the same moment | See BUG-002 |
| TC-41 | Leave the booking page open overnight, then book | See BUG-003 |
| TC-42 | Use the site with a screen reader (NVDA) for one full booking | Headings, labels and error alerts should be announced |
| TC-43 | Slow 3G network (browser dev tools) | Loading texts appear, double-clicking "Időpont foglalása" does not create two bookings |
