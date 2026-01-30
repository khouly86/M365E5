using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for interacting with OpenAI API for compliance analysis.
/// </summary>
public interface IOpenAiService
{
    /// <summary>
    /// Checks if OpenAI integration is enabled and properly configured.
    /// </summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes security assessment findings against a compliance standard.
    /// Uses grounded prompting when a document is provided, otherwise uses AI's built-in knowledge.
    /// </summary>
    /// <param name="assessmentFindingsJson">JSON containing the assessment findings.</param>
    /// <param name="standardDocumentContent">Optional compliance standard document content. If null, AI uses built-in knowledge.</param>
    /// <param name="standard">The compliance standard to analyze against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<OpenAiComplianceAnalysisResult> AnalyzeComplianceAsync(
        string assessmentFindingsJson,
        string? standardDocumentContent,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured OpenAI model name.
    /// </summary>
    string GetModelName();
}

/// <summary>
/// Result of an OpenAI compliance analysis.
/// </summary>
public record OpenAiComplianceAnalysisResult
{
    /// <summary>
    /// Whether the analysis was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the analysis failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Overall compliance score (0-100).
    /// </summary>
    public int ComplianceScore { get; init; }

    /// <summary>
    /// Total controls evaluated from the standard.
    /// </summary>
    public int TotalControls { get; init; }

    /// <summary>
    /// Number of fully compliant controls.
    /// </summary>
    public int CompliantControls { get; init; }

    /// <summary>
    /// Number of partially compliant controls.
    /// </summary>
    public int PartiallyCompliantControls { get; init; }

    /// <summary>
    /// Number of non-compliant controls.
    /// </summary>
    public int NonCompliantControls { get; init; }

    /// <summary>
    /// JSON array of compliance gaps.
    /// </summary>
    public string? ComplianceGapsJson { get; init; }

    /// <summary>
    /// JSON array of recommendations.
    /// </summary>
    public string? RecommendationsJson { get; init; }

    /// <summary>
    /// JSON array of compliant areas.
    /// </summary>
    public string? CompliantAreasJson { get; init; }

    /// <summary>
    /// Number of tokens used in the request.
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Number of tokens used in the response.
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;

    /// <summary>
    /// Raw JSON response from OpenAI (for debugging/auditing).
    /// </summary>
    public string? RawResponseJson { get; init; }
}
