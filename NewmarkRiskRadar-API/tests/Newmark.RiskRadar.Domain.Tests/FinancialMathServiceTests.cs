using FluentAssertions;
using Newmark.RiskRadar.Domain.Exceptions;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;
using Xunit;

namespace Newmark.RiskRadar.Domain.Tests;

/// <summary>
/// Contract for <see cref="FinancialMathService"/>: DSCR, debt yield and term math.
/// Every denominator is guarded, so a zero or negative debt service or loan balance
/// surfaces as <see cref="InvalidFinancialInputException"/> and never as a silent
/// divide-by-zero or a misleading ratio.
/// </summary>
public class FinancialMathServiceTests
{
    private static readonly DateOnly AsOf = new(2026, 1, 15);

    public class CalculateDscr
    {
        [Theory]
        [InlineData(1_250_000, 1_000_000, 1.25)]
        [InlineData(1_000_000, 1_000_000, 1.00)]
        [InlineData(750_000, 1_000_000, 0.75)]
        [InlineData(2_400_000, 1_600_000, 1.50)]
        public void DividesNetOperatingIncomeByAnnualDebtService(decimal noi, decimal debtService, decimal expected)
        {
            var dscr = FinancialMathService.CalculateDscr(Money.FromDecimal(noi), Money.FromDecimal(debtService));

            dscr.IsDefined.Should().BeTrue();
            dscr.Value.Should().Be(expected);
        }

        [Fact]
        public void RoundsToFourDecimalPlaces()
        {
            var dscr = FinancialMathService.CalculateDscr(Money.FromDecimal(1_000_000m), Money.FromDecimal(3_000_000m));

            dscr.Value.Should().Be(0.3333m);
        }

        [Fact]
        public void AllowsNegativeCoverage_WhenPropertyOperatesAtALoss()
        {
            // A negative NOI is real data, not bad input, so it must flow through to the ratio.
            var dscr = FinancialMathService.CalculateDscr(Money.FromDecimal(-250_000m), Money.FromDecimal(1_000_000m));

            dscr.Value.Should().Be(-0.25m);
            dscr.IsBelow(LoanRiskClassifier.CriticalDscrThreshold).Should().BeTrue();
        }

        [Fact]
        public void Throws_WhenAnnualDebtServiceIsZero()
        {
            var act = () => FinancialMathService.CalculateDscr(Money.FromDecimal(900_000m), Money.Zero);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("annualDebtService");
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-1)]
        [InlineData(-1_000_000)]
        public void Throws_WhenAnnualDebtServiceIsNegative(decimal debtService)
        {
            var act = () => FinancialMathService.CalculateDscr(
                Money.FromDecimal(900_000m),
                Money.FromDecimal(debtService));

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("annualDebtService");
        }

