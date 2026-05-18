using System.Text;

namespace EmailAgent.Core.Templates
{
    public static class SvgChartBuilder
    {
        private static readonly string[] Colors = { "#4F46E5", "#7C3AED", "#2563EB", "#0891B2", "#059669", "#D97706", "#DC2626", "#DB2777" };

        public static string BuildBarChart(List<(string label, decimal value)> data, int width = 520, int barHeight = 32, int gap = 12)
        {
            if (data == null || data.Count == 0) return "";

            var maxVal = data.Max(d => d.value);
            if (maxVal == 0) maxVal = 1;

            int labelW = 160, chartW = width - labelW - 80;
            int totalH = data.Count * (barHeight + gap) + 40;

            var sb = new StringBuilder();
            sb.AppendLine($@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{width}"" height=""{totalH}"" viewBox=""0 0 {width} {totalH}"">");
            sb.AppendLine(@"<style>.cl{font-family:'Segoe UI',Arial,sans-serif;font-size:13px;fill:#374151}.cv{font-family:'Segoe UI',Arial,sans-serif;font-size:12px;fill:#6B7280;font-weight:600}</style>");

            int y = 20;
            for (int i = 0; i < data.Count; i++)
            {
                var (label, value) = data[i];
                int barW = Math.Max(4, (int)(value / maxVal * chartW));
                var color = Colors[i % Colors.Length];
                var shortLabel = label.Length > 20 ? label[..17] + "..." : label;
                var fmtVal = value >= 1_000_000 ? $"{value / 1_000_000:0.#}tr" : $"{value:N0}";

                sb.AppendLine($@"<text x=""{labelW - 8}"" y=""{y + barHeight / 2 + 5}"" text-anchor=""end"" class=""cl"">{Esc(shortLabel)}</text>");
                sb.AppendLine($@"<rect x=""{labelW}"" y=""{y}"" width=""{barW}"" height=""{barHeight}"" rx=""4"" fill=""{color}"" opacity=""0.9""/>");
                sb.AppendLine($@"<text x=""{labelW + barW + 8}"" y=""{y + barHeight / 2 + 5}"" class=""cv"">{fmtVal}</text>");
                y += barHeight + gap;
            }
            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static string Esc(string t) => t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
