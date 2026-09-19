using FluentAssertions;
using System.Globalization;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Exceptions;
using Newmark.RiskRadar.Domain.ValueObjects;
using Xunit;

namespace Newmark.RiskRadar.Domain.Tests;

/// <summary>
/// Contract for <see cref="CommercialLoan"/>: calendar-accurate months to maturity and the
/// construction guards that keep an unusable loan (zero balance, zero debt service) out of the book.
/// </summary>
public class CommercialLoanTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 15);

    public class MonthsToMaturity
    {
        [Theory]
        [InlineData("2026-01-15", 0)]   // matures today
        [InlineData("2026-01-31", 0)]   // later this month is still under one whole month
        [InlineData("2026-02-14", 0)]   // one day short of the anniversary is not a whole month
        [InlineData("2026-02-15", 1)]   // exactly one calendar month
        [InlineData("2026-07-15", 6)]
        [InlineData("2027-01-15", 12)]
        [InlineData("2028-02-15", 25)]
        public void CountsWholeCalendarMonths(string maturity, int expected)
        {
            var loan = BuildLoan(ParseDate(maturity));

            loan.MonthsToMaturity(AsOf).Should().Be(expected);
        }

        [Theory]
        [InlineData("2026-01-14", -1)]  // yesterday: already rolled
        [InlineData("2025-12-15", -1)]
        [InlineData("2025-01-15", -12)]
        public void IsNegative_ForLoansPastTheirMaturityDate(string maturity, int expected)
        {
            var loan = BuildLoan(ParseDate(maturity));

            loan.MonthsToMaturity(AsOf).Should().Be(expected);
        }

        [Fact]
        public void UsesCalendarDiffing_NotAverageDayLength()
        {
            // 28 day February: raw timestamp division would report 0 months here, the calendar says 1.
            var loan = BuildLoan(new DateOnly(2026, 2, 28));

            loan.MonthsToMaturity(new DateOnly(2026, 1, 31)).Should().Be(0);
            loan.MonthsToMaturity(new DateOnly(2026, 1, 28)).Should().Be(1);
        }

        [Theory]
        [InlineData("2026-12-15", 12, true)]
        [InlineData("2027-06-15", 12, false)]
        [InlineData("2027-06-15", 24, true)]
        public void MaturesWithin_AnswersTheRolloverWindow(string maturity, int window, bool expected)
        {
            var loan = BuildLoan(ParseDate(maturity));

            loan.MaturesWithin(window, AsOf).Should().Be(expected);
        }
    }

    public class Create
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-2_500_000)]
        public void Throws_WhenOutstandingBalanceIsNotPositive(decimal balance)
        {
            var act = () => NewLoan(outstandingBalance: balance);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("outstandingBalance");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-750_000)]
        public void Throws_WhenAnnualDebtServiceIsNotPositive(decimal debtService)
        {
            var act = () => NewLoan(annualDebtService: debtService);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("annualDebtService");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Throws_WhenAppraisedValueIsNotPositive(decimal appraisedValue)
        {
            var act = () => NewLoan(appraisedValue: appraisedValue);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("appraisedValue");
        }

        [Fact]
        public void Throws_WhenInterestRateIsNegative()
        {
            var act = () => NewLoan(interestRate: -0.01m);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("interestRate");
        }

        [Fact]
        public void Throws_WhenMaturityDoesNotFollowOrigination()
        {
            var act = () => NewLoan(originationDate: new DateOnly(2026, 1, 1), maturityDate: new DateOnly(2026, 1, 1));

            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maturityDate");
        }

        [Fact]
        public void AcceptsNegativeNetOperatingIncome_BecauseDistressIsValidData()
        {
            var loan = NewLoan(netOperatingIncome: -400_000m);

            loan.Dscr.Value.Should().BeNegative();
            loan.DebtYield.Should().BeNegative();
        }

        [Fact]
        public void ExposesRatiosDerivedFromTheStoredAmounts()
        {
            var loan = NewLoan(
                outstandingBalance: 10_000_000m,
                netOperatingIncome: 1_000_000m,
                annualDebtService: 800_000m,
                appraisedValue: 20_000_000m);

            loan.Dscr.Value.Should().Be(1.25m);
            loan.DebtYield.Should().Be(0.10m);
            loan.LoanToValue.Should().Be(0.50m);
        }
    }

    private static DateOnly ParseDate(string value) =>
        DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static CommercialLoan BuildLoan(DateOnly maturity) =>
        NewLoan(originationDate: maturity.AddYears(-10), maturityDate: maturity);

    private static CommercialLoan NewLoan(
        decimal outstandingBalance = 10_000_000m,
        decimal netOperatingIncome = 1_500_000m,
        decimal annualDebtService = 1_000_000m,
        decimal appraisedValue = 20_000_000m,
        decimal interestRate = 0.055m,
        int amortizationYears = 30,
        DateOnly? originationDate = null,
        DateOnly? maturityDate = null) =>
        CommercialLoan.Create(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            loanNumber: "NMK-TEST-0001",
            borrowerName: "Test Borrower",
            propertyName: "Test Property",
            market: "New York, NY",
            propertyType: PropertyType.Office,
            originationDate: originationDate ?? new DateOnly(2020, 1, 15),
            maturityDate: maturityDate ?? new DateOnly(2030, 1, 15),
            originalBalance: Money.FromDecimal(10_000_000m),
            outstandingBalance: Money.FromDecimal(outstandingBalance),
            netOperatingIncome: Money.FromDecimal(netOperatingIncome),
            annualDebtService: Money.FromDecimal(annualDebtService),
            appraisedValue: Money.FromDecimal(appraisedValue),
            interestRate: interestRate,
            amortizationYears: amortizationYears);
}
