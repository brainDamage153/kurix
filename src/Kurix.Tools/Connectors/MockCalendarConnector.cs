using Kurix.Core.Connectors;

namespace Kurix.Tools.Connectors;

/// <summary>
/// Demo <see cref="ICalendarConnector"/> with deterministic, in-memory behaviour.
/// Replaced per client by a real integration (Google Calendar, Calendly, a
/// clinic's system, …) that implements the same interface. No real scheduling
/// system is hardcoded here.
/// </summary>
public class MockCalendarConnector : ICalendarConnector
{
    private static readonly TimeZoneInfo Tz = TimeZoneInfo.Utc;

    public Task<IReadOnlyList<AvailabilitySlot>> GetAvailabilityAsync(
        Guid tenantId, DateOnly date, string? serviceType, CancellationToken ct = default)
    {
        // Business hours 09:00–17:00, hourly slots; pretend 12:00 and 15:00 are taken.
        var taken = new HashSet<int> { 12, 15 };
        var slots = new List<AvailabilitySlot>();

        for (var hour = 9; hour < 17; hour++)
        {
            if (taken.Contains(hour))
                continue;

            var start = new DateTimeOffset(date.Year, date.Month, date.Day, hour, 0, 0, Tz.BaseUtcOffset);
            slots.Add(new AvailabilitySlot(start, start.AddHours(1)));
        }

        return Task.FromResult<IReadOnlyList<AvailabilitySlot>>(slots);
    }

    public Task<BookingResult> CreateBookingAsync(
        Guid tenantId, BookingRequest request, CancellationToken ct = default)
    {
        var bookingId = $"BK-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var result = new BookingResult(
            Success: true,
            BookingId: bookingId,
            Message: $"Reserva confirmada para {request.CustomerName} el " +
                     $"{request.Start:dd/MM/yyyy HH:mm}. Código: {bookingId}.");
        return Task.FromResult(result);
    }
}
