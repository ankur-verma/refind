namespace Cortex.Application.DTOs.Common;

/// <summary>
/// Standardized API response wrapper.
/// All API endpoints return this format for consistency.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Fail(params string[] errors) => new() { Success = false, Errors = errors.ToList() };
}

/// <summary>
/// Paginated API response wrapping a paged list.
/// </summary>
public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public IReadOnlyList<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}
