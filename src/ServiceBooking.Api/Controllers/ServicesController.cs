using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Data;

namespace ServiceBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>The services the workshop offers, with duration and price.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ServiceTypeDto>>> GetAll(CancellationToken ct)
    {
        var services = await _db.ServiceTypes
            .OrderBy(s => s.Id)
            .Select(s => new ServiceTypeDto(s.Id, s.Name, s.DurationMinutes, s.PriceHuf))
            .ToListAsync(ct);

        return Ok(services);
    }
}
