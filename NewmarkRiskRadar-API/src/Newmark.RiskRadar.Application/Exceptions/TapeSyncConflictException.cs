namespace Newmark.RiskRadar.Application.Exceptions;

/// <summary>
/// The servicing book could not be rewritten because another writer holds the lock. Callers can
/// retry; the previous tape is left intact.
/// </summary>
public sealed class TapeSyncConflictException(string message, Exception innerException)
    : Exception(message, innerException);
