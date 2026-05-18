using EmailAgent.Core.Models;
using System.Text;

namespace EmailAgent.Core.Templates
{
    /// <summary>
    /// Builder tạo HTML email chuyên nghiệp.
    /// Dùng inline CSS để tương thích Gmail, Outlook, Apple Mail.
    /// </summary>
    public class EmailTemplateBuilder
    {
        /// <summary>
        /// Tạo HTML email báo cáo doanh thu đầy đủ:
        /// Header gradient + logo text → Bảng tóm tắt → Bảng chi tiết → SVG chart → Footer
        /// </summary>
        public string BuildSalesReportEmail(
            string analysisText,
            string recipientTitle,
            List<SalesRecord> records)
        {
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            var totalRevenue = records.Sum(r => r.Revenue);
            var bestSeller = records.OrderByDescending(r => r.Revenue).FirstOrDefault();
            var top3 = records.OrderByDescending(r => r.Revenue).Take(5).ToList();

            // Build SVG chart
            var chartData = top3.Select(r => (r.Product, r.Revenue)).ToList();
            var svgChart = SvgChartBuilder.BuildBarChart(chartData);

            var sb = new StringBuilder();

            // ═══════════ HTML Shell ═══════════
            sb.AppendLine(@"<!DOCTYPE html>
<html lang=""vi"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""margin:0; padding:0; background-color:#F3F4F6; font-family:'Segoe UI',Roboto,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F3F4F6;"">
<tr><td align=""center"" style=""padding:24px 0;"">");

            // ═══════════ Main Container ═══════════
            sb.AppendLine(@"<table role=""presentation"" width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFFFFF; border-radius:12px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.08);"">");

            // ── Header gradient ──
            sb.AppendLine(@"<tr><td style=""background:linear-gradient(135deg,#4F46E5 0%,#7C3AED 50%,#2563EB 100%); padding:32px 40px; text-align:center;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr><td style=""text-align:center;"">
    <div style=""display:inline-block; background:rgba(255,255,255,0.15); border-radius:8px; padding:8px 20px; margin-bottom:12px;"">
      <span style=""color:#FFFFFF; font-size:16px; font-weight:700; letter-spacing:1px;"">📊 EMAIL AGENT</span>
    </div>
    <h1 style=""color:#FFFFFF; margin:12px 0 4px; font-size:22px; font-weight:700;"">Báo Cáo Doanh Thu</h1>
    <p style=""color:rgba(255,255,255,0.85); margin:0; font-size:14px;"">Cập nhật ngày " + today + @"</p>
  </td></tr></table>
</td></tr>");

            // ── Greeting ──
            sb.AppendLine($@"<tr><td style=""padding:28px 40px 0;"">
  <p style=""color:#1F2937; font-size:15px; margin:0 0 6px;"">Kính gửi <strong>{Escape(recipientTitle)}</strong>,</p>
  <p style=""color:#4B5563; font-size:14px; margin:0; line-height:1.6;"">Phòng Kinh doanh xin gửi báo cáo tóm tắt kết quả doanh thu như sau:</p>
</td></tr>");

            // ── Summary Cards ──
            sb.AppendLine($@"<tr><td style=""padding:20px 40px;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr>
    <td width=""48%"" style=""background:#EEF2FF; border-radius:10px; padding:18px 20px; text-align:center;"">
      <p style=""color:#6366F1; font-size:11px; font-weight:700; text-transform:uppercase; letter-spacing:1px; margin:0 0 6px;"">Tổng Doanh Thu</p>
      <p style=""color:#312E81; font-size:22px; font-weight:800; margin:0;"">{totalRevenue:N0} <span style=""font-size:13px;font-weight:600;"">VNĐ</span></p>
    </td>
    <td width=""4%"">&nbsp;</td>
    <td width=""48%"" style=""background:#F0FDF4; border-radius:10px; padding:18px 20px; text-align:center;"">
      <p style=""color:#16A34A; font-size:11px; font-weight:700; text-transform:uppercase; letter-spacing:1px; margin:0 0 6px;"">Sản Phẩm Bán Chạy</p>
      <p style=""color:#14532D; font-size:16px; font-weight:700; margin:0;"">{Escape(bestSeller?.Product ?? "N/A")}</p>
      <p style=""color:#166534; font-size:12px; margin:4px 0 0;"">{bestSeller?.Quantity ?? 0} đơn — {bestSeller?.Revenue ?? 0:N0} VNĐ</p>
    </td>
  </tr></table>
</td></tr>");

            // ── Data Table ──
            sb.AppendLine(@"<tr><td style=""padding:0 40px 20px;"">
  <p style=""color:#1F2937; font-size:14px; font-weight:700; margin:0 0 10px;"">📋 Chi Tiết Doanh Thu</p>
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;"">
  <tr style=""background:#4F46E5;"">
    <th style=""color:#FFF; padding:10px 12px; font-size:12px; text-align:left; font-weight:600;"">Ngày</th>
    <th style=""color:#FFF; padding:10px 12px; font-size:12px; text-align:left; font-weight:600;"">Sản phẩm</th>
    <th style=""color:#FFF; padding:10px 12px; font-size:12px; text-align:center; font-weight:600;"">SL</th>
    <th style=""color:#FFF; padding:10px 12px; font-size:12px; text-align:right; font-weight:600;"">Doanh thu</th>
  </tr>");

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                var bgColor = i % 2 == 0 ? "#FFFFFF" : "#F9FAFB";
                sb.AppendLine($@"  <tr style=""background:{bgColor};"">
    <td style=""padding:10px 12px; font-size:13px; color:#374151; border-bottom:1px solid #E5E7EB;"">{Escape(r.Date)}</td>
    <td style=""padding:10px 12px; font-size:13px; color:#374151; border-bottom:1px solid #E5E7EB; font-weight:500;"">{Escape(r.Product)}</td>
    <td style=""padding:10px 12px; font-size:13px; color:#374151; border-bottom:1px solid #E5E7EB; text-align:center;"">{r.Quantity}</td>
    <td style=""padding:10px 12px; font-size:13px; color:#374151; border-bottom:1px solid #E5E7EB; text-align:right; font-weight:600;"">{r.Revenue:N0}</td>
  </tr>");
            }

            // Total row
            sb.AppendLine($@"  <tr style=""background:#EEF2FF;"">
    <td colspan=""3"" style=""padding:12px; font-size:14px; color:#312E81; font-weight:700;"">TỔNG CỘNG</td>
    <td style=""padding:12px; font-size:14px; color:#312E81; font-weight:800; text-align:right;"">{totalRevenue:N0} VNĐ</td>
  </tr>
  </table>
</td></tr>");

            // ── SVG Chart ──
            if (!string.IsNullOrEmpty(svgChart))
            {
                sb.AppendLine($@"<tr><td style=""padding:0 40px 24px;"">
  <p style=""color:#1F2937; font-size:14px; font-weight:700; margin:0 0 10px;"">📊 Biểu Đồ Doanh Thu Top Sản Phẩm</p>
  <div style=""background:#F9FAFB; border-radius:8px; padding:16px; text-align:center;"">
    {svgChart}
  </div>
</td></tr>");
            }

            // ── AI Analysis ──
            var analysisHtml = analysisText.Replace("\n", "<br>");
            sb.AppendLine($@"<tr><td style=""padding:0 40px 24px;"">
  <div style=""background:linear-gradient(135deg,#FFFBEB 0%,#FEF3C7 100%); border-radius:10px; padding:20px; border-left:4px solid #F59E0B;"">
    <p style=""color:#92400E; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px; margin:0 0 8px;"">🤖 Phân Tích AI</p>
    <p style=""color:#78350F; font-size:13px; line-height:1.7; margin:0;"">{analysisHtml}</p>
  </div>
</td></tr>");

            // ── Closing ──
            sb.AppendLine($@"<tr><td style=""padding:0 40px 24px;"">
  <p style=""color:#4B5563; font-size:14px; line-height:1.6; margin:0;"">
    Phòng Kinh doanh sẽ tiếp tục theo dõi và cập nhật số liệu trong các tháng tiếp theo.<br>
    Mọi thắc mắc, kính mong {Escape(recipientTitle)} phản hồi để chúng tôi giải đáp kịp thời.
  </p>
  <p style=""color:#1F2937; font-size:14px; margin:20px 0 0;"">
    Trân trọng,<br>
    <strong>Phòng Kinh doanh</strong>
  </p>
</td></tr>");

            // ── Footer ──
            sb.AppendLine($@"<tr><td style=""background:#1F2937; padding:20px 40px; text-align:center;"">
  <p style=""color:rgba(255,255,255,0.6); font-size:11px; margin:0 0 4px;"">Email được tạo tự động bởi <strong style=""color:rgba(255,255,255,0.85);"">AI Email Agent</strong></p>
  <p style=""color:rgba(255,255,255,0.4); font-size:11px; margin:0;"">Powered by Semantic Kernel + Groq • {today}</p>
</td></tr>");

            // ═══════════ Close ═══════════
            sb.AppendLine(@"</table>
</td></tr></table>
</body>
</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Tạo HTML email nhắc việc (reminder) chuyên nghiệp.
        /// </summary>
        public string BuildReminderEmail(string title, DateTime reminderAt, string note)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"<!DOCTYPE html>
<html lang=""vi"">
<head><meta charset=""UTF-8""></head>
<body style=""margin:0; padding:0; background-color:#F3F4F6; font-family:'Segoe UI',Roboto,Arial,sans-serif;"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F3F4F6;"">
<tr><td align=""center"" style=""padding:24px 0;"">");

            sb.AppendLine(@"<table role=""presentation"" width=""520"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#FFFFFF; border-radius:12px; overflow:hidden; box-shadow:0 4px 24px rgba(0,0,0,0.08);"">");

            // Header
            sb.AppendLine(@"<tr><td style=""background:linear-gradient(135deg,#F59E0B 0%,#D97706 100%); padding:28px 36px; text-align:center;"">
  <h1 style=""color:#FFFFFF; margin:0; font-size:20px; font-weight:700;"">⏰ Nhắc Việc</h1>
</td></tr>");

            // Content
            sb.AppendLine($@"<tr><td style=""padding:28px 36px;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"">
  <tr><td style=""padding:12px 16px; background:#FEF3C7; border-radius:8px; margin-bottom:16px;"">
    <p style=""color:#92400E; font-size:18px; font-weight:700; margin:0;"">{Escape(title)}</p>
  </td></tr>
  <tr><td style=""padding:16px 0 0;"">
    <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
    <tr>
      <td style=""padding:6px 0; color:#6B7280; font-size:13px; width:100px;"">🕐 Thời gian:</td>
      <td style=""padding:6px 0; color:#1F2937; font-size:14px; font-weight:600;"">{reminderAt:dd/MM/yyyy HH:mm}</td>
    </tr>");

            if (!string.IsNullOrWhiteSpace(note))
            {
                sb.AppendLine($@"    <tr>
      <td style=""padding:6px 0; color:#6B7280; font-size:13px;"">📝 Ghi chú:</td>
      <td style=""padding:6px 0; color:#1F2937; font-size:14px;"">{Escape(note)}</td>
    </tr>");
            }

            sb.AppendLine(@"    </table>
  </td></tr></table>
  <p style=""color:#4B5563; font-size:13px; line-height:1.6; margin:20px 0 0;"">Vui lòng xử lý công việc này đúng hạn.</p>
</td></tr>");

            // Footer
            sb.AppendLine($@"<tr><td style=""background:#1F2937; padding:16px 36px; text-align:center;"">
  <p style=""color:rgba(255,255,255,0.5); font-size:11px; margin:0;"">🤖 AI Agent System — Tự động gửi lúc {DateTime.Now:HH:mm dd/MM/yyyy}</p>
</td></tr>");

            sb.AppendLine(@"</table>
</td></tr></table>
</body>
</html>");

            return sb.ToString();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
