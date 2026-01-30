using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cloudativ.Assessment.Infrastructure.Data.Repositories;

public class GovernanceAnalysisRepository : Repository<GovernanceAnalysis>, IGovernanceAnalysisRepository
{
    public GovernanceAnalysisRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<GovernanceAnalysis>> GetByAssessmentRunIdAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.AssessmentRunId == assessmentRunId)
            .OrderBy(g => g.Standard)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GovernanceAnalysis>> GetByTenantIdAsync(
        Guid tenantId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.TenantId == tenantId)
            .OrderByDescending(g => g.AnalyzedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<GovernanceAnalysis?> GetByRunAndStandardAsync(
        Guid assessmentRunId,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.AssessmentRunId == assessmentRunId && g.Standard == standard, cancellationToken);
    }

    public async Task<GovernanceAnalysis?> GetLatestByTenantAndStandardAsync(
        Guid tenantId,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.TenantId == tenantId && g.Standard == standard)
            .OrderByDescending(g => g.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
