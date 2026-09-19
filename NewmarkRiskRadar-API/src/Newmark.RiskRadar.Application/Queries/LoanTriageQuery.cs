using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Application.Mapping;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;

namespace Newmark.RiskRadar.Application.Queries;

public sealed record LoanTriageRequest
{
    public const int MaxPageSize = 200;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public RiskCategory? RiskCategory { get; init; }

    /// <summary>Collateral asset class filter, e.g. Office.</summary>
    public PropertyType? AssetClass { get; init; }

    /// <summary>Free text match across loan number, borrower, property and market.</summary>
    public string? Search { get; init; }

    public StressScenario Scenario { get; init; } = StressScenario.InPlace;

    public LoanTriageRequest Normalized() => this with
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize switch
        {
            < 1 => 25,
            > MaxPageSize => MaxPageSize,
            _ => PageSize
        },
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim()
    };
}

/// <summary>
/// Paginated triage list. Risk badges are derived in the domain and depend on the stress scenario,
/// so filtering and ordering on risk happen here rather than in the data store.
/// </summary>
public sealed class LoanTriageQuery(ILoanRepository loanRepository, IClock clock)
{
    public async Task<PagedResultDto<LoanSummaryDto>> ExecuteAsync(
        LoanTriageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalized = request.Normalized();
        var scenario = normalized.Scenario;
        var asOf = clock.Today;
        var loans = await loanRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var assessed = loans
            .Where(loan => normalized.AssetClass is null || loan.PropertyType == normalized.AssetClass)
            .Where(loan => Matches(loan, normalized.Search))
            .Select(loan => (Loan: loan, Assessment: LoanRiskClassifier.Classify(loan, asOf, scenario.InterestRate)))
            .Where(entry => normalized.RiskCategory is null || entry.Assessment.Category == normalized.RiskCategory)
            .OrderByDescending(entry => entry.Assessment.Category)
            .ThenBy(entry => entry.Loan.MaturityDate)
            .ThenByDescending(entry => entry.Loan.OutstandingBalance)
            .ToList();

        var items = assessed
            .Skip((normalized.Page - 1) * normalized.PageSize)
            .Take(normalized.PageSize)
            .Select(entry => LoanMapper.ToSummaryDto(entry.Loan, entry.Assessment, asOf, scenario))
            .ToList();

        return new PagedResultDto<LoanSummaryDto>
        {
            Items = items,
            Page = normalized.Page,
            PageSize = normalized.PageSize,
            TotalCount = assessed.Count
        };
    }

    private static bool Matches(CommercialLoan loan, string? search)
    {
        if (search is null)
        {
            return true;
        }

        return loan.LoanNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
            || loan.BorrowerName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || loan.PropertyName.Contains(search, StringComparison.OrdinalIgnoreCase)
            || loan.Market.Contains(search, StringComparison.OrdinalIgnoreCase);
    }
}
