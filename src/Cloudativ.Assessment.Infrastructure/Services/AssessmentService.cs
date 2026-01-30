using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class AssessmentService : IAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssessmentEngine _assessmentEngine;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(
        IUnitOfWork unitOfWork,
        IAssessmentEngine assessmentEngine,
        ILogger<AssessmentService> logger)
    {
        _unitOfWork = unitOfWork;
        _assessmentEngine = assessmentEngine;
        _logger = logger;
    }

    public async Task<Guid> StartAssessmentAsync(StartAssessmentRequest request, CancellationToken cancellationToken = default)
    {
        return await _assessmentEngine.StartAssessmentAsync(
            request.TenantId,
            request.DomainsToAssess,
            request.InitiatedBy,
            cancellationToken);
    }

    public async Task<AssessmentRunDto?> GetRunByIdAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            return null;

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(run.TenantId, cancellationToken);

        return MapToDto(run, tenant?.Name ?? "Unknown");
    }

    public async Task<IReadOnlyList<AssessmentRunDto>> GetAllRunsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var runs = await _unitOfWork.AssessmentRuns.GetAllAsync(take, cancellationToken);
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var tenantDict = tenants.ToDictionary(t => t.Id, t => t.Name);

        return runs.Select(r => MapToDto(r, tenantDict.GetValueOrDefault(r.TenantId, "Unknown"))).ToList();
    }

    public async Task<IReadOnlyList<AssessmentRunDto>> GetRunsByTenantAsync(Guid tenantId, int take = 10, CancellationToken cancellationToken = default)
    {
        var runs = await _unitOfWork.AssessmentRuns.GetByTenantAsync(tenantId, take, cancellationToken);
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);

        return runs.Select(r => MapToDto(r, tenant?.Name ?? "Unknown")).ToList();
    }

    public async Task<AssessmentProgressDto> GetProgressAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Assessment run with ID '{runId}' not found");

        var domains = !string.IsNullOrEmpty(run.AssessedDomainsJson)
            ? JsonSerializer.Deserialize<List<AssessmentDomain>>(run.AssessedDomainsJson) ?? new()
            : new List<AssessmentDomain>();

        return new AssessmentProgressDto
        {
            RunId = runId,
            Status = run.Status,
            ProgressPercentage = run.Status == AssessmentStatus.Completed ? 100 :
                                 run.Status == AssessmentStatus.Running ? 50 : 0,
            CurrentOperation = run.Status.ToString(),
            PendingDomains = domains.Select(d => d.GetDisplayName()).ToList()
        };
    }

    public async Task CancelAssessmentAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetByIdAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Assessment run with ID '{runId}' not found");

        if (run.Status != AssessmentStatus.Running && run.Status != AssessmentStatus.Pending)
            throw new InvalidOperationException("Only running or pending assessments can be cancelled");

        run.Status = AssessmentStatus.Cancelled;
        run.CompletedAt = DateTime.UtcNow;
        await _unitOfWork.AssessmentRuns.UpdateAsync(run, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FindingDto>> GetFindingsAsync(Guid runId, AssessmentDomain? domain = null, Severity? severity = null, CancellationToken cancellationToken = default)
    {
        var findings = domain.HasValue
            ? await _unitOfWork.Findings.GetByDomainAsync(runId, domain.Value, cancellationToken)
            : await _unitOfWork.Findings.GetByRunIdAsync(runId, cancellationToken);

        var filteredFindings = findings.AsEnumerable();

        if (severity.HasValue)
            filteredFindings = filteredFindings.Where(f => f.Severity == severity.Value);

        return filteredFindings.Select(MapToDto).ToList();
    }

    public async Task<DomainDetailDto> GetDomainDetailAsync(Guid runId, AssessmentDomain domain, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetWithFindingsAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Assessment run with ID '{runId}' not found");

        var domainFindings = run.Findings.Where(f => f.Domain == domain).ToList();
        var rawSnapshot = run.RawSnapshots.FirstOrDefault(s => s.Domain == domain);

        // Get domain score from summary
        var domainScore = new DomainScoreSummary
        {
            Domain = domain,
            DisplayName = domain.GetDisplayName()
        };

        if (!string.IsNullOrEmpty(run.SummaryScoresJson))
        {
            var scores = JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(run.SummaryScoresJson);
            if (scores?.TryGetValue(domain.ToString(), out var score) == true)
            {
                domainScore = score;
            }
        }

        // Extract metrics from raw data
        var metrics = new Dictionary<string, object?>();
        if (rawSnapshot != null && !string.IsNullOrEmpty(rawSnapshot.PayloadJson))
        {
            try
            {
                var rawData = JsonSerializer.Deserialize<Dictionary<string, object?>>(rawSnapshot.PayloadJson);
                if (rawData != null)
                {
                    // Extract counts and key metrics
                    foreach (var kvp in rawData)
                    {
                        if (kvp.Value is JsonElement element && element.ValueKind == JsonValueKind.Number)
                        {
                            metrics[kvp.Key] = element.GetInt32();
                        }
                    }
                }
            }
            catch { }
        }

        // Generate recommendations from non-compliant findings
        var recommendations = domainFindings
            .Where(f => !f.IsCompliant && !string.IsNullOrEmpty(f.Remediation))
            .OrderBy(f => f.Severity)
            .Take(5)
            .Select(f => f.Remediation!)
            .ToList();

        return new DomainDetailDto
        {
            Domain = domain,
            DisplayName = domain.GetDisplayName(),
            Description = GetDomainDescription(domain),
            Score = domainScore,
            Findings = domainFindings.Select(MapToDto).ToList(),
            Metrics = metrics,
            RawDataJson = rawSnapshot?.PayloadJson,
            Recommendations = recommendations,
            IsAvailable = domainScore.IsAvailable,
            UnavailableReason = domainScore.UnavailableReason
        };
    }

    public async Task<byte[]> ExportReportAsync(Guid runId, ReportFormat format, CancellationToken cancellationToken = default)
    {
        var run = await _unitOfWork.AssessmentRuns.GetWithFindingsAsync(runId, cancellationToken);
        if (run == null)
            throw new InvalidOperationException($"Assessment run with ID '{runId}' not found");

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(run.TenantId, cancellationToken);

        // Generate report content based on format
        return format switch
        {
            ReportFormat.Json => await GenerateJsonReportAsync(run, tenant, cancellationToken),
            ReportFormat.Html => await GenerateHtmlReportAsync(run, tenant, cancellationToken),
            ReportFormat.Csv => await GenerateCsvReportAsync(run, tenant, cancellationToken),
            _ => await GeneratePdfReportAsync(run, tenant, cancellationToken)
        };
    }

    public async Task<byte[]> ExportDomainReportAsync(Guid runId, AssessmentDomain domain, ReportFormat format, CancellationToken cancellationToken = default)
    {
        var domainDetail = await GetDomainDetailAsync(runId, domain, cancellationToken);
        var run = await _unitOfWork.AssessmentRuns.GetByIdAsync(runId, cancellationToken);
        var tenant = run != null ? await _unitOfWork.Tenants.GetByIdAsync(run.TenantId, cancellationToken) : null;

        return format switch
        {
            ReportFormat.Json => System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(domainDetail, new JsonSerializerOptions { WriteIndented = true })),
            _ => await GenerateDomainHtmlReportAsync(domainDetail, tenant?.Name ?? "Unknown", cancellationToken)
        };
    }

    private AssessmentRunDto MapToDto(AssessmentRun run, string tenantName)
    {
        return new AssessmentRunDto
        {
            Id = run.Id,
            TenantId = run.TenantId,
            TenantName = tenantName,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Status = run.Status,
            InitiatedBy = run.InitiatedBy,
            OverallScore = run.OverallScore,
            DomainScores = !string.IsNullOrEmpty(run.SummaryScoresJson)
                ? JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(run.SummaryScoresJson) ?? new()
                : new(),
            ErrorMessage = run.ErrorMessage
        };
    }

    private FindingDto MapToDto(Finding finding)
    {
        return new FindingDto
        {
            Id = finding.Id,
            AssessmentRunId = finding.AssessmentRunId,
            Domain = finding.Domain,
            DomainDisplayName = finding.Domain.GetDisplayName(),
            Severity = finding.Severity,
            Title = finding.Title,
            Description = finding.Description,
            Category = finding.Category,
            Evidence = finding.EvidenceJson,
            Remediation = finding.Remediation,
            References = finding.References,
            AffectedResources = !string.IsNullOrEmpty(finding.AffectedResources)
                ? finding.AffectedResources.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                : new(),
            IsCompliant = finding.IsCompliant,
            CheckId = finding.CheckId,
            CreatedAt = finding.CreatedAt
        };
    }

    private static string GetDomainDescription(AssessmentDomain domain) => domain switch
    {
        AssessmentDomain.IdentityAndAccess => "Assesses user accounts, admin roles, MFA status, Conditional Access policies, and authentication settings.",
        AssessmentDomain.PrivilegedAccess => "Evaluates PIM configuration, eligible vs active roles, approval requirements, and just-in-time access.",
        AssessmentDomain.DeviceEndpoint => "Reviews Intune enrollment status, device compliance policies, and endpoint security configurations.",
        AssessmentDomain.ExchangeEmailSecurity => "Analyzes anti-phishing policies, mail flow rules, and email authentication (DKIM/DMARC/SPF).",
        AssessmentDomain.MicrosoftDefender => "Examines Defender for Office 365 configuration and Microsoft Secure Score.",
        AssessmentDomain.DataProtectionCompliance => "Evaluates DLP policies, sensitivity labels, retention policies, and compliance posture.",
        AssessmentDomain.AuditLogging => "Checks unified audit log status and sign-in/audit log configuration.",
        AssessmentDomain.AppGovernance => "Reviews enterprise applications, OAuth grants, and third-party app permissions.",
        AssessmentDomain.CollaborationSecurity => "Assesses Teams and SharePoint sharing settings, external access, and guest policies.",
        _ => "Security assessment domain"
    };

    private Task<byte[]> GenerateJsonReportAsync(AssessmentRun run, Tenant? tenant, CancellationToken cancellationToken)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow,
            TenantName = tenant?.Name,
            TenantDomain = tenant?.Domain,
            AssessmentRun = new
            {
                run.Id,
                run.StartedAt,
                run.CompletedAt,
                run.Status,
                run.OverallScore
            },
            Findings = run.Findings.Select(f => new
            {
                f.Domain,
                f.Severity,
                f.Title,
                f.Description,
                f.IsCompliant,
                f.Remediation
            })
        };

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(json));
    }

    private Task<byte[]> GenerateHtmlReportAsync(AssessmentRun run, Tenant? tenant, CancellationToken cancellationToken)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Security Assessment Report - {tenant?.Name}</title>
    <style>
        body {{ font-family: 'Poppins', sans-serif; margin: 40px; }}
        .header {{ background: linear-gradient(135deg, #51627A, #3d4a5c); color: white; padding: 24px; border-radius: 8px; }}
        .logo {{ background: #E0FC8E; padding: 8px 16px; border-radius: 4px; display: inline-block; color: #51627A; font-weight: bold; }}
        .score {{ font-size: 48px; font-weight: bold; }}
        .finding {{ border: 1px solid #ddd; padding: 16px; margin: 8px 0; border-radius: 8px; }}
        .critical {{ border-left: 4px solid #f44336; }}
        .high {{ border-left: 4px solid #ff9800; }}
        .medium {{ border-left: 4px solid #2196f3; }}
        .low {{ border-left: 4px solid #9e9e9e; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='logo'>CLOUDATIV</div>
        <h1>Security Assessment Report</h1>
        <p>Tenant: {tenant?.Name} ({tenant?.Domain})</p>
        <p>Generated: {DateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC</p>
    </div>
    <h2>Overall Score: <span class='score'>{run.OverallScore}%</span></h2>
    <h2>Findings</h2>
    {string.Join("", run.Findings.OrderBy(f => f.Severity).Select(f => $@"
    <div class='finding {f.Severity.ToString().ToLower()}'>
        <strong>{f.Severity}: {f.Title}</strong>
        <p>{f.Description}</p>
        {(f.IsCompliant ? "<span style='color:green'>✓ Compliant</span>" : "<span style='color:red'>✗ Non-Compliant</span>")}
        {(!string.IsNullOrEmpty(f.Remediation) ? $"<p><strong>Remediation:</strong> {f.Remediation}</p>" : "")}
    </div>"))}
</body>
</html>";

        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(html));
    }

    private Task<byte[]> GenerateCsvReportAsync(AssessmentRun run, Tenant? tenant, CancellationToken cancellationToken)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Domain,Severity,Title,Description,Compliant,Remediation");

        foreach (var finding in run.Findings.OrderBy(f => f.Severity))
        {
            csv.AppendLine($"\"{finding.Domain}\",\"{finding.Severity}\",\"{finding.Title}\",\"{finding.Description?.Replace("\"", "\"\"")}\",\"{finding.IsCompliant}\",\"{finding.Remediation?.Replace("\"", "\"\"")}\"");
        }

        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private Task<byte[]> GeneratePdfReportAsync(AssessmentRun run, Tenant? tenant, CancellationToken cancellationToken)
    {
        // For PDF, we'd use QuestPDF, but for now return HTML
        return GenerateHtmlReportAsync(run, tenant, cancellationToken);
    }

    private Task<byte[]> GenerateDomainHtmlReportAsync(DomainDetailDto detail, string tenantName, CancellationToken cancellationToken)
    {
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{detail.DisplayName} Report</title>
    <style>
        body {{ font-family: 'Poppins', sans-serif; margin: 40px; }}
        .header {{ background: linear-gradient(135deg, #51627A, #3d4a5c); color: white; padding: 24px; border-radius: 8px; }}
        .score {{ font-size: 48px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>{detail.DisplayName}</h1>
        <p>Tenant: {tenantName}</p>
    </div>
    <h2>Score: <span class='score'>{detail.Score.Score}%</span> (Grade: {detail.Score.Grade})</h2>
    <p>{detail.Description}</p>
    <h3>Findings: {detail.Findings.Count}</h3>
</body>
</html>";

        return Task.FromResult(System.Text.Encoding.UTF8.GetBytes(html));
    }
}
