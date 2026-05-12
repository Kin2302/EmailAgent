using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EmailAgent.Core.Agents
{
    /// <summary>
    /// SK Plugin: Soạn thảo nội dung email tiếng Việt.
    /// AI Orchestrator tự quyết định gọi hàm này sau khi có bản tóm tắt phân tích.
    /// </summary>
    public class WriterPlugin
    {
        [KernelFunction("WriteEmailContent")]
        [Description("Soạn nội dung email tiếng Việt chuyên nghiệp dựa trên bản tóm tắt phân tích doanh thu. Gọi hàm này sau khi đã có kết quả từ AnalyzeSalesData. Trả về định dạng SUBJECT: ... và BODY: ...")]
        public string WriteEmailContent(
            [Description("Bản tóm tắt phân tích doanh thu từ AnalyzeSalesData")] string analysisSummary,
            [Description("Tên hoặc chức danh người nhận email (VD: Anh/Chị, Giám đốc)")] string recipientTitle = "Anh/Chị")
        {
            if (string.IsNullOrWhiteSpace(analysisSummary) || analysisSummary.StartsWith("LỖI"))
                return "LỖI: Không có dữ liệu phân tích để soạn email.";

            var today = DateTime.Now.ToString("dd/MM/yyyy");

            var subject = $"Báo cáo Doanh thu Tháng 5/2026 – Cập nhật {today}";

            var body = $"""
                Kính gửi {recipientTitle},

                Phòng Kinh doanh xin gửi báo cáo tóm tắt kết quả doanh thu tháng 5/2026 như sau:

                {analysisSummary}

                Phòng Kinh doanh sẽ tiếp tục theo dõi và cập nhật số liệu trong các tháng tiếp theo.
                Mọi thắc mắc, kính mong {recipientTitle} phản hồi để chúng tôi giải đáp kịp thời.

                Trân trọng,
                Phòng Kinh doanh
                Ngày {today}
                """;

            return $"SUBJECT: {subject}\nBODY: {body}";
        }
    }
}
