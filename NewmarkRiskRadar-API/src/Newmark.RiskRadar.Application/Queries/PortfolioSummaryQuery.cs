using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Application.Queries;

/// <summary>Aggregates the servicing book into the dashboard headline metrics for a given scenario.</summary>
public sealed class PortfolioSummaryQuery(ILoanRepository loanRepository, IClock clock)
{
    public async Task<PortfolioSummaryDto> ExecuteAsync(
        StressScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var asOf = clock.Today;
        var loans = await loanRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        var totalVolume = Money.Sum(loans.Select(loan => loan.OutstandingBalance));

        var exposureByCategory = loans
            .GroupBy(loan => LoanRiskClassifier.Classify(loan, asOf, scenario.InterestRate).Category)
            .ToDictionary(
                group => group.Key,
                group => (Balance: Money.Sum(group.Select(loan => loan.OutstandingBalance)), Count: group.Count()));

        var critical = exposureByCategory.GetValueOrDefault(RiskCategory.Critical);
        var warning = exposureByCategory.GetValueOrDefault(RiskCategory.Warning);
        var performing = exposureByCategory.GetValueOrDefault(RiskCategory.Performing);

        var maturingWithinTwelveMonths = Money.Sum(loans
            .Where(loan => loan.MaturesWithin(12, asOf))
            .Select(loan => loan.OutstandingBalance));

        return new PortfolioSummaryDto
        {
            AsOfDate = asOf,
            AppliedStressRate = scenario.InterestRate,
            LoanCount = loans.Count,
            TotalServicingVolume = totalVolume.Amount,
            WeightedAverageDscr = WeightedDscrOf(loans, loan => loan.Dscr),
            StressedWeightedAverageDscr = scenario.InterestRate is { } rate
                ? WeightedDscrOf(loans, loan => loan.DscrAt(rate))
                : null,
            WeightedAverageLoanTermYears = FinancialMathService
                .CalculateWalt(loans.Select(loan => new WeightedMaturity(loan.OutstandingBalance, loan.MaturityDate)), asOf),
            CriticalDefaultExposure = critical.Balance.Amount,
            CriticalDefaultExposureShare = totalVolume.IsZero
                ? 0m
                : Math.Round(critical.Balance.Amount / totalVolume.Amount, 4, MidpointRounding.ToEven),
            WarningExposure = warning.Balance.Amount,
            PerformingExposure = performing.Balance.Amount,
            CriticalLoanCount = critical.Count,
            WarningLoanCount = warning.Count,
            PerformingLoanCount = performing.Count,
            MaturingWithinTwelveMonths = maturingWithinTwelveMonths.Amount
        };
    }

    private static decimal? WeightedDscrOf(
        IReadOnlyList<CommercialLoan> loans,
        Func<CommercialLoan, Dscr> coverage) =>
        FinancialMathService
            .CalculateWeightedAverageDscr(loans.Select(loan => new WeightedDscr(loan.OutstandingBalance, coverage(loan))))
            .Value;
}
