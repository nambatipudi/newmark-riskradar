using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Infrastructure.Persistence.Converters;

/// <summary>Flattens the <see cref="Money"/> value object to a single decimal column.</summary>
public sealed class MoneyConverter() : ValueConverter<Money, decimal>(
    money => money.Amount,
    amount => Money.FromDecimal(amount));
