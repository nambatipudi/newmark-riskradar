using Newmark.RiskRadar.Domain.Exceptions;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Domain.Entities;

/// <summary>
/// Aggregate root for a single commercial real estate loan in the servicing book.
/// All ratios are derived on demand so stored data can never disagree with the math.
/// </summary>
public sealed class CommercialLoan
{
    private CommercialLoan()
    {
        LoanNumber = string.Empty;
        BorrowerName = string.Empty;
        PropertyName = string.Empty;
        Market = string.Empty;
    }

    public Guid Id { get; private set; }

    public string LoanNumber { get; private set; }

    public string BorrowerName { get; private set; }

    public string PropertyName { get; private set; }

    public string Market { get; private set; }

    public PropertyType PropertyType { get; private set; }

    public DateOnly OriginationDate { get; private set; }

    public DateOnly MaturityDate { get; private set; }

    public Money OriginalBalance { get; private set; }

    public Money OutstandingBalance { get; private set; }

    /// <summary>Trailing twelve month net operating income for the collateral.</summary>
    public Money NetOperatingIncome { get; private set; }

    public Money AnnualDebtService { get; private set; }

    public Money AppraisedValue { get; private set; }

    /// <summary>Note rate as a decimal fraction, e.g. 0.0625 for 6.25%.</summary>
    public decimal InterestRate { get; private set; }

    /// <summary>Amortization schedule in years. Zero means interest only.</summary>
    public int AmortizationYears { get; private set; }

    public Dscr Dscr => FinancialMathService.CalculateDscr(NetOperatingIncome, AnnualDebtService);

    public decimal DebtYield => FinancialMathService.CalculateDebtYield(NetOperatingIncome, OutstandingBalance);

    public decimal LoanToValue => FinancialMathService.CalculateLoanToValue(OutstandingBalance, AppraisedValue);

    /// <summary>Debt service this loan would carry if it were re-priced at <paramref name="annualInterestRate"/>.</summary>
    public Money DebtServiceAt(decimal annualInterestRate)
    {
        InvalidFinancialInputException.ThrowIfNotPositive(
            annualInterestRate,
            nameof(annualInterestRate),
            "Pro-forma interest rate");

        return FinancialMathService.CalculateAnnualDebtService(
            OutstandingBalance,
            annualInterestRate,
            AmortizationYears);
    }

    /// <summary>Coverage this loan would post at a pro-forma take-out rate, holding NOI flat.</summary>
    public Dscr DscrAt(decimal annualInterestRate) =>
        FinancialMathService.CalculateDscr(NetOperatingIncome, DebtServiceAt(annualInterestRate));

    public static CommercialLoan Create(
        Guid id,
        string loanNumber,
        string borrowerName,
        string propertyName,
        string market,
        PropertyType propertyType,
        DateOnly originationDate,
        DateOnly maturityDate,
        Money originalBalance,
        Money outstandingBalance,
        Money netOperatingIncome,
        Money annualDebtService,
        Money appraisedValue,
        decimal interestRate,
        int amortizationYears)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Loan id is required.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(loanNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(borrowerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(market);

        if (maturityDate <= originationDate)
        {
            throw new ArgumentOutOfRangeException(nameof(maturityDate), "Maturity must fall after origination.");
        }

        // Every denominator behind DSCR, debt yield and LTV has to be positive for the ratios to exist.
        InvalidFinancialInputException.ThrowIfNotPositive(originalBalance.Amount, nameof(originalBalance), "Original balance");
        InvalidFinancialInputException.ThrowIfNotPositive(outstandingBalance.Amount, nameof(outstandingBalance), "Outstanding balance");
        InvalidFinancialInputException.ThrowIfNotPositive(annualDebtService.Amount, nameof(annualDebtService), "Annual debt service");
        InvalidFinancialInputException.ThrowIfNotPositive(appraisedValue.Amount, nameof(appraisedValue), "Appraised value");
        InvalidFinancialInputException.ThrowIfNegative(interestRate, nameof(interestRate), "Interest rate");

        if (amortizationYears < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amortizationYears), "Amortization term cannot be negative.");
        }

        return new CommercialLoan
        {
            Id = id,
            LoanNumber = loanNumber.Trim(),
            BorrowerName = borrowerName.Trim(),
            PropertyName = propertyName.Trim(),
            Market = market.Trim(),
            PropertyType = propertyType,
            OriginationDate = originationDate,
            MaturityDate = maturityDate,
            OriginalBalance = originalBalance,
            OutstandingBalance = outstandingBalance,
            NetOperatingIncome = netOperatingIncome,
            AnnualDebtService = annualDebtService,
            AppraisedValue = appraisedValue,
            InterestRate = interestRate,
            AmortizationYears = amortizationYears
        };
    }

    /// <summary>Whole months until maturity. Negative once the loan has already rolled past its maturity date.</summary>
    public int MonthsToMaturity(DateOnly asOf)
    {
        var months = ((MaturityDate.Year - asOf.Year) * 12) + MaturityDate.Month - asOf.Month;
        if (MaturityDate.Day < asOf.Day)
        {
            months--;
        }

        return months;
    }

    public decimal YearsToMaturity(DateOnly asOf) => FinancialMathService.CalculateYearsToMaturity(MaturityDate, asOf);

    public bool MaturesWithin(int months, DateOnly asOf) => MonthsToMaturity(asOf) <= months;
}
