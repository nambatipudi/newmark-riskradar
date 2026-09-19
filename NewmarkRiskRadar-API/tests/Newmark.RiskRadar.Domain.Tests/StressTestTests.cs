using FluentAssertions;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Exceptions;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;
using Xunit;

namespace Newmark.RiskRadar.Domain.Tests;

/// <summary>
/// Contract for the refinancing stress test: re-price the loan at a pro-forma take-out rate,
/// hold NOI flat, and re-run triage against the resulting coverage.
/// </summary>
public class StressTestTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 15);

    [Fact]
    public void DebtServiceAt_ReturnsTheInPlaceAmount_WhenRePricedAtTheNoteRate()
    {
        var loan = AmortizingLoan(noteRate: 0.05m, balance: 10_000_000m);

        loan.DebtServiceAt(0.05m).Amount.Should().Be(loan.AnnualDebtService.Amount);
    }

    [Fact]
    public void DebtServiceAt_RisesWithTheProFormaRate()
    {
        var loan = AmortizingLoan(noteRate: 0.05m, balance: 10_000_000m);

        loan.DebtServiceAt(0.085m).Amount.Should().BeGreaterThan(loan.DebtServiceAt(0.05m).Amount);
    }

    [Fact]
    public void DebtServiceAt_TracksTheRate_ForInterestOnlyLoans()
    {
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m);

        loan.DebtServiceAt(0.09m).Amount.Should().Be(1_800_000m);
    }

    [Fact]
    public void DscrAt_FallsAsTheProFormaRateRises()
    {
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m, netOperatingIncome: 1_600_000m);

        loan.Dscr.Value.Should().Be(2.00m);
        loan.DscrAt(0.08m).Value.Should().Be(1.00m);
        loan.DscrAt(0.10m).Value.Should().Be(0.80m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.05)]
    public void DscrAt_Throws_WhenTheProFormaRateIsNotPositive(decimal rate)
    {
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m);

        var act = () => loan.DscrAt(rate);

        act.Should().Throw<InvalidFinancialInputException>()
            .Which.ParameterName.Should().Be("annualInterestRate");
    }

    [Fact]
    public void Classify_UsesInPlaceCoverage_WhenNoScenarioIsSupplied()
    {
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m, netOperatingIncome: 1_600_000m);

        LoanRiskClassifier.Classify(loan, AsOf).Category.Should().Be(RiskCategory.Performing);
    }

    [Fact]
    public void Classify_EscalatesAPerformingLoan_WhenTheTakeOutRateSpikes()
    {
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m, netOperatingIncome: 1_600_000m);

        var stressed = LoanRiskClassifier.Classify(loan, AsOf, stressRate: 0.10m);

        stressed.Category.Should().Be(RiskCategory.Critical);
        stressed.Reasons.Should().Contain(reason => reason.Contains("Stressed DSCR"));
    }

    [Fact]
    public void Classify_LeavesLeverageAndMaturityReasonsUnstressed()
    {
        // Only coverage is re-priced: debt yield and LTV are functions of NOI and value, not of rate.
        var loan = InterestOnlyLoan(noteRate: 0.04m, balance: 20_000_000m, netOperatingIncome: 1_600_000m);

        var stressed = LoanRiskClassifier.Classify(loan, AsOf, stressRate: 0.10m);

        stressed.Reasons.Should().NotContain(reason => reason.Contains("Debt yield"));
    }

    private static CommercialLoan InterestOnlyLoan(
        decimal noteRate,
        decimal balance,
        decimal netOperatingIncome = 1_600_000m) =>
        Build(noteRate, balance, netOperatingIncome, amortizationYears: 0);

    private static CommercialLoan AmortizingLoan(decimal noteRate, decimal balance) =>
        Build(noteRate, balance, netOperatingIncome: 1_000_000m, amortizationYears: 30);

    private static CommercialLoan Build(
        decimal noteRate,
        decimal balance,
        decimal netOperatingIncome,
        int amortizationYears)
    {
        var money = Money.FromDecimal(balance);

        return CommercialLoan.Create(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            loanNumber: "NMK-STRESS-0001",
            borrowerName: "Test Borrower",
            propertyName: "Test Property",
            market: "Chicago, IL",
            propertyType: PropertyType.Office,
            originationDate: new DateOnly(2021, 1, 15),
            maturityDate: new DateOnly(2031, 1, 15),
            originalBalance: money,
            outstandingBalance: money,
            netOperatingIncome: Money.FromDecimal(netOperatingIncome),
            annualDebtService: FinancialMathService.CalculateAnnualDebtService(money, noteRate, amortizationYears),
            appraisedValue: Money.FromDecimal(balance * 2m),
            interestRate: noteRate,
            amortizationYears: amortizationYears);
    }
}
