using System.Text.Json;
using Kurix.Core.Conversations;
using Kurix.Core.Entities;
using Kurix.Core.Enums;
using Kurix.Core.Escalation;
using Kurix.Core.Knowledge;
using Kurix.Core.MultiTenancy;
using Kurix.Core.Tools;
using Kurix.Infrastructure.Configuration;
using Kurix.Infrastructure.Conversations;
using Kurix.Infrastructure.Escalation;
using Kurix.Infrastructure.Persistence;
using Kurix.Infrastructure.Tools;
using Kurix.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurix.Tests.Conversations;

public class ConversationServiceTests
{
    // ---- Fakes -------------------------------------------------------------

    private sealed class ScriptedChat(params ChatCompletionResult[] responses) : IChatCompletionService
    {
        private readonly Queue<ChatCompletionResult> _responses = new(responses);
        public List<IReadOnlyList<LlmMessage>> Calls { get; } = [];

        public Task<ChatCompletionResult> CompleteAsync(
            IReadOnlyList<LlmMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct)
        {
            Calls.Add(messages.ToList());
            var response = _responses.Count > 0
                ? _responses.Dequeue()
                : new ChatCompletionResult("(fin)", [], 0, 0);
            return Task.FromResult(response);
        }
    }

    private sealed class LoopingToolChat : IChatCompletionService
    {
        public int CallCount { get; private set; }

        public Task<ChatCompletionResult> CompleteAsync(
            IReadOnlyList<LlmMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(new ChatCompletionResult(
                null, [new LlmToolCall($"call_{CallCount}", "echo", "{}")], 1, 1));
        }
    }

    private sealed class ThrowingChat : IChatCompletionService
    {
        public Task<ChatCompletionResult> CompleteAsync(
            IReadOnlyList<LlmMessage> messages, IReadOnlyList<ToolDefinition> tools, CancellationToken ct) =>
            throw new InvalidOperationException("boom");
    }

    private sealed class EmptyKnowledge : IKnowledgeService
    {
        public Task<KnowledgeIngestionResult> IngestAsync(Guid t, KnowledgeDocumentInput d, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(Guid t, string q, int k, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<KnowledgeSearchResult>>([]);
        public Task DeleteDocumentAsync(Guid t, string s, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class EchoTool : ITool
    {
        public string Name => "echo";
        public string Description => "echoes";
        public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Empty;
        public Task<ToolResult> ExecuteAsync(ToolExecutionContext c, JsonElement a, CancellationToken ct) =>
            Task.FromResult(ToolResult.Ok("echoed"));
    }

    private sealed class UnusedHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new InvalidOperationException("no webhook expected");
    }

    // ---- Harness -----------------------------------------------------------

    private static KurixDbContext NewContext(string dbName) =>
        new(new DbContextOptionsBuilder<KurixDbContext>().UseInMemoryDatabase(dbName).Options);

    private static async Task<(Guid tenantId, Guid conversationId)> SeedAsync(
        KurixDbContext ctx, TenantSettings? settings = null)
    {
        var tenantId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        ctx.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = "Demo",
            ApiKeyHash = "h",
            SettingsJson = (settings ?? new TenantSettings()).ToJson()
        });
        ctx.Conversations.Add(new Conversation
        {
            Id = conversationId,
            TenantId = tenantId,
            SessionId = "sess-1",
            Status = ConversationStatus.Active
        });
        await ctx.SaveChangesAsync();
        return (tenantId, conversationId);
    }

    private static ConversationService Build(
        KurixDbContext ctx, IChatCompletionService chat, IToolRegistry registry) =>
        new(ctx, new EmptyKnowledge(), registry, chat,
            Options.Create(new ConversationOptions()),
            Options.Create(new RagOptions()),
            NullLogger<ConversationService>.Instance);

    // ---- Tests -------------------------------------------------------------

    [Fact]
    public async Task Simple_Reply_Persists_Turn_And_Sums_Tokens()
    {
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx);

        await using (var ctx = NewContext(db))
        {
            var chat = new ScriptedChat(new ChatCompletionResult("¡Hola!", [], 10, 5));
            var service = Build(ctx, chat, new ToolRegistry([]));

            var result = await service.ProcessMessageAsync(tenantId, conversationId, "hola");

            Assert.Equal("¡Hola!", result.Reply);
            Assert.False(result.Escalated);
            Assert.Empty(result.ToolsUsed);
            Assert.Equal(10, result.TokensIn);
            Assert.Equal(5, result.TokensOut);
        }

