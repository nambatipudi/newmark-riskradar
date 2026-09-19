namespace Newmark.RiskRadar.Application.Interfaces;

/// <summary>Supplies the valuation date so every calculation in a request shares one "today".</summary>
public interface IClock
{
    DateOnly Today { get; }
}
