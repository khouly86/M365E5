using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Infrastructure.Services.Export;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly IAssessmentService _assessmentService;
    private readonly ITenantService _tenantService;
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<ReportService> _logger;

    // Brand colors
    private static readonly string PrimaryColor = "#51627A";
    private static readonly string CriticalColor = "#DC2626";
    private static readonly string HighColor = "#EA580C";
    private static readonly string MediumColor = "#CA8A04";
    private static readonly string LowColor = "#16A34A";

    public ReportService(
        IAssessmentService assessmentService,
        ITenantService tenantService,
        IDashboardService dashboardService,
        ILogger<ReportService> logger)
    {
        _assessmentService = assessmentService;
        _tenantService = tenantService;
        _dashboardService = dashboardService;
        _logger = logger;

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfReportAsync(Guid assessmentRunId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating PDF report for assessment {AssessmentRunId}", assessmentRunId);

        var assessment = await _assessmentService.GetRunByIdAsync(assessmentRunId, cancellationToken);
        if (assessment == null)
            throw new InvalidOperationException($"Assessment run {assessmentRunId} not found");

        var findings = await _assessmentService.GetFindingsAsync(assessmentRunId, null, null, cancellationToken);
        var tenant = await _tenantService.GetByIdAsync(assessment.TenantId, cancellationToken);
        var resourceStats = await _dashboardService.GetResourceStatisticsAsync(assessment.TenantId, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, assessment, tenant?.Name ?? "Unknown"));
                page.Content().Element(c => ComposeContent(c, assessment, findings, resourceStats));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, AssessmentRunDto assessment, string tenantName)
    {
        container.Column(column =>
        {
            // Accent bar
            column.Item().Height(4).Background(PdfReportComponents.SecondaryColor);

            column.Item().PaddingTop(8).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Cloudativ M365 Security Assessment").FontSize(20).Bold()
                        .FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().PaddingTop(2).Text(tenantName).FontSize(11)
                        .FontColor(PdfReportComponents.LightTextColor);
                });
                row.ConstantItem(120).Column(col =>
                {
                    var score = assessment.OverallScore ?? 0;
                    col.Item().AlignRight().Text($"{score}%").FontSize(28).Bold()
                        .FontColor(GetScoreColor(score));
                    col.Item().AlignRight().Text($"Grade {GetGrade(score)}").FontSize(12)
                        .FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().AlignRight().PaddingTop(2)
                        .Text($"{assessment.StartedAt:yyyy-MM-dd HH:mm}").FontSize(8)
                        .FontColor(PdfReportComponents.LightTextColor);
                });
            });

            column.Item().LineHorizontal(1).LineColor(PdfReportComponents.BorderColor);
        });
    }

    private void ComposeContent(IContainer container, AssessmentRunDto assessment, IEnumerable<FindingDto> findings, ResourceStatisticsDto? resourceStats)
    {
        var findingsList = findings.ToList();

        container.Column(column =>
        {
            // Executive Summary with stat cards + charts
            column.Item().Element(c => ComposeExecutiveSummary(c, assessment, findingsList));

            column.Item().PaddingVertical(12);

            // M365 Resources Overview with stat cards
            if (resourceStats != null && resourceStats.TotalUsers > 0)
            {
                column.Item().Element(c => ComposeResourcesOverview(c, resourceStats));
                column.Item().PaddingVertical(12);
            }

            // Domain Scores with bar chart + branded table
            column.Item().Element(c => ComposeDomainScores(c, assessment));

            column.Item().PaddingVertical(12);

            // Findings Summary with charts
            column.Item().Element(c => ComposeFindingsSummary(c, findingsList));

            column.Item().PaddingVertical(12);

            // Detailed Findings with branded cards
            column.Item().Element(c => ComposeDetailedFindings(c, findingsList));
        });
    }

    private void ComposeResourcesOverview(IContainer container, ResourceStatisticsDto resourceStats)
    {
        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "M365 Resources Overview"));
            column.Item().PaddingTop(10);

            // Row 1: Identity & Access stat cards
            column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
            {
                new("Total Users", $"{resourceStats.TotalUsers:N0}", $"{resourceStats.EnabledUsers:N0} enabled", PdfReportComponents.SecondaryColor),
                new("Guest Users", $"{resourceStats.GuestUsers:N0}", null, PdfReportComponents.InfoColor),
                new("Admin Users", $"{resourceStats.AdminUsers:N0}", $"{resourceStats.GlobalAdmins} Global Admins", HighColor),
                new("Risky Users", $"{resourceStats.RiskyUsers}", null, resourceStats.RiskyUsers > 0 ? CriticalColor : LowColor)
            }));

            column.Item().PaddingTop(8);

            // Row 2: Infrastructure stat cards
            column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
            {
                new("Devices", $"{resourceStats.TotalDevices:N0}", $"{resourceStats.ManagedDevices:N0} managed"),
                new("Enterprise Apps", $"{resourceStats.EnterpriseApps:N0}", $"{resourceStats.AppRegistrations:N0} registrations"),
                new("CA Policies", $"{resourceStats.ConditionalAccessPolicies:N0}", $"{resourceStats.EnabledCaPolicies} enabled"),
                new("Groups", $"{resourceStats.TotalGroups:N0}", $"{resourceStats.Microsoft365Groups:N0} M365 groups")
            }));

            column.Item().PaddingTop(8);

            // Row 3: Collaboration stat cards
            column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
            {
                new("Mailboxes", $"{resourceStats.Mailboxes:N0}", $"{resourceStats.SharedMailboxes:N0} shared"),
                new("SharePoint Sites", $"{resourceStats.SharePointSites:N0}"),
                new("Teams", $"{resourceStats.TeamsCount:N0}"),
                new("Licenses", $"{resourceStats.TotalLicenses:N0}", $"{resourceStats.AssignedLicenses:N0} assigned")
            }));
        });
    }

    private void ComposeExecutiveSummary(IContainer container, AssessmentRunDto assessment, List<FindingDto> findings)
    {
        var criticalCount = findings.Count(f => f.Severity == Severity.Critical);
        var highCount = findings.Count(f => f.Severity == Severity.High);
        var mediumCount = findings.Count(f => f.Severity == Severity.Medium);
        var lowCount = findings.Count(f => f.Severity == Severity.Low);
        var score = assessment.OverallScore ?? 0;

        var totalPassed = assessment.DomainScores.Values.Sum(d => d.PassedChecks);
        var totalFailed = assessment.DomainScores.Values.Sum(d => d.FailedChecks);

        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Executive Summary"));
            column.Item().PaddingTop(10);

            // Stat cards row
            column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
            {
                new("Overall Score", $"{score}%", $"Grade {GetGrade(score)}", GetScoreColor(score)),
                new("Domains Assessed", $"{assessment.DomainScores.Count}"),
                new("Total Findings", $"{findings.Count}", $"{criticalCount + highCount} Critical/High",
                    criticalCount + highCount > 0 ? PdfReportComponents.DangerColor : null),
                new("Checks Passed", $"{totalPassed}", $"{totalFailed} failed", PdfReportComponents.SuccessColor)
            }));

            column.Item().PaddingTop(12);

            // Chart Row 1: Findings by Severity donut + Domain Scores bar chart
            column.Item().Element(c => PdfReportComponents.ChartRow(c,
                left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                {
                    new("Critical", criticalCount, CriticalColor),
                    new("High", highCount, HighColor),
                    new("Medium", mediumCount, MediumColor),
                    new("Low", lowCount, LowColor)
                }, "Findings", $"{findings.Count}", "Findings by Severity"),
                right => PdfChartHelper.HorizontalBarChart(right,
                    assessment.DomainScores.Values.Select(d =>
                        new BarChartItem(d.DisplayName, d.Score, GetScoreBarColor(d.Score))).ToList(),
                    "Domain Scores", 8)
            ));

            column.Item().PaddingTop(10);

            // Chart Row 2: Passed vs Failed donut + Findings by Domain bar chart
            var domainFindings = findings
                .GroupBy(f => f.DomainDisplayName)
                .Select(g => new BarChartItem(g.Key, g.Count(), PdfReportComponents.SecondaryColor))
                .OrderByDescending(b => b.Value)
                .ToList();

            column.Item().Element(c => PdfReportComponents.ChartRow(c,
                left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                {
                    new("Passed", totalPassed, PdfReportComponents.SuccessColor),
                    new("Failed", totalFailed, PdfReportComponents.DangerColor)
                }, "Checks", $"{(totalPassed + totalFailed > 0 ? totalPassed * 100 / (totalPassed + totalFailed) : 0)}%", "Compliance Checks"),
                right => PdfChartHelper.HorizontalBarChart(right, domainFindings,
                    "Findings by Domain", 8)
            ));
        });
    }

    private void ComposeDomainScores(IContainer container, AssessmentRunDto assessment)
    {
        var headers = new[] { "Domain", "Score", "Grade", "Critical", "High", "Medium", "Low" };
        var rows = assessment.DomainScores.Values
            .OrderByDescending(d => d.CriticalCount + d.HighCount)
            .Select(d => new[]
            {
                d.DisplayName, $"{d.Score}%", d.Grade,
                $"{d.CriticalCount}", $"{d.HighCount}", $"{d.MediumCount}", $"{d.LowCount}"
            })
            .ToList();

        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Domain Scores"));
            column.Item().PaddingTop(10);
            column.Item().Element(c => PdfReportComponents.ComposeDataTable(c, headers, rows,
                new[] { 3, 4, 5, 6 }));
        });
    }

    private void ComposeFindingsSummary(IContainer container, List<FindingDto> findings)
    {
        var groupedFindings = findings
            .GroupBy(f => f.DomainDisplayName)
            .OrderByDescending(g => g.Count(f => f.Severity == Severity.Critical) + g.Count(f => f.Severity == Severity.High))
            .ToList();

        var headers = new[] { "Domain", "Critical", "High", "Medium", "Low", "Total" };
        var rows = groupedFindings.Select(g => new[]
        {
            g.Key,
            $"{g.Count(f => f.Severity == Severity.Critical)}",
            $"{g.Count(f => f.Severity == Severity.High)}",
            $"{g.Count(f => f.Severity == Severity.Medium)}",
            $"{g.Count(f => f.Severity == Severity.Low)}",
            $"{g.Count()}"
        }).ToList();

        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Findings by Domain"));
            column.Item().PaddingTop(10);
            column.Item().Element(c => PdfReportComponents.ComposeDataTable(c, headers, rows,
                new[] { 1, 2, 3, 4 }));
        });
    }

    private void ComposeDetailedFindings(IContainer container, List<FindingDto> findings)
    {
        var sortedFindings = findings
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.DomainDisplayName)
            .ToList();

        container.Column(column =>
        {
            column.Item().PageBreak();
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Findings"));
            column.Item().PaddingTop(10);

            foreach (var finding in sortedFindings)
            {
                column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(10).Column(findingCol =>
                {
                    findingCol.Item().Row(row =>
                    {
                        row.ConstantItem(70).Element(c => PdfReportComponents.StatusBadge(c, finding.Severity.ToString()));
                        row.ConstantItem(5);
                        row.RelativeItem().Text(finding.Title).FontSize(11).SemiBold()
                            .FontColor(PdfReportComponents.TextColor);
                    });

                    findingCol.Item().PaddingTop(5);
                    findingCol.Item().Text($"Domain: {finding.DomainDisplayName}").FontSize(8)
                        .FontColor(PdfReportComponents.LightTextColor);

                    if (!string.IsNullOrEmpty(finding.Description))
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text("Description:").FontSize(9).SemiBold()
                            .FontColor(PdfReportComponents.TextColor);
                        findingCol.Item().Text(finding.Description).FontSize(9)
                            .FontColor(PdfReportComponents.TextColor);
                    }

                    if (!string.IsNullOrEmpty(finding.Remediation))
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text("Remediation:").FontSize(9).SemiBold()
                            .FontColor(PdfReportComponents.SuccessColor);
                        findingCol.Item().Text(finding.Remediation).FontSize(9)
                            .FontColor(PdfReportComponents.TextColor);
                    }

                    if (finding.AffectedResources.Any())
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text($"Affected Resources: {finding.AffectedResources.Count}").FontSize(8)
                            .FontColor(PdfReportComponents.LightTextColor);
                    }
                });

                column.Item().PaddingTop(8);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        PdfReportComponents.ComposeFooter(container);
    }

    public async Task<byte[]> GenerateExcelReportAsync(Guid assessmentRunId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating Excel report for assessment {AssessmentRunId}", assessmentRunId);

        var assessment = await _assessmentService.GetRunByIdAsync(assessmentRunId, cancellationToken);
        if (assessment == null)
            throw new InvalidOperationException($"Assessment run {assessmentRunId} not found");

        var findings = await _assessmentService.GetFindingsAsync(assessmentRunId, null, null, cancellationToken);
        var tenant = await _tenantService.GetByIdAsync(assessment.TenantId, cancellationToken);
        var resourceStats = await _dashboardService.GetResourceStatisticsAsync(assessment.TenantId, cancellationToken);

        using var workbook = new XLWorkbook();

        // Summary Sheet
        CreateSummarySheet(workbook, assessment, tenant?.Name ?? "Unknown", findings.ToList());

        // M365 Resources Sheet
        if (resourceStats != null && resourceStats.TotalUsers > 0)
            CreateResourcesSheet(workbook, resourceStats);

        // Domain Scores Sheet
        CreateDomainScoresSheet(workbook, assessment);

        // Findings Sheet
        CreateFindingsSheet(workbook, findings.ToList());

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateResourcesSheet(IXLWorkbook workbook, ResourceStatisticsDto resourceStats)
    {
        var ws = workbook.Worksheets.Add("M365 Resources");

        // Title
        ws.Cell("A1").Value = "M365 Resources Overview";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:C1").Merge();

        // Identity & Access Section
        ws.Cell("A3").Value = "Identity & Access";
        ws.Cell("A3").Style.Font.Bold = true;
        ws.Cell("A3").Style.Font.FontSize = 12;
        ws.Cell("A3").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A3").Style.Font.FontColor = XLColor.White;
        ws.Range("A3:C3").Merge();

        ws.Cell("A4").Value = "Total Users";
        ws.Cell("B4").Value = resourceStats.TotalUsers;
        ws.Cell("B4").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A5").Value = "Enabled Users";
        ws.Cell("B5").Value = resourceStats.EnabledUsers;
        ws.Cell("B5").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A6").Value = "Guest Users";
        ws.Cell("B6").Value = resourceStats.GuestUsers;
        ws.Cell("B6").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A7").Value = "Admin Users";
        ws.Cell("B7").Value = resourceStats.AdminUsers;
        ws.Cell("B7").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A8").Value = "Global Admins";
        ws.Cell("B8").Value = resourceStats.GlobalAdmins;
        ws.Cell("B8").Style.Font.FontColor = XLColor.FromHtml(HighColor);

        ws.Cell("A9").Value = "Risky Users";
        ws.Cell("B9").Value = resourceStats.RiskyUsers;
        if (resourceStats.RiskyUsers > 0)
            ws.Cell("B9").Style.Font.FontColor = XLColor.FromHtml(CriticalColor);
        else
            ws.Cell("B9").Style.Font.FontColor = XLColor.FromHtml(LowColor);

        // Groups Section
        ws.Cell("A11").Value = "Groups";
        ws.Cell("A11").Style.Font.Bold = true;
        ws.Cell("A11").Style.Font.FontSize = 12;
        ws.Cell("A11").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A11").Style.Font.FontColor = XLColor.White;
        ws.Range("A11:C11").Merge();

        ws.Cell("A12").Value = "Total Groups";
        ws.Cell("B12").Value = resourceStats.TotalGroups;
        ws.Cell("B12").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A13").Value = "Security Groups";
        ws.Cell("B13").Value = resourceStats.SecurityGroups;
        ws.Cell("B13").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A14").Value = "Microsoft 365 Groups";
        ws.Cell("B14").Value = resourceStats.Microsoft365Groups;
        ws.Cell("B14").Style.NumberFormat.Format = "#,##0";

        // Applications Section
        ws.Cell("A16").Value = "Applications";
        ws.Cell("A16").Style.Font.Bold = true;
        ws.Cell("A16").Style.Font.FontSize = 12;
        ws.Cell("A16").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A16").Style.Font.FontColor = XLColor.White;
        ws.Range("A16:C16").Merge();

        ws.Cell("A17").Value = "Enterprise Apps";
        ws.Cell("B17").Value = resourceStats.EnterpriseApps;
        ws.Cell("B17").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A18").Value = "App Registrations";
        ws.Cell("B18").Value = resourceStats.AppRegistrations;
        ws.Cell("B18").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A19").Value = "High-Risk Apps";
        ws.Cell("B19").Value = resourceStats.HighRiskApps;
        if (resourceStats.HighRiskApps > 0)
            ws.Cell("B19").Style.Font.FontColor = XLColor.FromHtml(CriticalColor);

        // Devices Section
        ws.Cell("A21").Value = "Devices";
        ws.Cell("A21").Style.Font.Bold = true;
        ws.Cell("A21").Style.Font.FontSize = 12;
        ws.Cell("A21").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A21").Style.Font.FontColor = XLColor.White;
        ws.Range("A21:C21").Merge();

        ws.Cell("A22").Value = "Total Devices";
        ws.Cell("B22").Value = resourceStats.TotalDevices;
        ws.Cell("B22").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A23").Value = "Managed Devices";
        ws.Cell("B23").Value = resourceStats.ManagedDevices;
        ws.Cell("B23").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A24").Value = "Compliant Devices";
        ws.Cell("B24").Value = resourceStats.CompliantDevices;
        ws.Cell("B24").Style.NumberFormat.Format = "#,##0";

        // Policies Section
        ws.Cell("A26").Value = "Policies";
        ws.Cell("A26").Style.Font.Bold = true;
        ws.Cell("A26").Style.Font.FontSize = 12;
        ws.Cell("A26").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A26").Style.Font.FontColor = XLColor.White;
        ws.Range("A26:C26").Merge();

        ws.Cell("A27").Value = "Conditional Access Policies";
        ws.Cell("B27").Value = resourceStats.ConditionalAccessPolicies;

        ws.Cell("A28").Value = "Enabled CA Policies";
        ws.Cell("B28").Value = resourceStats.EnabledCaPolicies;

        ws.Cell("A29").Value = "DLP Policies";
        ws.Cell("B29").Value = resourceStats.DlpPolicies;

        // Collaboration Section
        ws.Cell("A31").Value = "Collaboration";
        ws.Cell("A31").Style.Font.Bold = true;
        ws.Cell("A31").Style.Font.FontSize = 12;
        ws.Cell("A31").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A31").Style.Font.FontColor = XLColor.White;
        ws.Range("A31:C31").Merge();

        ws.Cell("A32").Value = "Mailboxes";
        ws.Cell("B32").Value = resourceStats.Mailboxes;
        ws.Cell("B32").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A33").Value = "Shared Mailboxes";
        ws.Cell("B33").Value = resourceStats.SharedMailboxes;
        ws.Cell("B33").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A34").Value = "SharePoint Sites";
        ws.Cell("B34").Value = resourceStats.SharePointSites;
        ws.Cell("B34").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A35").Value = "Teams";
        ws.Cell("B35").Value = resourceStats.TeamsCount;
        ws.Cell("B35").Style.NumberFormat.Format = "#,##0";

        // Licenses Section
        ws.Cell("A37").Value = "Licenses";
        ws.Cell("A37").Style.Font.Bold = true;
        ws.Cell("A37").Style.Font.FontSize = 12;
        ws.Cell("A37").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A37").Style.Font.FontColor = XLColor.White;
        ws.Range("A37:C37").Merge();

        ws.Cell("A38").Value = "Total Licenses";
        ws.Cell("B38").Value = resourceStats.TotalLicenses;
        ws.Cell("B38").Style.NumberFormat.Format = "#,##0";

        ws.Cell("A39").Value = "Assigned Licenses";
        ws.Cell("B39").Value = resourceStats.AssignedLicenses;
        ws.Cell("B39").Style.NumberFormat.Format = "#,##0";

        ws.Columns().AdjustToContents();
        ws.Column("A").Width = 25;
        ws.Column("B").Width = 15;
    }

    private void CreateSummarySheet(IXLWorkbook workbook, AssessmentRunDto assessment, string tenantName, List<FindingDto> findings)
    {
        var ws = workbook.Worksheets.Add("Summary");

        // Title
        ws.Cell("A1").Value = "Cloudativ M365 Security Assessment Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:D1").Merge();

        // Assessment Info
        ws.Cell("A3").Value = "Tenant:";
        ws.Cell("B3").Value = tenantName;
        ws.Cell("A4").Value = "Assessment Date:";
        ws.Cell("B4").Value = assessment.StartedAt.ToString("yyyy-MM-dd HH:mm");
        ws.Cell("A5").Value = "Status:";
        ws.Cell("B5").Value = assessment.Status.ToString();
        ws.Cell("A6").Value = "Overall Score:";
        ws.Cell("B6").Value = $"{assessment.OverallScore ?? 0}%";
        ws.Cell("B6").Style.Font.Bold = true;

        // Findings Summary
        ws.Cell("A8").Value = "Findings Summary";
        ws.Cell("A8").Style.Font.Bold = true;
        ws.Cell("A8").Style.Font.FontSize = 12;

        ws.Cell("A9").Value = "Critical:";
        ws.Cell("B9").Value = findings.Count(f => f.Severity == Severity.Critical);
        ws.Cell("B9").Style.Font.FontColor = XLColor.FromHtml(CriticalColor);

        ws.Cell("A10").Value = "High:";
        ws.Cell("B10").Value = findings.Count(f => f.Severity == Severity.High);
        ws.Cell("B10").Style.Font.FontColor = XLColor.FromHtml(HighColor);

        ws.Cell("A11").Value = "Medium:";
        ws.Cell("B11").Value = findings.Count(f => f.Severity == Severity.Medium);
        ws.Cell("B11").Style.Font.FontColor = XLColor.FromHtml(MediumColor);

        ws.Cell("A12").Value = "Low:";
        ws.Cell("B12").Value = findings.Count(f => f.Severity == Severity.Low);
        ws.Cell("B12").Style.Font.FontColor = XLColor.FromHtml(LowColor);

        ws.Cell("A13").Value = "Total:";
        ws.Cell("B13").Value = findings.Count;
        ws.Cell("B13").Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private void CreateDomainScoresSheet(IXLWorkbook workbook, AssessmentRunDto assessment)
    {
        var ws = workbook.Worksheets.Add("Domain Scores");

        // Headers
        var headers = new[] { "Domain", "Score", "Grade", "Critical", "High", "Medium", "Low", "Passed", "Failed" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Data
        var row = 2;
        foreach (var domain in assessment.DomainScores.Values.OrderByDescending(d => d.CriticalCount + d.HighCount))
        {
            ws.Cell(row, 1).Value = domain.DisplayName;
            ws.Cell(row, 2).Value = $"{domain.Score}%";
            ws.Cell(row, 3).Value = domain.Grade;
            ws.Cell(row, 4).Value = domain.CriticalCount;
            ws.Cell(row, 5).Value = domain.HighCount;
            ws.Cell(row, 6).Value = domain.MediumCount;
            ws.Cell(row, 7).Value = domain.LowCount;
            ws.Cell(row, 8).Value = domain.PassedChecks;
            ws.Cell(row, 9).Value = domain.FailedChecks;

            if (domain.CriticalCount > 0)
                ws.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml(CriticalColor);
            if (domain.HighCount > 0)
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml(HighColor);

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.RangeUsed()?.SetAutoFilter();
    }

    private void CreateFindingsSheet(IXLWorkbook workbook, List<FindingDto> findings)
    {
        var ws = workbook.Worksheets.Add("Findings");

        // Headers
        var headers = new[] { "Severity", "Domain", "Title", "Description", "Remediation", "Category", "Affected Resources", "Check ID" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Data
        var row = 2;
        foreach (var finding in findings.OrderBy(f => f.Severity).ThenBy(f => f.DomainDisplayName))
        {
            ws.Cell(row, 1).Value = finding.Severity.ToString();
            ws.Cell(row, 2).Value = finding.DomainDisplayName;
            ws.Cell(row, 3).Value = finding.Title;
            ws.Cell(row, 4).Value = finding.Description;
            ws.Cell(row, 5).Value = finding.Remediation;
            ws.Cell(row, 6).Value = finding.Category;
            ws.Cell(row, 7).Value = string.Join(", ", finding.AffectedResources.Take(5));
            ws.Cell(row, 8).Value = finding.CheckId;

            // Color severity
            var severityColor = finding.Severity switch
            {
                Severity.Critical => XLColor.FromHtml(CriticalColor),
                Severity.High => XLColor.FromHtml(HighColor),
                Severity.Medium => XLColor.FromHtml(MediumColor),
                Severity.Low => XLColor.FromHtml(LowColor),
                _ => XLColor.Black
            };
            ws.Cell(row, 1).Style.Font.FontColor = severityColor;
            ws.Cell(row, 1).Style.Font.Bold = true;

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(4).Width = 50; // Description column wider
        ws.Column(5).Width = 50; // Remediation column wider
        ws.RangeUsed()?.SetAutoFilter();
    }

    private static string GetScoreColor(int score) => score switch
    {
        >= 90 => LowColor,
        >= 70 => MediumColor,
        >= 50 => HighColor,
        _ => CriticalColor
    };

    private static string GetGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };

    private static string GetSeverityColor(Severity severity) => severity switch
    {
        Severity.Critical => CriticalColor,
        Severity.High => HighColor,
        Severity.Medium => MediumColor,
        Severity.Low => LowColor,
        _ => "#6B7280"
    };

    private static string GetScoreBarColor(int score) => score switch
    {
        >= 90 => PdfReportComponents.SuccessColor,
        >= 70 => PdfReportComponents.WarningColor,
        >= 50 => HighColor,
        _ => PdfReportComponents.DangerColor
    };

    #region Domain-Specific Export Methods

    public async Task<byte[]> GenerateDomainPdfReportAsync(Guid assessmentRunId, AssessmentDomain domain, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating domain PDF report for {Domain} in assessment {AssessmentRunId}", domain, assessmentRunId);

        var assessment = await _assessmentService.GetRunByIdAsync(assessmentRunId, cancellationToken);
        if (assessment == null)
            throw new InvalidOperationException($"Assessment run {assessmentRunId} not found");

        var domainDetail = await _assessmentService.GetDomainDetailAsync(assessmentRunId, domain, cancellationToken);
        if (domainDetail == null)
            throw new InvalidOperationException($"Domain {domain} not found in assessment {assessmentRunId}");

        var tenant = await _tenantService.GetByIdAsync(assessment.TenantId, cancellationToken);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(c => ComposeDomainHeader(c, domainDetail, tenant?.Name ?? "Unknown", assessment.StartedAt));
                page.Content().Element(c => ComposeDomainContent(c, domainDetail));
                page.Footer().Element(ComposeDomainFooter);
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeDomainHeader(IContainer container, DomainDetailDto domainDetail, string tenantName, DateTime assessmentDate)
    {
        container.Column(column =>
        {
            // Accent bar
            column.Item().Height(4).Background(PdfReportComponents.SecondaryColor);

            column.Item().PaddingTop(8).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Cloudativ — {domainDetail.DisplayName}").FontSize(20).Bold()
                        .FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().PaddingTop(2).Text(tenantName).FontSize(11)
                        .FontColor(PdfReportComponents.LightTextColor);
                });
                row.ConstantItem(120).Column(col =>
                {
                    col.Item().AlignRight().Text($"{domainDetail.Score.Score}%").FontSize(28).Bold()
                        .FontColor(GetScoreColor(domainDetail.Score.Score));
                    col.Item().AlignRight().Text($"Grade {domainDetail.Score.Grade}").FontSize(12)
                        .FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().AlignRight().PaddingTop(2)
                        .Text($"{assessmentDate:yyyy-MM-dd HH:mm}").FontSize(8)
                        .FontColor(PdfReportComponents.LightTextColor);
                });
            });

            column.Item().LineHorizontal(1).LineColor(PdfReportComponents.BorderColor);
        });
    }

    private void ComposeDomainContent(IContainer container, DomainDetailDto domainDetail)
    {
        container.Column(column =>
        {
            // Domain Summary with stat cards + charts
            column.Item().Element(c => ComposeDomainSummary(c, domainDetail));

            column.Item().PaddingVertical(12);

            // Metrics Section
            if (domainDetail.Metrics.Any())
            {
                column.Item().Element(c => ComposeDomainMetrics(c, domainDetail.Metrics));
                column.Item().PaddingVertical(12);
            }

            // Recommendations
            if (domainDetail.Recommendations.Any())
            {
                column.Item().Element(c => ComposeDomainRecommendations(c, domainDetail.Recommendations));
                column.Item().PaddingVertical(12);
            }

            // Findings with branded cards
            column.Item().Element(c => ComposeDomainFindings(c, domainDetail.Findings));
        });
    }

    private void ComposeDomainSummary(IContainer container, DomainDetailDto domainDetail)
    {
        var score = domainDetail.Score;
        var totalChecks = score.PassedChecks + score.FailedChecks;
        var passPct = totalChecks > 0 ? score.PassedChecks * 100 / totalChecks : 0;

        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Domain Summary"));
            column.Item().PaddingTop(10);

            // Stat cards
            column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
            {
                new("Domain Score", $"{score.Score}%", $"Grade {score.Grade}", GetScoreColor(score.Score)),
                new("Total Findings", $"{domainDetail.Findings.Count}",
                    $"{score.CriticalCount + score.HighCount} Critical/High",
                    score.CriticalCount + score.HighCount > 0 ? PdfReportComponents.DangerColor : null),
                new("Passed Checks", $"{score.PassedChecks}", $"{passPct}% pass rate", PdfReportComponents.SuccessColor),
                new("Failed Checks", $"{score.FailedChecks}", null, score.FailedChecks > 0 ? PdfReportComponents.DangerColor : null)
            }));

            column.Item().PaddingTop(12);

            // Charts: Severity Breakdown donut + Compliance Status donut
            column.Item().Element(c => PdfReportComponents.ChartRow(c,
                left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                {
                    new("Critical", score.CriticalCount, CriticalColor),
                    new("High", score.HighCount, HighColor),
                    new("Medium", score.MediumCount, MediumColor),
                    new("Low", score.LowCount, LowColor)
                }, "Findings", $"{domainDetail.Findings.Count}", "Findings by Severity"),
                right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                {
                    new("Passed", score.PassedChecks, PdfReportComponents.SuccessColor),
                    new("Failed", score.FailedChecks, PdfReportComponents.DangerColor)
                }, "Checks", $"{passPct}%", "Compliance Checks")
            ));
        });
    }

    private void ComposeDomainMetrics(IContainer container, Dictionary<string, object?> metrics)
    {
        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Key Metrics"));
            column.Item().PaddingTop(10);

            // Display metrics in rows of 4 stat cards
            var metricList = metrics.Take(8).ToList();
            for (int i = 0; i < metricList.Count; i += 4)
            {
                if (i > 0) column.Item().PaddingTop(8);
                var batch = metricList.Skip(i).Take(4)
                    .Select(m => new StatCardData(FormatMetricName(m.Key), m.Value?.ToString() ?? "N/A"))
                    .ToList();
                column.Item().Element(c => PdfReportComponents.ComposeStatCards(c, batch));
            }
        });
    }

    private void ComposeDomainRecommendations(IContainer container, List<string> recommendations)
    {
        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Recommendations"));
            column.Item().PaddingTop(10);

            column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Background("#FAFFFE").Padding(15).Column(recCol =>
            {
                foreach (var rec in recommendations.Take(8))
                {
                    recCol.Item().PaddingBottom(5).Row(row =>
                    {
                        row.ConstantItem(15).AlignMiddle().Text("•").FontSize(12)
                            .FontColor(PdfReportComponents.SuccessColor);
                        row.RelativeItem().Text(rec).FontSize(10)
                            .FontColor(PdfReportComponents.TextColor);
                    });
                }
            });
        });
    }

    private void ComposeDomainFindings(IContainer container, List<FindingDto> findings)
    {
        var sortedFindings = findings
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.Title)
            .ToList();

        container.Column(column =>
        {
            column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Detailed Findings"));
            column.Item().PaddingTop(10);

            if (!sortedFindings.Any())
            {
                column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(15)
                    .Text("No findings for this domain.").FontSize(10)
                    .FontColor(PdfReportComponents.LightTextColor);
                return;
            }

            foreach (var finding in sortedFindings)
            {
                column.Item().Border(1).BorderColor(PdfReportComponents.BorderColor).Padding(10).Column(findingCol =>
                {
                    findingCol.Item().Row(row =>
                    {
                        row.ConstantItem(70).Element(c => PdfReportComponents.StatusBadge(c, finding.Severity.ToString()));
                        row.ConstantItem(5);
                        row.RelativeItem().Text(finding.Title).FontSize(11).SemiBold()
                            .FontColor(PdfReportComponents.TextColor);
                    });

                    if (!string.IsNullOrEmpty(finding.Category))
                    {
                        findingCol.Item().PaddingTop(3);
                        findingCol.Item().Text($"Category: {finding.Category}").FontSize(8)
                            .FontColor(PdfReportComponents.LightTextColor);
                    }

                    findingCol.Item().PaddingTop(3);
                    findingCol.Item().Row(row =>
                    {
                        row.ConstantItem(60).Text("Status:").FontSize(8)
                            .FontColor(PdfReportComponents.LightTextColor);
                        row.RelativeItem().Element(c => PdfReportComponents.StatusBadge(c,
                            finding.IsCompliant ? "Compliant" : "Non-Compliant"));
                    });

                    if (!string.IsNullOrEmpty(finding.Description))
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text("Description:").FontSize(9).SemiBold()
                            .FontColor(PdfReportComponents.TextColor);
                        findingCol.Item().Text(finding.Description).FontSize(9)
                            .FontColor(PdfReportComponents.TextColor);
                    }

                    if (!string.IsNullOrEmpty(finding.Remediation))
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text("Remediation:").FontSize(9).SemiBold()
                            .FontColor(PdfReportComponents.SuccessColor);
                        findingCol.Item().Text(finding.Remediation).FontSize(9)
                            .FontColor(PdfReportComponents.TextColor);
                    }

                    if (finding.AffectedResources.Any())
                    {
                        findingCol.Item().PaddingTop(5);
                        findingCol.Item().Text($"Affected Resources ({finding.AffectedResources.Count}):").FontSize(8)
                            .FontColor(PdfReportComponents.LightTextColor);
                        var resources = string.Join(", ", finding.AffectedResources.Take(5));
                        if (finding.AffectedResources.Count > 5)
                            resources += $" ... and {finding.AffectedResources.Count - 5} more";
                        findingCol.Item().Text(resources).FontSize(8)
                            .FontColor(PdfReportComponents.LightTextColor);
                    }
                });

                column.Item().PaddingTop(8);
            }
        });
    }

    private void ComposeDomainFooter(IContainer container)
    {
        PdfReportComponents.ComposeFooter(container);
    }

    public async Task<byte[]> GenerateDomainExcelReportAsync(Guid assessmentRunId, AssessmentDomain domain, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating domain Excel report for {Domain} in assessment {AssessmentRunId}", domain, assessmentRunId);

        var assessment = await _assessmentService.GetRunByIdAsync(assessmentRunId, cancellationToken);
        if (assessment == null)
            throw new InvalidOperationException($"Assessment run {assessmentRunId} not found");

        var domainDetail = await _assessmentService.GetDomainDetailAsync(assessmentRunId, domain, cancellationToken);
        if (domainDetail == null)
            throw new InvalidOperationException($"Domain {domain} not found in assessment {assessmentRunId}");

        var tenant = await _tenantService.GetByIdAsync(assessment.TenantId, cancellationToken);

        using var workbook = new XLWorkbook();

        // Summary Sheet
        CreateDomainSummarySheet(workbook, domainDetail, tenant?.Name ?? "Unknown", assessment.StartedAt);

        // Metrics Sheet
        if (domainDetail.Metrics.Any())
            CreateDomainMetricsSheet(workbook, domainDetail.Metrics);

        // Findings Sheet
        CreateDomainFindingsSheet(workbook, domainDetail.Findings);

        // Recommendations Sheet
        if (domainDetail.Recommendations.Any())
            CreateDomainRecommendationsSheet(workbook, domainDetail.Recommendations);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void CreateDomainSummarySheet(IXLWorkbook workbook, DomainDetailDto domainDetail, string tenantName, DateTime assessmentDate)
    {
        var ws = workbook.Worksheets.Add("Summary");

        // Title
        ws.Cell("A1").Value = $"{domainDetail.DisplayName} Assessment Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Range("A1:D1").Merge();

        // Assessment Info
        ws.Cell("A3").Value = "Tenant:";
        ws.Cell("B3").Value = tenantName;
        ws.Cell("A4").Value = "Assessment Date:";
        ws.Cell("B4").Value = assessmentDate.ToString("yyyy-MM-dd HH:mm");
        ws.Cell("A5").Value = "Domain:";
        ws.Cell("B5").Value = domainDetail.DisplayName;
        ws.Cell("A6").Value = "Description:";
        ws.Cell("B6").Value = domainDetail.Description;

        // Score Summary
        ws.Cell("A8").Value = "Score Summary";
        ws.Cell("A8").Style.Font.Bold = true;
        ws.Cell("A8").Style.Font.FontSize = 12;

        ws.Cell("A9").Value = "Domain Score:";
        ws.Cell("B9").Value = $"{domainDetail.Score.Score}%";
        ws.Cell("B9").Style.Font.Bold = true;

        ws.Cell("A10").Value = "Grade:";
        ws.Cell("B10").Value = domainDetail.Score.Grade;

        ws.Cell("A11").Value = "Passed Checks:";
        ws.Cell("B11").Value = domainDetail.Score.PassedChecks;
        ws.Cell("B11").Style.Font.FontColor = XLColor.FromHtml(LowColor);

        ws.Cell("A12").Value = "Failed Checks:";
        ws.Cell("B12").Value = domainDetail.Score.FailedChecks;
        ws.Cell("B12").Style.Font.FontColor = XLColor.FromHtml(CriticalColor);

        // Findings Summary
        ws.Cell("A14").Value = "Findings by Severity";
        ws.Cell("A14").Style.Font.Bold = true;
        ws.Cell("A14").Style.Font.FontSize = 12;

        ws.Cell("A15").Value = "Critical:";
        ws.Cell("B15").Value = domainDetail.Score.CriticalCount;
        ws.Cell("B15").Style.Font.FontColor = XLColor.FromHtml(CriticalColor);

        ws.Cell("A16").Value = "High:";
        ws.Cell("B16").Value = domainDetail.Score.HighCount;
        ws.Cell("B16").Style.Font.FontColor = XLColor.FromHtml(HighColor);

        ws.Cell("A17").Value = "Medium:";
        ws.Cell("B17").Value = domainDetail.Score.MediumCount;
        ws.Cell("B17").Style.Font.FontColor = XLColor.FromHtml(MediumColor);

        ws.Cell("A18").Value = "Low:";
        ws.Cell("B18").Value = domainDetail.Score.LowCount;
        ws.Cell("B18").Style.Font.FontColor = XLColor.FromHtml(LowColor);

        ws.Cell("A19").Value = "Total:";
        ws.Cell("B19").Value = domainDetail.Findings.Count;
        ws.Cell("B19").Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
    }

    private void CreateDomainMetricsSheet(IXLWorkbook workbook, Dictionary<string, object?> metrics)
    {
        var ws = workbook.Worksheets.Add("Metrics");

        // Headers
        ws.Cell("A1").Value = "Metric";
        ws.Cell("B1").Value = "Value";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("B1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("B1").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("B1").Style.Font.FontColor = XLColor.White;

        // Data
        var row = 2;
        foreach (var metric in metrics)
        {
            ws.Cell(row, 1).Value = FormatMetricName(metric.Key);
            ws.Cell(row, 2).Value = metric.Value?.ToString() ?? "N/A";
            row++;
        }

        ws.Columns().AdjustToContents();
    }

    private void CreateDomainFindingsSheet(IXLWorkbook workbook, List<FindingDto> findings)
    {
        var ws = workbook.Worksheets.Add("Findings");

        // Headers
        var headers = new[] { "Severity", "Status", "Title", "Description", "Remediation", "Category", "Affected Resources", "Check ID" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
            ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
            ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
        }

        // Data
        var row = 2;
        foreach (var finding in findings.OrderBy(f => f.Severity).ThenBy(f => f.Title))
        {
            ws.Cell(row, 1).Value = finding.Severity.ToString();
            ws.Cell(row, 2).Value = finding.IsCompliant ? "Compliant" : "Non-Compliant";
            ws.Cell(row, 3).Value = finding.Title;
            ws.Cell(row, 4).Value = finding.Description;
            ws.Cell(row, 5).Value = finding.Remediation;
            ws.Cell(row, 6).Value = finding.Category;
            ws.Cell(row, 7).Value = string.Join(", ", finding.AffectedResources.Take(10));
            ws.Cell(row, 8).Value = finding.CheckId;

            // Color severity
            var severityColor = finding.Severity switch
            {
                Severity.Critical => XLColor.FromHtml(CriticalColor),
                Severity.High => XLColor.FromHtml(HighColor),
                Severity.Medium => XLColor.FromHtml(MediumColor),
                Severity.Low => XLColor.FromHtml(LowColor),
                _ => XLColor.Black
            };
            ws.Cell(row, 1).Style.Font.FontColor = severityColor;
            ws.Cell(row, 1).Style.Font.Bold = true;

            // Color status
            ws.Cell(row, 2).Style.Font.FontColor = finding.IsCompliant
                ? XLColor.FromHtml(LowColor)
                : XLColor.FromHtml(CriticalColor);

            row++;
        }

        ws.Columns().AdjustToContents();
        ws.Column(4).Width = 50; // Description column wider
        ws.Column(5).Width = 50; // Remediation column wider
        ws.RangeUsed()?.SetAutoFilter();
    }

    private void CreateDomainRecommendationsSheet(IXLWorkbook workbook, List<string> recommendations)
    {
        var ws = workbook.Worksheets.Add("Recommendations");

        // Header
        ws.Cell("A1").Value = "#";
        ws.Cell("B1").Value = "Recommendation";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("B1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("B1").Style.Fill.BackgroundColor = XLColor.FromHtml(PrimaryColor);
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("B1").Style.Font.FontColor = XLColor.White;

        // Data
        var row = 2;
        foreach (var rec in recommendations)
        {
            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = rec;
            row++;
        }

        ws.Column(1).Width = 5;
        ws.Column(2).Width = 80;
    }

    private static string FormatMetricName(string name)
    {
        // Convert camelCase to Title Case with spaces
        return System.Text.RegularExpressions.Regex.Replace(name, "(\\B[A-Z])", " $1");
    }

    #endregion
}
