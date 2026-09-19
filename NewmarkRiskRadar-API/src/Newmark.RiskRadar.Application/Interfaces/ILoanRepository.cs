using Newmark.RiskRadar.Domain.Entities;

namespace Newmark.RiskRadar.Application.Interfaces;

/// <summary>
/// Read model over the servicing book. Risk classification is derived in the domain rather than
/// stored, so filtering by risk happens in the application layer and not in the query.
/// </summary>
public interface ILoanRepository
{
    Task<IReadOnlyList<CommercialLoan>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CommercialLoan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);
}
