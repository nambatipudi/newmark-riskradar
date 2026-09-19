namespace Newmark.RiskRadar.Domain.Entities;

/// <summary>Triage bucket assigned by <see cref="Services.LoanRiskClassifier"/>.</summary>
public enum RiskCategory
{
    Performing = 0,
    Warning = 1,
    Critical = 2
}
