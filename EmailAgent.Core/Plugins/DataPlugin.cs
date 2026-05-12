using EmailAgent.Core.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmailAgent.Core.Agents
{
    public class DataPlugin
    {
        [KernelFunction("GetSalesData")]
        [Description("Đọc và lấy dữ liệu doanh thu thô từ file CSV.")]
        public string GetSalesData(
            [Description("Đường dẫn đầy đủ tới file CSV")] string filePath)
        {
            if (!File.Exists(filePath))
                return $"LỖI: Không tìm thấy file dữ liệu tại '{filePath}'.";

            return File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }
    }
}
