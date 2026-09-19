using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newmark.RiskRadar.Application.Exceptions;
using Newmark.RiskRadar.Application.Interfaces;
using Newmark.RiskRadar.Infrastructure.Persistence;

namespace Newmark.RiskRadar.Infrastructure.Seed;

/// <summary>
/// Swaps the servicing book for a newly generated tape inside a single transaction, so a failed
/// ingest leaves the previous book in place rather than an empty table.
/// </summary>
public sealed class LoanTapeSynchronizer(
    RiskRadarDbContext dbContext,
    IClock clock,
    ILogger<LoanTapeSynchronizer> logger) : ILoanTapeSynchronizer
{
    /// <summary>SQLite reports a contended file as BUSY (5) or LOCKED (6).</summary>
    private static readonly int[] LockErrorCodes = [5, 6];

    public async Task<int> ResyncAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        // A fresh seed each run is what makes the simulated drop differ from the last one.
        var randomSeed = Environment.TickCount;
        var loans = SyntheticDataSeeder.Generate(clock.Today, randomSeed);

        try
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);

            var removed = await dbContext.Loans.ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);

            await dbContext.Loans.AddRangeAsync(loans, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation(
                "Servicing tape resynced: replaced {RemovedCount} loans with {InsertedCount}.",
                removed,
                loans.Count);

            return loans.Count;
        }
        catch (Exception exception) when (IsDatabaseLocked(exception))
        {
            logger.LogWarning(exception, "Servicing tape sync skipped: the book is locked by another writer.");

            throw new TapeSyncConflictException(
                "The servicing book is currently locked by another sync. Try again in a moment.",
                exception);
        }
        finally
        {
            // ExecuteDeleteAsync bypasses the change tracker, so drop any stale tracked entities.
            dbContext.ChangeTracker.Clear();
        }
    }

    private static bool IsDatabaseLocked(Exception exception) => exception switch
    {
        SqliteException sqlite => LockErrorCodes.Contains(sqlite.SqliteErrorCode),
        DbUpdateException { InnerException: SqliteException inner } => LockErrorCodes.Contains(inner.SqliteErrorCode),
        _ => false
    };
}
