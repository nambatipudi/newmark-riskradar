using Newmark.RiskRadar.Api.Middleware;
using Newmark.RiskRadar.Application;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Infrastructure;
using Newmark.RiskRadar.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

const string UiCorsPolicy = "RiskRadarUi";

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options => options.AddPolicy(UiCorsPolicy, policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new()
{
    Title = "Newmark RiskRadar API",
    Version = "v1",
    Description = "Commercial real estate servicing book analytics and loan triage."
}));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Newmark RiskRadar API v1"));
}

app.UseCors(UiCorsPolicy);
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).ExcludeFromDescription();

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<SyntheticDataSeeder>();
    var clock = scope.ServiceProvider.GetRequiredService<IClock>();
    await seeder.SeedAsync(clock.Today);
}

app.Run();

/// <summary>Exposed so the integration test host can boot the real pipeline.</summary>
public partial class Program;
