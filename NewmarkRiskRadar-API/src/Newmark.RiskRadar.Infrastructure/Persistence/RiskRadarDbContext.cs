using Microsoft.EntityFrameworkCore;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Infrastructure.Persistence.Configurations;

namespace Newmark.RiskRadar.Infrastructure.Persistence;

public sealed class RiskRadarDbContext(DbContextOptions<RiskRadarDbContext> options) : DbContext(options)
{
    public DbSet<CommercialLoan> Loans => Set<CommercialLoan>();

    public override int SaveChanges()
    {
        StampDerivedCoverage();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        StampDerivedCoverage();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommercialLoanConfiguration).Assembly);

    /// <summary>Refreshes the DSCR column from the domain calculation so the column cannot drift from the math.</summary>
    private void StampDerivedCoverage()
    {
        foreach (var entry in ChangeTracker.Entries<CommercialLoan>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Property(CommercialLoanConfiguration.DscrProperty).CurrentValue = entry.Entity.Dscr;
            }
        }
    }
}
