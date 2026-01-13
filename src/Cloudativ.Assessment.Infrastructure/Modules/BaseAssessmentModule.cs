using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public abstract class BaseAssessmentModule : IAssessmentModule
{
    protected readonly ILogger _logger;

    protected BaseAssessmentModule(ILogger logger)
    {
        _logger = logger;
    }

    public abstract AssessmentDomain Domain { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract IReadOnlyList<string> RequiredPermissions { get; }

    public abstract Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default);
    public abstract NormalizedFindings Normalize(CollectionResult rawData);
    public abstract DomainScore Score(NormalizedFindings findings);

    public virtual async Task<bool> ValidatePermissionsAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        foreach (var permission in RequiredPermissions)
        {
            if (!await graphClient.HasPermissionAsync(permission, cancellationToken))
            {
                _logger.LogWarning("Missing required permission for {Domain}: {Permission}", Domain, permission);
                return false;
            }
        }
        return true;
    }

    protected NormalizedFinding CreateFinding(
        string checkId,
        string checkName,
        string title,
        string description,
        Severity severity,
        bool isCompliant,
        string? category = null,
        string? evidence = null,
        string? remediation = null,
        string? references = null,
        List<string>? affectedResources = null)
    {
        return new NormalizedFinding
        {
            CheckId = checkId,
            CheckName = checkName,
            Title = title,
            Description = description,
            Severity = severity,
            IsCompliant = isCompliant,
            Category = category,
            Evidence = evidence,
            Remediation = remediation,
            References = references,
            AffectedResources = affectedResources ?? new List<string>()
        };
    }

    protected CollectionResult CreateErrorResult(string errorMessage)
    {
        return new CollectionResult
        {
            Domain = Domain,
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    protected CollectionResult CreateSuccessResult(Dictionary<string, object?> rawData, List<string>? warnings = null)
    {
        return new CollectionResult
        {
            Domain = Domain,
            Success = true,
            RawData = rawData,
            Warnings = warnings ?? new List<string>()
        };
    }
}
