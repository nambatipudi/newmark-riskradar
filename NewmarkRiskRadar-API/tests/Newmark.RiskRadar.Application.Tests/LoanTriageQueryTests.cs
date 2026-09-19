using FluentAssertions;
using NSubstitute;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Application.Tests.TestDoubles;
using Newmark.RiskRadar.Domain.Entities;
using Xunit;

namespace Newmark.RiskRadar.Application.Tests;

public class LoanTriageQueryTests
{
    private static readonly DateOnly AsOf = new(2026, 6, 15);

    private readonly ILoanRepository _loanRepository = Substitute.For<ILoanRepository>();
    private readonly LoanTriageQuery _query;

    public LoanTriageQueryTests() =>
        _query = new LoanTriageQuery(_loanRepository, new FixedClock(AsOf));

    private void GivenBook(params CommercialLoan[] loans) =>
        _loanRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(loans);

    private static CommercialLoan[] MixedBook() =>
    [
        LoanBuilder.Loan(loanNumber: "NMK-CRIT-01", targetDscr: 0.80m, propertyType: PropertyType.Office,
            market: "Dallas, TX", maturityDate: new DateOnly(2030, 1, 1)),
        LoanBuilder.Loan(loanNumber: "NMK-WARN-01", targetDscr: 1.10m, noteRate: 0.08m, propertyType: PropertyType.Retail,
            market: "Miami, FL", maturityDate: new DateOnly(2031, 1, 1)),
        LoanBuilder.Loan(loanNumber: "NMK-PERF-01", targetDscr: 2.00m, propertyType: PropertyType.Office,
            market: "Miami, FL", maturityDate: new DateOnly(2032, 1, 1)),
    ];

    [Fact]
    public async Task ReturnsEveryLoanWhenNoFilterIsApplied()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest());

        page.TotalCount.Should().Be(3);
        page.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task OrdersByRiskThenMaturity()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest());

        page.Items.Select(loan => loan.RiskCategory)
            .Should().Equal("Critical", "Warning", "Performing");
    }

    [Fact]
    public async Task FiltersByRiskCategory()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { RiskCategory = RiskCategory.Critical });

        page.TotalCount.Should().Be(1);
        page.Items.Single().LoanNumber.Should().Be("NMK-CRIT-01");
    }

    [Fact]
    public async Task FiltersByAssetClass()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { AssetClass = PropertyType.Office });

        page.TotalCount.Should().Be(2);
        page.Items.Should().OnlyContain(loan => loan.PropertyType == "Office");
    }

    [Fact]
    public async Task CombinesAssetClassAndRiskFilters()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest
        {
            AssetClass = PropertyType.Office,
            RiskCategory = RiskCategory.Performing,
        });

        page.TotalCount.Should().Be(1);
        page.Items.Single().LoanNumber.Should().Be("NMK-PERF-01");
    }

    [Fact]
    public async Task SearchesAcrossMarketCaseInsensitively()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { Search = "miami" });

        page.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task TreatsAWhitespaceSearchAsNoFilter()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { Search = "   " });

        page.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task PagesTheResults()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { Page = 2, PageSize = 2 });

        page.Items.Should().ContainSingle();
        page.Page.Should().Be(2);
        page.TotalPages.Should().Be(2);
        page.HasNextPage.Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public async Task ClampsAnInvalidPageNumber(int requested, int expected)
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { Page = requested });

        page.Page.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(9_000, LoanTriageRequest.MaxPageSize)]
    public async Task ClampsAnInvalidPageSize(int requested, int expected)
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { PageSize = requested });

        page.PageSize.Should().Be(expected);
    }

    [Fact]
    public async Task ProjectsStressedFiguresOnlyWhenAScenarioIsApplied()
    {
        GivenBook(LoanBuilder.Loan(balance: 20_000_000m, noteRate: 0.04m, targetDscr: 2.00m));

        var inPlace = await _query.ExecuteAsync(new LoanTriageRequest());
        var stressed = await _query.ExecuteAsync(new LoanTriageRequest
        {
            Scenario = StressScenario.FromPercent(8m),
        });

        inPlace.Items.Single().StressedDscr.Should().BeNull();
        inPlace.Items.Single().StressedAnnualDebtService.Should().BeNull();
        stressed.Items.Single().StressedDscr.Should().BeApproximately(1.00m, 0.0001m);
        stressed.Items.Single().StressedAnnualDebtService.Should().Be(1_600_000m);
    }

    [Fact]
    public async Task ReclassifiesLoansUnderStressBeforeFiltering()
    {
        GivenBook(LoanBuilder.Loan(
            balance: 20_000_000m,
            noteRate: 0.05m,
            targetDscr: 2.00m,
            maturityDate: new DateOnly(2032, 1, 1)));

        var request = new LoanTriageRequest { RiskCategory = RiskCategory.Critical };

        var inPlace = await _query.ExecuteAsync(request);
        var stressed = await _query.ExecuteAsync(request with { Scenario = StressScenario.FromPercent(11m) });

        inPlace.TotalCount.Should().Be(0);
        stressed.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ReturnsAnEmptyPageWhenNothingMatches()
    {
        GivenBook(MixedBook());

        var page = await _query.ExecuteAsync(new LoanTriageRequest { Search = "Nonexistent" });

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
        page.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task ReadsTheBookExactlyOncePerRequest()
    {
        GivenBook(MixedBook());

        await _query.ExecuteAsync(new LoanTriageRequest());

        await _loanRepository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectsANullRequest()
    {
        var act = () => _query.ExecuteAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
