using Newmark.RiskRadar.Application.Interfaces;

namespace Newmark.RiskRadar.Application.Tests.TestDoubles;

/// <summary>
/// Pins "today" so that maturity maths is a function of the fixture alone. Without this the
/// suite would quietly change behaviour as the wall clock moves.
/// </summary>
public sealed class FixedClock(DateOnly today) : IClock
{
    public DateOnly Today { get; } = today;
}
