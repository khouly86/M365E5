using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class GovernanceService : IGovernanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOpenAiService _openAiService;
    private readonly IStandardDocumentService _standardDocumentService;
    private readonly ILogger<GovernanceService> _logger;

    public GovernanceService(
        IUnitOfWork unitOfWork,
        IOpenAiService openAiService,
        IStandardDocumentService standardDocumentService,
        ILogger<GovernanceService> logger)
    {
        _unitOfWork = unitOfWork;
        _openAiService = openAiService;
        _standardDocumentService = standardDocumentService;
        _logger = logger;
    }

    public async Task<GovernanceAnalysisDto> RunAnalysisAsync(
        Guid tenantId,
        Guid assessmentRunId,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting governance analysis for tenant {TenantId}, run {RunId}, standard {Standard}",
            tenantId, assessmentRunId, standard);

        // Check if OpenAI is enabled
        if (!await _openAiService.IsEnabledAsync(cancellationToken))
        {
            return CreateFailedAnalysisDto(tenantId, assessmentRunId, standard,
                "OpenAI integration is not enabled. Please configure OpenAI in appsettings.json.");
        }

        // Get the assessment run with findings
        var assessmentRun = await _unitOfWork.AssessmentRuns.GetWithFindingsAsync(assessmentRunId, cancellationToken);
        if (assessmentRun == null)
        {
            return CreateFailedAnalysisDto(tenantId, assessmentRunId, standard,
                $"Assessment run {assessmentRunId} not found.");
        }

        if (assessmentRun.TenantId != tenantId)
        {
            return CreateFailedAnalysisDto(tenantId, assessmentRunId, standard,
                "Assessment run does not belong to the specified tenant.");
        }

        // Try to fetch the compliance document (optional - AI has built-in knowledge)
        string? documentContent = null;
        string? documentVersion = null;
        var documentResult = await _standardDocumentService.GetDocumentContentAsync(standard, cancellationToken);
        if (documentResult.Success && !string.IsNullOrEmpty(documentResult.Content))
        {
            documentContent = documentResult.Content;
            documentVersion = documentResult.Version;
            _logger.LogInformation("Using external compliance document for {Standard}", standard);
        }
        else
        {
            _logger.LogInformation("Using AI built-in knowledge for {Standard} (no document configured)", standard);
        }

        // Prepare findings JSON for analysis
        var findingsJson = SerializeFindingsForAnalysis(assessmentRun.Findings);

        // Call OpenAI for analysis
        var aiResult = await _openAiService.AnalyzeComplianceAsync(
            findingsJson,
            documentContent, // May be null - AI will use built-in knowledge
            standard,
            cancellationToken);

        // Create and save the governance analysis entity
        var analysis = new GovernanceAnalysis
        {
            TenantId = tenantId,
            AssessmentRunId = assessmentRunId,
            Standard = standard,
            IsSuccessful = aiResult.Success,
            ErrorMessage = aiResult.ErrorMessage,
            ComplianceScore = aiResult.ComplianceScore,
            TotalControls = aiResult.TotalControls,
            CompliantControls = aiResult.CompliantControls,
            PartiallyCompliantControls = aiResult.PartiallyCompliantControls,
            NonCompliantControls = aiResult.NonCompliantControls,
            ComplianceGapsJson = aiResult.ComplianceGapsJson,
            RecommendationsJson = aiResult.RecommendationsJson,
            CompliantAreasJson = aiResult.CompliantAreasJson,
            AiModelUsed = _openAiService.GetModelName(),
            TokensUsed = aiResult.TotalTokens,
            RawResponseJson = aiResult.RawResponseJson,
            StandardDocumentVersion = documentVersion ?? "AI Built-in Knowledge",
            AnalyzedAt = DateTime.UtcNow
        };

        // Check if an analysis already exists for this run/standard combo
        var existing = await _unitOfWork.GovernanceAnalyses.GetByRunAndStandardAsync(assessmentRunId, standard, cancellationToken);
        if (existing != null)
        {
            // Delete the existing analysis
            await _unitOfWork.GovernanceAnalyses.DeleteAsync(existing, cancellationToken);
        }

        await _unitOfWork.GovernanceAnalyses.AddAsync(analysis, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed governance analysis for standard {Standard}, score: {Score}",
            standard, aiResult.ComplianceScore);

        return MapToDto(analysis);
    }

    public async Task<IReadOnlyList<GovernanceAnalysisDto>> RunMultipleAnalysesAsync(
        Guid tenantId,
        Guid assessmentRunId,
        IEnumerable<ComplianceStandard> standards,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GovernanceAnalysisDto>();
        var standardsList = standards.Distinct().ToList();

        _logger.LogInformation("Starting multiple governance analyses for {Count} standards", standardsList.Count);

        // Run analyses sequentially to avoid rate limiting
        foreach (var standard in standardsList)
        {
            var result = await RunAnalysisAsync(tenantId, assessmentRunId, standard, cancellationToken);
            results.Add(result);
        }

        return results.AsReadOnly();
    }

    public async Task<GovernanceAnalysisDto?> GetAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _unitOfWork.GovernanceAnalyses.GetByIdAsync(analysisId, cancellationToken);
        return analysis != null ? MapToDto(analysis) : null;
    }

    public async Task<IReadOnlyList<GovernanceAnalysisDto>> GetAnalysesByRunAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default)
    {
        var analyses = await _unitOfWork.GovernanceAnalyses.GetByAssessmentRunIdAsync(assessmentRunId, cancellationToken);
        return analyses.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<GovernanceAnalysisDto>> GetAnalysesByTenantAsync(
        Guid tenantId,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var analyses = await _unitOfWork.GovernanceAnalyses.GetByTenantIdAsync(tenantId, take, cancellationToken);
        return analyses.Select(MapToDto).ToList().AsReadOnly();
    }

    public async Task<GovernanceAnalysisSummaryDto?> GetAnalysisSummaryAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default)
    {
        var analyses = await _unitOfWork.GovernanceAnalyses.GetByAssessmentRunIdAsync(assessmentRunId, cancellationToken);
        if (!analyses.Any())
            return null;

        var successfulAnalyses = analyses.Where(a => a.IsSuccessful).ToList();
        var averageScore = successfulAnalyses.Any()
            ? (int)Math.Round(successfulAnalyses.Average(a => a.ComplianceScore))
            : 0;

        var totalGaps = successfulAnalyses.Sum(a =>
        {
            if (string.IsNullOrEmpty(a.ComplianceGapsJson)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(a.ComplianceGapsJson);
                return doc.RootElement.GetArrayLength();
            }
            catch
            {
                return 0;
            }
        });

        var totalRecommendations = successfulAnalyses.Sum(a =>
        {
            if (string.IsNullOrEmpty(a.RecommendationsJson)) return 0;
            try
            {
                using var doc = JsonDocument.Parse(a.RecommendationsJson);
                return doc.RootElement.GetArrayLength();
            }
            catch
            {
                return 0;
            }
        });

        return new GovernanceAnalysisSummaryDto
        {
            TenantId = analyses.First().TenantId,
            AssessmentRunId = assessmentRunId,
            AnalyzedAt = analyses.Max(a => a.AnalyzedAt),
            StandardsAnalyzed = analyses.Count,
            OverallAverageScore = averageScore,
            TotalGaps = totalGaps,
            TotalRecommendations = totalRecommendations,
            StandardScores = analyses.Select(a => new StandardScoreDto
            {
                Standard = a.Standard,
                StandardDisplayName = a.Standard.GetDisplayName(),
                Score = a.ComplianceScore,
                GapsCount = CountJsonArrayItems(a.ComplianceGapsJson),
                IsSuccessful = a.IsSuccessful
            }).ToList()
        };
    }

    public Task<IReadOnlyList<ComplianceStandardInfoDto>> GetRecommendedStandardsForIndustryAsync(
        string? industry,
        CancellationToken cancellationToken = default)
    {
        var recommended = ComplianceStandardExtensions.GetStandardsForIndustry(industry);
        var allStandards = ComplianceStandardExtensions.GetAllStandards();

        var result = allStandards.Select(s => new ComplianceStandardInfoDto
        {
            Standard = s,
            DisplayName = s.GetDisplayName(),
            Description = s.GetDescription(),
            IsRecommended = recommended.Contains(s),
            IsEnabled = _standardDocumentService.IsDocumentAvailable(s)
        }).ToList();

        return Task.FromResult<IReadOnlyList<ComplianceStandardInfoDto>>(result.AsReadOnly());
    }

    public Task<IReadOnlyList<ComplianceStandardInfoDto>> GetAvailableStandardsAsync(
        CancellationToken cancellationToken = default)
    {
        var allStandards = ComplianceStandardExtensions.GetAllStandards();

        var result = allStandards.Select(s => new ComplianceStandardInfoDto
        {
            Standard = s,
            DisplayName = s.GetDisplayName(),
            Description = s.GetDescription(),
            IsRecommended = false,
            IsEnabled = _standardDocumentService.IsDocumentAvailable(s)
        }).ToList();

        return Task.FromResult<IReadOnlyList<ComplianceStandardInfoDto>>(result.AsReadOnly());
    }

    public Task<bool> IsOpenAiEnabledAsync(CancellationToken cancellationToken = default)
    {
        return _openAiService.IsEnabledAsync(cancellationToken);
    }

    public async Task DeleteAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _unitOfWork.GovernanceAnalyses.GetByIdAsync(analysisId, cancellationToken);
        if (analysis != null)
        {
            await _unitOfWork.GovernanceAnalyses.DeleteAsync(analysis, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteAnalysesByRunAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default)
    {
        var analyses = await _unitOfWork.GovernanceAnalyses.GetByAssessmentRunIdAsync(assessmentRunId, cancellationToken);
        foreach (var analysis in analyses)
        {
            await _unitOfWork.GovernanceAnalyses.DeleteAsync(analysis, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string SerializeFindingsForAnalysis(ICollection<Finding> findings)
    {
        var summaries = findings.Select(f => new
        {
            f.Domain,
            f.Category,
            f.CheckId,
            f.CheckName,
            f.Title,
            f.Description,
            f.Severity,
            f.IsCompliant,
            f.Remediation,
            f.AffectedResources
        });

        return JsonSerializer.Serialize(summaries, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    private static GovernanceAnalysisDto CreateFailedAnalysisDto(
        Guid tenantId,
        Guid assessmentRunId,
        ComplianceStandard standard,
        string errorMessage)
    {
        return new GovernanceAnalysisDto
        {
            Id = Guid.Empty,
            TenantId = tenantId,
            AssessmentRunId = assessmentRunId,
            Standard = standard,
            StandardDisplayName = standard.GetDisplayName(),
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private static GovernanceAnalysisDto MapToDto(GovernanceAnalysis analysis)
    {
        return new GovernanceAnalysisDto
        {
            Id = analysis.Id,
            TenantId = analysis.TenantId,
            AssessmentRunId = analysis.AssessmentRunId,
            Standard = analysis.Standard,
            StandardDisplayName = analysis.Standard.GetDisplayName(),
            ComplianceScore = analysis.ComplianceScore,
            TotalControls = analysis.TotalControls,
            CompliantControls = analysis.CompliantControls,
            PartiallyCompliantControls = analysis.PartiallyCompliantControls,
            NonCompliantControls = analysis.NonCompliantControls,
            ComplianceGaps = ParseJsonArray<ComplianceGapDto>(analysis.ComplianceGapsJson),
            Recommendations = ParseJsonArray<ComplianceRecommendationDto>(analysis.RecommendationsJson),
            CompliantAreas = ParseJsonArray<CompliantAreaDto>(analysis.CompliantAreasJson),
            AiModelUsed = analysis.AiModelUsed,
            TokensUsed = analysis.TokensUsed,
            AnalyzedAt = analysis.AnalyzedAt,
            IsSuccessful = analysis.IsSuccessful,
            ErrorMessage = analysis.ErrorMessage
        };
    }

    private static List<T> ParseJsonArray<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new List<T>();

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static int CountJsonArrayItems(string? json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetArrayLength();
        }
        catch
        {
            return 0;
        }
    }
}
