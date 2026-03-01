using ClosedXML.Excel;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Services.Export;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class GovernanceExportService : IGovernanceExportService
{
    private readonly IGovernanceService _governanceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GovernanceExportService> _logger;

    // Cloudativ brand colors
    private const string PrimaryColor = "#E0FC8E";
    private const string SecondaryColor = "#51627A";
    private const string AccentColor = "#EF7348";

    static GovernanceExportService()
    {
        // Set QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public GovernanceExportService(
        IGovernanceService governanceService,
        IUnitOfWork unitOfWork,
        ILogger<GovernanceExportService> logger)
    {
        _governanceService = governanceService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> ExportToPdfAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting governance analysis to PDF for run {RunId}", assessmentRunId);

        var analyses = await _governanceService.GetAnalysesByRunAsync(assessmentRunId, cancellationToken);
        var summary = await _governanceService.GetAnalysisSummaryAsync(assessmentRunId, cancellationToken);

        if (!analyses.Any())
        {
            throw new InvalidOperationException("No governance analyses found for this assessment run.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, tenantName, summary));
                page.Content().Element(c => ComposeContent(c, analyses.ToList(), summary));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public async Task<byte[]> ExportToExcelAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting governance analysis to Excel for run {RunId}", assessmentRunId);

        var analyses = await _governanceService.GetAnalysesByRunAsync(assessmentRunId, cancellationToken);
        var summary = await _governanceService.GetAnalysisSummaryAsync(assessmentRunId, cancellationToken);

        if (!analyses.Any())
        {
            throw new InvalidOperationException("No governance analyses found for this assessment run.");
        }

        using var workbook = new XLWorkbook();

        // Summary Sheet
        var summarySheet = workbook.Worksheets.Add("Summary");
        CreateSummarySheet(summarySheet, tenantName, summary, analyses.ToList());

        // Individual sheets for each standard
        foreach (var analysis in analyses.Where(a => a.IsSuccessful))
        {
            var sheetName = SanitizeSheetName(analysis.StandardDisplayName);
            var sheet = workbook.Worksheets.Add(sheetName);
            CreateAnalysisSheet(sheet, analysis);
        }

        // Gaps Sheet
        var gapsSheet = workbook.Worksheets.Add("All Compliance Gaps");
        CreateGapsSheet(gapsSheet, analyses.ToList());

        // Recommendations Sheet
        var recsSheet = workbook.Worksheets.Add("All Recommendations");
        CreateRecommendationsSheet(recsSheet, analyses.ToList());

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportSingleAnalysisToPdfAsync(
        Guid analysisId,
        string tenantName,
        CancellationToken cancellationToken = default)
    {
        var analysis = await _governanceService.GetAnalysisAsync(analysisId, cancellationToken);
        if (analysis == null)
        {
            throw new InvalidOperationException("Governance analysis not found.");
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeSingleHeader(c, tenantName, analysis));
                page.Content().Element(c => ComposeSingleContent(c, analysis));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    #region PDF Composition Methods

    private void ComposeHeader(IContainer container, string tenantName, GovernanceAnalysisSummaryDto? summary)
    {
        container.Column(column =>
        {
            // Branded accent bar
            column.Item().Height(4).Background(PdfReportComponents.SecondaryColor);

            column.Item().PaddingTop(8).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Governance Compliance Report")
                        .FontSize(20).Bold().FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().PaddingTop(2).Text(tenantName)
                        .FontSize(11).FontColor(PdfReportComponents.LightTextColor);
                });
                row.ConstantItem(180).Column(col =>
                {
                    col.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(8).FontColor(PdfReportComponents.LightTextColor);
                    if (summary != null)
                    {
                        col.Item().AlignRight().PaddingTop(2)
                            .Text($"Standards: {summary.StandardsAnalyzed}")
                            .FontSize(10).Bold().FontColor(PdfReportComponents.SecondaryColor);
                    }
                });
            });

            column.Item().LineHorizontal(1).LineColor(PdfReportComponents.BorderColor);

            // Stat cards
            if (summary != null)
            {
                column.Item().PaddingTop(10).Element(c =>
                    PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                    {
                        new("Overall Score", $"{summary.OverallAverageScore}%", "Average across standards",
                            GetScoreColor(summary.OverallAverageScore)),
                        new("Standards Analyzed", $"{summary.StandardsAnalyzed}", null,
                            PdfReportComponents.SecondaryColor),
                        new("Compliance Gaps", $"{summary.TotalGaps}", "Across all standards",
                            summary.TotalGaps > 0 ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor),
                        new("Recommendations", $"{summary.TotalRecommendations}", "Remediation actions",
                            PdfReportComponents.InfoColor)
                    }));
            }

            column.Item().PaddingVertical(10);
        });
    }

    private void ComposeContent(IContainer container, List<GovernanceAnalysisDto> analyses, GovernanceAnalysisSummaryDto? summary)
    {
        var successful = analyses.Where(a => a.IsSuccessful).ToList();

        container.Column(column =>
        {
            // --- Executive Summary Charts ---
            if (successful.Count > 0)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c, "Executive Summary"));
                column.Item().PaddingTop(10);

                // Row 1: Control Status donut + Gap Severity donut
                var totalCompliant = successful.Sum(a => a.CompliantControls);
                var totalPartial = successful.Sum(a => a.PartiallyCompliantControls);
                var totalNonCompliant = successful.Sum(a => a.NonCompliantControls);
                var totalControls = totalCompliant + totalPartial + totalNonCompliant;

                var allGaps = successful.SelectMany(a => a.ComplianceGaps ?? new()).ToList();
                var criticalGaps = allGaps.Count(g => string.Equals(g.Severity, "Critical", StringComparison.OrdinalIgnoreCase));
                var highGaps = allGaps.Count(g => string.Equals(g.Severity, "High", StringComparison.OrdinalIgnoreCase));
                var mediumGaps = allGaps.Count(g => string.Equals(g.Severity, "Medium", StringComparison.OrdinalIgnoreCase));
                var lowGaps = allGaps.Count(g => string.Equals(g.Severity, "Low", StringComparison.OrdinalIgnoreCase));

                column.Item().Element(c => PdfReportComponents.ChartRow(c,
                    left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                    {
                        new("Compliant", totalCompliant, PdfReportComponents.SuccessColor),
                        new("Partial", totalPartial, PdfReportComponents.WarningColor),
                        new("Non-Compliant", totalNonCompliant, PdfReportComponents.DangerColor)
                    }, "Controls", $"{totalControls}", "Control Compliance Status"),

                    right => PdfChartHelper.DonutChart(right, new List<ChartSegment>
                    {
                        new("Critical", criticalGaps, "#DC2626"),
                        new("High", highGaps, "#EF7348"),
                        new("Medium", mediumGaps, "#CA8A04"),
                        new("Low", lowGaps, "#0078D4")
                    }, "Gaps", $"{allGaps.Count}", "Gaps by Severity")
                ));

                column.Item().PaddingTop(12);

                // Row 2: Recommendation Priority donut + Standards Score bar chart
                var allRecs = successful.SelectMany(a => a.Recommendations ?? new()).ToList();
                var criticalRecs = allRecs.Count(r => string.Equals(r.Priority, "Critical", StringComparison.OrdinalIgnoreCase));
                var highRecs = allRecs.Count(r => string.Equals(r.Priority, "High", StringComparison.OrdinalIgnoreCase));
                var mediumRecs = allRecs.Count(r => string.Equals(r.Priority, "Medium", StringComparison.OrdinalIgnoreCase));
                var lowRecs = allRecs.Count(r => string.Equals(r.Priority, "Low", StringComparison.OrdinalIgnoreCase));

                column.Item().Element(c => PdfReportComponents.ChartRow(c,
                    left => PdfChartHelper.DonutChart(left, new List<ChartSegment>
                    {
                        new("Critical", criticalRecs, "#DC2626"),
                        new("High", highRecs, "#EF7348"),
                        new("Medium", mediumRecs, "#CA8A04"),
                        new("Low", lowRecs, "#0078D4")
                    }, "Total", $"{allRecs.Count}", "Recommendations by Priority"),

                    right => PdfChartHelper.HorizontalBarChart(right,
                        successful.Select((a, i) => new BarChartItem(
                            a.StandardDisplayName,
                            a.ComplianceScore,
                            GetScoreBarColor(a.ComplianceScore)
                        )).ToList(),
                        "Compliance Scores by Standard", 10)
                ));

                column.Item().PaddingTop(12);

                // Row 3: Effort Distribution donut + Gaps per Standard bar chart
                var quickWin = allRecs.Count(r => string.Equals(r.EstimatedEffort, "Quick Win", StringComparison.OrdinalIgnoreCase));
                var shortTerm = allRecs.Count(r => string.Equals(r.EstimatedEffort, "Short Term", StringComparison.OrdinalIgnoreCase));
                var longTerm = allRecs.Count(r => string.Equals(r.EstimatedEffort, "Long Term", StringComparison.OrdinalIgnoreCase));
                var otherEffort = allRecs.Count - quickWin - shortTerm - longTerm;

                var effortSegments = new List<ChartSegment>();
                if (quickWin > 0) effortSegments.Add(new("Quick Win", quickWin, PdfReportComponents.SuccessColor));
                if (shortTerm > 0) effortSegments.Add(new("Short Term", shortTerm, PdfReportComponents.InfoColor));
                if (longTerm > 0) effortSegments.Add(new("Long Term", longTerm, PdfReportComponents.WarningColor));
                if (otherEffort > 0) effortSegments.Add(new("Other", otherEffort, "#9CA3AF"));
                if (!effortSegments.Any()) effortSegments.Add(new("None", 1, "#E5E7EB"));

                column.Item().Element(c => PdfReportComponents.ChartRow(c,
                    left => PdfChartHelper.DonutChart(left, effortSegments,
                        "Actions", $"{allRecs.Count}", "Effort Distribution"),

                    right => PdfChartHelper.HorizontalBarChart(right,
                        successful.Where(a => (a.ComplianceGaps?.Count ?? 0) > 0)
                            .Select(a => new BarChartItem(
                                a.StandardDisplayName,
                                a.ComplianceGaps!.Count,
                                PdfReportComponents.DangerColor
                            )).ToList(),
                        "Gaps per Standard", 10)
                ));

                column.Item().PaddingTop(15);
            }

            // --- Per-Standard Detail Sections ---
            foreach (var analysis in successful)
            {
                column.Item().Element(c => ComposeAnalysisSection(c, analysis));
                column.Item().PaddingVertical(10);
            }
        });
    }

    private void ComposeAnalysisSection(IContainer container, GovernanceAnalysisDto analysis)
    {
        container.Column(column =>
        {
            // Standard header bar
            column.Item().Background(PdfReportComponents.SecondaryColor).Padding(10).Row(row =>
            {
                row.RelativeItem().Text(analysis.StandardDisplayName)
                    .FontSize(14).Bold().FontColor("#FFFFFF");
                row.ConstantItem(100).AlignRight().Text($"Score: {analysis.ComplianceScore}%")
                    .FontSize(14).Bold().FontColor(PdfReportComponents.PrimaryColor);
            });

            // Control status stat cards
            column.Item().PaddingTop(8).Element(c =>
                PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                {
                    new("Total Controls", $"{analysis.TotalControls}", null, PdfReportComponents.SecondaryColor),
                    new("Compliant", $"{analysis.CompliantControls}", null, PdfReportComponents.SuccessColor),
                    new("Partial", $"{analysis.PartiallyCompliantControls}", null, PdfReportComponents.WarningColor),
                    new("Non-Compliant", $"{analysis.NonCompliantControls}", null, PdfReportComponents.DangerColor)
                }));

            // Charts row: Control Status donut + Gap Severity donut
            column.Item().PaddingTop(10).Element(c =>
            {
                var controlSegments = new List<ChartSegment>
                {
                    new("Compliant", analysis.CompliantControls, PdfReportComponents.SuccessColor),
                    new("Partial", analysis.PartiallyCompliantControls, PdfReportComponents.WarningColor),
                    new("Non-Compliant", analysis.NonCompliantControls, PdfReportComponents.DangerColor)
                };

                var gapSeveritySegments = new List<ChartSegment>();
                if (analysis.ComplianceGaps?.Any() == true)
                {
                    var crit = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Critical", StringComparison.OrdinalIgnoreCase));
                    var high = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "High", StringComparison.OrdinalIgnoreCase));
                    var med = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Medium", StringComparison.OrdinalIgnoreCase));
                    var low = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Low", StringComparison.OrdinalIgnoreCase));
                    if (crit > 0) gapSeveritySegments.Add(new("Critical", crit, "#DC2626"));
                    if (high > 0) gapSeveritySegments.Add(new("High", high, "#EF7348"));
                    if (med > 0) gapSeveritySegments.Add(new("Medium", med, "#CA8A04"));
                    if (low > 0) gapSeveritySegments.Add(new("Low", low, "#0078D4"));
                }
                if (!gapSeveritySegments.Any())
                    gapSeveritySegments.Add(new("No Gaps", 1, PdfReportComponents.SuccessColor));

                PdfReportComponents.ChartRow(c,
                    left => PdfChartHelper.DonutChart(left, controlSegments,
                        "Controls", $"{analysis.TotalControls}", "Control Status"),
                    right => PdfChartHelper.DonutChart(right, gapSeveritySegments,
                        "Gaps", $"{analysis.ComplianceGaps?.Count ?? 0}", "Gap Severity"));
            });

            column.Item().PaddingTop(10);

            // Gaps table
            if (analysis.ComplianceGaps?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Compliance Gaps ({analysis.ComplianceGaps.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Control ID", "Control Name", "Gap Description", "Severity" },
                        analysis.ComplianceGaps.Select(g => new[]
                        {
                            g.ControlId ?? "-",
                            g.ControlName ?? "-",
                            g.GapDescription ?? "-",
                            g.Severity ?? "-"
                        }).ToList(),
                        new[] { 3 } // Badge column for severity
                    ));
                column.Item().PaddingTop(8);
            }

            // Recommendations table
            if (analysis.Recommendations?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Recommendations ({analysis.Recommendations.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Title", "Priority", "Implementation Guidance", "Effort" },
                        analysis.Recommendations.Select(r => new[]
                        {
                            r.Title ?? "-",
                            r.Priority ?? "-",
                            r.ImplementationGuidance ?? "-",
                            r.EstimatedEffort ?? "-"
                        }).ToList(),
                        new[] { 1, 3 } // Badge columns for priority and effort
                    ));
                column.Item().PaddingTop(8);
            }

            // Compliant Areas table
            if (analysis.CompliantAreas?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Compliant Areas ({analysis.CompliantAreas.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Control ID", "Control Name", "Status", "Evidence" },
                        analysis.CompliantAreas.Select(a => new[]
                        {
                            a.ControlId ?? "-",
                            a.ControlName ?? "-",
                            a.ComplianceStatus ?? "-",
                            a.Evidence ?? "-"
                        }).ToList(),
                        new[] { 2 } // Badge column for status
                    ));
            }
        });
    }

    private void ComposeSingleHeader(IContainer container, string tenantName, GovernanceAnalysisDto analysis)
    {
        container.Column(column =>
        {
            // Branded accent bar
            column.Item().Height(4).Background(PdfReportComponents.SecondaryColor);

            column.Item().PaddingTop(8).PaddingBottom(8).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"{analysis.StandardDisplayName} Compliance Report")
                        .FontSize(20).Bold().FontColor(PdfReportComponents.SecondaryColor);
                    col.Item().PaddingTop(2).Text(tenantName)
                        .FontSize(11).FontColor(PdfReportComponents.LightTextColor);
                });
                row.ConstantItem(180).Column(col =>
                {
                    col.Item().AlignRight().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC")
                        .FontSize(8).FontColor(PdfReportComponents.LightTextColor);
                    col.Item().AlignRight().PaddingTop(2)
                        .Text($"Score: {analysis.ComplianceScore}%")
                        .FontSize(12).Bold()
                        .FontColor(GetScoreColor(analysis.ComplianceScore));
                });
            });

            column.Item().LineHorizontal(1).LineColor(PdfReportComponents.BorderColor);

            // Stat cards for single analysis
            column.Item().PaddingTop(10).Element(c =>
                PdfReportComponents.ComposeStatCards(c, new List<StatCardData>
                {
                    new("Total Controls", $"{analysis.TotalControls}", null, PdfReportComponents.SecondaryColor),
                    new("Compliant", $"{analysis.CompliantControls}", null, PdfReportComponents.SuccessColor),
                    new("Gaps Found", $"{analysis.ComplianceGaps?.Count ?? 0}", null,
                        (analysis.ComplianceGaps?.Count ?? 0) > 0
                            ? PdfReportComponents.DangerColor : PdfReportComponents.SuccessColor),
                    new("Recommendations", $"{analysis.Recommendations?.Count ?? 0}", null, PdfReportComponents.InfoColor)
                }));

            column.Item().PaddingVertical(10);
        });
    }

    private void ComposeSingleContent(IContainer container, GovernanceAnalysisDto analysis)
    {
        container.Column(column =>
        {
            // Charts: Control Status + Gap Severity
            var controlSegments = new List<ChartSegment>
            {
                new("Compliant", analysis.CompliantControls, PdfReportComponents.SuccessColor),
                new("Partial", analysis.PartiallyCompliantControls, PdfReportComponents.WarningColor),
                new("Non-Compliant", analysis.NonCompliantControls, PdfReportComponents.DangerColor)
            };

            var gapSegments = new List<ChartSegment>();
            if (analysis.ComplianceGaps?.Any() == true)
            {
                var crit = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Critical", StringComparison.OrdinalIgnoreCase));
                var high = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "High", StringComparison.OrdinalIgnoreCase));
                var med = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Medium", StringComparison.OrdinalIgnoreCase));
                var low = analysis.ComplianceGaps.Count(g => string.Equals(g.Severity, "Low", StringComparison.OrdinalIgnoreCase));
                if (crit > 0) gapSegments.Add(new("Critical", crit, "#DC2626"));
                if (high > 0) gapSegments.Add(new("High", high, "#EF7348"));
                if (med > 0) gapSegments.Add(new("Medium", med, "#CA8A04"));
                if (low > 0) gapSegments.Add(new("Low", low, "#0078D4"));
            }
            if (!gapSegments.Any())
                gapSegments.Add(new("No Gaps", 1, PdfReportComponents.SuccessColor));

            column.Item().Element(c => PdfReportComponents.ChartRow(c,
                left => PdfChartHelper.DonutChart(left, controlSegments,
                    "Controls", $"{analysis.TotalControls}", "Control Status"),
                right => PdfChartHelper.DonutChart(right, gapSegments,
                    "Gaps", $"{analysis.ComplianceGaps?.Count ?? 0}", "Gap Severity")));

            column.Item().PaddingTop(12);

            // Gaps table
            if (analysis.ComplianceGaps?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Compliance Gaps ({analysis.ComplianceGaps.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Control ID", "Control Name", "Gap Description", "Severity" },
                        analysis.ComplianceGaps.Select(g => new[]
                        {
                            g.ControlId ?? "-",
                            g.ControlName ?? "-",
                            g.GapDescription ?? "-",
                            g.Severity ?? "-"
                        }).ToList(),
                        new[] { 3 }));
                column.Item().PaddingTop(8);
            }

            // Recommendations table
            if (analysis.Recommendations?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Recommendations ({analysis.Recommendations.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Title", "Priority", "Implementation Guidance", "Effort" },
                        analysis.Recommendations.Select(r => new[]
                        {
                            r.Title ?? "-",
                            r.Priority ?? "-",
                            r.ImplementationGuidance ?? "-",
                            r.EstimatedEffort ?? "-"
                        }).ToList(),
                        new[] { 1, 3 }));
                column.Item().PaddingTop(8);
            }

            // Compliant Areas table
            if (analysis.CompliantAreas?.Any() == true)
            {
                column.Item().Element(c => PdfReportComponents.SectionTitle(c,
                    $"Compliant Areas ({analysis.CompliantAreas.Count})"));
                column.Item().PaddingTop(5).Element(c =>
                    PdfReportComponents.ComposeDataTable(c,
                        new[] { "Control ID", "Control Name", "Status", "Evidence" },
                        analysis.CompliantAreas.Select(a => new[]
                        {
                            a.ControlId ?? "-",
                            a.ControlName ?? "-",
                            a.ComplianceStatus ?? "-",
                            a.Evidence ?? "-"
                        }).ToList(),
                        new[] { 2 }));
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        PdfReportComponents.ComposeFooter(container);
    }

    private static string GetScoreColor(int score) => score switch
    {
        >= 80 => PdfReportComponents.SuccessColor,
        >= 60 => PdfReportComponents.WarningColor,
        _ => PdfReportComponents.DangerColor
    };

    private static string GetScoreBarColor(int score) => score switch
    {
        >= 80 => PdfReportComponents.SuccessColor,
        >= 60 => PdfReportComponents.WarningColor,
        _ => PdfReportComponents.DangerColor
    };

    #endregion

    #region Excel Composition Methods

    private void CreateSummarySheet(IXLWorksheet sheet, string tenantName, GovernanceAnalysisSummaryDto? summary, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        // Title
        sheet.Cell(row, 1).Value = "Cloudativ Assessment - Governance Compliance Report";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 16;
        sheet.Range(row, 1, row, 4).Merge();
        row += 2;

        // Tenant info
        sheet.Cell(row, 1).Value = "Tenant:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = tenantName;
        row++;

        sheet.Cell(row, 1).Value = "Generated:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
        row += 2;

        if (summary != null)
        {
            // Summary section
            sheet.Cell(row, 1).Value = "Summary";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 14;
            row++;

            sheet.Cell(row, 1).Value = "Overall Score:";
            sheet.Cell(row, 2).Value = $"{summary.OverallAverageScore}%";
            sheet.Cell(row, 2).Style.Font.Bold = true;
            row++;

            sheet.Cell(row, 1).Value = "Standards Analyzed:";
            sheet.Cell(row, 2).Value = summary.StandardsAnalyzed;
            row++;

            sheet.Cell(row, 1).Value = "Total Gaps:";
            sheet.Cell(row, 2).Value = summary.TotalGaps;
            row++;

            sheet.Cell(row, 1).Value = "Total Recommendations:";
            sheet.Cell(row, 2).Value = summary.TotalRecommendations;
            row += 2;
        }

        // Standards table
        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Score";
        sheet.Cell(row, 3).Value = "Compliant";
        sheet.Cell(row, 4).Value = "Partial";
        sheet.Cell(row, 5).Value = "Non-Compliant";
        sheet.Cell(row, 6).Value = "Gaps";
        var headerRange = sheet.Range(row, 1, row, 6);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(SecondaryColor);
        headerRange.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses)
        {
            sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
            sheet.Cell(row, 2).Value = $"{analysis.ComplianceScore}%";
            sheet.Cell(row, 3).Value = analysis.CompliantControls;
            sheet.Cell(row, 4).Value = analysis.PartiallyCompliantControls;
            sheet.Cell(row, 5).Value = analysis.NonCompliantControls;
            sheet.Cell(row, 6).Value = analysis.ComplianceGaps?.Count ?? 0;

            // Color code score
            if (analysis.ComplianceScore >= 80)
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Green;
            else if (analysis.ComplianceScore >= 60)
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Orange;
            else
                sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;

            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private void CreateAnalysisSheet(IXLWorksheet sheet, GovernanceAnalysisDto analysis)
    {
        var row = 1;

        // Header
        sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 14;
        row += 2;

        // Summary
        sheet.Cell(row, 1).Value = "Compliance Score:";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 2).Value = $"{analysis.ComplianceScore}%";
        row++;

        sheet.Cell(row, 1).Value = "Total Controls:";
        sheet.Cell(row, 2).Value = analysis.TotalControls;
        row++;

        sheet.Cell(row, 1).Value = "Compliant:";
        sheet.Cell(row, 2).Value = analysis.CompliantControls;
        row++;

        sheet.Cell(row, 1).Value = "Partially Compliant:";
        sheet.Cell(row, 2).Value = analysis.PartiallyCompliantControls;
        row++;

        sheet.Cell(row, 1).Value = "Non-Compliant:";
        sheet.Cell(row, 2).Value = analysis.NonCompliantControls;
        row += 2;

        // Gaps
        if (analysis.ComplianceGaps?.Any() == true)
        {
            sheet.Cell(row, 1).Value = "Compliance Gaps";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            sheet.Cell(row, 1).Value = "Control ID";
            sheet.Cell(row, 2).Value = "Control Name";
            sheet.Cell(row, 3).Value = "Gap Description";
            sheet.Cell(row, 4).Value = "Severity";
            sheet.Cell(row, 5).Value = "Current State";
            sheet.Cell(row, 6).Value = "Required State";
            var gapHeader = sheet.Range(row, 1, row, 6);
            gapHeader.Style.Font.Bold = true;
            gapHeader.Style.Fill.BackgroundColor = XLColor.FromHtml(AccentColor);
            gapHeader.Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var gap in analysis.ComplianceGaps)
            {
                sheet.Cell(row, 1).Value = gap.ControlId ?? "-";
                sheet.Cell(row, 2).Value = gap.ControlName ?? "-";
                sheet.Cell(row, 3).Value = gap.GapDescription ?? "-";
                sheet.Cell(row, 4).Value = gap.Severity ?? "-";
                sheet.Cell(row, 5).Value = gap.CurrentState ?? "-";
                sheet.Cell(row, 6).Value = gap.RequiredState ?? "-";
                row++;
            }
            row++;
        }

        // Recommendations
        if (analysis.Recommendations?.Any() == true)
        {
            sheet.Cell(row, 1).Value = "Recommendations";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            row++;

            sheet.Cell(row, 1).Value = "Title";
            sheet.Cell(row, 2).Value = "Priority";
            sheet.Cell(row, 3).Value = "Implementation Guidance";
            sheet.Cell(row, 4).Value = "Estimated Effort";
            var recHeader = sheet.Range(row, 1, row, 4);
            recHeader.Style.Font.Bold = true;
            recHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976d2");
            recHeader.Style.Font.FontColor = XLColor.White;
            row++;

            foreach (var rec in analysis.Recommendations)
            {
                sheet.Cell(row, 1).Value = rec.Title ?? "-";
                sheet.Cell(row, 2).Value = rec.Priority ?? "-";
                sheet.Cell(row, 3).Value = rec.ImplementationGuidance ?? "-";
                sheet.Cell(row, 4).Value = rec.EstimatedEffort ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(3).Width = 60; // Wider for descriptions
    }

    private void CreateGapsSheet(IXLWorksheet sheet, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Control ID";
        sheet.Cell(row, 3).Value = "Control Name";
        sheet.Cell(row, 4).Value = "Gap Description";
        sheet.Cell(row, 5).Value = "Severity";
        sheet.Cell(row, 6).Value = "Current State";
        sheet.Cell(row, 7).Value = "Required State";
        var header = sheet.Range(row, 1, row, 7);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml(AccentColor);
        header.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses.Where(a => a.IsSuccessful && a.ComplianceGaps?.Any() == true))
        {
            foreach (var gap in analysis.ComplianceGaps!)
            {
                sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
                sheet.Cell(row, 2).Value = gap.ControlId ?? "-";
                sheet.Cell(row, 3).Value = gap.ControlName ?? "-";
                sheet.Cell(row, 4).Value = gap.GapDescription ?? "-";
                sheet.Cell(row, 5).Value = gap.Severity ?? "-";
                sheet.Cell(row, 6).Value = gap.CurrentState ?? "-";
                sheet.Cell(row, 7).Value = gap.RequiredState ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(4).Width = 60;
    }

    private void CreateRecommendationsSheet(IXLWorksheet sheet, List<GovernanceAnalysisDto> analyses)
    {
        var row = 1;

        sheet.Cell(row, 1).Value = "Standard";
        sheet.Cell(row, 2).Value = "Title";
        sheet.Cell(row, 3).Value = "Priority";
        sheet.Cell(row, 4).Value = "Implementation Guidance";
        sheet.Cell(row, 5).Value = "Estimated Effort";
        var header = sheet.Range(row, 1, row, 5);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1976d2");
        header.Style.Font.FontColor = XLColor.White;
        row++;

        foreach (var analysis in analyses.Where(a => a.IsSuccessful && a.Recommendations?.Any() == true))
        {
            foreach (var rec in analysis.Recommendations!)
            {
                sheet.Cell(row, 1).Value = analysis.StandardDisplayName;
                sheet.Cell(row, 2).Value = rec.Title ?? "-";
                sheet.Cell(row, 3).Value = rec.Priority ?? "-";
                sheet.Cell(row, 4).Value = rec.ImplementationGuidance ?? "-";
                sheet.Cell(row, 5).Value = rec.EstimatedEffort ?? "-";
                row++;
            }
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(4).Width = 60;
    }

    /// <summary>
    /// Sanitizes a string to be valid as an Excel sheet name.
    /// Excel sheet names cannot contain: : \ / ? * [ ]
    /// Maximum length is 31 characters.
    /// </summary>
    private static string SanitizeSheetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Sheet";

        // Replace invalid characters
        var sanitized = name
            .Replace(":", "-")
            .Replace("\\", "-")
            .Replace("/", "-")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("[", "(")
            .Replace("]", ")");

        // Trim to max 31 characters
        if (sanitized.Length > 31)
            sanitized = sanitized.Substring(0, 31);

        return sanitized;
    }

    #endregion
}
