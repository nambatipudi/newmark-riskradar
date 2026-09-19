namespace Newmark.RiskRadar.Application.Dtos;

/// <summary>Balance rolling in a single calendar year, split by triage bucket for the stacked chart.</summary>
public sealed record MaturityWallBucketDto
{
    public required int Year { get; init; }

    public required int LoanCount { get; init; }

    public required decimal TotalBalance { get; init; }

    public required decimal CriticalBalance { get; init; }

    public required decimal WarningBalance { get; init; }

    public required decimal PerformingBalance { get; init; }

    /// <summary>Share of the whole book rolling in this year, as a decimal fraction.</summary>
    public required decimal ShareOfPortfolio { get; init; }
}
