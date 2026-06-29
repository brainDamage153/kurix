using Kurix.Core.Connectors;
using Kurix.Core.Tools;
using Kurix.Tools.Connectors;
using Microsoft.Extensions.DependencyInjection;

namespace Kurix.Tools;

/// <summary>
/// Registers the Module 1 tools and their connectors. The connectors are mocks
/// for the demo; replacing them per client is done by registering a real
/// implementation of <see cref="ICalendarConnector"/> / <see cref="IInventoryConnector"/>
/// in the host before calling this (or by overriding afterwards).
///
/// <see cref="IToolRegistry"/> and <see cref="IEscalationService"/> are provided by
/// the Infrastructure layer; this module only contributes <see cref="ITool"/>s.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKurixTools(this IServiceCollection services)
    {
        // Mock connectors (demo). TryAdd so a host can register real ones first.
        services.AddSingleton<ICalendarConnector, MockCalendarConnector>();
        services.AddSingleton<IInventoryConnector, MockInventoryConnector>();

        // Module 1 tools.
        services.AddScoped<ITool, CheckAvailabilityTool>();
        services.AddScoped<ITool, CreateBookingTool>();
        services.AddScoped<ITool, SearchInventoryTool>();
        services.AddScoped<ITool, EscalateToHumanTool>();

        return services;
    }
}