        [Fact]
        public void Throws_WithAMessageNamingTheOffendingValue()
        {
            var act = () => FinancialMathService.CalculateDscr(Money.FromDecimal(1m), Money.Zero);

            act.Should().Throw<InvalidFinancialInputException>()
                .WithMessage("Annual debt service must be greater than zero, but was 0.");
        }
    }

    public class CalculateDebtYield
    {
        [Theory]
        [InlineData(700_000, 10_000_000, 0.07)]
        [InlineData(1_200_000, 10_000_000, 0.12)]
        [InlineData(450_000, 9_000_000, 0.05)]
        public void DividesNetOperatingIncomeByOutstandingBalance(decimal noi, decimal balance, decimal expected)
        {
            FinancialMathService
                .CalculateDebtYield(Money.FromDecimal(noi), Money.FromDecimal(balance))
                .Should().Be(expected);
        }

        [Fact]
        public void RoundsToFourDecimalPlaces()
        {
            FinancialMathService
                .CalculateDebtYield(Money.FromDecimal(1_000_000m), Money.FromDecimal(14_000_000m))
                .Should().Be(0.0714m);
        }

        [Fact]
        public void AllowsNegativeYield_WhenNetOperatingIncomeIsNegative()
        {
            FinancialMathService
                .CalculateDebtYield(Money.FromDecimal(-500_000m), Money.FromDecimal(10_000_000m))
                .Should().Be(-0.05m);
        }

        [Fact]
        public void Throws_WhenOutstandingBalanceIsZero()
        {
            var act = () => FinancialMathService.CalculateDebtYield(Money.FromDecimal(400_000m), Money.Zero);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("outstandingBalance");
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-1)]
        [InlineData(-5_000_000)]
        public void Throws_WhenOutstandingBalanceIsNegative(decimal balance)
        {
            var act = () => FinancialMathService.CalculateDebtYield(
                Money.FromDecimal(400_000m),
                Money.FromDecimal(balance));

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("outstandingBalance");
        }
    }

    public class CalculateLoanToValue
    {
        [Fact]
        public void DividesBalanceByAppraisedValue()
        {
            FinancialMathService
                .CalculateLoanToValue(Money.FromDecimal(7_500_000m), Money.FromDecimal(10_000_000m))
                .Should().Be(0.75m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Throws_WhenLoanBalanceIsNotPositive(decimal balance)
        {
            var act = () => FinancialMathService.CalculateLoanToValue(
                Money.FromDecimal(balance),
                Money.FromDecimal(10_000_000m));

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("outstandingBalance");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Throws_WhenAppraisedValueIsNotPositive(decimal appraisedValue)
        {
            var act = () => FinancialMathService.CalculateLoanToValue(
                Money.FromDecimal(5_000_000m),
                Money.FromDecimal(appraisedValue));

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("appraisedValue");
        }
    }

    public class CalculateYearsToMaturity
    {
        [Fact]
        public void IsPositive_ForLoansStillOutstanding()
        {
            FinancialMathService.CalculateYearsToMaturity(AsOf.AddYears(5), AsOf)
                .Should().BeApproximately(5m, 0.01m);
        }

        [Fact]
        public void IsNegative_ForLoansAlreadyPastDue()
        {
            FinancialMathService.CalculateYearsToMaturity(AsOf.AddYears(-1), AsOf).Should().BeNegative();
        }

        [Fact]
        public void IsZero_OnTheMaturityDateItself()
        {
            FinancialMathService.CalculateYearsToMaturity(AsOf, AsOf).Should().Be(0m);
        }
    }

    public class CalculateWalt
    {
        [Fact]
        public void WeightsTermByBalance()
        {
            var positions = new[]
            {
                new WeightedMaturity(Money.FromDecimal(75_000_000m), AsOf.AddYears(1)),
                new WeightedMaturity(Money.FromDecimal(25_000_000m), AsOf.AddYears(5))
            };

            FinancialMathService.CalculateWalt(positions, AsOf).Should().BeApproximately(2.0m, 0.01m);
        }

        [Fact]
        public void ReturnsNull_ForAnEmptyBook()
        {
            FinancialMathService.CalculateWalt([], AsOf).Should().BeNull();
        }

        [Fact]
        public void Throws_WhenAPositionCarriesANegativeBalance()
        {
            var act = () => FinancialMathService.CalculateWalt(
                [new WeightedMaturity(Money.FromDecimal(-1m), AsOf.AddYears(2))],
                AsOf);

            act.Should().Throw<InvalidFinancialInputException>();
        }
    }

    public class CalculateWeightedAverageDscr
    {
        [Fact]
        public void WeightsCoverageByBalance()
        {
            var positions = new[]
            {
                new WeightedDscr(Money.FromDecimal(60_000_000m), Dscr.FromRatio(1.50m)),
                new WeightedDscr(Money.FromDecimal(40_000_000m), Dscr.FromRatio(1.00m))
            };

            FinancialMathService.CalculateWeightedAverageDscr(positions).Value.Should().Be(1.30m);
        }

        [Fact]
        public void ExcludesUndefinedRatiosFromBothSidesOfTheAverage()
        {
            var positions = new[]
            {
                new WeightedDscr(Money.FromDecimal(60_000_000m), Dscr.FromRatio(1.50m)),
                new WeightedDscr(Money.FromDecimal(40_000_000m), Dscr.FromRatio(1.00m)),
                new WeightedDscr(Money.FromDecimal(900_000_000m), Dscr.Undefined)
            };

            FinancialMathService.CalculateWeightedAverageDscr(positions).Value.Should().Be(1.30m);
        }

        [Fact]
        public void ReturnsUndefined_WhenNothingInTheBookIsCovered()
        {
            FinancialMathService
                .CalculateWeightedAverageDscr([new WeightedDscr(Money.FromDecimal(10_000_000m), Dscr.Undefined)])
                .IsDefined.Should().BeFalse();
        }
    }

    public class CalculateAnnualDebtService
    {
        [Fact]
        public void ReturnsInterestOnly_WhenTheLoanDoesNotAmortize()
        {
            FinancialMathService
                .CalculateAnnualDebtService(Money.FromDecimal(10_000_000m), 0.05m, 0)
                .Amount.Should().Be(500_000m);
        }

        [Fact]
        public void MatchesTheStandardAmortizationPayment()
        {
            // $10M at 5.00% on a 30 year schedule amortises to $53,682.16 a month.
            FinancialMathService
                .CalculateAnnualDebtService(Money.FromDecimal(10_000_000m), 0.05m, 30)
                .Amount.Should().BeApproximately(644_185.92m, 1m);
        }

        [Fact]
        public void Throws_OnNegativePrincipal()
        {
            var act = () => FinancialMathService.CalculateAnnualDebtService(Money.FromDecimal(-5m), 0.05m, 30);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("principal");
        }

        [Fact]
        public void Throws_OnNegativeInterestRate()
        {
            var act = () => FinancialMathService.CalculateAnnualDebtService(Money.FromDecimal(5m), -0.01m, 30);

            act.Should().Throw<InvalidFinancialInputException>()
                .Which.ParameterName.Should().Be("annualInterestRate");
        }
    }
}
