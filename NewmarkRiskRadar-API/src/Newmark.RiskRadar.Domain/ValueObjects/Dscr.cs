namespace Newmark.RiskRadar.Domain.ValueObjects;

/// <summary>
/// Debt Service Coverage Ratio. Undefined when the loan carries no scheduled debt service,
/// which keeps a divide-by-zero out of the calculation pipeline instead of surfacing it as a crash.
/// </summary>
public readonly record struct Dscr : IComparable<Dscr>
{
    public const int Scale = 4;

    private Dscr(decimal? value) => Value = value.HasValue
        ? Math.Round(value.Value, Scale, MidpointRounding.ToEven)
        : null;

    public decimal? Value { get; }

    public bool IsDefined => Value.HasValue;

    public static Dscr Undefined => default;

    public static Dscr FromRatio(decimal ratio) => new(ratio);

    public decimal ValueOr(decimal fallback) => Value ?? fallback;

    public bool IsBelow(decimal threshold) => Value.HasValue && Value.Value < threshold;

    public bool IsAtOrAbove(decimal threshold) => Value.HasValue && Value.Value >= threshold;

    /// <summary>Undefined ratios sort below every defined ratio.</summary>
    public int CompareTo(Dscr other) => (Value, other.Value) switch
    {
        (null, null) => 0,
        (null, _) => -1,
        (_, null) => 1,
        var (left, right) => left!.Value.CompareTo(right!.Value)
    };

    public override string ToString() => Value?.ToString("0.00##", System.Globalization.CultureInfo.InvariantCulture) ?? "n/a";
}
