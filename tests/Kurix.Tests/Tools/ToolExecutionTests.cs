using System.Text.Json;
using Kurix.Core.Escalation;
using Kurix.Core.Tools;
using Kurix.Tools;
using Kurix.Tools.Connectors;

namespace Kurix.Tests.Tools;

public class ToolExecutionTests
{
    private static readonly ToolExecutionContext Context =
        new(Guid.NewGuid(), Guid.NewGuid(), "sess-1");

    private static JsonElement Args(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    [Fact]
    public async Task CheckAvailability_Returns_Slots_For_Valid_Date()
    {
        var tool = new CheckAvailabilityTool(new MockCalendarConnector());

        var result = await tool.ExecuteAsync(Context, Args("""{"date":"2026-07-01"}"""), default);

        Assert.True(result.Success);
        Assert.Contains("09:00", result.Content);
        // 12:00 is pretend-taken by the mock.
        Assert.DoesNotContain("12:00 a 13:00", result.Content);
    }

    [Fact]
    public async Task CheckAvailability_Fails_On_Invalid_Date()
    {
        var tool = new CheckAvailabilityTool(new MockCalendarConnector());

        var result = await tool.ExecuteAsync(Context, Args("""{"date":"not-a-date"}"""), default);

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CheckAvailability_Fails_When_Date_Missing()
    {
        var tool = new CheckAvailabilityTool(new MockCalendarConnector());

        var result = await tool.ExecuteAsync(Context, Args("""{}"""), default);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateBooking_Confirms_With_Booking_Code()
    {
        var tool = new CreateBookingTool(new MockCalendarConnector());

        var result = await tool.ExecuteAsync(
            Context, Args("""{"customerName":"Ana","dateTime":"2026-07-01T15:00"}"""), default);

        Assert.True(result.Success);
        Assert.Contains("Ana", result.Content);
        Assert.Contains("BK-", result.Content);
    }

    [Fact]
    public async Task SearchInventory_Finds_Matching_Items()
    {
        var tool = new SearchInventoryTool(new MockInventoryConnector());

        var result = await tool.ExecuteAsync(Context, Args("""{"query":"monitor"}"""), default);

        Assert.True(result.Success);
        Assert.Contains("Monitor", result.Content);
    }

    private sealed class FakeEscalationService : IEscalationService
    {
        public bool Called { get; private set; }
        public Guid LastConversationId { get; private set; }

        public Task<EscalationResult> EscalateAsync(
            Guid tenantId, Guid conversationId, EscalationRequest request, CancellationToken ct = default)
        {
            Called = true;
            LastConversationId = conversationId;
            return Task.FromResult(new EscalationResult(true, "Derivado a un agente."));
        }
    }

    [Fact]
    public async Task EscalateToHuman_Invokes_Escalation_Service()
    {
        var fake = new FakeEscalationService();
        var tool = new EscalateToHumanTool(fake);

        var result = await tool.ExecuteAsync(
            Context, Args("""{"reason":"caso complejo"}"""), default);

        Assert.True(result.Success);
        Assert.True(fake.Called);
        Assert.Equal(Context.ConversationId, fake.LastConversationId);
    }

    [Fact]
    public async Task EscalateToHuman_Fails_When_Reason_Missing()
    {
        var tool = new EscalateToHumanTool(new FakeEscalationService());

        var result = await tool.ExecuteAsync(Context, Args("""{}"""), default);

        Assert.False(result.Success);
    }
}
