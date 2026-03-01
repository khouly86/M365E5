using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Cloudativ.Assessment.Infrastructure.Services.Export;

public record ChartSegment(string Label, float Value, string Color);
public record BarChartItem(string Label, float Value, string Color);

/// <summary>
/// Generates SVG chart strings for embedding in QuestPDF documents via .Svg() API.
/// </summary>
public static class PdfChartHelper
{
    /// <summary>
    /// Renders a donut chart with legend inside a QuestPDF container.
    /// </summary>
    public static void DonutChart(IContainer container, List<ChartSegment> segments,
        string? centerLabel = null, string? centerValue = null, string title = "")
    {
        var validSegments = segments.Where(s => s.Value > 0).ToList();
        if (!validSegments.Any()) validSegments = segments.Take(1).ToList();

        container.Column(col =>
        {
            if (!string.IsNullOrEmpty(title))
            {
                col.Item().PaddingBottom(5).Text(title).FontSize(10).Bold()
                    .FontColor(PdfReportComponents.SecondaryColor);
            }

            col.Item().Row(row =>
            {
                // Chart SVG
                row.ConstantItem(140).Height(140).Svg(size =>
                    GenerateDonutSvg(size.Width, size.Height, validSegments, centerLabel, centerValue));

                // Legend
                row.RelativeItem().PaddingLeft(10).AlignMiddle().Column(legend =>
                {
                    var total = validSegments.Sum(s => s.Value);
                    foreach (var seg in validSegments)
                    {
                        var pct = total > 0 ? (seg.Value / total * 100) : 0;
                        legend.Item().PaddingBottom(4).Row(lr =>
                        {
                            lr.ConstantItem(10).Height(10).Svg(_ =>
                                $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'><rect width='10' height='10' rx='2' fill='{seg.Color}'/></svg>");
                            lr.RelativeItem().PaddingLeft(5).Text($"{seg.Label} ({seg.Value:N0}, {pct:F0}%)")
                                .FontSize(8).FontColor(PdfReportComponents.TextColor);
                        });
                    }
                });
            });
        });
    }

    /// <summary>
    /// Renders a horizontal bar chart inside a QuestPDF container.
    /// </summary>
    public static void HorizontalBarChart(IContainer container, List<BarChartItem> items,
        string title = "", int maxItems = 6)
    {
        var displayItems = items.OrderByDescending(i => i.Value).Take(maxItems).ToList();
        if (!displayItems.Any()) return;

        var maxValue = displayItems.Max(i => i.Value);
        if (maxValue <= 0) maxValue = 1;

        container.Column(col =>
        {
            if (!string.IsNullOrEmpty(title))
            {
                col.Item().PaddingBottom(8).Text(title).FontSize(10).Bold()
                    .FontColor(PdfReportComponents.SecondaryColor);
            }

            foreach (var item in displayItems)
            {
                col.Item().PaddingBottom(5).Row(row =>
                {
                    // Label
                    row.ConstantItem(120).AlignRight().PaddingRight(8)
                        .Text(Truncate(item.Label, 18)).FontSize(8).FontColor(PdfReportComponents.TextColor);

                    // Bar
                    var barWidthPct = item.Value / maxValue;
                    row.RelativeItem().Height(16).Svg(size =>
                        GenerateBarSvg(size.Width, size.Height, barWidthPct, item.Color));

                    // Value
                    row.ConstantItem(45).PaddingLeft(5).AlignLeft()
                        .Text(item.Value.ToString("N0")).FontSize(8).Bold()
                        .FontColor(PdfReportComponents.TextColor);
                });
            }
        });
    }

    #region SVG Generators

    private static string GenerateDonutSvg(float width, float height,
        List<ChartSegment> segments, string? centerLabel, string? centerValue)
    {
        var cx = width / 2;
        var cy = height / 2;
        var outerR = Math.Min(cx, cy) - 4;
        var innerR = outerR * 0.55f;
        var total = segments.Sum(s => s.Value);
        if (total <= 0) total = 1;

        var svg = $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {F(width)} {F(height)}'>";

        // Draw segments
        float startAngle = -90; // Start from top
        foreach (var seg in segments)
        {
            var sweepAngle = seg.Value / total * 360f;
            if (sweepAngle < 0.5f) { startAngle += sweepAngle; continue; }

            var path = DonutArcPath(cx, cy, outerR, innerR, startAngle, sweepAngle);
            svg += $"<path d='{path}' fill='{seg.Color}' />";
            startAngle += sweepAngle;
        }

        // Center circle (white)
        svg += $"<circle cx='{F(cx)}' cy='{F(cy)}' r='{F(innerR - 2)}' fill='white' />";

        // Center text
        if (!string.IsNullOrEmpty(centerValue))
        {
            svg += $"<text x='{F(cx)}' y='{F(cy - 2)}' text-anchor='middle' dominant-baseline='auto' " +
                   $"font-size='14' font-weight='bold' fill='#333'>{EscXml(centerValue)}</text>";
        }
        if (!string.IsNullOrEmpty(centerLabel))
        {
            svg += $"<text x='{F(cx)}' y='{F(cy + 12)}' text-anchor='middle' dominant-baseline='auto' " +
                   $"font-size='9' fill='#888'>{EscXml(centerLabel)}</text>";
        }

        svg += "</svg>";
        return svg;
    }

    private static string GenerateBarSvg(float width, float height, float fillPct, string color)
    {
        var barWidth = Math.Max(width * fillPct, 2);
        return $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {F(width)} {F(height)}'>" +
               $"<rect width='{F(width)}' height='{F(height)}' rx='3' fill='#F0F0F0' />" +
               $"<rect width='{F(barWidth)}' height='{F(height)}' rx='3' fill='{color}' />" +
               "</svg>";
    }

    private static string DonutArcPath(float cx, float cy, float outerR, float innerR,
        float startAngleDeg, float sweepAngleDeg)
    {
        // Clamp near-360 to avoid rendering glitch
        if (sweepAngleDeg >= 359.9f) sweepAngleDeg = 359.9f;

        var startRad = startAngleDeg * MathF.PI / 180f;
        var endRad = (startAngleDeg + sweepAngleDeg) * MathF.PI / 180f;
        var largeArc = sweepAngleDeg > 180 ? 1 : 0;

        float ox1 = cx + outerR * MathF.Cos(startRad);
        float oy1 = cy + outerR * MathF.Sin(startRad);
        float ox2 = cx + outerR * MathF.Cos(endRad);
        float oy2 = cy + outerR * MathF.Sin(endRad);

        float ix1 = cx + innerR * MathF.Cos(endRad);
        float iy1 = cy + innerR * MathF.Sin(endRad);
        float ix2 = cx + innerR * MathF.Cos(startRad);
        float iy2 = cy + innerR * MathF.Sin(startRad);

        return $"M{F(ox1)},{F(oy1)} A{F(outerR)},{F(outerR)} 0 {largeArc},1 {F(ox2)},{F(oy2)} " +
               $"L{F(ix1)},{F(iy1)} A{F(innerR)},{F(innerR)} 0 {largeArc},0 {F(ix2)},{F(iy2)} Z";
    }

    #endregion

    #region Helpers

    private static string F(float v) => v.ToString("F2", CultureInfo.InvariantCulture);
    private static string EscXml(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    #endregion
}
