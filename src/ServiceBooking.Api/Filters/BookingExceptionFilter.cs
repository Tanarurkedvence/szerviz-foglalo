using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Filters;

/// <summary>
/// Turns business-rule errors into HTTP status codes in one place,
/// so the controllers stay free of try/catch blocks.
/// </summary>
public class BookingExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not BookingException exception)
        {
            return;
        }

        var statusCode = exception.Error switch
        {
            BookingError.Validation => StatusCodes.Status400BadRequest,
            BookingError.NotFound => StatusCodes.Status404NotFound,
            BookingError.Conflict => StatusCodes.Status409Conflict,
            BookingError.InvalidState => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        context.Result = new ObjectResult(new ErrorResponse(exception.Messages)) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
