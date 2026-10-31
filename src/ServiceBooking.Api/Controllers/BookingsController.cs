using Microsoft.AspNetCore.Mvc;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Filters;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api")]
public class BookingsController : ControllerBase
{
    private readonly BookingService _service;

    public BookingsController(BookingService service)
    {
        _service = service;
    }

    // ---- Public endpoints (customers) ----

    /// <summary>Free start times for a service on a given day.</summary>
    [HttpGet("availability")]
    public async Task<ActionResult<IReadOnlyList<DateTime>>> GetAvailability(
        [FromQuery] int serviceTypeId,
        [FromQuery] DateTime date,
        CancellationToken ct)
    {
        var slots = await _service.GetAvailableSlotsAsync(serviceTypeId, date, ct);
        return Ok(slots);
    }

    /// <summary>Book an appointment. Returns 409 if the time is already taken.</summary>
    [HttpPost("bookings")]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request, CancellationToken ct)
    {
        var booking = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, BookingDto.From(booking));
    }

    /// <summary>Cancel your own booking (needs the e-mail address it was made with).</summary>
    [HttpPost("bookings/{id:int}/cancel")]
    public async Task<ActionResult<BookingDto>> Cancel(int id, CancelBookingRequest request, CancellationToken ct)
    {
        var booking = await _service.CancelAsync(id, request.Email, ct);
        return Ok(BookingDto.From(booking));
    }

    // ---- Workshop endpoints (need the X-Admin-Key header) ----

    /// <summary>All bookings, optionally for one day.</summary>
    [HttpGet("bookings")]
    [AdminKey]
    public async Task<ActionResult<List<BookingDto>>> List([FromQuery] DateTime? date, CancellationToken ct)
    {
        var bookings = await _service.ListAsync(date, ct);
        return Ok(bookings.Select(BookingDto.From).ToList());
    }

    [HttpGet("bookings/{id:int}")]
    [AdminKey]
    public async Task<ActionResult<BookingDto>> GetById(int id, CancellationToken ct)
    {
        var booking = await _service.GetAsync(id, ct);
        return Ok(BookingDto.From(booking));
    }

    /// <summary>Move a booking to its next state (e.g. Booked -> InProgress -> Completed).</summary>
    [HttpPost("bookings/{id:int}/status")]
    [AdminKey]
    public async Task<ActionResult<BookingDto>> ChangeStatus(int id, ChangeStatusRequest request, CancellationToken ct)
    {
        var booking = await _service.ChangeStatusAsync(id, request.Status, ct);
        return Ok(BookingDto.From(booking));
    }
}
