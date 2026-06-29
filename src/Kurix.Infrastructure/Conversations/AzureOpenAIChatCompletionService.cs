using System.ClientModel;
using Azure.AI.OpenAI;
using Kurix.Core.Conversations;
using Kurix.Core.Enums;
using Kurix.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace Kurix.Infrastructure.Conversations;

/// <summary>
/// <see cref="IChatCompletionService"/> backed by an Azure OpenAI chat deployment
/// (<c>gpt-4o-mini</c>). Maps the provider-agnostic message/tool model to the
/// OpenAI SDK types and back.
/// </summary>
public class AzureOpenAIChatCompletionService : IChatCompletionService
{
    private readonly ChatClient _client;
    private readonly float _temperature;

    public AzureOpenAIChatCompletionService(
        IOptions<AzureOpenAIOptions> openAIOptions,
        IOptions<ConversationOptions> conversationOptions)
    {
        var o = openAIOptions.Value;
        var azureClient = new AzureOpenAIClient(new Uri(o.Endpoint), new ApiKeyCredential(o.ApiKey));
        _client = azureClient.GetChatClient(o.ChatDeployment);
        _temperature = conversationOptions.Value.Temperature;
    }

    public async Task<ChatCompletionResult> CompleteAsync(
        IReadOnlyList<LlmMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        CancellationToken ct = default)
    {
        var options = new ChatCompletionOptions { Temperature = _temperature };
        foreach (var tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(
                tool.Name, tool.Description, BinaryData.FromString(tool.ParametersJsonSchema)));
        }

        var oaMessages = messages.Select(ToOpenAI).ToList();

        ChatCompletion completion = (await _client.CompleteChatAsync(oaMessages, options, ct)).Value;

        var text = string.Concat(completion.Content.Select(part => part.Text));
        var toolCalls = completion.ToolCalls
            .Select(c => new LlmToolCall(c.Id, c.FunctionName, c.FunctionArguments.ToString()))
            .ToList();

        return new ChatCompletionResult(
            string.IsNullOrEmpty(text) ? null : text,
            toolCalls,
            completion.Usage.InputTokenCount,
            completion.Usage.OutputTokenCount);
    }

    private static ChatMessage ToOpenAI(LlmMessage message) => message.Role switch
    {
        MessageRole.System => new SystemChatMessage(message.Content ?? string.Empty),
        MessageRole.User => new UserChatMessage(message.Content ?? string.Empty),
        MessageRole.Assistant when message.ToolCalls.Count > 0 =>
            new AssistantChatMessage(message.ToolCalls.Select(tc =>
                ChatToolCall.CreateFunctionToolCall(tc.Id, tc.ToolName, BinaryData.FromString(tc.ArgumentsJson)))),
        MessageRole.Assistant => new AssistantChatMessage(message.Content ?? string.Empty),
        MessageRole.Tool => new ToolChatMessage(message.ToolCallId ?? string.Empty, message.Content ?? string.Empty),
        _ => new UserChatMessage(message.Content ?? string.Empty)
    };
}
