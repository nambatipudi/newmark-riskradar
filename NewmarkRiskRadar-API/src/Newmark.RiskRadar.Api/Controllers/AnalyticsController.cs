using Microsoft.AspNetCore.Mvc;
using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Queries;

namespace Newmark.RiskRadar.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class AnalyticsController(
    PortfolioSummaryQuery portfolioSummaryQuery,
    MaturityWallQuery maturityWallQuery) : ControllerBase
{
    /// <summary>Total servicing volume, weighted average DSCR and critical default exposure.</summary>
    /// <param name="stressRate">Pro-forma take-out rate as a percentage, e.g. 8.5. Omit for in-place rates.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("portfolio/summary")]
    [ProducesResponseType(typeof(PortfolioSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortfolioSummaryDto>> GetPortfolioSummaryAsync(
        [FromQuery] decimal? stressRate,
        CancellationToken cancellationToken) =>
        Ok(await portfolioSummaryQuery
            .ExecuteAsync(StressScenario.FromPercent(stressRate), cancellationToken)
            .ConfigureAwait(false));

    /// <summary>Outstanding balance grouped by maturity year, split by risk bucket.</summary>
    /// <param name="stressRate">Pro-forma take-out rate as a percentage, e.g. 8.5. Omit for in-place rates.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("analytics/maturity-wall")]
    [ProducesResponseType(typeof(IReadOnlyList<MaturityWallBucketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MaturityWallBucketDto>>> GetMaturityWallAsync(
        [FromQuery] decimal? stressRate,
        CancellationToken cancellationToken) =>
        Ok(await maturityWallQuery
            .ExecuteAsync(StressScenario.FromPercent(stressRate), cancellationToken)
            .ConfigureAwait(false));
}
