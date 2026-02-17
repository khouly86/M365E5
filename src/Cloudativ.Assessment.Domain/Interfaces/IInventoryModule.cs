using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Interfaces;

/// <summary>
/// Interface for inventory collection modules. Each module collects inventory data for a specific domain.
/// </summary>
public interface IInventoryModule
{
    /// <summary>
    /// The inventory domain this module handles.
    /// </summary>
    InventoryDomain Domain { get; }

    /// <summary>
    /// Display name for the module.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of what this module collects.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Microsoft Graph API permissions required by this module.
    /// </summary>
    IReadOnlyList<string> RequiredPermissions { get; }

    /// <summary>
    /// Collects inventory data from Microsoft Graph API and stores it in the database.
    /// </summary>
    /// <param name="graphClient">Graph API client wrapper.</param>
    /// <param name="tenantId">The tenant ID to collect inventory for.</param>
    /// <param name="snapshotId">The snapshot ID to associate collected data with.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success/failure and item counts.</returns>
    Task<InventoryCollectionResult> CollectAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the required Graph API permissions are granted.
    /// </summary>
    Task<bool> ValidatePermissionsAsync(
        IGraphClientWrapper graphClient,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of an inventory collection operation.
/// </summary>
public class InventoryCollectionResult
{
    /// <summary>
    /// The inventory domain that was collected.
    /// </summary>
    public InventoryDomain Domain { get; set; }

    /// <summary>
    /// Whether the collection was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if collection failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of items collected.
    /// </summary>
    public int ItemCount { get; set; }

    /// <summary>
    /// Timestamp when collection completed.
    /// </summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duration of the collection operation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Warnings encountered during collection.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Graph API endpoints that were unavailable (permission issues).
    /// </summary>
    public List<string> UnavailableEndpoints { get; set; } = new();

    /// <summary>
    /// Breakdown of items collected by type.
    /// </summary>
    public Dictionary<string, int> ItemBreakdown { get; set; } = new();

    /// <summary>
    /// Number of items added since last snapshot.
    /// </summary>
    public int? ItemsAdded { get; set; }

    /// <summary>
    /// Number of items removed since last snapshot.
    /// </summary>
    public int? ItemsRemoved { get; set; }

    /// <summary>
    /// Number of items modified since last snapshot.
    /// </summary>
    public int? ItemsModified { get; set; }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static InventoryCollectionResult Succeeded(
        InventoryDomain domain,
        int itemCount,
        TimeSpan duration,
        Dictionary<string, int>? breakdown = null,
        List<string>? warnings = null)
    {
        return new InventoryCollectionResult
        {
            Domain = domain,
            Success = true,
            ItemCount = itemCount,
            Duration = duration,
            ItemBreakdown = breakdown ?? new Dictionary<string, int>(),
            Warnings = warnings ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static InventoryCollectionResult Failed(
        InventoryDomain domain,
        string errorMessage,
        TimeSpan duration,
        List<string>? warnings = null)
    {
        return new InventoryCollectionResult
        {
            Domain = domain,
            Success = false,
            ErrorMessage = errorMessage,
            Duration = duration,
            Warnings = warnings ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a partial success result.
    /// </summary>
    public static InventoryCollectionResult PartialSuccess(
        InventoryDomain domain,
        int itemCount,
        TimeSpan duration,
        List<string> warnings,
        List<string> unavailableEndpoints)
    {
        return new InventoryCollectionResult
        {
            Domain = domain,
            Success = true,
            ItemCount = itemCount,
            Duration = duration,
            Warnings = warnings,
            UnavailableEndpoints = unavailableEndpoints
        };
    }
}

/// <summary>
/// Progress information for an ongoing inventory collection.
/// </summary>
public class InventoryProgress
{
    public Guid SnapshotId { get; set; }
    public InventoryStatus Status { get; set; }
    public double ProgressPercentage { get; set; }
    public InventoryDomain? CurrentDomain { get; set; }
    public List<InventoryDomain> CompletedDomains { get; set; } = new();
    public List<InventoryDomain> PendingDomains { get; set; } = new();
    public List<InventoryDomain> FailedDomains { get; set; } = new();
    public int TotalItemsCollected { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}
