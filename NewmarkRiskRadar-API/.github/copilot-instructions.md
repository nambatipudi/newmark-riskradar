# Newmark Risk Radar - API Constraints

## Architecture (Strict Clean Architecture / DDD)
- `Newmark.RiskRadar.Domain`: Pure C#. Zero dependencies on EF Core, ASP.NET, or any third-party NuGet packages.
- `Newmark.RiskRadar.Application`: Depends only on Domain. Orchestrates MediatR/handlers and defines Repository interfaces.
- `Newmark.RiskRadar.Infrastructure`: Depends on Application. Implements EF Core with SQLite.
- `Newmark.RiskRadar.Api`: Depends on Infrastructure and Application. Configures ASP.NET Core controllers.

## Financial & Math Safety
- Never use `float` or `double`. All monetary values, rates, and ratios must use `decimal`.
- Implement explicit guards against division by zero (e.g., when Annual Debt Service is 0 during DSCR calculations).
- Calculate "Months to Maturity" using explicit calendar month diffing, not raw timestamp division.

## Quality & Standards
- Enforce strict nullability (`<Nullable>enable</Nullable>`).
- Use XML documentation (`///`) on all public domain entities, value objects, and domain services.
- Tests must be written in xUnit with FluentAssertions.