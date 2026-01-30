using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for fetching and managing compliance standard documents.
/// </summary>
public interface IStandardDocumentService
{
    /// <summary>
    /// Gets the document content for a compliance standard.
    /// Documents are cached in memory to avoid repeated fetches.
    /// </summary>
    Task<StandardDocumentResult> GetDocumentContentAsync(
        ComplianceStandard standard,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configured URL for a compliance standard document.
    /// </summary>
    string? GetDocumentUrl(ComplianceStandard standard);

    /// <summary>
    /// Gets the configured display name for a compliance standard.
    /// </summary>
    string GetDocumentName(ComplianceStandard standard);

    /// <summary>
    /// Checks if a document is configured and available for a standard.
    /// </summary>
    bool IsDocumentAvailable(ComplianceStandard standard);

    /// <summary>
    /// Gets all standards that have documents configured.
    /// </summary>
    IReadOnlyList<ComplianceStandard> GetConfiguredStandards();

    /// <summary>
    /// Clears the cache for a specific standard or all standards.
    /// </summary>
    void ClearCache(ComplianceStandard? standard = null);
}

/// <summary>
/// Result of fetching a standard document.
/// </summary>
public record StandardDocumentResult
{
    /// <summary>
    /// Whether the document was successfully fetched.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the fetch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The document content (text/markdown).
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// The URL the document was fetched from.
    /// </summary>
    public string? SourceUrl { get; init; }

    /// <summary>
    /// Version or last modified date of the document.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Whether the content was served from cache.
    /// </summary>
    public bool FromCache { get; init; }
}
