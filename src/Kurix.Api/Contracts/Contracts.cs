using System.ComponentModel.DataAnnotations;

namespace Kurix.Api.Contracts;

// ---- Chat (widget, API key) ----

public record ChatRequest
{
    /// <summary>Existing widget session id; omit to start a new session.</summary>
    public string? SessionId { get; init; }

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Message { get; init; } = string.Empty;
}

public record ChatResponse(
    string Reply, bool Escalated, string SessionId, Guid ConversationId, IReadOnlyList<string> ToolsUsed);

// ---- Auth (dashboard) ----

public record LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse(string Token, string Email, string Role, Guid TenantId);

// ---- Knowledge (dashboard) ----

public record IngestDocumentRequest
{
    [Required, StringLength(512, MinimumLength = 1)]
    public string FileName { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public string Content { get; init; } = string.Empty;

    public string? Metadata { get; init; }
}

public record DocumentResponse(
    Guid Id, string FileName, string Status, int ChunkCount, DateTimeOffset IngestedAt, string? ErrorMessage);

// ---- Conversations (dashboard) ----

public record ConversationSummaryResponse(
    Guid Id, string SessionId, string Status, DateTimeOffset StartedAt, DateTimeOffset? LastMessageAt);

public record MessageResponse(string Role, string Content, string? ToolName, DateTimeOffset CreatedAt);

public record ConversationDetailResponse(
    Guid Id, string SessionId, string Status, DateTimeOffset StartedAt, DateTimeOffset? LastMessageAt,
    IReadOnlyList<MessageResponse> Messages);

// ---- Tenant settings (dashboard) ----

public record TenantSettingsDto
{
    public string Persona { get; init; } = string.Empty;
    public List<string>? EnabledTools { get; init; }
    public string? EscalationWebhookUrl { get; init; }
    public string FallbackMessage { get; init; } = string.Empty;
}
