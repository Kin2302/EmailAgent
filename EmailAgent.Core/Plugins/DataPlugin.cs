using ClosedXML.Excel;
using EmailAgent.Core.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;

namespace EmailAgent.Core.Agents
{
    public class DataPlugin
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

        [KernelFunction("GetSalesData")]
        [Description("Đọc dữ liệu từ file CSV.")]
        public string GetSalesData(
            [Description("Đường dẫn tới file CSV")] string filePath)
        {
            if (!File.Exists(filePath))
                return $"LỖI: Không tìm thấy file tại '{filePath}'.";

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            ContentStore.Save(content, "rawdata");
            return Truncate(content);
        }

        [KernelFunction("GetDataFromExcel")]
        [Description("Đọc dữ liệu từ file Excel (.xlsx), trả về dạng CSV.")]
        public string GetExcelData(
            [Description("Đường dẫn tới file .xlsx")] string filePath,
            [Description("Tên sheet, mặc định Sheet1")] string sheetName = "Sheet1")
        {
            if (!File.Exists(filePath))
                return $"LỖI: Không tìm thấy file tại '{filePath}'.";
            try
            {
                using var wb = new XLWorkbook(filePath);
                var ws = wb.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
                         ?? wb.Worksheets.FirstOrDefault();
                if (ws == null) return "LỖI: Không có worksheet.";

                var range = ws.RangeUsed();
                if (range == null) return "LỖI: Sheet trống.";

                var sb = new StringBuilder();
                foreach (var row in range.Rows())
                    sb.AppendLine(string.Join(",", row.Cells().Select(c => c.GetString().Trim())));

                var result = sb.ToString().Trim();
                ContentStore.Save(result, "rawdata");
                return Truncate(result);
            }
            catch (Exception ex) { return $"LỖI: {ex.Message}"; }
        }

        [KernelFunction("GetDataFromApi")]
        [Description("Lấy dữ liệu từ REST API (GET), trả về JSON.")]
        public async Task<string> GetApiData(
            [Description("URL API (http/https)")] string apiUrl)
        {
            if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                return $"LỖI: URL không hợp lệ.";
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, uri);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var resp = await _httpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return $"LỖI: HTTP {(int)resp.StatusCode}.";
                var content = await resp.Content.ReadAsStringAsync();
                if (content.Length > 50_000) content = content[..50_000];
                ContentStore.Save(content, "rawdata");
                return Truncate(content);
            }
            catch (TaskCanceledException) { return "LỖI: Timeout (30s)."; }
            catch (Exception ex) { return $"LỖI: {ex.Message}"; }
        }

        private static string Truncate(string content, int maxLines = 12)
        {
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length <= maxLines) return content;
            return string.Join("\n", lines.Take(maxLines)) + $"\n... (tổng {lines.Length} dòng)";
        }
    }
}
