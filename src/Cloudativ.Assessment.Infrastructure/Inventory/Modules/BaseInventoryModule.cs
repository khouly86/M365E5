using System.Text.Json;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory.Modules;

/// <summary>
/// Base class for inventory collection modules.
/// </summary>
public abstract class BaseInventoryModule : IInventoryModule
{
    protected readonly ApplicationDbContext DbContext;
    protected readonly ILogger Logger;

    public abstract InventoryDomain Domain { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public abstract IReadOnlyList<string> RequiredPermissions { get; }

    protected BaseInventoryModule(ApplicationDbContext dbContext, ILogger logger)
    {
        DbContext = dbContext;
        Logger = logger;
    }

    public abstract Task<InventoryCollectionResult> CollectAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        CancellationToken cancellationToken = default);

    public virtual async Task<bool> ValidatePermissionsAsync(
        IGraphClientWrapper graphClient,
        CancellationToken cancellationToken = default)
    {
        foreach (var permission in RequiredPermissions)
        {
            if (!await graphClient.HasPermissionAsync(permission, cancellationToken))
            {
                Logger.LogWarning("Missing required permission: {Permission}", permission);
                return false;
            }
        }

        return true;
    }

    #region Helper Methods

    /// <summary>
    /// Collects all items from a paginated Graph API endpoint.
    /// </summary>
    protected async Task<List<T>> CollectAllWithPaginationAsync<T>(
        IGraphClientWrapper graphClient,
        string endpoint,
        CancellationToken ct) where T : class
    {
        var allItems = new List<T>();
        var currentEndpoint = endpoint;

        while (!string.IsNullOrEmpty(currentEndpoint))
        {
            try
            {
                var response = await graphClient.GetRawJsonAsync(currentEndpoint, ct);
                if (string.IsNullOrEmpty(response))
                    break;

                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    var items = JsonSerializer.Deserialize<List<T>>(valueElement.GetRawText(),
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null)
                    {
                        allItems.AddRange(items);
                    }
                }

                // Check for next page
                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement)
                    ? nextLinkElement.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error fetching page from {Endpoint}", currentEndpoint);
                break;
            }
        }

        return allItems;
    }

    /// <summary>
    /// Safely gets a string property from a JSON element.
    /// </summary>
    protected static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }

    /// <summary>
    /// Safely gets a boolean property from a JSON element.
    /// </summary>
    protected static bool GetBool(JsonElement element, string propertyName, bool defaultValue = false)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True
            ? true
            : element.TryGetProperty(propertyName, out prop) && prop.ValueKind == JsonValueKind.False
                ? false
                : defaultValue;
    }

    /// <summary>
    /// Safely gets an int property from a JSON element.
    /// </summary>
    protected static int GetInt(JsonElement element, string propertyName, int defaultValue = 0)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
            ? prop.GetInt32()
            : defaultValue;
    }

    /// <summary>
    /// Safely gets a DateTime property from a JSON element.
    /// </summary>
    protected static DateTime? GetDateTime(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            var str = prop.GetString();
            if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, out var dt))
                return dt;
        }

        return null;
    }

    /// <summary>
    /// Serializes an object to JSON string.
    /// </summary>
    protected static string? ToJson(object? value)
    {
        if (value == null) return null;
        return JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    protected InventoryCollectionResult Success(int itemCount, TimeSpan duration, Dictionary<string, int>? breakdown = null, List<string>? warnings = null)
    {
        return InventoryCollectionResult.Succeeded(Domain, itemCount, duration, breakdown, warnings);
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    protected InventoryCollectionResult Failure(string error, TimeSpan duration, List<string>? warnings = null)
    {
        return InventoryCollectionResult.Failed(Domain, error, duration, warnings);
    }

    #endregion
}
