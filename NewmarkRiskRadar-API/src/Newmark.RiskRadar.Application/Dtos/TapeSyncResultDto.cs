namespace Newmark.RiskRadar.Application.Dtos;

public sealed record TapeSyncResultDto
{
    public required string Message { get; init; }

    public required int RecordsInserted { get; init; }

    public required DateTimeOffset SyncedAtUtc { get; init; }
}
