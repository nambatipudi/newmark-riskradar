using Microsoft.AspNetCore.Mvc;
using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Interfaces;

namespace Newmark.RiskRadar.Api.Controllers;

[ApiController]
[Route("api/v1/system")]
public sealed class SystemController(ILoanTapeSynchronizer tapeSynchronizer) : ControllerBase
{
    /// <summary>
    /// Simulates ingesting a new servicing tape: clears the book and writes a fresh batch of loans.
    /// </summary>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(TapeSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TapeSyncResultDto>> SyncAsync(CancellationToken cancellationToken)
    {
        var recordsInserted = await tapeSynchronizer.ResyncAsync(cancellationToken).ConfigureAwait(false);

        return Ok(new TapeSyncResultDto
        {
            Message = "Servicing tape successfully synced.",
            RecordsInserted = recordsInserted,
            SyncedAtUtc = DateTimeOffset.UtcNow
        });
    }
}
