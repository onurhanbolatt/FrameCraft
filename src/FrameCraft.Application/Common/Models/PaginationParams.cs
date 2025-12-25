using System.ComponentModel.DataAnnotations;

namespace FrameCraft.Application.Common.Models;

public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    public string? Sort { get; set; }
    public string SortDirection { get; set; } = "asc";
    public string? Search { get; set; }

    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;

    public bool IsDescending => SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

public class FilterParams : PaginationParams
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsActive { get; set; }
}
