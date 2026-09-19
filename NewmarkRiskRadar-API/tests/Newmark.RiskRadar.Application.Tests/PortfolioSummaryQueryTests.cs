using FluentAssertions;
using NSubstitute;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Application.Tests.TestDoubles;
using Newmark.RiskRadar.Domain.Entities;
using Xunit;

namespace Newmark.RiskRadar.Application.Tests;

/// <summary>
/// The repository and the clock are both substituted, so these assertions are about the
/// aggregation logic alone: no database, no HTTP, no wall clock.
/// </summary>
public class PortfolioSummaryQueryTests
{
    private static readonly DateOnly AsOf = new(2026, 6, 15);

    private readonly ILoanRepository _loanRepository = Substitute.For<ILoanRepository>();
    private readonly PortfolioSummaryQuery _query;

    public PortfolioSummaryQueryTests() =>
        _query = new PortfolioSummaryQuery(_loanRepository, new FixedClock(AsOf));

    private void GivenBook(params CommercialLoan[] loans) =>
        _loanRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(loans);

    [Fact]
    public async Task SumsTheServicingVolumeAcrossTheBook()
    {
        GivenBook(
            LoanBuilder.Loan(balance: 10_000_000m),
            LoanBuilder.Loan(balance: 25_000_000m),
            LoanBuilder.Loan(balance: 5_500_000m));

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.LoanCount.Should().Be(3);
        summary.TotalServicingVolume.Should().Be(40_500_000m);
        summary.AsOfDate.Should().Be(AsOf);
    }

    [Fact]
    public async Task BucketsExposureByRiskCategory()
    {
        GivenBook(
            LoanBuilder.Loan(targetDscr: 0.80m, balance: 10_000_000m, maturityDate: AsOf.AddYears(6)),
            LoanBuilder.Loan(targetDscr: 1.10m, noteRate: 0.08m, balance: 20_000_000m, maturityDate: AsOf.AddYears(6)),
            LoanBuilder.Loan(targetDscr: 2.00m, balance: 30_000_000m, maturityDate: AsOf.AddYears(6)));

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.CriticalLoanCount.Should().Be(1);
        summary.CriticalDefaultExposure.Should().Be(10_000_000m);
        summary.WarningLoanCount.Should().Be(1);
        summary.WarningExposure.Should().Be(20_000_000m);
        summary.PerformingLoanCount.Should().Be(1);
        summary.PerformingExposure.Should().Be(30_000_000m);
        summary.CriticalDefaultExposureShare.Should().BeApproximately(0.1667m, 0.0001m);
    }

    [Fact]
    public async Task WeightsAverageDscrByBalance()
    {
        GivenBook(
            LoanBuilder.Loan(targetDscr: 1.50m, balance: 60_000_000m),
            LoanBuilder.Loan(targetDscr: 1.00m, balance: 40_000_000m));

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.WeightedAverageDscr.Should().BeApproximately(1.30m, 0.0001m);
    }

    [Fact]
    public async Task CountsOnlyBalanceMaturingInsideTwelveMonths()
    {
        GivenBook(
            LoanBuilder.Loan(balance: 10_000_000m, maturityDate: AsOf.AddMonths(6)),
            LoanBuilder.Loan(balance: 15_000_000m, maturityDate: AsOf.AddMonths(11)),
            LoanBuilder.Loan(balance: 90_000_000m, maturityDate: AsOf.AddYears(4)));

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.MaturingWithinTwelveMonths.Should().Be(25_000_000m);
    }

    [Fact]
    public async Task LeavesStressFieldsNull_WhenNoScenarioIsApplied()
    {
        GivenBook(LoanBuilder.Loan());

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.AppliedStressRate.Should().BeNull();
        summary.StressedWeightedAverageDscr.Should().BeNull();
    }

    [Fact]
    public async Task RePricesCoverageUnderAStressScenario()
    {
        // Interest only at 4% with 2.00x coverage: doubling the rate halves the ratio.
        GivenBook(LoanBuilder.Loan(balance: 20_000_000m, noteRate: 0.04m, targetDscr: 2.00m));

        var summary = await _query.ExecuteAsync(StressScenario.FromPercent(8m));

        summary.AppliedStressRate.Should().Be(0.08m);
        summary.WeightedAverageDscr.Should().BeApproximately(2.00m, 0.0001m);
        summary.StressedWeightedAverageDscr.Should().BeApproximately(1.00m, 0.0001m);
    }

    [Fact]
    public async Task EscalatesExposureUnderStress()
    {
        GivenBook(LoanBuilder.Loan(balance: 20_000_000m, noteRate: 0.05m, targetDscr: 2.00m, maturityDate: AsOf.AddYears(6)));

        var inPlace = await _query.ExecuteAsync(StressScenario.InPlace);
        var stressed = await _query.ExecuteAsync(StressScenario.FromPercent(11m));

        inPlace.PerformingLoanCount.Should().Be(1);
        stressed.CriticalLoanCount.Should().Be(1);
        stressed.TotalServicingVolume.Should().Be(inPlace.TotalServicingVolume);
    }

    [Fact]
    public async Task HandlesAnEmptyBookWithoutDividingByZero()
    {
        GivenBook();

        var summary = await _query.ExecuteAsync(StressScenario.InPlace);

        summary.LoanCount.Should().Be(0);
        summary.TotalServicingVolume.Should().Be(0m);
        summary.WeightedAverageDscr.Should().BeNull();
        summary.WeightedAverageLoanTermYears.Should().BeNull();
        summary.CriticalDefaultExposureShare.Should().Be(0m);
    }

    [Fact]
    public async Task ForwardsTheCancellationTokenToTheRepository()
    {
        GivenBook(LoanBuilder.Loan());
        using var cts = new CancellationTokenSource();

        await _query.ExecuteAsync(StressScenario.InPlace, cts.Token);

        await _loanRepository.Received(1).GetAllAsync(cts.Token);
    }

    [Fact]
    public async Task RejectsANullScenario()
    {
        var act = () => _query.ExecuteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
