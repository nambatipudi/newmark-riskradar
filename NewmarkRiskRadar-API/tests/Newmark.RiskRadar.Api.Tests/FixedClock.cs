using Newmark.RiskRadar.Application.Interfaces;

namespace Newmark.RiskRadar.Api.Tests;

/// <summary>Pins the valuation date so seeded maturities and assertions never drift with the wall clock.</summary>
public sealed class FixedClock(DateOnly today) : IClock
{
    public DateOnly Today { get; } = today;
}
