using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class StandardDocumentService : IStandardDocumentService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StandardDocumentService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
    private const string CacheKeyPrefix = "ComplianceStandardDoc_";

    public StandardDocumentService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<StandardDocumentService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<StandardDocumentResult> GetDocumentContentAsync(
        ComplianceStandard standard,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{standard}";

        // Check cache first
        if (_cache.TryGetValue<StandardDocumentResult>(cacheKey, out var cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("Returning cached document for standard: {Standard}", standard);
            return cachedResult with { FromCache = true };
        }

        var url = GetDocumentUrl(standard);
        if (string.IsNullOrWhiteSpace(url))
        {
            var errorResult = new StandardDocumentResult
            {
                Success = false,
                ErrorMessage = $"No document URL configured for {standard.GetDisplayName()}"
            };
            return errorResult;
        }

        try
        {
            _logger.LogInformation("Fetching compliance standard document from: {Url}", url);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorResult = new StandardDocumentResult
                {
                    Success = false,
                    ErrorMessage = $"Failed to fetch document: HTTP {response.StatusCode}",
                    SourceUrl = url
                };
                return errorResult;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var lastModified = response.Content.Headers.LastModified?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

            var result = new StandardDocumentResult
            {
                Success = true,
                Content = content,
                SourceUrl = url,
                Version = lastModified,
                FromCache = false
            };

            // Cache the result
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(CacheDuration)
                .SetSlidingExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, result, cacheOptions);

            _logger.LogInformation("Successfully fetched and cached document for standard: {Standard}", standard);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching document for standard: {Standard}", standard);
            return new StandardDocumentResult
            {
                Success = false,
                ErrorMessage = $"Error fetching document: {ex.Message}",
                SourceUrl = url
            };
        }
    }

    public string? GetDocumentUrl(ComplianceStandard standard)
    {
        var configKey = GetConfigKey(standard);
        return _configuration.GetValue<string>($"ComplianceStandards:{configKey}:DocumentUrl");
    }

    public string GetDocumentName(ComplianceStandard standard)
    {
        var configKey = GetConfigKey(standard);
        return _configuration.GetValue<string>($"ComplianceStandards:{configKey}:Name")
               ?? standard.GetDisplayName();
    }

    public bool IsDocumentAvailable(ComplianceStandard standard)
    {
        // All standards are available - we can use AI's built-in knowledge
        // when no document URL is configured
        return true;
    }

    public bool HasDocumentUrl(ComplianceStandard standard)
    {
        var url = GetDocumentUrl(standard);
        return !string.IsNullOrWhiteSpace(url);
    }

    public IReadOnlyList<ComplianceStandard> GetConfiguredStandards()
    {
        // Return all standards since AI has built-in knowledge
        return Enum.GetValues<ComplianceStandard>()
            .ToList()
            .AsReadOnly();
    }

    public void ClearCache(ComplianceStandard? standard = null)
    {
        if (standard.HasValue)
        {
            var cacheKey = $"{CacheKeyPrefix}{standard.Value}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Cleared cache for standard: {Standard}", standard.Value);
        }
        else
        {
            foreach (var std in Enum.GetValues<ComplianceStandard>())
            {
                var cacheKey = $"{CacheKeyPrefix}{std}";
                _cache.Remove(cacheKey);
            }
            _logger.LogInformation("Cleared cache for all standards");
        }
    }

    private static string GetConfigKey(ComplianceStandard standard)
    {
        return standard switch
        {
            ComplianceStandard.NcaCcc => "NcaCcc",
            ComplianceStandard.Iso27001 => "Iso27001",
            ComplianceStandard.PciDss => "PciDss",
            ComplianceStandard.Hipaa => "Hipaa",
            ComplianceStandard.NistCsf => "NistCsf",
            _ => standard.ToString()
        };
    }
}
