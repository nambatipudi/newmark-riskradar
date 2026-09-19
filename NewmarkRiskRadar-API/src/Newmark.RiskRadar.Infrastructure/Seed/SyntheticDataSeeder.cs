using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.Services;
using Newmark.RiskRadar.Domain.ValueObjects;
using Newmark.RiskRadar.Infrastructure.Persistence;

namespace Newmark.RiskRadar.Infrastructure.Seed;

/// <summary>
/// Generates a fixed institutional CRE book of 50 loans. The random source is seeded with a
/// constant so every environment gets byte-identical data, while maturities are anchored to the
/// current date so the rollover cliff stays in the future as time passes.
/// </summary>
public sealed class SyntheticDataSeeder(RiskRadarDbContext dbContext, ILogger<SyntheticDataSeeder> logger)
{
    public const int LoanCount = 50;
    private const int RandomSeed = 20260918;

    private static readonly string[] Borrowers =
    [
        "Halstead Capital Partners", "Brickfield Holdings", "Rivermark Equities", "Auburn Ridge Trust",
        "Copperline Property Group", "Meridian Stack REIT", "Northbrook Asset Management", "Vantage Point Realty",
        "Stonegate Development", "Kestrel Industrial Partners", "Cordova Urban Ventures", "Lakeshore Yield Fund",
        "Ironwood Commercial Trust", "Pinnacle Harbor Group", "Saltmarsh Hospitality Holdings", "Granite Row Investors",
        "Elmcrest Residential Partners", "Quarry Bend Capital", "Silverline Logistics Trust", "Belmont Arch Holdings",
        "Clearwater Retail Partners", "Foxglove Real Estate", "Tarrant Creek Capital", "Willow Bank Properties",
        "Summit Lane Advisors"
    ];

    private static readonly string[] Markets =
    [
        "New York, NY", "Chicago, IL", "Dallas, TX", "Los Angeles, CA", "Atlanta, GA", "Boston, MA",
        "Seattle, WA", "Denver, CO", "Miami, FL", "Phoenix, AZ", "Charlotte, NC", "Nashville, TN",
        "Philadelphia, PA", "Austin, TX", "Minneapolis, MN"
    ];

    private static readonly Dictionary<PropertyType, string[]> PropertyNames = new()
    {
        [PropertyType.Office] = ["Tower", "Center", "Plaza", "Exchange"],
        [PropertyType.Retail] = ["Marketplace", "Shops", "Commons", "Galleria"],
        [PropertyType.Industrial] = ["Logistics Park", "Distribution Center", "Industrial Campus", "Freight Hub"],
        [PropertyType.Multifamily] = ["Residences", "Flats", "Apartments", "Lofts"],
        [PropertyType.Hospitality] = ["Hotel", "Inn & Suites", "Resort", "Lodge"],
        [PropertyType.MixedUse] = ["District", "Quarter", "Yards", "Junction"]
    };

    private static readonly string[] PropertyPrefixes =
    [
        "Harborview", "Cedar Point", "Union", "Beacon Hill", "Crosstown", "Fairmount", "Highland", "Riverbend",
        "Sterling", "Oakmont", "Lakeside", "Brookline", "Westgate", "Canal Street", "Belvedere", "Ashbury",
        "Trinity", "Kingsport", "Maplewood", "Emerald Bay", "Cobalt", "Prairie", "Sunset", "Northfield", "Gramercy"
    ];

