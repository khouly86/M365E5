using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Master record for an inventory collection snapshot.
/// </summary>
public class InventorySnapshot : BaseEntity
{
    public Guid TenantId { get; set; }
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public InventoryDomain Domain { get; set; }
    public string? InitiatedBy { get; set; }
    public InventoryStatus Status { get; set; } = InventoryStatus.Pending;
    public int ItemCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? WarningsJson { get; set; }
    public TimeSpan? Duration { get; set; }

    // Delta tracking
    public int? ItemsAdded { get; set; }
    public int? ItemsRemoved { get; set; }
    public int? ItemsModified { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
}
