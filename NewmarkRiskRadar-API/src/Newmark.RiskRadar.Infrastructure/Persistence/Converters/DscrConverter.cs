using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Infrastructure.Persistence.Converters;

/// <summary>
/// Flattens the <see cref="Dscr"/> value object to a single integer column of basis points.
/// SQLite persists decimal as TEXT, which sorts lexicographically, so an exact integer scaling
/// is what keeps SQL comparisons on coverage correct.
/// </summary>
public sealed class DscrConverter() : ValueConverter<Dscr, long>(
    dscr => ToBasisPoints(dscr),
    basisPoints => Dscr.FromRatio(basisPoints / (decimal)BasisPointsPerUnit))
{
    public const int BasisPointsPerUnit = 10_000;

    /// <summary>
    /// A persisted loan always carries scheduled debt service, so an undefined ratio here means the
    /// write path bypassed the domain guards rather than that the column should be null.
    /// </summary>
    private static long ToBasisPoints(Dscr dscr)
    {
        var ratio = dscr.Value
            ?? throw new InvalidOperationException("An undefined DSCR cannot be persisted.");

        return (long)Math.Round(ratio * BasisPointsPerUnit, MidpointRounding.ToEven);
    }
}
