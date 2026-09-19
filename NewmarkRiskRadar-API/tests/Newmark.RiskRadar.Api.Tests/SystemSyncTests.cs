using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Newmark.RiskRadar.Application.Dtos;
using Newmark.RiskRadar.Infrastructure.Seed;
using Xunit;

namespace Newmark.RiskRadar.Api.Tests;

/// <summary>
/// Owns its own factory, and therefore its own database file, because these tests rewrite the book.
/// </summary>
public class SystemSyncTests(RiskRadarApiFactory factory) : IClassFixture<RiskRadarApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Sync_ReplacesTheBookAndReportsTheInsertedCount()
    {
        var response = await _client.PostAsync("/api/v1/system/sync", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TapeSyncResultDto>();

        result.Should().NotBeNull();
        result!.Message.Should().Be("Servicing tape successfully synced.");
        result.RecordsInserted.Should().Be(SyntheticDataSeeder.LoanCount);
    }

    [Fact]
    public async Task Sync_LeavesExactlyOneTapeBehind()
    {
        await _client.PostAsync("/api/v1/system/sync", content: null);
        await _client.PostAsync("/api/v1/system/sync", content: null);

        var summary = await _client.GetFromJsonAsync<PortfolioSummaryDto>("/api/v1/portfolio/summary");

        summary!.LoanCount.Should().Be(SyntheticDataSeeder.LoanCount);
    }

    [Fact]
    public async Task Sync_ProducesADifferentTapeThanTheLastOne()
    {
        var before = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>(
            "/api/v1/loans/triage?pageSize=200");

        await _client.PostAsync("/api/v1/system/sync", content: null);

        var after = await _client.GetFromJsonAsync<PagedResultDto<LoanSummaryDto>>(
            "/api/v1/loans/triage?pageSize=200");

        after!.Items.Should().HaveCount(before!.Items.Count);
        after.Items.Select(loan => loan.Id).Should().NotEqual(before.Items.Select(loan => loan.Id));
    }
}