    /// <summary>Creates the schema when missing and seeds it once. Existing data is left untouched.</summary>
    public async Task SeedAsync(DateOnly asOf, CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        if (await dbContext.Loans.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            logger.LogInformation("Servicing book already seeded, skipping synthetic data generation.");
            return;
        }

        var loans = Generate(asOf);
        await dbContext.Loans.AddRangeAsync(loans, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Seeded {LoanCount} synthetic commercial loans as of {AsOf}.", loans.Count, asOf);
    }

    /// <param name="randomSeed">
    /// Overrides the fixed seed. The default keeps first boot byte-identical everywhere, while a
    /// re-sync passes a fresh seed so the simulated tape actually changes.
    /// </param>
    public static IReadOnlyList<CommercialLoan> Generate(DateOnly asOf, int? randomSeed = null)
    {
        var seed = randomSeed ?? RandomSeed;
        var random = new Random(seed);
        var propertyTypes = Enum.GetValues<PropertyType>();
        var loans = new List<CommercialLoan>(LoanCount);

        for (var index = 0; index < LoanCount; index++)
        {
            var propertyType = propertyTypes[index % propertyTypes.Length];
            var market = Markets[random.Next(Markets.Length)];
            var borrower = Borrowers[random.Next(Borrowers.Length)];
            var propertyName = $"{PropertyPrefixes[random.Next(PropertyPrefixes.Length)]} {PropertyNames[propertyType][random.Next(4)]}";

            var termYears = (index % 3) switch { 0 => 5, 1 => 7, _ => 10 };
            var maturity = NextMaturity(random, asOf, index);
            var origination = maturity.AddYears(-termYears);

            var originalBalance = Money.FromDecimal(random.Next(8_000, 95_000) * 1_000m);
            var amortizationYears = index % 5 == 0 ? 0 : 30; // one in five is interest only
            var paydown = amortizationYears == 0 ? 1.00m : 1m - Fraction(random, 800);
            var outstandingBalance = originalBalance * paydown;

            var interestRate = 0.0425m + Fraction(random, 400);
            var annualDebtService = FinancialMathService.CalculateAnnualDebtService(outstandingBalance, interestRate, amortizationYears);

            var targetDscr = NextTargetDscr(random);
            var netOperatingIncome = annualDebtService * targetDscr;

            var loanToValue = targetDscr < 1.00m
                ? 0.80m + Fraction(random, 1_700)
                : 0.45m + Fraction(random, 3_200);
            var appraisedValue = outstandingBalance / loanToValue;

            loans.Add(CommercialLoan.Create(
                id: DeterministicId(seed, index),
                loanNumber: $"NMK-{2020 + (index % 6)}-{1000 + index:0000}",
                borrowerName: borrower,
                propertyName: propertyName,
                market: market,
                propertyType: propertyType,
                originationDate: origination,
                maturityDate: maturity,
                originalBalance: originalBalance,
                outstandingBalance: outstandingBalance,
                netOperatingIncome: netOperatingIncome,
                annualDebtService: annualDebtService,
                appraisedValue: appraisedValue,
                interestRate: interestRate,
                amortizationYears: amortizationYears));
        }

        return loans;
    }

    /// <summary>Spreads maturities over seven forward years while keeping the near-term cliff heavy.</summary>
    private static DateOnly NextMaturity(Random random, DateOnly asOf, int index)
    {
        var yearOffset = (index % 10) switch
        {
            0 => 0,
            1 or 2 => 1,
            3 or 4 => 2,
            5 => 3,
            6 => 4,
            7 => 5,
            8 => 6,
            _ => 7
        };

        var month = random.Next(1, 13);
        var year = asOf.Year + yearOffset;
        var day = random.Next(1, DateTime.DaysInMonth(year, month) + 1);
        var maturity = new DateOnly(year, month, day);

        // Keep the near bucket inside the forward twelve months instead of already past due.
        return maturity <= asOf ? maturity.AddYears(1) : maturity;
    }

    /// <summary>Roughly 20% of the book underwrites below break-even and 30% carries thin coverage.</summary>
    private static decimal NextTargetDscr(Random random) => random.Next(10) switch
    {
        0 or 5 => 0.62m + Fraction(random, 3_600),      // sub break-even
        1 or 6 or 9 => 1.01m + Fraction(random, 2_300), // thin coverage
        _ => 1.26m + Fraction(random, 8_400)
    };

    /// <summary>Random decimal fraction in [0, basisPoints / 10000], kept off binary floating point.</summary>
    private static decimal Fraction(Random random, int basisPoints) => random.Next(basisPoints + 1) / 10_000m;

    private static Guid DeterministicId(int seed, int index)
    {
        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 4), seed);
        BitConverter.TryWriteBytes(bytes.AsSpan(12, 4), index + 1);
        return new Guid(bytes);
    }
}
