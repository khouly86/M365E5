using ClosedXML.Excel;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class InventoryExportService : IInventoryExportService
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryExportService> _logger;

    private const string PrimaryColor = "#E0FC8E";
    private const string SecondaryColor = "#51627A";

    static InventoryExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InventoryExportService(
        IInventoryService inventoryService,
        ILogger<InventoryExportService> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    #region Users Export

    public async Task<byte[]> ExportUsersToExcelAsync(Guid tenantId, UserInventoryFilter filter, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting users to Excel for tenant {TenantId}", tenantId);

        filter.Page = 1;
        filter.PageSize = 50000;
        var result = await _inventoryService.GetUsersAsync(tenantId, filter, null, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Users");

        // Headers
        var headers = new[] { "Display Name", "UPN", "Email", "Type", "Status", "MFA", "Risk", "Privileged", "License", "Department", "Last Sign-In" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var user in result.Items)
        {
            sheet.Cell(row, 1).Value = user.DisplayName ?? "";
            sheet.Cell(row, 2).Value = user.UserPrincipalName ?? "";
            sheet.Cell(row, 3).Value = user.Mail ?? "";
            sheet.Cell(row, 4).Value = user.UserType ?? "";
            sheet.Cell(row, 5).Value = user.AccountEnabled ? "Active" : "Disabled";
            sheet.Cell(row, 6).Value = user.IsMfaRegistered ? "Yes" : "No";
            sheet.Cell(row, 7).Value = user.RiskLevel ?? "None";
            sheet.Cell(row, 8).Value = user.IsPrivileged ? "Yes" : "No";
            sheet.Cell(row, 9).Value = user.PrimaryLicenseCategoryName ?? "Unlicensed";
            sheet.Cell(row, 10).Value = user.Department ?? "";
            sheet.Cell(row, 11).Value = user.LastSignInDateTime?.ToString("yyyy-MM-dd") ?? "Never";

            if (user.RiskLevel?.ToLower() == "high")
                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFEBEE");
            else if (!user.IsMfaRegistered && user.UserType == "Member")
                sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3E0");

            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportUsersToPdfAsync(Guid tenantId, UserInventoryFilter filter, string tenantName, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await _inventoryService.GetUsersAsync(tenantId, filter, null, ct);

        return GeneratePdf("User Inventory", tenantName, result.TotalCount,
            new[] { "Name", "UPN", "Type", "Status", "MFA", "Risk", "License" },
            result.Items.Select(u => new[] {
                u.DisplayName ?? "", u.UserPrincipalName ?? "", u.UserType ?? "",
                u.AccountEnabled ? "Active" : "Disabled", u.IsMfaRegistered ? "Yes" : "No",
                u.RiskLevel ?? "None", u.PrimaryLicenseCategoryName ?? "Unlicensed"
            }).ToList());
    }

    #endregion

    #region Groups Export

    public async Task<byte[]> ExportGroupsToExcelAsync(Guid tenantId, GroupInventoryFilter filter, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting groups to Excel for tenant {TenantId}", tenantId);

        filter.Page = 1;
        filter.PageSize = 50000;
        var result = await _inventoryService.GetGroupsAsync(tenantId, filter, null, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Groups");

        var headers = new[] { "Display Name", "Type", "Mail", "Members", "Owners", "Dynamic", "Role-Assignable", "External Members" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var group in result.Items)
        {
            sheet.Cell(row, 1).Value = group.DisplayName ?? "";
            sheet.Cell(row, 2).Value = group.GroupType ?? "";
            sheet.Cell(row, 3).Value = group.Mail ?? "";
            sheet.Cell(row, 4).Value = group.MemberCount;
            sheet.Cell(row, 5).Value = group.OwnerCount;
            sheet.Cell(row, 6).Value = group.IsDynamicMembership ? "Yes" : "No";
            sheet.Cell(row, 7).Value = group.IsRoleAssignable ? "Yes" : "No";
            sheet.Cell(row, 8).Value = group.HasExternalMembers ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportGroupsToPdfAsync(Guid tenantId, GroupInventoryFilter filter, string tenantName, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await _inventoryService.GetGroupsAsync(tenantId, filter, null, ct);

        return GeneratePdf("Group Inventory", tenantName, result.TotalCount,
            new[] { "Name", "Type", "Mail", "Members", "Owners", "Dynamic" },
            result.Items.Select(g => new[] {
                g.DisplayName ?? "", g.GroupType ?? "", g.Mail ?? "",
                g.MemberCount.ToString(), g.OwnerCount.ToString(), g.IsDynamicMembership ? "Yes" : "No"
            }).ToList());
    }

    #endregion

    #region Devices Export

    public async Task<byte[]> ExportDevicesToExcelAsync(Guid tenantId, DeviceInventoryFilter filter, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting devices to Excel for tenant {TenantId}", tenantId);

        filter.Page = 1;
        filter.PageSize = 50000;
        var result = await _inventoryService.GetDevicesAsync(tenantId, filter, null, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Devices");

        var headers = new[] { "Device Name", "OS", "OS Version", "Owner Type", "Compliant", "Managed", "Encrypted", "Azure AD Joined" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var device in result.Items)
        {
            sheet.Cell(row, 1).Value = device.DeviceName ?? "";
            sheet.Cell(row, 2).Value = device.OperatingSystem ?? "";
            sheet.Cell(row, 3).Value = device.OsVersion ?? "";
            sheet.Cell(row, 4).Value = device.OwnerType ?? "";
            sheet.Cell(row, 5).Value = device.ComplianceState ?? "";
            sheet.Cell(row, 6).Value = device.IsManaged ? "Yes" : "No";
            sheet.Cell(row, 7).Value = device.IsEncrypted ? "Yes" : "No";
            sheet.Cell(row, 8).Value = device.IsAzureAdJoined ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportDevicesToPdfAsync(Guid tenantId, DeviceInventoryFilter filter, string tenantName, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await _inventoryService.GetDevicesAsync(tenantId, filter, null, ct);

        return GeneratePdf("Device Inventory", tenantName, result.TotalCount,
            new[] { "Device", "OS", "Version", "Owner", "Compliant", "Managed", "Encrypted" },
            result.Items.Select(d => new[] {
                d.DeviceName ?? "", d.OperatingSystem ?? "", d.OsVersion ?? "",
                d.OwnerType ?? "", d.ComplianceState ?? "", d.IsManaged ? "Yes" : "No", d.IsEncrypted ? "Yes" : "No"
            }).ToList());
    }

    #endregion

    #region Applications Export

    public async Task<byte[]> ExportAppsToExcelAsync(Guid tenantId, AppInventoryFilter filter, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting apps to Excel for tenant {TenantId}", tenantId);

        filter.Page = 1;
        filter.PageSize = 50000;
        var result = await _inventoryService.GetAppsAsync(tenantId, filter, null, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Applications");

        var headers = new[] { "Display Name", "App ID", "Publisher", "Status", "Microsoft App", "Verified", "High Privilege" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var app in result.Items)
        {
            sheet.Cell(row, 1).Value = app.DisplayName ?? "";
            sheet.Cell(row, 2).Value = app.AppId ?? "";
            sheet.Cell(row, 3).Value = app.PublisherName ?? "";
            sheet.Cell(row, 4).Value = app.AccountEnabled ? "Enabled" : "Disabled";
            sheet.Cell(row, 5).Value = app.IsMicrosoftApp ? "Yes" : "No";
            sheet.Cell(row, 6).Value = app.IsVerifiedPublisher ? "Yes" : "No";
            sheet.Cell(row, 7).Value = app.HasHighPrivilegePermissions ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportAppsToPdfAsync(Guid tenantId, AppInventoryFilter filter, string tenantName, CancellationToken ct = default)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await _inventoryService.GetAppsAsync(tenantId, filter, null, ct);

        return GeneratePdf("Application Inventory", tenantName, result.TotalCount,
            new[] { "Name", "App ID", "Publisher", "Status", "MS App", "High Priv" },
            result.Items.Select(a => new[] {
                a.DisplayName ?? "", a.AppId ?? "", a.PublisherName ?? "",
                a.AccountEnabled ? "Enabled" : "Disabled", a.IsMicrosoftApp ? "Yes" : "No", a.HasHighPrivilegePermissions ? "Yes" : "No"
            }).ToList());
    }

    #endregion

    #region Roles Export

    public async Task<byte[]> ExportRolesToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting roles to Excel for tenant {TenantId}", tenantId);

        var roles = await _inventoryService.GetDirectoryRolesAsync(tenantId, snapshotId, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Directory Roles");

        var headers = new[] { "Role Name", "Description", "Is Privileged", "Built-In", "User Members", "SP Members", "Total Members" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var role in roles)
        {
            sheet.Cell(row, 1).Value = role.DisplayName ?? "";
            sheet.Cell(row, 2).Value = role.Description ?? "";
            sheet.Cell(row, 3).Value = role.IsPrivileged ? "Yes" : "No";
            sheet.Cell(row, 4).Value = role.IsBuiltIn ? "Yes" : "No";
            sheet.Cell(row, 5).Value = role.UserMemberCount;
            sheet.Cell(row, 6).Value = role.ServicePrincipalMemberCount;
            sheet.Cell(row, 7).Value = role.MemberCount;
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportRolesToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default)
    {
        var roles = await _inventoryService.GetDirectoryRolesAsync(tenantId, snapshotId, ct);

        return GeneratePdf("Directory Roles", tenantName, roles.Count,
            new[] { "Role Name", "Privileged", "Built-In", "Users", "SPs", "Total" },
            roles.Select(r => new[] {
                r.DisplayName ?? "", r.IsPrivileged ? "Yes" : "No", r.IsBuiltIn ? "Yes" : "No",
                r.UserMemberCount.ToString(), r.ServicePrincipalMemberCount.ToString(),
                r.MemberCount.ToString()
            }).ToList());
    }

    #endregion

    #region Conditional Access Export

    public async Task<byte[]> ExportCAPolicesToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting CA policies to Excel for tenant {TenantId}", tenantId);

        var policies = await _inventoryService.GetConditionalAccessPoliciesAsync(tenantId, snapshotId, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("CA Policies");

        var headers = new[] { "Policy Name", "State", "Requires MFA", "Blocks Legacy Auth", "All Users", "All Apps" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var policy in policies)
        {
            sheet.Cell(row, 1).Value = policy.DisplayName ?? "";
            sheet.Cell(row, 2).Value = policy.State ?? "";
            sheet.Cell(row, 3).Value = policy.RequiresMfa ? "Yes" : "No";
            sheet.Cell(row, 4).Value = policy.BlocksLegacyAuth ? "Yes" : "No";
            sheet.Cell(row, 5).Value = policy.IncludeAllUsers ? "Yes" : "No";
            sheet.Cell(row, 6).Value = policy.IncludeAllApplications ? "Yes" : "No";
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportCAPoliciesToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default)
    {
        var policies = await _inventoryService.GetConditionalAccessPoliciesAsync(tenantId, snapshotId, ct);

        return GeneratePdf("Conditional Access Policies", tenantName, policies.Count,
            new[] { "Policy Name", "State", "MFA", "Block Legacy", "All Users", "All Apps" },
            policies.Select(p => new[] {
                p.DisplayName ?? "", p.State ?? "", p.RequiresMfa ? "Yes" : "No",
                p.BlocksLegacyAuth ? "Yes" : "No", p.IncludeAllUsers ? "Yes" : "No", p.IncludeAllApplications ? "Yes" : "No"
            }).ToList());
    }

    #endregion

    #region Service Principals Export

    public async Task<byte[]> ExportServicePrincipalsToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting service principals to Excel for tenant {TenantId}", tenantId);

        var sps = await _inventoryService.GetServicePrincipalsAsync(tenantId, snapshotId, ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Service Principals");

        var headers = new[] { "Display Name", "App ID", "Type", "Microsoft App", "High Privilege", "App Permissions", "Delegated Permissions" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var sp in sps)
        {
            sheet.Cell(row, 1).Value = sp.DisplayName ?? "";
            sheet.Cell(row, 2).Value = sp.AppId ?? "";
            sheet.Cell(row, 3).Value = sp.ServicePrincipalType ?? "";
            sheet.Cell(row, 4).Value = sp.IsMicrosoftFirstParty ? "Yes" : "No";
            sheet.Cell(row, 5).Value = sp.HasHighPrivilegePermissions ? "Yes" : "No";
            sheet.Cell(row, 6).Value = sp.ApplicationPermissions.Count;
            sheet.Cell(row, 7).Value = sp.DelegatedPermissions.Count;
            row++;
        }

        sheet.Columns().AdjustToContents();
        sheet.RangeUsed()?.SetAutoFilter();
        sheet.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportServicePrincipalsToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default)
    {
        var sps = await _inventoryService.GetServicePrincipalsAsync(tenantId, snapshotId, ct);

        return GeneratePdf("Service Principals", tenantName, sps.Count,
            new[] { "Name", "App ID", "Type", "MS App", "High Priv", "App Perms" },
            sps.Select(s => new[] {
                s.DisplayName ?? "", s.AppId ?? "", s.ServicePrincipalType ?? "",
                s.IsMicrosoftFirstParty ? "Yes" : "No", s.HasHighPrivilegePermissions ? "Yes" : "No",
                s.ApplicationPermissions.Count.ToString()
            }).ToList());
    }

    #endregion

    #region PDF Helper

    private byte[] GeneratePdf(string title, string tenantName, int totalItems, string[] headers, List<string[]> rows)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                // Header
                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(title).FontSize(18).Bold().FontColor(Colors.Grey.Darken3);
                            col.Item().Text(tenantName).FontSize(11).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(150).Column(col =>
                        {
                            col.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                            col.Item().AlignRight().Text($"Total: {totalItems:N0}").FontSize(10).Bold();
                        });
                    });
                    column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // Content
                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in headers)
                            columns.RelativeColumn();
                    });

                    // Header row
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Grey.Darken3).Padding(5)
                                .Text(h).FontColor(Colors.White).FontSize(8).Bold();
                        }
                    });

                    // Data rows
                    bool alternate = false;
                    foreach (var row in rows)
                    {
                        var bgColor = alternate ? Colors.Grey.Lighten4 : Colors.White;
                        foreach (var cell in row)
                        {
                            table.Cell().Background(bgColor).Padding(4).Text(cell).FontSize(8);
                        }
                        alternate = !alternate;
                    }
                });

                // Footer
                page.Footer().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(5).Row(footerRow =>
                        {
                            footerRow.RelativeItem().Text("Cloudativ Assessment Tool").FontSize(8).FontColor(Colors.Grey.Medium);
                            footerRow.RelativeItem().AlignRight().Text(text =>
                            {
                                text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                                text.CurrentPageNumber().FontSize(8);
                                text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                                text.TotalPages().FontSize(8);
                            });
                        });
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    #endregion
}
