using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cloudativ.Assessment.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    private ITenantRepository? _tenants;
    private IAssessmentRunRepository? _assessmentRuns;
    private IFindingRepository? _findings;
    private IRepository<RawSnapshot>? _rawSnapshots;
    private IRepository<TenantSettings>? _tenantSettings;
    private IAppUserRepository? _appUsers;
    private IRepository<TenantUserAccess>? _tenantUserAccess;
    private IRepository<UserDomainAccess>? _userDomainAccess;
    private ISubscriptionRepository? _subscriptions;
    private IGovernanceAnalysisRepository? _governanceAnalyses;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ITenantRepository Tenants => _tenants ??= new TenantRepository(_context);
    public IAssessmentRunRepository AssessmentRuns => _assessmentRuns ??= new AssessmentRunRepository(_context);
    public IFindingRepository Findings => _findings ??= new FindingRepository(_context);
    public IRepository<RawSnapshot> RawSnapshots => _rawSnapshots ??= new Repository<RawSnapshot>(_context);
    public IRepository<TenantSettings> TenantSettings => _tenantSettings ??= new Repository<TenantSettings>(_context);
    public IAppUserRepository AppUsers => _appUsers ??= new AppUserRepository(_context);
    public IRepository<TenantUserAccess> TenantUserAccess => _tenantUserAccess ??= new Repository<TenantUserAccess>(_context);
    public IRepository<UserDomainAccess> UserDomainAccess => _userDomainAccess ??= new Repository<UserDomainAccess>(_context);
    public ISubscriptionRepository Subscriptions => _subscriptions ??= new SubscriptionRepository(_context);
    public IGovernanceAnalysisRepository GovernanceAnalyses => _governanceAnalyses ??= new GovernanceAnalysisRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
