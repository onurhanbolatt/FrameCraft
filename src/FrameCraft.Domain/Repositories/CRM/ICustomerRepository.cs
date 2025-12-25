using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.Common;

namespace FrameCraft.Domain.Repositories.CRM;

/// <summary>
/// Customer repository interface
/// Customer'a özel query'ler buraya
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<List<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sayfalanmış ve filtrelenmiş müşteri listesi
    /// </summary>
    Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(
        int skip,
        int take,
        string? search = null,
        bool? isActive = null,
        string sortBy = "name",
        string sortOrder = "asc",
        CancellationToken cancellationToken = default);
}
