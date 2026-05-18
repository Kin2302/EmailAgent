using System.Text;

namespace EmailAgent.Core.Templates
{
    /// <summary>
    /// Tạo SVG chart inline cho email HTML.
    /// Dùng horizontal bar chart vì hiển thị tốt trong email clients.
    /// </summary>
    public static class SvgChartBuilder
    {
        // Bảng màu gradient chuyên nghiệp
        private static readonly string[] BarColors = new[]
        {
            "#4F46E5", // Indigo
            "#7C3AED", // Violet
            "#2563EB", // Blue
            "#0891B2", // Cyan
            "#059669", // Emerald
            "#D97706", // Amber
            "#DC2626", // Red
            "#DB2777", // Pink
        };

        /// <summary>
        /// Tạo horizontal bar chart SVG từ data.
        /// </summary>
        public static string BuildBarChart(
            List<(string label, decimal value)> data,
            int width = 520,
            int barHeight = 32,
            int gap = 12)
        {
            if (data == null || data.Count == 0)
                return string.Empty;

            var maxValue = data.Max(d => d.value);
            if (maxValue == 0) maxValue = 1;

            int labelWidth = 160;
            int chartWidth = width - labelWidth - 80; // 80 cho value text
            int totalHeight = data.Count * (barHeight + gap) + 40; // 40 padding

            var sb = new StringBuilder();
            sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{totalHeight}"" viewBox=""0 0 {width} {totalHeight}"">");
            sb.AppendLine(@"  <style>
    .chart-label { font-family: 'Segoe UI', Arial, sans-serif; font-size: 13px; fill: #374151; }
    .chart-value { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; fill: #6B7280; font-weight: 600; }
  </style>");

            int y = 20;
            for (int i = 0; i < data.Count; i++)
            {
                var (label, value) = data[i];
                var barWidth = (int)(value / maxValue * chartWidth);
                if (barWidth < 4) barWidth = 4; // minimum visible bar
                var color = BarColors[i % BarColors.Length];

                // Truncate label if too long
                var displayLabel = label.Length > 20 ? label[..17] + "..." : label;
                var formattedValue = FormatCurrency(value);

                // Label text
                sb.AppendLine($@"  <text x=""{labelWidth - 8}"" y=""{y + barHeight / 2 + 5}"" text-anchor=""end"" class=""chart-label"">{EscapeXml(displayLabel)}</text>");

                // Bar with rounded corners
                sb.AppendLine($@"  <rect x=""{labelWidth}"" y=""{y}"" width=""{barWidth}"" height=""{barHeight}"" rx=""4"" ry=""4"" fill=""{color}"" opacity=""0.9""/>");

                // Value text
                sb.AppendLine($@"  <text x=""{labelWidth + barWidth + 8}"" y=""{y + barHeight / 2 + 5}"" class=""chart-value"">{formattedValue}</text>");

                y += barHeight + gap;
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string FormatCurrency(decimal value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000:0.#} tỷ";
            if (value >= 1_000_000)
                return $"{value / 1_000_000:0.#} triệu";
            if (value >= 1_000)
                return $"{value / 1_000:0.#}k";
            return value.ToString("N0");
        }

        private static string EscapeXml(string text)
        {
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
