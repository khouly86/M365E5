using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;

namespace Cloudativ.Assessment.Infrastructure.Inventory;

/// <summary>
/// Engine interface for orchestrating inventory collection across modules.
/// </summary>
public interface IInventoryEngine
{
    /// <summary>
    /// Starts a new inventory collection and returns the snapshot ID.
    /// </summary>
    Task<Guid> StartCollectionAsync(
        Guid tenantId,
        List<InventoryDomain>? domains,
        string? initiatedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Executes inventory collection for a specific snapshot.
    /// </summary>
    Task ExecuteCollectionAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current progress of an inventory collection.
    /// </summary>
    Task<InventoryProgress> GetProgressAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Cancels an ongoing inventory collection.
    /// </summary>
    Task CancelCollectionAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when collection progress changes.
    /// </summary>
    event EventHandler<InventoryProgressEventArgs>? ProgressChanged;
}

/// <summary>
/// Event args for inventory progress changes.
/// </summary>
public class InventoryProgressEventArgs : EventArgs
{
    public Guid SnapshotId { get; init; }
    public InventoryProgress Progress { get; init; } = null!;
}
