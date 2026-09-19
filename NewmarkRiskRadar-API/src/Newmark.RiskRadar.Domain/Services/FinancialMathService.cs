using Newmark.RiskRadar.Domain.Exceptions;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Domain.Services;

/// <summary>A balance paired with the maturity date it rolls on, used for WALT.</summary>
public readonly record struct WeightedMaturity(Money Balance, DateOnly MaturityDate);

/// <summary>A balance paired with the coverage ratio it carries, used for weighted average DSCR.</summary>
public readonly record struct WeightedDscr(Money Balance, Dscr Dscr);

/// <summary>
/// Pure financial math. No I/O, no framework types, no ambient state: every result is a
/// function of its arguments only, which is what makes the rules unit-testable in isolation.
/// </summary>
public static class FinancialMathService
{
    /// <summary>Mean solar year length, the market convention for expressing loan term in years.</summary>
    private const decimal DaysPerYear = 365.25m;

    /// <summary>
    /// Debt Service Coverage Ratio = NOI / annual debt service.
    /// </summary>
    /// <exception cref="InvalidFinancialInputException">Annual debt service is zero or negative.</exception>
    public static Dscr CalculateDscr(Money netOperatingIncome, Money annualDebtService)
    {
        InvalidFinancialInputException.ThrowIfNotPositive(
            annualDebtService.Amount,
            nameof(annualDebtService),
            "Annual debt service");

        return Dscr.FromRatio(netOperatingIncome.Amount / annualDebtService.Amount);
    }

    /// <summary>Debt yield = NOI / outstanding balance.</summary>
    /// <exception cref="InvalidFinancialInputException">Outstanding balance is zero or negative.</exception>
    public static decimal CalculateDebtYield(Money netOperatingIncome, Money outstandingBalance)
    {
        InvalidFinancialInputException.ThrowIfNotPositive(
            outstandingBalance.Amount,
            nameof(outstandingBalance),
            "Outstanding balance");

        return Math.Round(netOperatingIncome.Amount / outstandingBalance.Amount, 4, MidpointRounding.ToEven);
    }

    /// <summary>Loan to value = outstanding balance / appraised value.</summary>
    /// <exception cref="InvalidFinancialInputException">Balance or appraised value is zero or negative.</exception>
    public static decimal CalculateLoanToValue(Money outstandingBalance, Money appraisedValue)
    {
        InvalidFinancialInputException.ThrowIfNotPositive(
            outstandingBalance.Amount,
            nameof(outstandingBalance),
            "Outstanding balance");

        InvalidFinancialInputException.ThrowIfNotPositive(
            appraisedValue.Amount,
            nameof(appraisedValue),
            "Appraised value");

        return Math.Round(outstandingBalance.Amount / appraisedValue.Amount, 4, MidpointRounding.ToEven);
    }

    /// <summary>Years between <paramref name="asOf"/> and maturity. Negative for loans already past due.</summary>
    public static decimal CalculateYearsToMaturity(DateOnly maturityDate, DateOnly asOf) =>
        Math.Round((maturityDate.DayNumber - asOf.DayNumber) / DaysPerYear, 4, MidpointRounding.ToEven);

    /// <summary>
    /// Weighted Average Loan Term: balance weighted years to maturity across the book.
    /// Null when the supplied positions carry no balance to weight by.
    /// </summary>
    public static decimal? CalculateWalt(IEnumerable<WeightedMaturity> positions, DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var weightedTerm = 0m;
        var totalBalance = 0m;

        foreach (var position in positions)
        {
            InvalidFinancialInputException.ThrowIfNegative(position.Balance.Amount, nameof(positions), "Position balance");

            weightedTerm += position.Balance.Amount * CalculateYearsToMaturity(position.MaturityDate, asOf);
            totalBalance += position.Balance.Amount;
        }

        if (totalBalance == 0m)
        {
            return null;
        }

        return Math.Round(weightedTerm / totalBalance, 4, MidpointRounding.ToEven);
    }

    /// <summary>
    /// Balance weighted average DSCR. Positions with an undefined ratio are excluded from both the
    /// numerator and the denominator so they cannot dilute the average.
    /// </summary>
    public static Dscr CalculateWeightedAverageDscr(IEnumerable<WeightedDscr> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);

        var weightedRatio = 0m;
        var totalBalance = 0m;

        foreach (var position in positions)
        {
            InvalidFinancialInputException.ThrowIfNegative(position.Balance.Amount, nameof(positions), "Position balance");

            if (!position.Dscr.IsDefined || position.Balance.IsZero)
            {
                continue;
            }

            weightedRatio += position.Balance.Amount * position.Dscr.Value!.Value;
            totalBalance += position.Balance.Amount;
        }

        return totalBalance == 0m ? Dscr.Undefined : Dscr.FromRatio(weightedRatio / totalBalance);
    }

    /// <summary>
    /// Level annual debt service for a fully amortising loan, or interest only when
    /// <paramref name="amortizationYears"/> is zero.
    /// </summary>
    public static Money CalculateAnnualDebtService(Money principal, decimal annualInterestRate, int amortizationYears)
    {
        InvalidFinancialInputException.ThrowIfNegative(principal.Amount, nameof(principal), "Principal");
        InvalidFinancialInputException.ThrowIfNegative(annualInterestRate, nameof(annualInterestRate), "Interest rate");

        if (amortizationYears < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amortizationYears), "Amortization term cannot be negative.");
        }

        if (amortizationYears == 0 || annualInterestRate == 0m)
        {
            return principal * annualInterestRate;
        }

        var monthlyRate = annualInterestRate / 12m;
        var growth = Power(1m + monthlyRate, amortizationYears * 12);
        var monthlyPayment = principal.Amount * monthlyRate * growth / (growth - 1m);

        return Money.FromDecimal(monthlyPayment * 12m);
    }

    /// <summary>Integer exponentiation kept in decimal so no step of the payment math touches binary floating point.</summary>
    private static decimal Power(decimal value, int exponent)
    {
        var result = 1m;

        for (var step = 0; step < exponent; step++)
        {
            result *= value;
        }

        return result;
    }
}
