namespace ServiceBooking.Api.Services;

public enum BookingError
{
    /// <summary>Bad input -> 400</summary>
    Validation,

    /// <summary>No such booking -> 404</summary>
    NotFound,

    /// <summary>The time is taken -> 409</summary>
    Conflict,

    /// <summary>Not allowed in the booking's current state -> 422</summary>
    InvalidState
}

/// <summary>
/// A business rule was broken. BookingExceptionFilter turns it into an HTTP response.
/// </summary>
public class BookingException : Exception
{
    public BookingException(BookingError error, string message)
        : this(error, new[] { message })
    {
    }

    public BookingException(BookingError error, IReadOnlyList<string> messages)
        : base(string.Join(" ", messages))
    {
        Error = error;
        Messages = messages;
    }

    public BookingError Error { get; }

    /// <summary>User-facing messages (Hungarian), shown as they are in the UI.</summary>
    public IReadOnlyList<string> Messages { get; }
}
