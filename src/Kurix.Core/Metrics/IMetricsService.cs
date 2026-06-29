namespace Kurix.Core.Metrics;

/// <summary>A frequently-asked question and how many times it appeared.</summary>
public record FrequentQuestion(string Question, int Count);

/// <summary>Aggregated metrics for a tenant's dashboard.</summary>
public record MetricsSummary(
    int TotalConversations,
    int EscalatedConversations,
    double EscalationRate,
    long TotalTokensIn,
    long TotalTokensOut,
    decimal EstimatedCostUsd,
    IReadOnlyList<FrequentQuestion> FrequentQuestions);

/// <summary>Computes aggregated, tenant-scoped metrics over conversations and usage.</summary>
public interface IMetricsService
{
    Task<MetricsSummary> GetSummaryAsync(Guid tenantId, CancellationToken ct = default);
}
