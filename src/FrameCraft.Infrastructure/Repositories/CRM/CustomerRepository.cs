using FrameCraft.Domain.Entities.CRM;
using FrameCraft.Domain.Repositories.CRM;
using FrameCraft.Infrastructure.Persistence;
using FrameCraft.Infrastructure.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace FrameCraft.Infrastructure.Repositories.CRM;

/// <summary>
/// Customer repository implementation
/// </summary>
public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public override async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public override async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<List<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<(List<Customer> Items, int TotalCount)> GetPagedAsync(
        int skip,
        int take,
        string? search = null,
        bool? isActive = null,
        string sortBy = "name",
        string sortOrder = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        // Filtering - Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Email != null && c.Email.ToLower().Contains(search)) ||
                (c.Phone != null && c.Phone.Contains(search))
            );
        }

        // Filtering - IsActive
        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        // Total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "email" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(c => c.Email)
                : query.OrderBy(c => c.Email),
            "createdat" => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(c => c.CreatedAt)
                : query.OrderBy(c => c.CreatedAt),
            "name" or _ => sortOrder?.ToLower() == "desc"
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name)
        };

        // Pagination
        var items = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}