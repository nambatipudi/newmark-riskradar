namespace Newmark.RiskRadar.Application.Interfaces;

/// <summary>
/// Replaces the servicing book with a freshly generated tape. Stands in for the nightly
/// servicer file drop that a real deployment would ingest.
/// </summary>
public interface ILoanTapeSynchronizer
{
    /// <summary>Returns the number of loans written.</summary>
    /// <exception cref="Exceptions.TapeSyncConflictException">The book is locked by another writer.</exception>
    Task<int> ResyncAsync(CancellationToken cancellationToken = default);
}
