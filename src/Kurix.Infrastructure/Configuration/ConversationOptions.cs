namespace Kurix.Infrastructure.Configuration;

/// <summary>
/// Binds the <c>Conversation</c> section: limits and tuning for the engine.
/// </summary>
public class ConversationOptions
{
    public const string SectionName = "Conversation";

    /// <summary>Maximum tool-calling iterations per user message.</summary>
    public int MaxToolIterations { get; set; } = 5;

    /// <summary>How many prior messages to replay as context.</summary>
    public int HistoryLimit { get; set; } = 20;

    /// <summary>Sampling temperature for the chat model.</summary>
    public float Temperature { get; set; } = 0.3f;

    /// <summary>Per-turn timeout (seconds) for the whole engine round-trip.</summary>
    public int TimeoutSeconds { get; set; } = 60;
}
