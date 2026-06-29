using Kurix.Core.Entities;

namespace Kurix.Core.Tools;

/// <summary>
/// Central registry of all available <see cref="ITool"/> implementations. Resolves
/// which tools are enabled for a given tenant (per its settings) so the
/// conversation engine only exposes the right capabilities to the model.
/// </summary>
public interface IToolRegistry
{
    /// <summary>Every registered tool, regardless of tenant.</summary>
    IReadOnlyList<ITool> GetAllTools();

    /// <summary>
    /// Tools enabled for the tenant. If the tenant's settings don't specify a
    /// list, all registered tools are returned.
    /// </summary>
    IReadOnlyList<ITool> GetEnabledTools(Tenant tenant);

    /// <summary>Looks up a tool by its <see cref="ITool.Name"/>.</summary>
    ITool? GetTool(string name);
}
