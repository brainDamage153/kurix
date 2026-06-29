using System.Text.Json;
using Kurix.Core.Entities;
using Kurix.Core.MultiTenancy;
using Kurix.Core.Tools;
using Kurix.Infrastructure.Tools;

namespace Kurix.Tests.Tools;

public class ToolRegistryTests
{
    private sealed class FakeTool(string name) : ITool
    {
        public string Name => name;
        public string Description => $"fake {name}";
        public ToolParameterSchema GetParameterSchema() => ToolParameterSchema.Empty;
        public Task<ToolResult> ExecuteAsync(ToolExecutionContext c, JsonElement a, CancellationToken ct) =>
            Task.FromResult(ToolResult.Ok("ok"));
    }

    private static Tenant TenantWith(params string[]? enabledTools)
    {
        var settings = new TenantSettings
        {
            EnabledTools = enabledTools is null ? null : [.. enabledTools]
        };
        return new Tenant { Name = "T", ApiKeyHash = "h", SettingsJson = settings.ToJson() };
    }

    private static ToolRegistry Registry() =>
        new([new FakeTool("alpha"), new FakeTool("beta"), new FakeTool("gamma")]);

    [Fact]
    public void GetAllTools_Returns_Everything()
    {
        Assert.Equal(3, Registry().GetAllTools().Count);
    }

    [Fact]
    public void GetTool_Is_Case_Insensitive()
    {
        var registry = Registry();
        Assert.NotNull(registry.GetTool("ALPHA"));
        Assert.Null(registry.GetTool("missing"));
    }

    [Fact]
    public void Null_EnabledTools_Means_All_Enabled()
    {
        var tenant = TenantWith(enabledTools: null);
        Assert.Equal(3, Registry().GetEnabledTools(tenant).Count);
    }

    [Fact]
    public void Explicit_List_Filters_Tools()
    {
        var tenant = TenantWith("alpha", "gamma");
        var enabled = Registry().GetEnabledTools(tenant).Select(t => t.Name).ToHashSet();
        Assert.Equal(new HashSet<string> { "alpha", "gamma" }, enabled);
    }

    [Fact]
    public void Empty_List_Means_No_Tools()
    {
        var tenant = TenantWith();
        Assert.Empty(Registry().GetEnabledTools(tenant));
    }
}
