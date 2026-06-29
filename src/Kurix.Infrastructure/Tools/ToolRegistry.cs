using Kurix.Core.Entities;
using Kurix.Core.MultiTenancy;
using Kurix.Core.Tools;

namespace Kurix.Infrastructure.Tools;

/// <summary>
/// Default <see cref="IToolRegistry"/>. Aggregates every <see cref="ITool"/>
/// registered in DI and filters them per tenant according to the tenant's
/// <c>EnabledTools</c> setting.
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ITool> _tools;
    private readonly Dictionary<string, ITool> _byName;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToList();
        _byName = _tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ITool> GetAllTools() => _tools;

    public ITool? GetTool(string name) =>
        _byName.TryGetValue(name, out var tool) ? tool : null;

    public IReadOnlyList<ITool> GetEnabledTools(Tenant tenant)
    {
        var settings = TenantSettings.Parse(tenant.SettingsJson);

        // null => all tools enabled; explicit (possibly empty) list => filter.
        if (settings.EnabledTools is null)
            return _tools;

        var enabled = settings.EnabledTools.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _tools.Where(t => enabled.Contains(t.Name)).ToList();
    }
}
