using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Infrastructure.Persistence;
using Newmark.RiskRadar.Infrastructure.Repositories;
using Newmark.RiskRadar.Infrastructure.Seed;

namespace Newmark.RiskRadar.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "SqliteConnection";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? "Data Source=riskradar.db";

        EnsureDataDirectoryExists(connectionString);

        services.AddDbContext<RiskRadarDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ILoanRepository, SqliteLoanRepository>();
        services.AddScoped<SyntheticDataSeeder>();
        services.AddScoped<ILoanTapeSynchronizer, LoanTapeSynchronizer>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    /// <summary>SQLite will not create missing directories for the database file, so do it up front.</summary>
    private static void EnsureDataDirectoryExists(string connectionString)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        var directory = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}

internal sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);
}
