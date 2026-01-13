using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Infrastructure.Services;
using Hangfire;

namespace Cloudativ.Assessment.Web.Services;

public class AssessmentHubService
{
    private readonly IAssessmentEngine _assessmentEngine;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AssessmentHubService> _logger;

    private readonly Dictionary<Guid, AssessmentProgressDto> _progressCache = new();

    public event EventHandler<AssessmentProgressDto>? OnProgressUpdated;

    public AssessmentHubService(
        IAssessmentEngine assessmentEngine,
        IBackgroundJobClient backgroundJobClient,
        ILogger<AssessmentHubService> logger)
    {
        _assessmentEngine = assessmentEngine;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;

        _assessmentEngine.ProgressChanged += OnEngineProgressChanged;
    }

    public async Task<Guid> StartAssessmentAsync(StartAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        var runId = await _assessmentEngine.StartAssessmentAsync(
            request.TenantId,
            request.DomainsToAssess,
            request.InitiatedBy,
            cancellationToken);

        // Initialize progress
        _progressCache[runId] = new AssessmentProgressDto
        {
            RunId = runId,
            Status = AssessmentStatus.Pending,
            ProgressPercentage = 0,
            CurrentOperation = "Initializing...",
            PendingDomains = (request.DomainsToAssess ?? Enum.GetValues<AssessmentDomain>().ToList())
                .Select(d => d.GetDisplayName())
                .ToList()
        };

        // Queue the assessment job
        _backgroundJobClient.Enqueue<IAssessmentEngine>(
            engine => engine.ExecuteAssessmentAsync(runId, CancellationToken.None));

        _logger.LogInformation("Queued assessment job for run {RunId}", runId);

        return runId;
    }

    public AssessmentProgressDto? GetProgress(Guid runId)
    {
        return _progressCache.GetValueOrDefault(runId);
    }

    private void OnEngineProgressChanged(object? sender, AssessmentProgressEventArgs e)
    {
        var progress = new AssessmentProgressDto
        {
            RunId = e.RunId,
            Status = e.ProgressPercentage >= 100 ? AssessmentStatus.Completed : AssessmentStatus.Running,
            ProgressPercentage = e.ProgressPercentage,
            CurrentOperation = e.CurrentOperation,
            CurrentDomain = e.CurrentDomain?.GetDisplayName()
        };

        _progressCache[e.RunId] = progress;
        OnProgressUpdated?.Invoke(this, progress);
    }
}
