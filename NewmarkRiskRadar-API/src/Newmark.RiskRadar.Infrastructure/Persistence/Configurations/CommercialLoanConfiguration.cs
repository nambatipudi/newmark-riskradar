using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Domain.ValueObjects;
using Newmark.RiskRadar.Infrastructure.Persistence.Converters;

namespace Newmark.RiskRadar.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="CommercialLoan"/> aggregate onto a single flat table. The entity keeps its
/// private setters and derived ratios; persistence adapts to those boundaries rather than the
/// other way around.
/// </summary>
public sealed class CommercialLoanConfiguration : IEntityTypeConfiguration<CommercialLoan>
{
    /// <summary>Shadow property holding the persisted DSCR projection, written on every save.</summary>
    public const string DscrProperty = "DscrRatio";

    public void Configure(EntityTypeBuilder<CommercialLoan> builder)
    {
        builder.ToTable("Loans");

        builder.HasKey(loan => loan.Id);
        builder.Property(loan => loan.Id).ValueGeneratedNever();

        builder.HasIndex(loan => loan.LoanNumber).IsUnique();
        builder.HasIndex(loan => loan.MaturityDate);

        builder.Property(loan => loan.LoanNumber).HasMaxLength(32).IsRequired();
        builder.Property(loan => loan.BorrowerName).HasMaxLength(200).IsRequired();
        builder.Property(loan => loan.PropertyName).HasMaxLength(200).IsRequired();
        builder.Property(loan => loan.Market).HasMaxLength(120).IsRequired();

        builder.Property(loan => loan.PropertyType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(loan => loan.OriginationDate).IsRequired();
        builder.Property(loan => loan.MaturityDate).IsRequired();
        builder.Property(loan => loan.InterestRate).HasPrecision(9, 6).IsRequired();
        builder.Property(loan => loan.AmortizationYears).IsRequired();

        ConfigureMoney(builder, loan => loan.OriginalBalance);
        ConfigureMoney(builder, loan => loan.OutstandingBalance);
        ConfigureMoney(builder, loan => loan.NetOperatingIncome);
        ConfigureMoney(builder, loan => loan.AnnualDebtService);
        ConfigureMoney(builder, loan => loan.AppraisedValue);

        // Coverage is recomputed from the stored amounts on read, so the entity property stays out
        // of the model; the scalar column below exists purely so SQL can filter and sort on DSCR.
        builder.Ignore(loan => loan.Dscr);
        builder.Ignore(loan => loan.DebtYield);
        builder.Ignore(loan => loan.LoanToValue);

        builder.Property<Dscr>(DscrProperty)
            .HasColumnName("DscrBasisPoints")
            .HasConversion(new DscrConverter())
            .IsRequired();

        builder.HasIndex(DscrProperty);
    }

    private static void ConfigureMoney(
        EntityTypeBuilder<CommercialLoan> builder,
        System.Linq.Expressions.Expression<Func<CommercialLoan, Money>> property) =>
        builder.Property(property)
            .HasConversion(new MoneyConverter())
            .HasPrecision(18, 2)
            .IsRequired();
}
