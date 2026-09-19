using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newmark.RiskRadar.Application.Interfaces;

namespace Newmark.RiskRadar.Api.Tests;

/// <summary>
/// Boots the real pipeline against a throwaway SQLite file so the seeded book is identical to
/// production without touching the developer's database.
/// </summary>
public sealed class RiskRadarApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Valuation date the whole suite runs against, seeding included.</summary>
    public static readonly DateOnly AsOf = new(2026, 6, 15);

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"riskradar-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting lands in host configuration, which WebApplication.CreateBuilder honours;
        // ConfigureAppConfiguration alone is overridden by the app's own appsettings.json.
        builder.UseSetting("ConnectionStrings:SqliteConnection", $"Data Source={_databasePath}");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new FixedClock(AsOf));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
