using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;

namespace Newmark.RiskRadar.Application.Tests.TestDoubles;

/// <summary>
/// Builds loans with explicit, readable characteristics so each query test states the book it
/// expects rather than depending on the synthetic seeder.
/// </summary>
/// <remarks>
/// For an interest-only loan, debt yield works out to noteRate * targetDscr. Thin-coverage
/// fixtures therefore need a high enough note rate to stay clear of the 6% debt yield floor,
/// otherwise they classify as Critical for a reason the test did not intend.
/// </remarks>
public static class LoanBuilder
{
    private static int _sequence;

    public static CommercialLoan Loan(
        string loanNumber = "NMK-TEST-0001",
        decimal balance = 10_000_000m,
        decimal targetDscr = 2.00m,
        PropertyType propertyType = PropertyType.Office,
        string market = "New York, NY",
        string borrowerName = "Test Borrower",
        string propertyName = "Test Property",
        DateOnly? maturityDate = null,
        decimal noteRate = 0.05m,
        int amortizationYears = 0,
        decimal loanToValue = 0.50m)
    {
        var maturity = maturityDate ?? new DateOnly(2032, 6, 15);
        var money = Money.FromDecimal(balance);
        var debtService = FinancialMathService.CalculateAnnualDebtService(money, noteRate, amortizationYears);

        return CommercialLoan.Create(
            id: NextId(),
            loanNumber: loanNumber,
            borrowerName: borrowerName,
            propertyName: propertyName,
            market: market,
            propertyType: propertyType,
            originationDate: maturity.AddYears(-10),
            maturityDate: maturity,
            originalBalance: money,
            outstandingBalance: money,
            netOperatingIncome: debtService * targetDscr,
            annualDebtService: debtService,
            appraisedValue: money / loanToValue,
            interestRate: noteRate,
            amortizationYears: amortizationYears);
    }

    private static Guid NextId()
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), Interlocked.Increment(ref _sequence));
        return new Guid(bytes);
    }
}