        await using (var ctx = NewContext(db))
        {
            var roles = await ctx.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.CreatedAt).Select(m => m.Role).ToListAsync();
            Assert.Equal([MessageRole.User, MessageRole.Assistant], roles);
        }
    }

    [Fact]
    public async Task Tool_Call_Is_Executed_Then_Final_Answer_Returned()
    {
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx);

        await using var ctx2 = NewContext(db);
        var chat = new ScriptedChat(
            new ChatCompletionResult(null, [new LlmToolCall("c1", "echo", "{}")], 8, 2),
            new ChatCompletionResult("Listo.", [], 6, 3));
        var service = Build(ctx2, chat, new ToolRegistry([new EchoTool()]));

        var result = await service.ProcessMessageAsync(tenantId, conversationId, "hacé algo");

        Assert.Equal("Listo.", result.Reply);
        Assert.Equal(["echo"], result.ToolsUsed);
        Assert.Equal(14, result.TokensIn);   // 8 + 6
        Assert.Equal(5, result.TokensOut);   // 2 + 3

        // The second LLM call must have seen the assistant tool-call turn + tool result.
        var lastCall = chat.Calls[^1];
        Assert.Contains(lastCall, m => m.Role == MessageRole.Tool && m.Content == "echoed");

        // A tool audit row was persisted.
        var toolRows = await ctx2.Messages.CountAsync(m => m.Role == MessageRole.Tool);
        Assert.Equal(1, toolRows);
    }

    [Fact]
    public async Task Escalation_Tool_Flips_Conversation_Status()
    {
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx);

        await using var ctx2 = NewContext(db);
        var escalation = new EscalationService(
            ctx2, new UnusedHttpClientFactory(), NullLogger<EscalationService>.Instance);
        var registry = new ToolRegistry([new EscalateToHumanTool(escalation)]);

        var chat = new ScriptedChat(
            new ChatCompletionResult(
                null, [new LlmToolCall("c1", "escalate_to_human", """{"reason":"no sé"}""")], 5, 1),
            new ChatCompletionResult("Te derivo con un agente.", [], 4, 2));
        var service = Build(ctx2, chat, registry);

        var result = await service.ProcessMessageAsync(tenantId, conversationId, "quiero un humano");

        Assert.True(result.Escalated);
        Assert.Contains("escalate_to_human", result.ToolsUsed);

        var conversation = await ctx2.Conversations.SingleAsync(c => c.Id == conversationId);
        Assert.Equal(ConversationStatus.Escalated, conversation.Status);
    }

    [Fact]
    public async Task Loop_Stops_At_Max_Iterations_With_Fallback()
    {
        var settings = new TenantSettings { FallbackMessage = "FALLBACK" };
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx, settings);

        await using var ctx2 = NewContext(db);
        var chat = new LoopingToolChat();
        var service = Build(ctx2, chat, new ToolRegistry([new EchoTool()]));

        var result = await service.ProcessMessageAsync(tenantId, conversationId, "loop");

        Assert.Equal("FALLBACK", result.Reply);
        Assert.Equal(5, chat.CallCount); // MaxToolIterations default
    }

    [Fact]
    public async Task Engine_Error_Returns_Fallback_And_Persists_Assistant()
    {
        var settings = new TenantSettings { FallbackMessage = "FALLBACK" };
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx, settings);

        await using (var ctx2 = NewContext(db))
        {
            var service = Build(ctx2, new ThrowingChat(), new ToolRegistry([]));
            var result = await service.ProcessMessageAsync(tenantId, conversationId, "rompé");
            Assert.Equal("FALLBACK", result.Reply);
        }

        await using (var ctx = NewContext(db))
        {
            var assistant = await ctx.Messages
                .SingleAsync(m => m.ConversationId == conversationId && m.Role == MessageRole.Assistant);
            Assert.Equal("FALLBACK", assistant.Content);
        }
    }

    [Fact]
    public async Task Prior_History_Is_Replayed_To_The_Model()
    {
        var db = Guid.NewGuid().ToString();
        Guid tenantId, conversationId;
        await using (var ctx = NewContext(db))
            (tenantId, conversationId) = await SeedAsync(ctx);

        // First turn.
        await using (var ctx = NewContext(db))
        {
            var chat = new ScriptedChat(new ChatCompletionResult("Primera respuesta.", [], 1, 1));
            await Build(ctx, chat, new ToolRegistry([]))
                .ProcessMessageAsync(tenantId, conversationId, "primer mensaje");
        }

        // Second turn — assert the model sees the first turn as history.
        await using (var ctx = NewContext(db))
        {
            var chat = new ScriptedChat(new ChatCompletionResult("Segunda respuesta.", [], 1, 1));
            await Build(ctx, chat, new ToolRegistry([]))
                .ProcessMessageAsync(tenantId, conversationId, "segundo mensaje");

            var sentMessages = chat.Calls[^1];
            Assert.Contains(sentMessages, m => m.Role == MessageRole.User && m.Content == "primer mensaje");
            Assert.Contains(sentMessages, m => m.Role == MessageRole.Assistant && m.Content == "Primera respuesta.");
            // Current message present exactly once and not duplicated by history.
            Assert.Single(sentMessages.Where(m => m.Role == MessageRole.User && m.Content == "segundo mensaje"));
        }
    }
}
