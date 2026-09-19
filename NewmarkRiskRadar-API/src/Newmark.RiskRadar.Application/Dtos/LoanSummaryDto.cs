namespace Newmark.RiskRadar.Application.Dtos;

/// <summary>One loan flattened for the triage grid, with every ratio pre-calculated server side.</summary>
public sealed record LoanSummaryDto
{
    public required Guid Id { get; init; }

    public required string LoanNumber { get; init; }

    public required string BorrowerName { get; init; }

    public required string PropertyName { get; init; }

    public required string Market { get; init; }

    public required string PropertyType { get; init; }

    public required DateOnly OriginationDate { get; init; }

    public required DateOnly MaturityDate { get; init; }

    public required int MaturityYear { get; init; }

    public required int MonthsToMaturity { get; init; }

    public required decimal OutstandingBalance { get; init; }

    public required decimal OriginalBalance { get; init; }

    public required decimal NetOperatingIncome { get; init; }

    public required decimal AnnualDebtService { get; init; }

    public required decimal AppraisedValue { get; init; }

    public required decimal InterestRate { get; init; }

    /// <summary>Amortization schedule in years. Zero means interest only.</summary>
    public required int AmortizationYears { get; init; }

    /// <summary>Null when the loan carries no scheduled debt service.</summary>
    public required decimal? Dscr { get; init; }

    /// <summary>Debt service the loan would carry at the pro-forma rate. Null outside a stress scenario.</summary>
    public required decimal? StressedAnnualDebtService { get; init; }

    /// <summary>Coverage at the pro-forma rate. Null outside a stress scenario.</summary>
    public required decimal? StressedDscr { get; init; }

    public required decimal? DebtYield { get; init; }

    public required decimal? LoanToValue { get; init; }

    public required string RiskCategory { get; init; }

    public required IReadOnlyList<string> RiskReasons { get; init; }
}
