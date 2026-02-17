namespace Cloudativ.Assessment.Domain.Enums;

/// <summary>
/// Represents the 12 inventory domains for M365 E5 asset tracking.
/// </summary>
public enum InventoryDomain
{
    TenantBaseline = 1,
    IdentityAccess = 2,
    DeviceEndpoint = 3,
    DefenderXDR = 4,
    EmailExchange = 5,
    DataProtection = 6,
    SharePointOneDriveTeams = 7,
    ApplicationsOAuth = 8,
    LogsMonitoring = 9,
    SecureScore = 10,
    LicenseUtilization = 11,
    HighRiskFindings = 12
}

public static class InventoryDomainExtensions
{
    public static string GetDisplayName(this InventoryDomain domain) => domain switch
    {
        InventoryDomain.TenantBaseline => "Tenant & Org Baseline",
        InventoryDomain.IdentityAccess => "Identity & Access (Entra ID)",
        InventoryDomain.DeviceEndpoint => "Device & Endpoint (Intune)",
        InventoryDomain.DefenderXDR => "Microsoft Defender XDR",
        InventoryDomain.EmailExchange => "Email & Exchange Online",
        InventoryDomain.DataProtection => "Data Protection (Purview)",
        InventoryDomain.SharePointOneDriveTeams => "SharePoint, OneDrive & Teams",
        InventoryDomain.ApplicationsOAuth => "Applications & OAuth",
        InventoryDomain.LogsMonitoring => "Logs & Monitoring",
        InventoryDomain.SecureScore => "Secure Score & Posture",
        InventoryDomain.LicenseUtilization => "License Utilization",
        InventoryDomain.HighRiskFindings => "High-Risk Findings",
        _ => domain.ToString()
    };

    public static string GetDescription(this InventoryDomain domain) => domain switch
    {
        InventoryDomain.TenantBaseline => "Tenant ID, domains, subscriptions, service health, org-wide settings",
        InventoryDomain.IdentityAccess => "Users, groups, roles, CA policies, MFA status, authentication methods",
        InventoryDomain.DeviceEndpoint => "Device inventory, compliance policies, encryption status",
        InventoryDomain.DefenderXDR => "Defender for Endpoint, Office 365, Identity, and Cloud Apps",
        InventoryDomain.EmailExchange => "Mail flow, transport rules, forwarding, mailbox settings",
        InventoryDomain.DataProtection => "Sensitivity labels, DLP policies, compliance features",
        InventoryDomain.SharePointOneDriveTeams => "Sites, sharing settings, Teams configuration",
        InventoryDomain.ApplicationsOAuth => "Enterprise apps, OAuth consents, high-privilege permissions",
        InventoryDomain.LogsMonitoring => "Audit log settings, SIEM integration, alerting",
        InventoryDomain.SecureScore => "Secure Score metrics, improvement actions",
        InventoryDomain.LicenseUtilization => "E5 utilization, feature enablement, value leakage",
        InventoryDomain.HighRiskFindings => "Auto-generated high-risk security findings",
        _ => string.Empty
    };

    public static string GetIconClass(this InventoryDomain domain) => domain switch
    {
        InventoryDomain.TenantBaseline => "fas fa-building",
        InventoryDomain.IdentityAccess => "fas fa-user-shield",
        InventoryDomain.DeviceEndpoint => "fas fa-laptop",
        InventoryDomain.DefenderXDR => "fas fa-shield-alt",
        InventoryDomain.EmailExchange => "fas fa-envelope",
        InventoryDomain.DataProtection => "fas fa-lock",
        InventoryDomain.SharePointOneDriveTeams => "fas fa-share-alt",
        InventoryDomain.ApplicationsOAuth => "fas fa-puzzle-piece",
        InventoryDomain.LogsMonitoring => "fas fa-clipboard-list",
        InventoryDomain.SecureScore => "fas fa-chart-line",
        InventoryDomain.LicenseUtilization => "fas fa-id-card",
        InventoryDomain.HighRiskFindings => "fas fa-exclamation-triangle",
        _ => "fas fa-circle"
    };

    public static string GetMudIcon(this InventoryDomain domain) => domain switch
    {
        InventoryDomain.TenantBaseline => "Business",
        InventoryDomain.IdentityAccess => "VerifiedUser",
        InventoryDomain.DeviceEndpoint => "Devices",
        InventoryDomain.DefenderXDR => "Security",
        InventoryDomain.EmailExchange => "Email",
        InventoryDomain.DataProtection => "Lock",
        InventoryDomain.SharePointOneDriveTeams => "Share",
        InventoryDomain.ApplicationsOAuth => "Apps",
        InventoryDomain.LogsMonitoring => "Assessment",
        InventoryDomain.SecureScore => "Timeline",
        InventoryDomain.LicenseUtilization => "CardMembership",
        InventoryDomain.HighRiskFindings => "Warning",
        _ => "Circle"
    };

    public static string GetColor(this InventoryDomain domain) => domain switch
    {
        InventoryDomain.TenantBaseline => "#51627A",
        InventoryDomain.IdentityAccess => "#1976D2",
        InventoryDomain.DeviceEndpoint => "#7B1FA2",
        InventoryDomain.DefenderXDR => "#D32F2F",
        InventoryDomain.EmailExchange => "#00796B",
        InventoryDomain.DataProtection => "#E64A19",
        InventoryDomain.SharePointOneDriveTeams => "#0288D1",
        InventoryDomain.ApplicationsOAuth => "#689F38",
        InventoryDomain.LogsMonitoring => "#5D4037",
        InventoryDomain.SecureScore => "#FFA000",
        InventoryDomain.LicenseUtilization => "#512DA8",
        InventoryDomain.HighRiskFindings => "#C62828",
        _ => "#757575"
    };
}
