using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;

namespace Newmark.RiskRadar.Application.Mapping;

internal static class LoanMapper
{
    public static LoanSummaryDto ToSummaryDto(
        CommercialLoan loan,
        RiskAssessment assessment,
        DateOnly asOf,
        StressScenario scenario) => new()
    {
        Id = loan.Id,
        LoanNumber = loan.LoanNumber,
        BorrowerName = loan.BorrowerName,
        PropertyName = loan.PropertyName,
        Market = loan.Market,
        PropertyType = loan.PropertyType.ToString(),
        OriginationDate = loan.OriginationDate,
        MaturityDate = loan.MaturityDate,
        MaturityYear = loan.MaturityDate.Year,
        MonthsToMaturity = loan.MonthsToMaturity(asOf),
        OutstandingBalance = loan.OutstandingBalance.Amount,
        OriginalBalance = loan.OriginalBalance.Amount,
        NetOperatingIncome = loan.NetOperatingIncome.Amount,
        AnnualDebtService = loan.AnnualDebtService.Amount,
        AppraisedValue = loan.AppraisedValue.Amount,
        InterestRate = loan.InterestRate,
        AmortizationYears = loan.AmortizationYears,
        Dscr = loan.Dscr.Value,
        StressedAnnualDebtService = scenario.InterestRate is { } debtServiceRate
            ? loan.DebtServiceAt(debtServiceRate).Amount
            : null,
        StressedDscr = scenario.InterestRate is { } coverageRate
            ? loan.DscrAt(coverageRate).Value
            : null,
        DebtYield = loan.DebtYield,
        LoanToValue = loan.LoanToValue,
        RiskCategory = assessment.Category.ToString(),
        RiskReasons = assessment.Reasons
    };
}
