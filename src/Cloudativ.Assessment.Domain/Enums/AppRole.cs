namespace Cloudativ.Assessment.Domain.Enums;

public enum AppRole
{
    SuperAdmin = 1,
    TenantAdmin = 2,
    Auditor = 3,
    DomainAdmin = 4
}

public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Auditor = "Auditor";
    public const string DomainAdmin = "DomainAdmin";

    public const string SuperAdminOrTenantAdmin = $"{SuperAdmin},{TenantAdmin}";
    public const string AdminRoles = $"{SuperAdmin},{TenantAdmin},{DomainAdmin}";
    public const string AllRoles = $"{SuperAdmin},{TenantAdmin},{Auditor},{DomainAdmin}";
}
