using ClosedXML.Excel;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Infrastructure.Services.Export;
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
        var items = result.Items;
        var total = result.TotalCount;

        var mfaYes = items.Count(u => u.IsMfaRegistered);
        var mfaNo = items.Count - mfaYes;
        var privileged = items.Count(u => u.IsPrivileged);
        var highRisk = items.Count(u => u.RiskLevel?.ToLower() == "high");
        var members = items.Count(u => u.UserType == "Member");
        var guests = items.Count(u => u.UserType == "Guest");
        var active = items.Count(u => u.AccountEnabled);
        var disabled = items.Count - active;
        var mfaPct = total > 0 ? mfaYes * 100 / total : 0;

        // License distribution
        var licenseGroups = items.GroupBy(u => string.IsNullOrEmpty(u.PrimaryLicenseCategoryName) ? "Unlicensed" : u.PrimaryLicenseCategoryName)
            .Select(g => new BarChartItem(g.Key, g.Count(), PdfReportComponents.ChartPalette[0]))
            .OrderByDescending(b => b.Value).ToList();
        for (int i = 0; i < licenseGroups.Count; i++)
            licenseGroups[i] = licenseGroups[i] with { Color = PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length] };

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "User Inventory Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    // Stat cards
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Users", total.ToString("N0")),
                        new("MFA Registered", $"{mfaPct}%", $"{mfaYes:N0} of {total:N0}",
                            mfaPct >= 80 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor),
                        new("Privileged Users", privileged.ToString("N0"), null,
                            privileged > 10 ? PdfReportComponents.WarningColor : null),
                        new("High Risk Users", highRisk.ToString("N0"), null,
                            highRisk > 0 ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor)
                    }));

                    column.Item().PaddingTop(15);

                    // Charts row 1
                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Members", members, "#51627A"),
                            new("Guests", guests, "#9C27B0")
                        }, "Total", total.ToString("N0"), "User Type Distribution"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Registered", mfaYes, PdfReportComponents.SuccessColor),
                            new("Not Registered", mfaNo, PdfReportComponents.DangerColor)
                        }, "MFA", $"{mfaPct}%", "MFA Registration Status")
                    ));

                    column.Item().PaddingTop(10);

                    // Charts row 2
                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Active", active, PdfReportComponents.SuccessColor),
                            new("Disabled", disabled, PdfReportComponents.DangerColor)
                        }, "Status", active.ToString("N0"), "Account Status"),
                        right => PdfChartHelper.HorizontalBarChart(right, licenseGroups,
                            "License Distribution", 6)
                    ));

                    column.Item().PaddingTop(15);

                    // Data table
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed User Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Name", "UPN", "Type", "Status", "MFA", "Risk", "License" },
                        items.Select(u => new[] {
                            u.DisplayName ?? "", u.UserPrincipalName ?? "", u.UserType ?? "",
                            u.AccountEnabled ? "Active" : "Disabled", u.IsMfaRegistered ? "Yes" : "No",
                            u.RiskLevel ?? "None", u.PrimaryLicenseCategoryName ?? "Unlicensed"
                        }).ToList(),
                        new[] { 3, 4, 5 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var items = result.Items;
        var total = result.TotalCount;

        var security = items.Count(g => g.IsSecurityGroup);
        var m365 = items.Count(g => g.IsMicrosoft365Group);
        var external = items.Count(g => g.HasExternalMembers);
        var dynamic = items.Count(g => g.IsDynamicMembership);
        var staticG = items.Count - dynamic;

        var typeGroups = items.GroupBy(g => g.GroupType ?? "Unknown")
            .Select(g => new ChartSegment(g.Key, g.Count(), ""))
            .OrderByDescending(s => s.Value).ToList();
        for (int i = 0; i < typeGroups.Count; i++)
            typeGroups[i] = typeGroups[i] with { Color = PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length] };

        var topGroups = items.OrderByDescending(g => g.MemberCount).Take(8)
            .Select((g, i) => new BarChartItem(g.DisplayName ?? "N/A", g.MemberCount,
                PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length])).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Group Inventory Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Groups", total.ToString("N0")),
                        new("Security Groups", security.ToString("N0")),
                        new("M365 Groups", m365.ToString("N0")),
                        new("External Members", external.ToString("N0"), null,
                            external > 0 ? PdfReportComponents.WarningColor : PdfReportComponents.SuccessColor)
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, typeGroups,
                            "Total", total.ToString("N0"), "Group Type Distribution"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Dynamic", dynamic, "#0078D4"),
                            new("Static", staticG, "#51627A")
                        }, "Type", null, "Membership Type")
                    ));

                    if (topGroups.Any())
                    {
                        column.Item().PaddingTop(10);
                        column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(10)
                            .Element(c => PdfChartHelper.HorizontalBarChart(c, topGroups, "Top Groups by Member Count", 8));
                    }

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Group Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Name", "Type", "Mail", "Members", "Owners", "Dynamic" },
                        items.Select(g => new[] {
                            g.DisplayName ?? "", g.GroupType ?? "", g.Mail ?? "",
                            g.MemberCount.ToString(), g.OwnerCount.ToString(),
                            g.IsDynamicMembership ? "Yes" : "No"
                        }).ToList(),
                        new[] { 5 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var items = result.Items;
        var total = result.TotalCount;

        var compliant = items.Count(d => d.ComplianceState?.ToLower() == "compliant");
        var managed = items.Count(d => d.IsManaged);
        var encrypted = items.Count(d => d.IsEncrypted);
        var compPct = total > 0 ? compliant * 100 / total : 0;
        var mgdPct = total > 0 ? managed * 100 / total : 0;
        var encPct = total > 0 ? encrypted * 100 / total : 0;

        var osGroups = items.GroupBy(d => string.IsNullOrEmpty(d.OperatingSystem) ? "Unknown" : d.OperatingSystem)
            .Select(g => new ChartSegment(g.Key, g.Count(), ""))
            .OrderByDescending(s => s.Value).ToList();
        for (int i = 0; i < osGroups.Count; i++)
            osGroups[i] = osGroups[i] with { Color = PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length] };

        var nonCompliant = total - compliant;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Device Inventory Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Devices", total.ToString("N0")),
                        new("Compliant", $"{compPct}%", $"{compliant:N0} of {total:N0}",
                            compPct >= 80 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor),
                        new("Managed", $"{mgdPct}%", $"{managed:N0} of {total:N0}",
                            mgdPct >= 80 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor),
                        new("Encrypted", $"{encPct}%", $"{encrypted:N0} of {total:N0}",
                            encPct >= 80 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor)
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, osGroups,
                            "Total", total.ToString("N0"), "OS Distribution"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Compliant", compliant, PdfReportComponents.SuccessColor),
                            new("Non-Compliant", nonCompliant, PdfReportComponents.DangerColor)
                        }, "Status", $"{compPct}%", "Compliance Status")
                    ));

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Device Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Device", "OS", "Version", "Owner", "Compliant", "Managed", "Encrypted" },
                        items.Select(d => new[] {
                            d.DeviceName ?? "", d.OperatingSystem ?? "", d.OsVersion ?? "",
                            d.OwnerType ?? "", d.ComplianceState ?? "",
                            d.IsManaged ? "Yes" : "No", d.IsEncrypted ? "Yes" : "No"
                        }).ToList(),
                        new[] { 4, 5, 6 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var items = result.Items;
        var total = result.TotalCount;

        var msApps = items.Count(a => a.IsMicrosoftApp);
        var thirdParty = total - msApps;
        var highPriv = items.Count(a => a.HasHighPrivilegePermissions);
        var verified = items.Count(a => a.IsVerifiedPublisher);
        var unverified = total - verified;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Application Inventory Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Apps", total.ToString("N0")),
                        new("Microsoft Apps", msApps.ToString("N0")),
                        new("Third-Party", thirdParty.ToString("N0")),
                        new("High Privilege", highPriv.ToString("N0"), null,
                            highPriv > 0 ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor)
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Microsoft", msApps, "#0078D4"),
                            new("Third-Party", thirdParty, "#EF7348")
                        }, "Apps", total.ToString("N0"), "App Origin"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Verified", verified, PdfReportComponents.SuccessColor),
                            new("Unverified", unverified, PdfReportComponents.WarningColor)
                        }, "Trust", null, "Publisher Verification")
                    ));

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Application Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Name", "App ID", "Publisher", "Status", "MS App", "High Priv" },
                        items.Select(a => new[] {
                            a.DisplayName ?? "", a.AppId ?? "", a.PublisherName ?? "",
                            a.AccountEnabled ? "Enabled" : "Disabled",
                            a.IsMicrosoftApp ? "Yes" : "No",
                            a.HasHighPrivilegePermissions ? "Yes" : "No"
                        }).ToList(),
                        new[] { 3, 4, 5 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var total = roles.Count;

        var privileged = roles.Count(r => r.IsPrivileged);
        var nonPriv = total - privileged;
        var builtIn = roles.Count(r => r.IsBuiltIn);
        var custom = total - builtIn;
        var totalMembers = roles.Sum(r => r.MemberCount);

        var topRoles = roles.OrderByDescending(r => r.MemberCount).Take(8)
            .Select((r, i) => new BarChartItem(r.DisplayName ?? "N/A", r.MemberCount,
                PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length])).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Directory Roles Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Roles", total.ToString("N0")),
                        new("Privileged Roles", privileged.ToString("N0"), null,
                            privileged > 0 ? PdfReportComponents.WarningColor : null),
                        new("Total Members", totalMembers.ToString("N0")),
                        new("Custom Roles", custom.ToString("N0"))
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Privileged", privileged, PdfReportComponents.DangerColor),
                            new("Non-Privileged", nonPriv, PdfReportComponents.SuccessColor)
                        }, "Roles", total.ToString("N0"), "Privilege Level"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Built-In", builtIn, "#51627A"),
                            new("Custom", custom, "#EF7348")
                        }, "Type", null, "Role Origin")
                    ));

                    if (topRoles.Any())
                    {
                        column.Item().PaddingTop(10);
                        column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(10)
                            .Element(c => PdfChartHelper.HorizontalBarChart(c, topRoles, "Top Roles by Member Count", 8));
                    }

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Role Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Role Name", "Privileged", "Built-In", "Users", "SPs", "Total" },
                        roles.Select(r => new[] {
                            r.DisplayName ?? "", r.IsPrivileged ? "Yes" : "No", r.IsBuiltIn ? "Yes" : "No",
                            r.UserMemberCount.ToString(), r.ServicePrincipalMemberCount.ToString(),
                            r.MemberCount.ToString()
                        }).ToList(),
                        new[] { 1, 2 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var total = policies.Count;

        var enabled = policies.Count(p => p.State?.ToLower() == "enabled");
        var disabled = policies.Count(p => p.State?.ToLower() == "disabled");
        var reportOnly = total - enabled - disabled;
        var mfaEnforcing = policies.Count(p => p.RequiresMfa);
        var blockLegacy = policies.Count(p => p.BlocksLegacyAuth);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Conditional Access Policies Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Policies", total.ToString("N0")),
                        new("Enabled", enabled.ToString("N0"), null, PdfReportComponents.SuccessColor),
                        new("MFA Enforcing", mfaEnforcing.ToString("N0"), null, PdfReportComponents.InfoColor),
                        new("Block Legacy Auth", blockLegacy.ToString("N0"))
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Enabled", enabled, PdfReportComponents.SuccessColor),
                            new("Report-Only", reportOnly, PdfReportComponents.InfoColor),
                            new("Disabled", disabled, PdfReportComponents.DangerColor)
                        }, "State", total.ToString("N0"), "Policy State"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("Requires MFA", mfaEnforcing, PdfReportComponents.SuccessColor),
                            new("No MFA", total - mfaEnforcing, "#D1D5DB")
                        }, "MFA", $"{(total > 0 ? mfaEnforcing * 100 / total : 0)}%", "MFA Enforcement")
                    ));

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Policy Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Policy Name", "State", "MFA", "Block Legacy", "All Users", "All Apps" },
                        policies.Select(p => new[] {
                            p.DisplayName ?? "", p.State ?? "", p.RequiresMfa ? "Yes" : "No",
                            p.BlocksLegacyAuth ? "Yes" : "No", p.IncludeAllUsers ? "Yes" : "No",
                            p.IncludeAllApplications ? "Yes" : "No"
                        }).ToList(),
                        new[] { 1, 2, 3 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
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
        var total = sps.Count;

        var msFirst = sps.Count(s => s.IsMicrosoftFirstParty);
        var thirdParty = total - msFirst;
        var highPriv = sps.Count(s => s.HasHighPrivilegePermissions);

        var topSps = sps.OrderByDescending(s => s.ApplicationPermissions.Count + s.DelegatedPermissions.Count)
            .Take(8)
            .Select((s, i) => new BarChartItem(s.DisplayName ?? "N/A",
                s.ApplicationPermissions.Count + s.DelegatedPermissions.Count,
                PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length])).ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c, "Service Principals Report", tenantName, total));

                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Service Principals", total.ToString("N0")),
                        new("Microsoft First-Party", msFirst.ToString("N0")),
                        new("Third-Party", thirdParty.ToString("N0")),
                        new("High Privilege", highPriv.ToString("N0"), null,
                            highPriv > 0 ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor)
                    }));

                    column.Item().PaddingTop(15);

                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Microsoft", msFirst, "#0078D4"),
                            new("Third-Party", thirdParty, "#EF7348")
                        }, "Origin", total.ToString("N0"), "Service Principal Origin"),
                        right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                        {
                            new("High Privilege", highPriv, PdfReportComponents.DangerColor),
                            new("Normal", total - highPriv, PdfReportComponents.SuccessColor)
                        }, "Access", null, "Privilege Level")
                    ));

                    if (topSps.Any())
                    {
                        column.Item().PaddingTop(10);
                        column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(10)
                            .Element(c => PdfChartHelper.HorizontalBarChart(c, topSps,
                                "Top Service Principals by Permission Count", 8));
                    }

                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Service Principal Data"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Name", "App ID", "Type", "MS App", "High Priv", "App Perms" },
                        sps.Select(s => new[] {
                            s.DisplayName ?? "", s.AppId ?? "", s.ServicePrincipalType ?? "",
                            s.IsMicrosoftFirstParty ? "Yes" : "No",
                            s.HasHighPrivilegePermissions ? "Yes" : "No",
                            s.ApplicationPermissions.Count.ToString()
                        }).ToList(),
                        new[] { 3, 4 }));
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    #endregion

    #region License Utilization Export

    public async Task<byte[]> ExportLicensesToExcelAsync(Guid tenantId, CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting license utilization to Excel for tenant {TenantId}", tenantId);

        var utilization = await _inventoryService.GetMultiLicenseUtilizationAsync(tenantId, null, ct);
        var subscriptions = await _inventoryService.GetLicenseSubscriptionsAsync(tenantId, null, ct);

        using var workbook = new XLWorkbook();

        // Sheet 1: Overview by Category
        var summarySheet = workbook.Worksheets.Add("License Summary");
        var sumHeaders = new[] { "Category", "Tier", "Total", "Assigned", "Available", "Utilization %", "Monthly Cost", "Est. Waste" };
        for (int i = 0; i < sumHeaders.Length; i++)
        {
            var cell = summarySheet.Cell(1, i + 1);
            cell.Value = sumHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var cat in utilization.Distribution.ByCategory)
        {
            summarySheet.Cell(row, 1).Value = cat.DisplayName;
            summarySheet.Cell(row, 2).Value = cat.TierGroup ?? "";
            summarySheet.Cell(row, 3).Value = cat.TotalLicenses;
            summarySheet.Cell(row, 4).Value = cat.AssignedLicenses;
            summarySheet.Cell(row, 5).Value = cat.AvailableLicenses;
            summarySheet.Cell(row, 6).Value = Math.Round(cat.UtilizationPercentage, 1);
            summarySheet.Cell(row, 7).Value = cat.TotalMonthlyCost;
            summarySheet.Cell(row, 7).Style.NumberFormat.Format = "$#,##0.00";
            summarySheet.Cell(row, 8).Value = cat.EstimatedWaste;
            summarySheet.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";

            if (cat.UtilizationPercentage < 50)
                summarySheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEE2E2");
            else if (cat.UtilizationPercentage < 75)
                summarySheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");

            row++;
        }

        summarySheet.Columns().AdjustToContents();
        summarySheet.RangeUsed()?.SetAutoFilter();
        summarySheet.SheetView.FreezeRows(1);

        // Sheet 2: All Subscriptions
        var subSheet = workbook.Worksheets.Add("Subscriptions");
        var subHeaders = new[] { "License Name", "SKU", "Category", "Tier", "Status", "Total", "Consumed", "Available", "Trial", "Price/User/Mo" };
        for (int i = 0; i < subHeaders.Length; i++)
        {
            var cell = subSheet.Cell(1, i + 1);
            cell.Value = subHeaders[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
            cell.Style.Font.FontColor = XLColor.White;
        }

        row = 2;
        foreach (var sub in subscriptions)
        {
            subSheet.Cell(row, 1).Value = sub.DisplayName ?? sub.SkuPartNumber;
            subSheet.Cell(row, 2).Value = sub.SkuPartNumber;
            subSheet.Cell(row, 3).Value = sub.LicenseCategoryDisplayName;
            subSheet.Cell(row, 4).Value = sub.TierGroup ?? "";
            subSheet.Cell(row, 5).Value = sub.CapabilityStatus ?? "";
            subSheet.Cell(row, 6).Value = sub.PrepaidUnits;
            subSheet.Cell(row, 7).Value = sub.ConsumedUnits;
            subSheet.Cell(row, 8).Value = sub.AvailableUnits;
            subSheet.Cell(row, 9).Value = sub.IsTrial ? "Yes" : "No";
            subSheet.Cell(row, 10).Value = sub.EstimatedMonthlyPricePerUser;
            subSheet.Cell(row, 10).Style.NumberFormat.Format = "$#,##0.00";
            row++;
        }

        subSheet.Columns().AdjustToContents();
        subSheet.RangeUsed()?.SetAutoFilter();
        subSheet.SheetView.FreezeRows(1);

        // Sheet 3: Feature Utilization
        if (utilization.UtilizationByCategory.Any())
        {
            var featSheet = workbook.Worksheets.Add("Feature Utilization");
            var featHeaders = new[] { "Category", "Tier", "Total Users", "MFA Users", "CA Users", "Defender Users", "Feature Util %", "Est. Waste/Mo" };
            for (int i = 0; i < featHeaders.Length; i++)
            {
                var cell = featSheet.Cell(1, i + 1);
                cell.Value = featHeaders[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
                cell.Style.Font.FontColor = XLColor.White;
            }

            row = 2;
            foreach (var cat in utilization.UtilizationByCategory)
            {
                featSheet.Cell(row, 1).Value = cat.DisplayName;
                featSheet.Cell(row, 2).Value = cat.TierGroup ?? "";
                featSheet.Cell(row, 3).Value = cat.TotalUsers;
                featSheet.Cell(row, 4).Value = cat.UsersUsingMfa;
                featSheet.Cell(row, 5).Value = cat.UsersUsingCa;
                featSheet.Cell(row, 6).Value = cat.UsersUsingDefender;
                featSheet.Cell(row, 7).Value = Math.Round(cat.OverallFeatureUtilization, 1);
                featSheet.Cell(row, 8).Value = cat.EstimatedMonthlyWaste;
                featSheet.Cell(row, 8).Style.NumberFormat.Format = "$#,##0.00";
                row++;
            }

            featSheet.Columns().AdjustToContents();
            featSheet.RangeUsed()?.SetAutoFilter();
            featSheet.SheetView.FreezeRows(1);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportLicensesToPdfAsync(Guid tenantId, string tenantName, CancellationToken ct = default)
    {
        var utilization = await _inventoryService.GetMultiLicenseUtilizationAsync(tenantId, null, ct);
        var subscriptions = await _inventoryService.GetLicenseSubscriptionsAsync(tenantId, null, ct);
        var dist = utilization.Distribution;

        var totalLic = utilization.TotalLicenses;
        var assigned = utilization.TotalAssigned;
        var assignPct = totalLic > 0 ? (int)(utilization.OverallAssignmentPercentage) : 0;
        var featPct = (int)utilization.OverallUtilization;
        var waste = utilization.TotalEstimatedWaste;

        // License distribution by category for charts
        var categoryBars = dist.ByCategory
            .OrderByDescending(c => c.TotalLicenses)
            .Select((c, i) => new BarChartItem(c.DisplayName, c.TotalLicenses,
                PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length]))
            .ToList();

        var tierSegments = dist.ByTierGroup
            .Where(t => t.TotalLicenses > 0)
            .Select((t, i) => new ChartSegment(t.TierGroup, t.TotalLicenses,
                PdfReportComponents.ChartPalette[i % PdfReportComponents.ChartPalette.Length]))
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => PdfReportComponents.ComposeHeader(c,
                    "License Utilization Report", tenantName, totalLic));

                page.Content().PaddingTop(10).Column(column =>
                {
                    // Stat cards
                    column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Total Licenses", totalLic.ToString("N0")),
                        new("Assignment Rate", $"{assignPct}%", $"{assigned:N0} assigned",
                            assignPct >= 80 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor),
                        new("Feature Utilization", $"{featPct}%", null,
                            featPct >= 60 ? PdfReportComponents.SuccessColor : PdfReportComponents.WarningColor),
                        new("Est. Monthly Waste", $"${waste:N0}", null,
                            waste > 0 ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor)
                    }));

                    column.Item().PaddingTop(15);

                    // Charts: Tier distribution donut + Category bar chart
                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left =>
                        {
                            if (tierSegments.Any())
                                PdfChartHelper.DonutChart(left, tierSegments,
                                    "Total", totalLic.ToString("N0"), "License Distribution by Tier");
                        },
                        right => PdfChartHelper.HorizontalBarChart(right, categoryBars,
                            "Licenses by Category", 8)
                    ));

                    // Assignment vs Available donut + Feature utilization
                    column.Item().PaddingTop(10);
                    column.Item().Element(c => PdfReportComponents.ChartRow(c,
                        left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                        {
                            new("Assigned", assigned, PdfReportComponents.SuccessColor),
                            new("Available", totalLic - assigned, "#D1D5DB")
                        }, "Usage", $"{assignPct}%", "License Assignment"),
                        right =>
                        {
                            // Value leakage summary
                            var noMfa = utilization.TotalUsersWithoutMfa;
                            var noCa = utilization.TotalUsersWithoutCa;
                            var noDef = utilization.TotalUsersWithoutDefender;
                            var leakageBars = new List<BarChartItem>();
                            if (noMfa > 0) leakageBars.Add(new("No MFA", noMfa, PdfReportComponents.DangerColor));
                            if (noCa > 0) leakageBars.Add(new("No Cond. Access", noCa, PdfReportComponents.WarningColor));
                            if (noDef > 0) leakageBars.Add(new("No Defender", noDef, "#EF7348"));
                            if (leakageBars.Any())
                                PdfChartHelper.HorizontalBarChart(right, leakageBars, "Value Leakage (Users)", 4);
                            else
                                right.Text("No value leakage detected").FontSize(9)
                                    .FontColor(PdfReportComponents.SuccessColor);
                        }
                    ));

                    // Category detail table
                    column.Item().PaddingTop(15);
                    column.Item().Element(c => PdfReportComponents.SectionTitle(c, "License Category Details"));
                    column.Item().PaddingTop(5);
                    column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                        new[] { "Category", "Tier", "Total", "Assigned", "Available", "Util %", "Cost/Mo", "Waste/Mo" },
                        dist.ByCategory.Select(cat => new[] {
                            cat.DisplayName, cat.TierGroup ?? "",
                            cat.TotalLicenses.ToString("N0"), cat.AssignedLicenses.ToString("N0"),
                            cat.AvailableLicenses.ToString("N0"), $"{cat.UtilizationPercentage:F0}%",
                            $"${cat.TotalMonthlyCost:N0}", $"${cat.EstimatedWaste:N0}"
                        }).ToList()));

                    // Subscription detail table
                    if (subscriptions.Any())
                    {
                        column.Item().PaddingTop(15);
                        column.Item().Element(c => PdfReportComponents.SectionTitle(c, "License Subscriptions"));
                        column.Item().PaddingTop(5);
                        column.Item().Element(c => PdfReportComponents.ComposeDataTable(c,
                            new[] { "License", "SKU", "Category", "Status", "Total", "Used", "Available" },
                            subscriptions.Select(s => new[] {
                                s.DisplayName ?? s.SkuPartNumber, s.SkuPartNumber,
                                s.LicenseCategoryDisplayName, s.CapabilityStatus ?? "",
                                s.PrepaidUnits.ToString("N0"), s.ConsumedUnits.ToString("N0"),
                                s.AvailableUnits.ToString("N0")
                            }).ToList(),
                            new[] { 3 }));
                    }
                });

                page.Footer().Element(PdfReportComponents.ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    #endregion
}
