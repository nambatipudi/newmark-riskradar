using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Application.Queries;
using Newmark.RiskRadar.Infrastructure.Seed;
using Xunit;

namespace Newmark.RiskRadar.Api.Tests;

public class ApiEndpointTests(RiskRadarApiFactory factory) : IClassFixture<RiskRadarApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PortfolioSummary_ReturnsSeededBookTotals()
    {
        var summary = await _client.GetFromJsonAsync<PortfolioSummaryDto>("/api/v1/portfolio/summary");

        summary.Should().NotBeNull();
        summary!.LoanCount.Should().Be(SyntheticDataSeeder.LoanCount);
        summary.AsOfDate.Should().Be(RiskRadarApiFactory.AsOf);
        summary.TotalServicingVolume.Should().BePositive();
        summary.WeightedAverageDscr.Should().NotBeNull();
        summary.WeightedAverageLoanTermYears.Should().NotBeNull();
        (summary.CriticalLoanCount + summary.WarningLoanCount + summary.PerformingLoanCount)
            .Should().Be(summary.LoanCount);
        summary.CriticalDefaultExposureShare.Should().BeInRange(0m, 1m);
        (summary.CriticalDefaultExposure + summary.WarningExposure + summary.PerformingExposure)
            .Should().Be(summary.TotalServicingVolume);
    }

    [Fact]
    public async Task MaturityWall_ReturnsContiguousYearsCoveringTheBook()
    {
        var buckets = await _client.GetFromJsonAsync<List<MaturityWallBucketDto>>("/api/v1/analytics/maturity-wall") ?? [];

        buckets.Should().NotBeEmpty();
        buckets.Select(bucket => bucket.Year).Should().BeInAscendingOrder();
        buckets.Should().OnlyContain(bucket =>
            bucket.TotalBalance == bucket.CriticalBalance + bucket.WarningBalance + bucket.PerformingBalance);
        buckets.Sum(bucket => bucket.LoanCount).Should().Be(SyntheticDataSeeder.LoanCount);

        for (var index = 1; index < buckets.Count; index++)
        {
            buckets[index].Year.Should().Be(buckets[index - 1].Year + 1);
        }
    }

    [Fact]
    public async Task Triage_IsPaginatedAndCarriesRiskBadges()
    {
        var page = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>("/api/v1/loans/triage?page=1&pageSize=10");

        page.Should().NotBeNull();
        page!.Items.Should().HaveCount(10);
        page.TotalCount.Should().Be(SyntheticDataSeeder.LoanCount);
        page.TotalPages.Should().Be(5);
        page.HasNextPage.Should().BeTrue();
        page.Items.Should().OnlyContain(loan =>
            loan.RiskCategory == "Critical" || loan.RiskCategory == "Warning" || loan.RiskCategory == "Performing");
        page.Items.Should().OnlyContain(loan => loan.RiskReasons.Count > 0);
    }

    [Fact]
    public async Task Triage_FiltersByRiskCategory()
    {
        var page = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>("/api/v1/loans/triage?risk=critical&pageSize=200");

        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(loan => loan.RiskCategory == "Critical");
    }

    [Fact]
    public async Task Triage_RejectsUnknownRiskCategory()
    {
        var response = await _client.GetAsync("/api/v1/loans/triage?risk=exploding");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Triage_ClampsOversizedPageRequests()
    {
        var page = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>("/api/v1/loans/triage?page=-4&pageSize=9000");

        page.Should().NotBeNull();
        page!.Page.Should().Be(1);
        page.PageSize.Should().Be(LoanTriageRequest.MaxPageSize);
    }

    [Fact]
    public async Task Triage_SearchesAcrossLoanAttributes()
    {
        var all = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>("/api/v1/loans/triage?pageSize=200");
        var market = all!.Items[0].Market;

        var filtered = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>(
            $"/api/v1/loans/triage?pageSize=200&search={Uri.EscapeDataString(market)}");

        filtered.Should().NotBeNull();
        filtered!.Items.Should().NotBeEmpty();
        filtered.Items.Should().OnlyContain(loan => loan.Market == market);
    }

    [Fact]
    public async Task Health_ReportsHealthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PortfolioSummary_EscalatesRiskUnderAStressScenario()
    {
        var inPlace = await _client.GetFromJsonAsync<PortfolioSummaryDto>("/api/v1/portfolio/summary");
        var stressed = await _client.GetFromJsonAsync<PortfolioSummaryDto>("/api/v1/portfolio/summary?stressRate=10");

        stressed!.AppliedStressRate.Should().Be(0.10m);
        stressed.StressedWeightedAverageDscr.Should().NotBeNull();
        stressed.StressedWeightedAverageDscr.Should().BeLessThan(stressed.WeightedAverageDscr!.Value);
        stressed.CriticalLoanCount.Should().BeGreaterThan(inPlace!.CriticalLoanCount);
        stressed.TotalServicingVolume.Should().Be(inPlace.TotalServicingVolume);
    }

    [Fact]
    public async Task PortfolioSummary_OmitsStressFields_WhenNoScenarioIsRequested()
    {
        var summary = await _client.GetFromJsonAsync<PortfolioSummaryDto>("/api/v1/portfolio/summary");

        summary!.AppliedStressRate.Should().BeNull();
        summary.StressedWeightedAverageDscr.Should().BeNull();
    }

    [Theory]
    [InlineData(0.4)]
    [InlineData(80)]
    public async Task PortfolioSummary_RejectsAStressRateOutsidePlausibleRange(decimal stressRate)
    {
        var response = await _client.GetAsync($"/api/v1/portfolio/summary?stressRate={stressRate}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Triage_RePricesEveryLoanAtTheProFormaRate()
    {
        var page = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>(
            "/api/v1/loans/triage?pageSize=200&stressRate=9.5");

        page!.Items.Should().OnlyContain(loan => loan.StressedDscr != null);
        page.Items.Should().OnlyContain(loan => loan.StressedAnnualDebtService != null);
        page.Items.Where(loan => loan.InterestRate < 0.095m)
            .Should().OnlyContain(loan => loan.StressedDscr < loan.Dscr);
    }

    [Fact]
    public async Task Triage_FiltersByAssetClass()
    {
        var page = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>(
            "/api/v1/loans/triage?pageSize=200&assetClass=Office");

        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(loan => loan.PropertyType == "Office");
    }

    [Fact]
    public async Task Triage_RejectsUnknownAssetClass()
    {
        var response = await _client.GetAsync("/api/v1/loans/triage?assetClass=Spaceport");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
