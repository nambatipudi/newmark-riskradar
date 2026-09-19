namespace Newmark.RiskRadar.Domain.ValueObjects;

/// <summary>
/// Currency amount held as a decimal and normalised to cents on every construction
/// so that repeated arithmetic cannot accumulate sub-cent drift.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public const int Scale = 2;

    private Money(decimal amount) => Amount = Math.Round(amount, Scale, MidpointRounding.ToEven);

    public decimal Amount { get; }

    public static Money Zero => default;

    public bool IsZero => Amount == 0m;

    public bool IsPositive => Amount > 0m;

    public bool IsNegative => Amount < 0m;

    public static Money FromDecimal(decimal amount) => new(amount);

    public static Money Sum(IEnumerable<Money> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);

        var total = 0m;
        foreach (var amount in amounts)
        {
            total += amount.Amount;
        }

        return new Money(total);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);

    public static Money operator *(Money value, decimal factor) => new(value.Amount * factor);

    public static Money operator *(decimal factor, Money value) => new(value.Amount * factor);

    public static Money operator /(Money value, decimal divisor) => divisor == 0m
        ? throw new DivideByZeroException("Money cannot be divided by zero.")
        : new Money(value.Amount / divisor);

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
