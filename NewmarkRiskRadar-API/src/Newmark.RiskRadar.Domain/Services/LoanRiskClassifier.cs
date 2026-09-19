using Newmark.RiskRadar.Domain.Entities;

namespace Newmark.RiskRadar.Domain.Services;

/// <summary>Triage outcome plus the human readable reasons that drove it.</summary>
public sealed record RiskAssessment(RiskCategory Category, IReadOnlyList<string> Reasons);

/// <summary>
/// Deterministic triage rules. Critical conditions are evaluated first and any single
/// hit is enough to escalate the loan; a loan that trips nothing is performing.
/// </summary>
public static class LoanRiskClassifier
{
    public const decimal CriticalDscrThreshold = 1.00m;
    public const decimal WarningDscrThreshold = 1.25m;
    public const decimal CriticalDebtYieldThreshold = 0.06m;
    public const decimal WarningDebtYieldThreshold = 0.08m;
    public const decimal CriticalLoanToValueThreshold = 0.85m;
    public const decimal WarningLoanToValueThreshold = 0.75m;
    public const int CriticalMaturityWindowMonths = 12;
    public const int WarningMaturityWindowMonths = 24;

    /// <param name="stressRate">
    /// Pro-forma take-out rate as a decimal fraction. When supplied, coverage is judged on the debt
    /// service the loan would carry at that rate instead of its in-place note rate.
    /// </param>
    public static RiskAssessment Classify(CommercialLoan loan, DateOnly asOf, decimal? stressRate = null)
    {
        ArgumentNullException.ThrowIfNull(loan);

        var dscr = stressRate is null ? loan.Dscr : loan.DscrAt(stressRate.Value);
        var coverage = stressRate is null ? "DSCR" : "Stressed DSCR";
        var coverageInline = stressRate is null ? "DSCR" : "stressed DSCR";
        var debtYield = loan.DebtYield;
        var loanToValue = loan.LoanToValue;
        var monthsToMaturity = loan.MonthsToMaturity(asOf);

        var critical = new List<string>();

        if (loan.NetOperatingIncome.IsNegative)
        {
            critical.Add("Net operating income is negative.");
        }

        if (dscr.IsBelow(CriticalDscrThreshold))
        {
            critical.Add($"{coverage} of {dscr} is below the {CriticalDscrThreshold:0.00} break-even threshold.");
        }

        if (monthsToMaturity < 0)
        {
            critical.Add("Loan is past its maturity date and has not been resolved.");
        }
        else if (monthsToMaturity <= CriticalMaturityWindowMonths && dscr.IsBelow(WarningDscrThreshold))
        {
            critical.Add($"Matures in {monthsToMaturity} months with a {coverageInline} of {dscr}, under the {WarningDscrThreshold:0.00} needed to clear a refinance.");
        }

        if (debtYield < CriticalDebtYieldThreshold)
        {
            critical.Add($"Debt yield of {debtYield:P2} is below the {CriticalDebtYieldThreshold:P0} takeout floor.");
        }

        if (loanToValue > CriticalLoanToValueThreshold)
        {
            critical.Add($"LTV of {loanToValue:P2} exceeds the {CriticalLoanToValueThreshold:P0} critical ceiling.");
        }

        if (critical.Count > 0)
        {
            return new RiskAssessment(RiskCategory.Critical, critical);
        }

        var warnings = new List<string>();

        if (dscr.IsBelow(WarningDscrThreshold))
        {
            warnings.Add($"{coverage} of {dscr} is below the {WarningDscrThreshold:0.00} underwriting standard.");
        }

        if (monthsToMaturity <= WarningMaturityWindowMonths)
        {
            warnings.Add($"Matures in {monthsToMaturity} months and needs a rollover plan.");
        }

        if (debtYield < WarningDebtYieldThreshold)
        {
            warnings.Add($"Debt yield of {debtYield:P2} is below the {WarningDebtYieldThreshold:P0} comfort level.");
        }

        if (loanToValue > WarningLoanToValueThreshold)
        {
            warnings.Add($"LTV of {loanToValue:P2} exceeds the {WarningLoanToValueThreshold:P0} guideline.");
        }

        return warnings.Count > 0
            ? new RiskAssessment(RiskCategory.Warning, warnings)
            : new RiskAssessment(RiskCategory.Performing, new[] { "Coverage, leverage and maturity runway are all within policy." });
    }
}
