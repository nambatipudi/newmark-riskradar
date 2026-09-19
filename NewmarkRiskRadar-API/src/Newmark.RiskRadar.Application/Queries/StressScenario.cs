using Newmark.RiskRadar.Domain.Exceptions;

namespace Newmark.RiskRadar.Application.Queries;

/// <summary>
/// A refinancing stress test: re-prices every loan at a pro-forma take-out rate while holding NOI
/// flat, which is how the desk judges whether a maturing loan can actually clear a refinance.
/// </summary>
public sealed class StressScenario
{
    /// <summary>Bounds that keep a bookmarked scenario inside plausible CRE take-out territory.</summary>
    public const decimal MinimumRate = 0.01m;
    public const decimal MaximumRate = 0.25m;

    private StressScenario(decimal? interestRate) => InterestRate = interestRate;

    /// <summary>Pro-forma annual rate as a decimal fraction, or null to use in-place note rates.</summary>
    public decimal? InterestRate { get; }

    public bool IsApplied => InterestRate is not null;

    public static StressScenario InPlace { get; } = new(null);

    /// <summary>Builds a scenario from a rate expressed as a percentage, e.g. 8.5 for 8.5%.</summary>
    public static StressScenario FromPercent(decimal? percent)
    {
        if (percent is null)
        {
            return InPlace;
        }

        var rate = percent.Value / 100m;

        if (rate < MinimumRate || rate > MaximumRate)
        {
            throw new InvalidFinancialInputException(
                nameof(percent),
                $"Stress rate must be between {MinimumRate:P0} and {MaximumRate:P0}, but was {percent.Value:0.##}%.");
        }

        return new StressScenario(rate);
    }
}
