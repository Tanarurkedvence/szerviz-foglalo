using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Filters;
using ServiceBooking.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms like Render tell the app which port to use via PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services
    .AddControllers(options => options.Filters.Add<BookingExceptionFilter>())
    .ConfigureApiBehaviorOptions(options =>
    {
        // Malformed JSON etc. -> same { "errors": [...] } shape as our own errors.
        options.InvalidModelStateResponseFactory = context =>
        {
            var messages = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Hibás kérés." : e.ErrorMessage)
                .ToList();
            return new BadRequestObjectResult(new ErrorResponse(messages));
        };
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The connection string is read from the final configuration, so tests can override it.
builder.Services.AddDbContext<AppDbContext>((services, options) =>
{
    var configuration = services.GetRequiredService<IConfiguration>();
    options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=servicebooking.db");
});

builder.Services.Configure<WorkshopOptions>(builder.Configuration.GetSection("Workshop"));
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddScoped<BookingService>();

var app = builder.Build();

// Create the database on first start and add the services.
// (A bigger project would use EF Core migrations instead.)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    SeedData.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();

// In production the built React app is served from wwwroot by this same server.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Unknown /api/... addresses get a JSON 404 instead of the React page (see BUG-001 in the e2e repo).
app.Map("/api/{**path}", () => Results.NotFound(new ErrorResponse(new[] { "Nincs ilyen API végpont." })));

// Everything else is a page of the React app.
app.MapFallbackToFile("index.html");

app.Run();

// Makes Program visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program
{
}
