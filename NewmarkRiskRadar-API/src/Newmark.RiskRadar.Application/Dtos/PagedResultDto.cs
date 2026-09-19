namespace Newmark.RiskRadar.Application.Dtos;

public sealed record PagedResultDto<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    public bool HasNextPage => Page < TotalPages;
}
