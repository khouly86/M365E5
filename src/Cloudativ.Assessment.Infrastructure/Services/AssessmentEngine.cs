using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Graph;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public interface IAssessmentEngine
{
    Task<Guid> StartAssessmentAsync(Guid tenantId, List<AssessmentDomain>? domains, string? initiatedBy, CancellationToken cancellationToken = default);
    Task ExecuteAssessmentAsync(Guid runId, CancellationToken cancellationToken = default);
    event EventHandler<AssessmentProgressEventArgs>? ProgressChanged;
}

public class AssessmentProgressEventArgs : EventArgs
{
    public Guid RunId { get; set; }
    public int ProgressPercentage { get; set; }
    public string CurrentOperation { get; set; } = string.Empty;
    public AssessmentDomain? CurrentDomain { get; set; }
}

public class AssessmentEngine : IAssessmentEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IEnumerable<IAssessmentModule> _modules;
    private readonly IScoringService _scoringService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<AssessmentEngine> _logger;

    public event EventHandler<AssessmentProgressEventArgs>? ProgressChanged;

    public AssessmentEngine(
        IUnitOfWork unitOfWork,
        IGraphClientFactory graphClientFactory,
        IEnumerable<IAssessmentModule> modules,
        IScoringService scoringService,
        ISubscriptionService subscriptionService,
        ILogger<AssessmentEngine> logger)
    {
        _unitOfWork = unitOfWork;
        _graphClientFactory = graphClientFactory;
        _modules = modules;
        _scoringService = scoringService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<Guid> StartAssessmentAsync(Guid tenantId, List<AssessmentDomain>? domains, string? initiatedBy, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        // Check subscription limits
        var canRun = await _subscriptionService.CanRunAssessmentAsync(tenantId, cancellationToken);
        if (!canRun)
        {
            var usage = await _subscriptionService.GetUsageAsync(tenantId, cancellationToken);
            var message = usage.LimitReachedMessage ?? "Assessment limit reached. Please upgrade your subscription.";
            throw new InvalidOperationException(message);
        }

        var domainsToAssess = domains ?? Enum.GetValues<AssessmentDomain>().ToList();

        var run = new AssessmentRun
        {
            TenantId = tenantId,
            Status = AssessmentStatus.Pending,
            InitiatedBy = initiatedBy,
            AssessedDomainsJson = JsonSerializer.Serialize(domainsToAssess)
        };

        await _unitOfWork.AssessmentRuns.AddAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created assessment run {RunId} for tenant {TenantId}", run.Id, tenantId);

        return run.Id;
    }

    public async Task ExecuteAssessmentAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Assessment run with ID '{runId}' not found");

        var tenant = await _unitOfWork.Tenants.GetWithSettingsAsync(run.TenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{run.TenantId}' not found");

        run.Status = AssessmentStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        await _unitOfWork.AssessmentRuns.UpdateAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var domainScores = new Dictionary<string, DomainScoreSummary>();
        var allFindings = new List<Finding>();

        try
        {
            // Create Graph client for this tenant
            var graphClient = await _graphClientFactory.CreateClientAsync(tenant, cancellationToken);

            // Determine which domains to assess
            var domainsToAssess = !string.IsNullOrEmpty(run.AssessedDomainsJson)
                ? JsonSerializer.Deserialize<List<AssessmentDomain>>(run.AssessedDomainsJson) ?? Enum.GetValues<AssessmentDomain>().ToList()
                : Enum.GetValues<AssessmentDomain>().ToList();

            var modulesToRun = _modules.Where(m => domainsToAssess.Contains(m.Domain)).ToList();
            var totalModules = modulesToRun.Count;
            var completedModules = 0;

            foreach (var module in modulesToRun)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    run.Status = AssessmentStatus.Cancelled;
                    break;
                }

                try
                {
                    _logger.LogInformation("Running assessment module: {Domain}", module.Domain);
                    ReportProgress(runId, (completedModules * 100) / totalModules, $"Assessing {module.DisplayName}", module.Domain);

                    // Collect data
                    var collectionResult = await module.CollectAsync(graphClient, cancellationToken);

                    // Store raw snapshot
                    var snapshot = new RawSnapshot
                    {
                        AssessmentRunId = runId,
                        Domain = module.Domain,
                        DataType = "CollectionResult",
                        PayloadJson = JsonSerializer.Serialize(collectionResult.RawData),
                        PayloadSizeBytes = JsonSerializer.Serialize(collectionResult.RawData).Length,
                        ErrorMessage = collectionResult.ErrorMessage
                    };
                    await _unitOfWork.RawSnapshots.AddAsync(snapshot, cancellationToken);

                    // Normalize findings
                    var normalizedFindings = module.Normalize(collectionResult);

                    // Score the domain
                    var domainScore = module.Score(normalizedFindings);

                    // Store domain score summary
                    domainScores[module.Domain.ToString()] = new DomainScoreSummary
                    {
                        Domain = module.Domain,
                        DisplayName = module.DisplayName,
                        Score = domainScore.Score,
                        Grade = domainScore.Grade,
                        CriticalCount = domainScore.CriticalCount,
                        HighCount = domainScore.HighCount,
                        MediumCount = domainScore.MediumCount,
                        LowCount = domainScore.LowCount,
                        PassedChecks = domainScore.PassedChecks,
                        FailedChecks = domainScore.FailedChecks,
                        IsAvailable = collectionResult.Success,
                        UnavailableReason = collectionResult.ErrorMessage
                    };

                    // Convert normalized findings to entities
                    foreach (var finding in normalizedFindings.Findings)
                    {
                        allFindings.Add(new Finding
                        {
                            AssessmentRunId = runId,
                            Domain = module.Domain,
                            Severity = finding.Severity,
                            Title = finding.Title,
                            Description = finding.Description,
                            Category = finding.Category,
                            EvidenceJson = finding.Evidence,
                            Remediation = finding.Remediation,
                            References = finding.References,
                            AffectedResources = finding.AffectedResources.Any() ? string.Join(", ", finding.AffectedResources) : null,
                            IsCompliant = finding.IsCompliant,
                            CheckId = finding.CheckId,
                            CheckName = finding.CheckName
                        });
                    }

                    completedModules++;
                    _logger.LogInformation("Completed assessment module: {Domain} with score {Score}", module.Domain, domainScore.Score);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running assessment module: {Domain}", module.Domain);
                    domainScores[module.Domain.ToString()] = new DomainScoreSummary
                    {
                        Domain = module.Domain,
                        DisplayName = module.DisplayName,
                        Score = 0,
                        Grade = "N/A",
                        IsAvailable = false,
                        UnavailableReason = ex.Message
                    };
                    completedModules++;
                }
            }

            // Save all findings
            await _unitOfWork.Findings.AddRangeAsync(allFindings, cancellationToken);

            // Calculate overall score
            var availableScores = domainScores.Values.Where(s => s.IsAvailable).ToList();
            var overallScore = _scoringService.CalculateOverallScore(
                availableScores.Select(s => new DomainScore
                {
                    Domain = s.Domain,
                    Score = s.Score,
                    Grade = s.Grade
                }));

            // Update run with results
            run.Status = cancellationToken.IsCancellationRequested ? AssessmentStatus.Cancelled : AssessmentStatus.Completed;
            run.CompletedAt = DateTime.UtcNow;
            run.OverallScore = overallScore;
            run.SummaryScoresJson = JsonSerializer.Serialize(domainScores);

            await _unitOfWork.AssessmentRuns.UpdateAsync(run, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Increment assessment count on successful completion
            if (run.Status == AssessmentStatus.Completed)
            {
                await _subscriptionService.IncrementAssessmentCountAsync(run.TenantId, cancellationToken);
            }

            ReportProgress(runId, 100, "Assessment completed", null);
            _logger.LogInformation("Assessment run {RunId} completed with overall score {Score}", runId, overallScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assessment run {RunId} failed", runId);
            run.Status = AssessmentStatus.Failed;
            run.CompletedAt = DateTime.UtcNow;
            run.ErrorMessage = ex.Message;
            await _unitOfWork.AssessmentRuns.UpdateAsync(run, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private void ReportProgress(Guid runId, int percentage, string operation, AssessmentDomain? domain)
    {
        ProgressChanged?.Invoke(this, new AssessmentProgressEventArgs
        {
            RunId = runId,
            ProgressPercentage = percentage,
            CurrentOperation = operation,
            CurrentDomain = domain
        });
    }
}
