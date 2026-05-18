using EmailAgent.Core.Models;
using System.Text;

namespace EmailAgent.Core.Templates
{
    public class EmailTemplateBuilder
    {
        public string BuildSalesReportEmail(string analysisText, string recipientTitle, List<SalesRecord> records)
        {
            var today = DateTime.Now.ToString("dd/MM/yyyy");
            var totalRevenue = records.Sum(r => r.Revenue);
            var best = records.OrderByDescending(r => r.Revenue).FirstOrDefault();
            var topItems = records.OrderByDescending(r => r.Revenue).Take(5).ToList();
            var svgChart = SvgChartBuilder.BuildBarChart(topItems.Select(r => (r.Product, r.Revenue)).ToList());

            var sb = new StringBuilder();
            sb.Append($@"<!DOCTYPE html>
<html lang=""vi""><head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#F3F4F6;font-family:'Segoe UI',Roboto,Arial,sans-serif"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#F3F4F6""><tr><td align=""center"" style=""padding:24px 0"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#FFF;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08)"">

<tr><td style=""background:linear-gradient(135deg,#4F46E5,#7C3AED,#2563EB);padding:32px 40px;text-align:center"">
<div style=""display:inline-block;background:rgba(255,255,255,.15);border-radius:8px;padding:8px 20px;margin-bottom:12px"">
<span style=""color:#FFF;font-size:16px;font-weight:700;letter-spacing:1px"">📊 EMAIL AGENT</span></div>
<h1 style=""color:#FFF;margin:12px 0 4px;font-size:22px"">Báo Cáo Doanh Thu</h1>
<p style=""color:rgba(255,255,255,.85);margin:0;font-size:14px"">Cập nhật ngày {today}</p>
</td></tr>

<tr><td style=""padding:28px 40px 0"">
<p style=""color:#1F2937;font-size:15px;margin:0 0 6px"">Kính gửi <b>{Esc(recipientTitle)}</b>,</p>
<p style=""color:#4B5563;font-size:14px;margin:0;line-height:1.6"">Phòng Kinh doanh xin gửi báo cáo doanh thu:</p>
</td></tr>

<tr><td style=""padding:20px 40px"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0""><tr>
<td width=""48%"" style=""background:#EEF2FF;border-radius:10px;padding:18px 20px;text-align:center"">
<p style=""color:#6366F1;font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;margin:0 0 6px"">Tổng Doanh Thu</p>
<p style=""color:#312E81;font-size:22px;font-weight:800;margin:0"">{totalRevenue:N0} <span style=""font-size:13px"">VNĐ</span></p>
</td><td width=""4%""></td>
<td width=""48%"" style=""background:#F0FDF4;border-radius:10px;padding:18px 20px;text-align:center"">
<p style=""color:#16A34A;font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px;margin:0 0 6px"">Sản Phẩm Bán Chạy</p>
<p style=""color:#14532D;font-size:16px;font-weight:700;margin:0"">{Esc(best?.Product ?? "N/A")}</p>
<p style=""color:#166534;font-size:12px;margin:4px 0 0"">{best?.Quantity ?? 0} đơn — {best?.Revenue ?? 0:N0} VNĐ</p>
</td></tr></table>
</td></tr>

<tr><td style=""padding:0 40px 20px"">
<p style=""color:#1F2937;font-size:14px;font-weight:700;margin:0 0 10px"">📋 Chi Tiết</p>
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse"">
<tr style=""background:#4F46E5"">
<th style=""color:#FFF;padding:10px 12px;font-size:12px;text-align:left"">Ngày</th>
<th style=""color:#FFF;padding:10px 12px;font-size:12px;text-align:left"">Sản phẩm</th>
<th style=""color:#FFF;padding:10px 12px;font-size:12px;text-align:center"">SL</th>
<th style=""color:#FFF;padding:10px 12px;font-size:12px;text-align:right"">Doanh thu</th></tr>");

            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                var bg = i % 2 == 0 ? "#FFF" : "#F9FAFB";
                sb.Append($@"<tr style=""background:{bg}"">
<td style=""padding:10px 12px;font-size:13px;color:#374151;border-bottom:1px solid #E5E7EB"">{Esc(r.Date)}</td>
<td style=""padding:10px 12px;font-size:13px;color:#374151;border-bottom:1px solid #E5E7EB;font-weight:500"">{Esc(r.Product)}</td>
<td style=""padding:10px 12px;font-size:13px;color:#374151;border-bottom:1px solid #E5E7EB;text-align:center"">{r.Quantity}</td>
<td style=""padding:10px 12px;font-size:13px;color:#374151;border-bottom:1px solid #E5E7EB;text-align:right;font-weight:600"">{r.Revenue:N0}</td></tr>");
            }

            sb.Append($@"<tr style=""background:#EEF2FF"">
<td colspan=""3"" style=""padding:12px;font-size:14px;color:#312E81;font-weight:700"">TỔNG CỘNG</td>
<td style=""padding:12px;font-size:14px;color:#312E81;font-weight:800;text-align:right"">{totalRevenue:N0} VNĐ</td></tr></table>
</td></tr>");

            if (!string.IsNullOrEmpty(svgChart))
            {
                sb.Append($@"<tr><td style=""padding:0 40px 24px"">
<p style=""color:#1F2937;font-size:14px;font-weight:700;margin:0 0 10px"">📊 Biểu Đồ Top Sản Phẩm</p>
<div style=""background:#F9FAFB;border-radius:8px;padding:16px;text-align:center"">{svgChart}</div>
</td></tr>");
            }

            var analysisHtml = analysisText.Replace("\n", "<br>");
            sb.Append($@"<tr><td style=""padding:0 40px 24px"">
<div style=""background:linear-gradient(135deg,#FFFBEB,#FEF3C7);border-radius:10px;padding:20px;border-left:4px solid #F59E0B"">
<p style=""color:#92400E;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:1px;margin:0 0 8px"">🤖 Phân Tích AI</p>
<p style=""color:#78350F;font-size:13px;line-height:1.7;margin:0"">{analysisHtml}</p></div>
</td></tr>

<tr><td style=""padding:0 40px 24px"">
<p style=""color:#4B5563;font-size:14px;line-height:1.6;margin:0"">Trân trọng,<br><b>Phòng Kinh doanh</b></p>
</td></tr>

<tr><td style=""background:#1F2937;padding:20px 40px;text-align:center"">
<p style=""color:rgba(255,255,255,.6);font-size:11px;margin:0 0 4px"">Email tạo bởi <b style=""color:rgba(255,255,255,.85)"">AI Email Agent</b></p>
<p style=""color:rgba(255,255,255,.4);font-size:11px;margin:0"">Semantic Kernel + Groq • {today}</p>
</td></tr>

</table></td></tr></table></body></html>");
            return sb.ToString();
        }

        public string BuildReminderEmail(string title, DateTime reminderAt, string note)
        {
            var sb = new StringBuilder();
            sb.Append($@"<!DOCTYPE html>
<html lang=""vi""><head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#F3F4F6;font-family:'Segoe UI',Roboto,Arial,sans-serif"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#F3F4F6""><tr><td align=""center"" style=""padding:24px 0"">
<table width=""520"" cellpadding=""0"" cellspacing=""0"" style=""background:#FFF;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08)"">

<tr><td style=""background:linear-gradient(135deg,#F59E0B,#D97706);padding:28px 36px;text-align:center"">
<h1 style=""color:#FFF;margin:0;font-size:20px"">⏰ Nhắc Việc</h1></td></tr>

<tr><td style=""padding:28px 36px"">
<div style=""padding:12px 16px;background:#FEF3C7;border-radius:8px;margin-bottom:16px"">
<p style=""color:#92400E;font-size:18px;font-weight:700;margin:0"">{Esc(title)}</p></div>
<p style=""color:#6B7280;font-size:13px;margin:0 0 4px"">🕐 Thời gian: <b style=""color:#1F2937;font-size:14px"">{reminderAt:dd/MM/yyyy HH:mm}</b></p>");

            if (!string.IsNullOrWhiteSpace(note))
                sb.Append($@"<p style=""color:#6B7280;font-size:13px;margin:4px 0"">📝 Ghi chú: <span style=""color:#1F2937"">{Esc(note)}</span></p>");

            sb.Append($@"<p style=""color:#4B5563;font-size:13px;margin:20px 0 0"">Vui lòng xử lý công việc này đúng hạn.</p>
</td></tr>

<tr><td style=""background:#1F2937;padding:16px 36px;text-align:center"">
<p style=""color:rgba(255,255,255,.5);font-size:11px;margin:0"">🤖 AI Agent — {DateTime.Now:HH:mm dd/MM/yyyy}</p></td></tr>

</table></td></tr></table></body></html>");
            return sb.ToString();
        }

        private static string Esc(string t) => string.IsNullOrEmpty(t) ? "" : t.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
