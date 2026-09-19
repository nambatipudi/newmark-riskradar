using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Application.Queries;

/// <summary>
/// Groups outstanding balance by maturity year to render the rollover cliff. Years with no
/// maturities inside the covered span are emitted as zero buckets so the chart keeps a
/// continuous x axis.
/// </summary>
public sealed class MaturityWallQuery(ILoanRepository loanRepository, IClock clock)
{
    public async Task<IReadOnlyList<MaturityWallBucketDto>> ExecuteAsync(
        StressScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var asOf = clock.Today;
        var loans = await loanRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        if (loans.Count == 0)
        {
            return [];
        }

        var totalVolume = Money.Sum(loans.Select(loan => loan.OutstandingBalance));

        var byYear = loans
            .Select(loan => (Loan: loan, Category: LoanRiskClassifier.Classify(loan, asOf, scenario.InterestRate).Category))
            .GroupBy(entry => entry.Loan.MaturityDate.Year)
            .ToDictionary(group => group.Key, group => group.ToList());

        var firstYear = Math.Min(byYear.Keys.Min(), asOf.Year);
        var lastYear = byYear.Keys.Max();

        var buckets = new List<MaturityWallBucketDto>(lastYear - firstYear + 1);

        for (var year = firstYear; year <= lastYear; year++)
        {
            var entries = byYear.GetValueOrDefault(year) ?? [];
            var total = Money.Sum(entries.Select(entry => entry.Loan.OutstandingBalance));

            Money BalanceFor(RiskCategory category) => Money.Sum(entries
                .Where(entry => entry.Category == category)
                .Select(entry => entry.Loan.OutstandingBalance));

            buckets.Add(new MaturityWallBucketDto
            {
                Year = year,
                LoanCount = entries.Count,
                TotalBalance = total.Amount,
                CriticalBalance = BalanceFor(RiskCategory.Critical).Amount,
                WarningBalance = BalanceFor(RiskCategory.Warning).Amount,
                PerformingBalance = BalanceFor(RiskCategory.Performing).Amount,
                ShareOfPortfolio = totalVolume.IsZero
                    ? 0m
                    : Math.Round(total.Amount / totalVolume.Amount, 4, MidpointRounding.ToEven)
            });
        }

        return buckets;
    }
}
