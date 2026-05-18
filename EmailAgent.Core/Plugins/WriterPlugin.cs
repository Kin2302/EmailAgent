using EmailAgent.Core.Models;
using EmailAgent.Core.Services;
using EmailAgent.Core.Templates;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EmailAgent.Core.Agents
{
    /// <summary>
    /// Soạn email HTML. Tự lấy raw data từ ContentStore — AI không truyền data lớn.
    /// Lưu HTML vào ContentStore, chỉ trả ID ngắn cho AI.
    /// </summary>
    public class WriterPlugin
    {
        private readonly EmailTemplateBuilder _templateBuilder = new();

        [KernelFunction("WriteEmailContent")]
        [Description("Soạn email HTML từ kết quả phân tích. Gọi sau AnalyzeSalesData. Trả về subject + emailId.")]
        public string WriteEmailContent(
            [Description("Kết quả phân tích từ AnalyzeSalesData")] string analysisSummary,
            [Description("Chức danh người nhận (VD: Anh/Chị, Giám đốc)")] string recipientTitle = "Anh/Chị")
        {
            if (string.IsNullOrWhiteSpace(analysisSummary) || analysisSummary.StartsWith("LỖI"))
                return "LỖI: Không có dữ liệu phân tích.";

            var today = DateTime.Now.ToString("dd/MM/yyyy");
            var subject = $"Báo cáo Doanh thu – Cập nhật {today}";

            // Lấy raw data từ ContentStore (DataPlugin đã lưu)
            var rawData = ContentStore.GetLatestByTag("rawdata");
            var records = rawData != null ? SalesRecord.ParseFromCsv(rawData) : new List<SalesRecord>();

            string body;
            if (records.Count > 0)
                body = _templateBuilder.BuildSalesReportEmail(analysisSummary, recipientTitle, records);
            else
                body = BuildFallback(analysisSummary, recipientTitle, today);

            // Lưu HTML vào store → chỉ trả ID ngắn
            var emailId = ContentStore.Save(body, "email");

            return $"SUBJECT: {subject}\nEMAIL_ID: {emailId}\n(Email HTML đã tạo xong, sẵn sàng review)";
        }

        private static string BuildFallback(string analysis, string recipient, string today)
        {
            var html = analysis.Replace("\n", "<br>");
            return $@"<!DOCTYPE html><html lang=""vi""><head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#F3F4F6;font-family:'Segoe UI',Arial,sans-serif"">
<table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#F3F4F6""><tr><td align=""center"" style=""padding:24px 0"">
<table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background:#FFF;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.08)"">
<tr><td style=""background:linear-gradient(135deg,#4F46E5,#7C3AED,#2563EB);padding:28px 40px;text-align:center"">
<h1 style=""color:#FFF;margin:0;font-size:20px"">📊 Báo Cáo Doanh Thu</h1></td></tr>
<tr><td style=""padding:28px 40px"">
<p style=""color:#1F2937;font-size:15px;margin:0 0 16px"">Kính gửi <b>{recipient}</b>,</p>
<div style=""background:#F9FAFB;border-radius:8px;padding:16px;border-left:4px solid #4F46E5"">
<p style=""color:#374151;font-size:13px;line-height:1.7;margin:0"">{html}</p></div>
<p style=""color:#4B5563;font-size:14px;margin:20px 0 0"">Trân trọng,<br><b>Phòng Kinh doanh</b></p></td></tr>
<tr><td style=""background:#1F2937;padding:16px 40px;text-align:center"">
<p style=""color:rgba(255,255,255,.5);font-size:11px;margin:0"">AI Email Agent • {today}</p></td></tr>
</table></td></tr></table></body></html>";
        }
    }
}
