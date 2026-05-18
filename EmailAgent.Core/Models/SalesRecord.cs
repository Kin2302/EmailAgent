namespace EmailAgent.Core.Models
{
    /// <summary>
    /// Model structured cho một dòng dữ liệu doanh thu.
    /// Dùng chung cho CSV, Excel, API — và để build bảng HTML + chart.
    /// </summary>
    public class SalesRecord
    {
        public string Date { get; set; } = "";
        public string Product { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Revenue { get; set; }

        /// <summary>
        /// Parse danh sách SalesRecord từ CSV text (header: Date,Product,Quantity,Price,Revenue).
        /// </summary>
        public static List<SalesRecord> ParseFromCsv(string csvContent)
        {
            var records = new List<SalesRecord>();
            if (string.IsNullOrWhiteSpace(csvContent)) return records;

            var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines.Skip(1)) // skip header
            {
                var cols = line.Split(',');
                if (cols.Length < 5) continue;

                if (int.TryParse(cols[2].Trim(), out int qty) &&
                    decimal.TryParse(cols[3].Trim(), out decimal price) &&
                    decimal.TryParse(cols[4].Trim(), out decimal revenue))
                {
                    records.Add(new SalesRecord
                    {
                        Date = cols[0].Trim(),
                        Product = cols[1].Trim(),
                        Quantity = qty,
                        Price = price,
                        Revenue = revenue
                    });
                }
            }

            return records;
        }
    }
}
