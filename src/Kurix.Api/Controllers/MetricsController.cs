using Kurix.Api.Auth;
using Kurix.Core.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kurix.Api.Controllers;

/// <summary>Aggregated metrics for the dashboard (JWT). Scoped to the caller's tenant.</summary>
[ApiController]
[Route("api/metrics")]
[Authorize]
public class MetricsController(IMetricsService metrics) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MetricsSummary>> Get(CancellationToken ct)
    {
        var summary = await metrics.GetSummaryAsync(User.GetTenantId(), ct);
        return Ok(summary);
    }
}
