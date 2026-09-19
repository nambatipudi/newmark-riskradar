using Microsoft.EntityFrameworkCore;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Domain.Entities;
using Newmark.RiskRadar.Infrastructure.Persistence;

namespace Newmark.RiskRadar.Infrastructure.Repositories;

/// <summary>
/// Read-only SQLite access to the servicing book. Queries are untracked because the aggregate is
/// immutable from the outside: every ratio is recomputed by the domain on read.
/// </summary>
public sealed class SqliteLoanRepository(RiskRadarDbContext dbContext) : ILoanRepository
{
    public async Task<IReadOnlyList<CommercialLoan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Loans
            .AsNoTracking()
            .OrderBy(loan => loan.MaturityDate)
            .ThenBy(loan => loan.LoanNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<CommercialLoan?> GetByLoanNumberAsync(
        string loanNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loanNumber);

        return await dbContext.Loans
            .AsNoTracking()
            .FirstOrDefaultAsync(loan => loan.LoanNumber == loanNumber, cancellationToken)
            .ConfigureAwait(false);
    }
}
