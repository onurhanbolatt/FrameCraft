namespace FrameCraft.Application.Common.Contracts;

/// <summary>
/// API Contract versiyonları
/// Frontend ile uyumluluğu korumak için
/// </summary>
public static class ApiContracts
{
    public const string Version = "v1";
    public const string ContentType = "application/json";

    /// <summary>
    /// Standart header isimleri
    /// </summary>
    public static class Headers
    {
        public const string TenantId = "X-Tenant-Id";
        public const string CorrelationId = "X-Correlation-Id";
        public const string ApiVersion = "X-Api-Version";
    }

    /// <summary>
    /// Standart query parameter isimleri
    /// </summary>
    public static class QueryParams
    {
        public const string Page = "page";
        public const string PageSize = "pageSize";
        public const string Sort = "sort";
        public const string SortDirection = "sortDirection";
        public const string Search = "search";
        public const string DateFrom = "dateFrom";
        public const string DateTo = "dateTo";
        public const string IsActive = "isActive";
    }
}