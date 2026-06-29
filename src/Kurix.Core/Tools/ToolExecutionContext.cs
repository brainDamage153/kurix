namespace Kurix.Core.Tools;

/// <summary>
/// Ambient information passed to a tool when it runs: which tenant and
/// conversation it is acting on. Tools resolve everything else (connectors,
/// services) via their constructor dependencies.
/// </summary>
public sealed record ToolExecutionContext(Guid TenantId, Guid ConversationId, string SessionId);
