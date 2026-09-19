using Microsoft.Extensions.DependencyInjection;
using Newmark.RiskRadar.Application.Queries;

namespace Newmark.RiskRadar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<PortfolioSummaryQuery>();
        services.AddScoped<MaturityWallQuery>();
        services.AddScoped<LoanTriageQuery>();

        return services;
    }
}
