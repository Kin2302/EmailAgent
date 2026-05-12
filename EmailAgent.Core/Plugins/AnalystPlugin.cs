using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EmailAgent.Core.Agents
{
    /// <summary>
    /// SK Plugin: Phân tích dữ liệu doanh thu CSV.
    /// AI Orchestrator tự quyết định gọi hàm này sau khi có dữ liệu thô.
    /// </summary>
    public class AnalystPlugin
    {
        [KernelFunction("AnalyzeSalesData")]
        [Description("Phân tích dữ liệu doanh thu CSV thô và trả về bản tóm tắt gồm: tổng doanh thu, sản phẩm bán chạy nhất, xu hướng. Gọi hàm này sau khi đã có dữ liệu CSV từ GetSalesData.")]
        public string AnalyzeSalesData(
            [Description("Nội dung CSV doanh thu thô cần phân tích")] string csvContent)
        {
            if (string.IsNullOrWhiteSpace(csvContent) || csvContent.StartsWith("LỖI"))
                return "LỖI: Không có dữ liệu để phân tích.";

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 2)
                return "LỖI: File CSV không có dữ liệu hợp lệ.";

            // Parse CSV
            var records = new List<(string product, int qty, decimal price, decimal revenue)>();
            foreach (var line in lines.Skip(1))
            {
                var cols = line.Split(',');
                if (cols.Length < 5) continue;

                if (int.TryParse(cols[2].Trim(), out int qty) &&
                    decimal.TryParse(cols[3].Trim(), out decimal price) &&
                    decimal.TryParse(cols[4].Trim(), out decimal revenue))
                {
                    records.Add((cols[1].Trim(), qty, price, revenue));
                }
            }

            if (records.Count == 0)
                return "LỖI: Không parse được dữ liệu từ CSV.";

            var totalRevenue = records.Sum(r => r.revenue);
            var bestSeller = records.OrderByDescending(r => r.revenue).First();
            var top3 = records.OrderByDescending(r => r.revenue).Take(3).ToList();

            var summary = $"""
                TỔNG DOANH THU: {totalRevenue:N0} VNĐ
                SẢN PHẨM BÁN CHẠY NHẤT: {bestSeller.product} ({bestSeller.qty} đơn, {bestSeller.revenue:N0} VNĐ)
                XU HƯỚNG: {(totalRevenue > 100_000_000 ? "Doanh thu tháng 5 khả quan, vượt mốc 100 triệu VNĐ" : "Doanh thu tháng 5 ổn định")}
                CHI TIẾT TOP 3:
                {string.Join("\n", top3.Select((r, i) => $"  {i + 1}. {r.product}: {r.revenue:N0} VNĐ ({r.qty} đơn)"))}
                """;

            return summary;
        }
    }
}
