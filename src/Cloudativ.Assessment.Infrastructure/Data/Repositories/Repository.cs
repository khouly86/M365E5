using System.Linq.Expressions;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cloudativ.Assessment.Infrastructure.Data.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        await _dbSet.AddRangeAsync(entityList, cancellationToken);
        return entityList;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }
}

public class TenantRepository : Repository<Tenant>, ITenantRepository
{
    public TenantRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Tenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.Domain == domain, cancellationToken);
    }

    public async Task<Tenant?> GetByAzureTenantIdAsync(Guid azureTenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.AzureTenantId == azureTenantId, cancellationToken);
    }

    public async Task<Tenant?> GetWithSettingsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Settings)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}

public class AssessmentRunRepository : Repository<AssessmentRun>, IAssessmentRunRepository
{
    public AssessmentRunRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AssessmentRun?> GetLatestByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AssessmentRun>> GetByTenantAsync(Guid tenantId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.StartedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssessmentRun?> GetWithFindingsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.Findings)
            .Include(r => r.RawSnapshots)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
}

public class FindingRepository : Repository<Finding>, IFindingRepository
{
    public FindingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Finding>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.AssessmentRunId == runId)
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.Domain)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Finding>> GetByDomainAsync(Guid runId, Domain.Enums.AssessmentDomain domain, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(f => f.AssessmentRunId == runId && f.Domain == domain)
            .OrderBy(f => f.Severity)
            .ToListAsync(cancellationToken);
    }
}

public class AppUserRepository : Repository<AppUser>, IAppUserRepository
{
    public AppUserRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<AppUser?> GetWithTenantAccessAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.TenantAccess)
            .ThenInclude(ta => ta.Tenant)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
