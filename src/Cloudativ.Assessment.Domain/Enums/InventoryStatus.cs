namespace Cloudativ.Assessment.Domain.Enums;

/// <summary>
/// Status of an inventory collection run.
/// </summary>
public enum InventoryStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    PartiallyCompleted = 5
}

public static class InventoryStatusExtensions
{
    public static string GetDisplayName(this InventoryStatus status) => status switch
    {
        InventoryStatus.Pending => "Pending",
        InventoryStatus.Running => "Running",
        InventoryStatus.Completed => "Completed",
        InventoryStatus.Failed => "Failed",
        InventoryStatus.Cancelled => "Cancelled",
        InventoryStatus.PartiallyCompleted => "Partially Completed",
        _ => status.ToString()
    };

    public static string GetColor(this InventoryStatus status) => status switch
    {
        InventoryStatus.Pending => "#9E9E9E",
        InventoryStatus.Running => "#1976D2",
        InventoryStatus.Completed => "#388E3C",
        InventoryStatus.Failed => "#D32F2F",
        InventoryStatus.Cancelled => "#FF9800",
        InventoryStatus.PartiallyCompleted => "#F57C00",
        _ => "#757575"
    };

    public static string GetMudColor(this InventoryStatus status) => status switch
    {
        InventoryStatus.Pending => "Default",
        InventoryStatus.Running => "Info",
        InventoryStatus.Completed => "Success",
        InventoryStatus.Failed => "Error",
        InventoryStatus.Cancelled => "Warning",
        InventoryStatus.PartiallyCompleted => "Warning",
        _ => "Default"
    };
}
