namespace Kurix.Core.Connectors;

/// <summary>An available time slot in a tenant's schedule.</summary>
public record AvailabilitySlot(DateTimeOffset Start, DateTimeOffset End);

/// <summary>A request to book an appointment.</summary>
public record BookingRequest(
    string CustomerName, DateTimeOffset Start, string? ServiceType, string? Notes);

/// <summary>Outcome of a booking attempt.</summary>
public record BookingResult(bool Success, string? BookingId, string Message);

/// <summary>
/// Abstraction over a tenant's scheduling system. Real integrations (Google
/// Calendar, Calendly, a clinic's system, etc.) are added later by implementing
/// this interface per client; the MVP ships a mock implementation. Tools must
/// depend on this abstraction, never on a concrete system.
/// </summary>
public interface ICalendarConnector
{
    Task<IReadOnlyList<AvailabilitySlot>> GetAvailabilityAsync(
        Guid tenantId, DateOnly date, string? serviceType, CancellationToken ct = default);

    Task<BookingResult> CreateBookingAsync(
        Guid tenantId, BookingRequest request, CancellationToken ct = default);
}
