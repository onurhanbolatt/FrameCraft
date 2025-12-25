using System.Text.Json.Serialization;

namespace FrameCraft.Application.Common.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string Message { get; set; } = "İşlem başarılı";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PaginationMeta? Pagination { get; set; }

    public static ApiResponse<T> SuccessResult(T data, string message = "İşlem başarılı")
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> SuccessResult(T data, PaginationMeta pagination, string message = "İşlem başarılı")
        => new() { Success = true, Data = data, Message = message, Pagination = pagination };
}

public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResult(string message = "İşlem başarılı")
    => new() { Success = true, Data = null, Message = message };
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    // Alias
    public int PageNumber => Page;
    public bool HasPreviousPage => HasPrevious;
    public bool HasNextPage => HasNext;
}
