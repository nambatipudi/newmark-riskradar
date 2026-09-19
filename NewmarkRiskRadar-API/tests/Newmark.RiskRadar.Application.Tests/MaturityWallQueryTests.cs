using FluentAssertions;
using NSubstitute;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Application.Tests.TestDoubles;
using Newmark.RiskRadar.Domain.Entities;
using Xunit;

namespace Newmark.RiskRadar.Application.Tests;

public class MaturityWallQueryTests
{
    private static readonly DateOnly AsOf = new(2026, 6, 15);

    private readonly ILoanRepository _loanRepository = Substitute.For<ILoanRepository>();
    private readonly MaturityWallQuery _query;

    public MaturityWallQueryTests() =>
        _query = new MaturityWallQuery(_loanRepository, new FixedClock(AsOf));

    private void GivenBook(params CommercialLoan[] loans) =>
        _loanRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(loans);

    [Fact]
    public async Task GroupsBalanceByMaturityYear()
    {
        GivenBook(
            LoanBuilder.Loan(balance: 10_000_000m, maturityDate: new DateOnly(2027, 3, 1)),
            LoanBuilder.Loan(balance: 15_000_000m, maturityDate: new DateOnly(2027, 11, 30)),
            LoanBuilder.Loan(balance: 25_000_000m, maturityDate: new DateOnly(2028, 5, 5)));

        var buckets = await _query.ExecuteAsync(StressScenario.InPlace);

        buckets.Single(bucket => bucket.Year == 2027).TotalBalance.Should().Be(25_000_000m);
        buckets.Single(bucket => bucket.Year == 2027).LoanCount.Should().Be(2);
        buckets.Single(bucket => bucket.Year == 2028).TotalBalance.Should().Be(25_000_000m);
    }

    [Fact]
    public async Task EmitsZeroBucketsForYearsWithNoMaturities()
    {
        GivenBook(
            LoanBuilder.Loan(maturityDate: new DateOnly(2027, 3, 1)),
            LoanBuilder.Loan(maturityDate: new DateOnly(2030, 3, 1)));

        var buckets = await _query.ExecuteAsync(StressScenario.InPlace);

        buckets.Select(bucket => bucket.Year).Should().Equal(2026, 2027, 2028, 2029, 2030);
        buckets.Single(bucket => bucket.Year == 2028).TotalBalance.Should().Be(0m);
        buckets.Single(bucket => bucket.Year == 2028).LoanCount.Should().Be(0);
    }

    [Fact]
    public async Task StartsAtTheCurrentYearEvenWhenNothingMaturesUntilLater()
    {
        GivenBook(LoanBuilder.Loan(maturityDate: new DateOnly(2029, 1, 1)));

        var buckets = await _query.ExecuteAsync(StressScenario.InPlace);

        buckets.First().Year.Should().Be(AsOf.Year);
    }

    [Fact]
    public async Task SplitsEachYearByRiskBucket()
    {
        GivenBook(
            LoanBuilder.Loan(balance: 10_000_000m, targetDscr: 0.80m, maturityDate: new DateOnly(2030, 4, 1)),
            LoanBuilder.Loan(balance: 20_000_000m, targetDscr: 2.00m, maturityDate: new DateOnly(2030, 8, 1)));

        var bucket = (await _query.ExecuteAsync(StressScenario.InPlace)).Single(b => b.Year == 2030);

        bucket.CriticalBalance.Should().Be(10_000_000m);
        bucket.PerformingBalance.Should().Be(20_000_000m);
        (bucket.CriticalBalance + bucket.WarningBalance + bucket.PerformingBalance)
            .Should().Be(bucket.TotalBalance);
    }

    [Fact]
    public async Task ShiftsBalanceIntoCriticalUnderStress()
    {
        GivenBook(LoanBuilder.Loan(
            balance: 20_000_000m,
            noteRate: 0.05m,
            targetDscr: 2.00m,
            maturityDate: new DateOnly(2031, 4, 1)));

        var inPlace = (await _query.ExecuteAsync(StressScenario.InPlace)).Single(b => b.Year == 2031);
        var stressed = (await _query.ExecuteAsync(StressScenario.FromPercent(11m))).Single(b => b.Year == 2031);

        inPlace.PerformingBalance.Should().Be(20_000_000m);
        stressed.CriticalBalance.Should().Be(20_000_000m);
        stressed.TotalBalance.Should().Be(inPlace.TotalBalance);
    }

    [Fact]
    public async Task SharesOfPortfolioSumToOne()
    {
        GivenBook(
            LoanBuilder.Loan(balance: 25_000_000m, maturityDate: new DateOnly(2027, 1, 1)),
            LoanBuilder.Loan(balance: 75_000_000m, maturityDate: new DateOnly(2028, 1, 1)));

        var buckets = await _query.ExecuteAsync(StressScenario.InPlace);

        buckets.Sum(bucket => bucket.ShareOfPortfolio).Should().Be(1m);
        buckets.Single(bucket => bucket.Year == 2027).ShareOfPortfolio.Should().Be(0.25m);
    }

    [Fact]
    public async Task ReturnsNothingForAnEmptyBook()
    {
        GivenBook();

        (await _query.ExecuteAsync(StressScenario.InPlace)).Should().BeEmpty();
    }
}
