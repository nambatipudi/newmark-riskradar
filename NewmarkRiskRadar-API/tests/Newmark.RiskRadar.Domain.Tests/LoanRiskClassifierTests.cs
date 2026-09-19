using FluentAssertions;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;
using Xunit;

namespace Newmark.RiskRadar.Domain.Tests;

public class LoanRiskClassifierTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 1);

    [Fact]
    public void Classify_IsCritical_WhenCoverageIsBelowBreakEven()
    {
        var loan = BuildLoan(netOperatingIncome: 800_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddYears(6));

        var assessment = LoanRiskClassifier.Classify(loan, AsOf);

        assessment.Category.Should().Be(RiskCategory.Critical);
        assessment.Reasons.Should().Contain(reason => reason.Contains("break-even"));
    }

    [Fact]
    public void Classify_IsCritical_WhenThinCoverageMeetsNearTermMaturity()
    {
        var loan = BuildLoan(netOperatingIncome: 1_150_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddMonths(8));

        LoanRiskClassifier.Classify(loan, AsOf).Category.Should().Be(RiskCategory.Critical);
    }

    [Fact]
    public void Classify_IsCritical_WhenLoanIsPastDue()
    {
        var loan = BuildLoan(netOperatingIncome: 2_000_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddMonths(-1));

        var assessment = LoanRiskClassifier.Classify(loan, AsOf);

        assessment.Category.Should().Be(RiskCategory.Critical);
        assessment.Reasons.Should().Contain(reason => reason.Contains("past its maturity"));
    }

    [Fact]
    public void Classify_IsCritical_WhenLeverageExceedsCeiling()
    {
        var loan = BuildLoan(
            netOperatingIncome: 2_000_000m,
            annualDebtService: 1_000_000m,
            maturity: AsOf.AddYears(6),
            appraisedValue: 11_000_000m);

        var assessment = LoanRiskClassifier.Classify(loan, AsOf);

        assessment.Category.Should().Be(RiskCategory.Critical);
        assessment.Reasons.Should().Contain(reason => reason.Contains("critical ceiling"));
    }

    [Fact]
    public void Classify_IsWarning_WhenCoverageIsThinButMaturityIsDistant()
    {
        var loan = BuildLoan(netOperatingIncome: 1_150_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddYears(6));

        LoanRiskClassifier.Classify(loan, AsOf).Category.Should().Be(RiskCategory.Warning);
    }

    [Fact]
    public void Classify_IsWarning_WhenMaturityIsInsideTheRolloverWindow()
    {
        var loan = BuildLoan(netOperatingIncome: 2_000_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddMonths(20));

        var assessment = LoanRiskClassifier.Classify(loan, AsOf);

        assessment.Category.Should().Be(RiskCategory.Warning);
        assessment.Reasons.Should().Contain(reason => reason.Contains("rollover plan"));
    }

    [Fact]
    public void Classify_IsPerforming_WhenEveryMetricIsWithinPolicy()
    {
        var loan = BuildLoan(netOperatingIncome: 2_000_000m, annualDebtService: 1_000_000m, maturity: AsOf.AddYears(6));

        var assessment = LoanRiskClassifier.Classify(loan, AsOf);

        assessment.Category.Should().Be(RiskCategory.Performing);
        assessment.Reasons.Should().ContainSingle();
    }

    [Fact]
    public void Classify_Throws_WhenLoanIsNull()
    {
        var act = () => LoanRiskClassifier.Classify(null!, AsOf);

        act.Should().Throw<ArgumentNullException>();
    }

    private static CommercialLoan BuildLoan(
        decimal netOperatingIncome,
        decimal annualDebtService,
        DateOnly maturity,
        decimal appraisedValue = 20_000_000m) =>
        CommercialLoan.Create(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            loanNumber: "NMK-TEST-0001",
            borrowerName: "Test Borrower",
            propertyName: "Test Property",
            market: "New York, NY",
            propertyType: PropertyType.Office,
            originationDate: maturity.AddYears(-10),
            maturityDate: maturity,
            originalBalance: Money.FromDecimal(10_000_000m),
            outstandingBalance: Money.FromDecimal(10_000_000m),
            netOperatingIncome: Money.FromDecimal(netOperatingIncome),
            annualDebtService: Money.FromDecimal(annualDebtService),
            appraisedValue: Money.FromDecimal(appraisedValue),
            interestRate: 0.055m,
            amortizationYears: 30);
}
