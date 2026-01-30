using System.Linq.Expressions;
using Cloudativ.Assessment.Domain.Entities;

namespace Cloudativ.Assessment.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> Query();
}

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByDomainAsync(string domain, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByAzureTenantIdAsync(Guid azureTenantId, CancellationToken cancellationToken = default);
    Task<Tenant?> GetWithSettingsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IAssessmentRunRepository : IRepository<AssessmentRun>
{
    Task<AssessmentRun?> GetLatestByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentRun>> GetAllAsync(int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentRun>> GetByTenantAsync(Guid tenantId, int take = 10, CancellationToken cancellationToken = default);
    Task<AssessmentRun?> GetWithFindingsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IFindingRepository : IRepository<Finding>
{
    Task<IReadOnlyList<Finding>> GetByRunIdAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Finding>> GetByDomainAsync(Guid runId, Enums.AssessmentDomain domain, CancellationToken cancellationToken = default);
}

public interface IAppUserRepository : IRepository<AppUser>
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<AppUser?> GetWithTenantAccessAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUser?> GetWithAllAccessAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
    Task<Subscription?> GetByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetExpiredSubscriptionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Subscription>> GetSubscriptionsNeedingResetAsync(DateTime periodStart, CancellationToken cancellationToken = default);
}

public interface IGovernanceAnalysisRepository : IRepository<GovernanceAnalysis>
{
    Task<IReadOnlyList<GovernanceAnalysis>> GetByAssessmentRunIdAsync(Guid assessmentRunId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GovernanceAnalysis>> GetByTenantIdAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
    Task<GovernanceAnalysis?> GetByRunAndStandardAsync(Guid assessmentRunId, Enums.ComplianceStandard standard, CancellationToken cancellationToken = default);
    Task<GovernanceAnalysis?> GetLatestByTenantAndStandardAsync(Guid tenantId, Enums.ComplianceStandard standard, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    ITenantRepository Tenants { get; }
    IAssessmentRunRepository AssessmentRuns { get; }
    IFindingRepository Findings { get; }
    IRepository<RawSnapshot> RawSnapshots { get; }
    IRepository<TenantSettings> TenantSettings { get; }
    IAppUserRepository AppUsers { get; }
    IRepository<TenantUserAccess> TenantUserAccess { get; }
    IRepository<UserDomainAccess> UserDomainAccess { get; }
    ISubscriptionRepository Subscriptions { get; }
    IGovernanceAnalysisRepository GovernanceAnalyses { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
