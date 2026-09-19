using Microsoft.AspNetCore.Mvc;
using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Domain.Entities;

namespace Newmark.RiskRadar.Api.Controllers;

[ApiController]
[Route("api/v1/loans")]
public sealed class LoansController(LoanTriageQuery loanTriageQuery) : ControllerBase
{
    /// <summary>Paginated triage list with pre-calculated risk badges attached to every loan.</summary>
    /// <param name="page">One based page number.</param>
    /// <param name="pageSize">Rows per page, capped at 200.</param>
    /// <param name="risk">Risk bucket filter: Performing, Warning, Critical or all.</param>
    /// <param name="assetClass">Collateral asset class filter, or all.</param>
    /// <param name="search">Free text match on loan number, borrower, property or market.</param>
    /// <param name="stressRate">Pro-forma take-out rate as a percentage, e.g. 8.5. Omit for in-place rates.</param>
    /// <param name="cancellationToken">Request cancellation.</param>
    [HttpGet("triage")]
    [ProducesResponseType(typeof(PagedResultDto<LoanSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResultDto<LoanSummaryDto>>> GetTriageAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? risk = null,
        [FromQuery] string? assetClass = null,
        [FromQuery] string? search = null,
        [FromQuery] decimal? stressRate = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseFilter<RiskCategory>(risk, out var riskCategory))
        {
            return InvalidFilter(nameof(risk), risk!, "Performing, Warning, Critical");
        }

        if (!TryParseFilter<PropertyType>(assetClass, out var propertyType))
        {
            return InvalidFilter(nameof(assetClass), assetClass!, string.Join(", ", Enum.GetNames<PropertyType>()));
        }

        var request = new LoanTriageRequest
        {
            Page = page,
            PageSize = pageSize,
            RiskCategory = riskCategory,
            AssetClass = propertyType,
            Search = search,
            Scenario = StressScenario.FromPercent(stressRate)
        };

        return Ok(await loanTriageQuery.ExecuteAsync(request, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Treats a blank value or "all" as no filter; anything unrecognised is a client error.</summary>
    private static bool TryParseFilter<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result))
        {
            return false;
        }

        parsed = result;
        return true;
    }

    private ObjectResult InvalidFilter(string parameterName, string value, string allowed) =>
        new(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = $"Invalid {parameterName} filter.",
            Detail = $"'{value}' is not recognised. Use one of: {allowed}, or all.",
            Instance = HttpContext.Request.Path,
            Extensions = { ["parameterName"] = parameterName }
        })
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" }
        };
}
