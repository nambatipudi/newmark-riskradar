namespace Newmark.RiskRadar.Application.Dtos;

/// <summary>Headline numbers for the servicing book, backing the dashboard metric cards.</summary>
public sealed record PortfolioSummaryDto
{
    public required DateOnly AsOfDate { get; init; }

    /// <summary>Pro-forma rate the risk figures were computed at, as a decimal fraction. Null when in place.</summary>
    public required decimal? AppliedStressRate { get; init; }

    public required int LoanCount { get; init; }

    public required decimal TotalServicingVolume { get; init; }

    /// <summary>Balance weighted DSCR at the in-place note rates.</summary>
    public required decimal? WeightedAverageDscr { get; init; }

    /// <summary>Balance weighted DSCR at the pro-forma rate. Null outside a stress scenario.</summary>
    public required decimal? StressedWeightedAverageDscr { get; init; }

    /// <summary>Weighted Average Loan Term in years. Null on an empty book.</summary>
    public required decimal? WeightedAverageLoanTermYears { get; init; }

    public required decimal CriticalDefaultExposure { get; init; }

    public required decimal CriticalDefaultExposureShare { get; init; }

    public required decimal WarningExposure { get; init; }

    public required decimal PerformingExposure { get; init; }

    public required int CriticalLoanCount { get; init; }

    public required int WarningLoanCount { get; init; }

    public required int PerformingLoanCount { get; init; }

    /// <summary>Balance maturing inside the next twelve months, the near-term rollover cliff.</summary>
    public required decimal MaturingWithinTwelveMonths { get; init; }
}
