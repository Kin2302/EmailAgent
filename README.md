# 🤖 AI Email Agent

> Trợ lý AI thông minh tự động hóa công việc văn phòng: phân tích doanh thu, soạn email HTML chuyên nghiệp, quản lý nhắc việc — tất cả bằng ngôn ngữ tự nhiên tiếng Việt.

**Công nghệ:** .NET 9 · Semantic Kernel · Groq LLM · ClosedXML

---

## ✨ Tính năng

### 📊 Phân tích doanh thu đa nguồn
- **CSV** — đọc file `.csv` truyền thống
- **Excel** — đọc file `.xlsx` qua ClosedXML (MIT license)
- **REST API** — fetch JSON từ bất kỳ API endpoint nào

### 📧 Email HTML chuyên nghiệp
- Template gradient header + logo text
- Bảng dữ liệu chi tiết có style
- Biểu đồ SVG inline (bar chart)
- Phần phân tích AI highlight
- Footer chuyên nghiệp
- Tương thích Gmail, Outlook, Apple Mail (inline CSS)

### ⏰ Quản lý nhắc việc
- Đặt lịch nhắc bằng ngôn ngữ tự nhiên
- Tự động gửi email HTML nhắc đúng giờ
- Xem / hoàn thành / xóa nhắc việc

### 🛡️ Human-in-the-Loop
- Review email trước khi gửi
- Sửa tiêu đề / nội dung
- Xem HTML preview trong browser
- Hủy gửi bất cứ lúc nào

### ⚡ Tối ưu Token
- `ContentStore` lưu data lớn (HTML, raw data) trong bộ nhớ
- LLM chỉ nhận bản tóm tắt ngắn + ID tham chiếu
- Giảm ~80% token so với truyền thẳng HTML qua chat history

---

## 🏗️ Kiến trúc

```
EmailAgent.Console/          ← Entry point (CLI chatbot)
│   Program.cs
│   appsettings.json
│
EmailAgent.Core/             ← Business logic
│
├── Plugins/                 ← Semantic Kernel Plugins
│   ├── DataPlugin.cs        ← Đọc CSV / Excel / API
│   ├── AnalystPlugin.cs     ← Phân tích doanh thu
│   ├── WriterPlugin.cs      ← Soạn email HTML
│   ├── ReviewPlugin.cs      ← Human review gate
│   ├── SenderPlugin.cs      ← Gửi SMTP (HTML/plain)
│   └── ReminderPlugin.cs    ← CRUD nhắc việc
│
├── Templates/               ← Email HTML builders
│   ├── EmailTemplateBuilder.cs  ← Sales report + Reminder templates
│   └── SvgChartBuilder.cs       ← Inline SVG bar chart
│
├── Services/
│   ├── ContentStore.cs              ← In-memory store giảm token
│   └── ReminderBackgroundService.cs ← Background email scheduler
│
├── Orchestrator/
│   └── AgentOrchestrator.cs  ← SK Agent + chat history
│
├── Models/
│   ├── SalesRecord.cs        ← Structured sales data + CSV parser
│   ├── EmailContent.cs
│   ├── ReportSummary.cs
│   └── SalesData.cs
│
├── Filters/
│   └── AgentLoggingFilter.cs ← Log realtime plugin calls
│
└── Data/
    ├── sales_may.csv         ← Dữ liệu mẫu CSV
    └── sample_sales.xlsx     ← Dữ liệu mẫu Excel
```

### Luồng xử lý báo cáo

```
User: "gửi báo cáo D:\data.xlsx cho abc@gmail.com"
  │
  ▼
① DataPlugin.GetDataFromExcel()
   → Đọc Excel, lưu raw data vào ContentStore
   → Trả preview ngắn (12 dòng) cho LLM
  │
  ▼
② AnalystPlugin.AnalyzeSalesData()
   → Tổng doanh thu, top sản phẩm, xu hướng
  │
  ▼
③ WriterPlugin.WriteEmailContent()
   → Lấy raw data từ ContentStore
   → Build HTML (bảng + chart + AI analysis)
   → Lưu HTML vào ContentStore
   → Trả subject + emailId ngắn cho LLM
  │
  ▼
④ ReviewPlugin.ReviewAndSend()
   → Lấy HTML từ ContentStore bằng emailId
   → Hiển thị tóm tắt trên console
   → User chọn: Gửi / Sửa / Preview / Hủy
   → Gửi qua SMTP (HTML)
```

---

## 🚀 Cài đặt

### Yêu cầu
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Tài khoản [Groq](https://console.groq.com/) (free tier OK)
- Gmail App Password (hoặc SMTP server khác)

### Clone

```bash
git clone https://github.com/<your-username>/EmailAgent.git
cd EmailAgent
```

### Cấu hình

#### Cách 1: `appsettings.json` (nhanh, cho dev)

Sửa file `EmailAgent.Console/appsettings.json`:

```json
{
  "Groq": {
    "ApiKey": "gsk_YOUR_GROQ_API_KEY",
    "WriterModel": "meta-llama/llama-4-scout-17b-16e-instruct"
  },
  "Smtp": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "your-email@gmail.com",
    "AppPassword": "xxxx xxxx xxxx xxxx"
  }
}
```

#### Cách 2: User Secrets (an toàn, khuyến nghị)

```bash
cd EmailAgent.Console
dotnet user-secrets set "Groq:ApiKey" "gsk_YOUR_GROQ_API_KEY"
dotnet user-secrets set "Smtp:SenderEmail" "your-email@gmail.com"
dotnet user-secrets set "Smtp:AppPassword" "xxxx xxxx xxxx xxxx"
```

### Lấy Groq API Key

1. Đăng ký tại [console.groq.com](https://console.groq.com/)
2. Vào **API Keys** → **Create API Key**
3. Copy key dạng `gsk_...`

### Lấy Gmail App Password

1. Vào [myaccount.google.com/security](https://myaccount.google.com/security)
2. Bật **Xác minh 2 bước** (nếu chưa bật)
3. Tìm **Mật khẩu ứng dụng** (App Passwords)
4. Tạo mật khẩu mới → copy 16 ký tự (dạng `xxxx xxxx xxxx xxxx`)

---

## ▶️ Chạy

```bash
dotnet run --project EmailAgent.Console
```

### Ví dụ sử dụng

```
╔══════════════════════════════════════════════════════╗
║      🤖 AI AGENT - TRỢ LÝ CÔNG VIỆC THÔNG MINH       ║
╚══════════════════════════════════════════════════════╝

Bạn: gửi báo cáo doanh thu D:\DotNet\Solution2\EmailAgent.Core\Data\sales_may.csv cho abc@gmail.com

🤖 Đang xử lý...
[SK Filter] → AI gọi: DataPlugin.GetSalesData
[SK Filter] ✓ Hoàn tất: GetSalesData (5ms)
[SK Filter] → AI gọi: AnalystPlugin.AnalyzeSalesData
[SK Filter] ✓ Hoàn tất: AnalyzeSalesData (2ms)
[SK Filter] → AI gọi: WriterPlugin.WriteEmailContent
[SK Filter] ✓ Hoàn tất: WriteEmailContent (8ms)
[SK Filter] → AI gọi: ReviewPlugin.ReviewAndSend

╔══════════════════════════════════════════════════════╗
║       📧 EMAIL AI ĐÃ SOẠN — VUI LÒNG KIỂM TRA        ║
╚══════════════════════════════════════════════════════╝
  Người nhận : abc@gmail.com
  Tiêu đề   : Báo cáo Doanh thu – Cập nhật 18/05/2026
  Định dạng  : 📄 HTML (bảng + biểu đồ)

  [1] Gửi luôn
  [2] Sửa tiêu đề
  [3] Sửa nội dung
  [4] Hủy
  [5] Xem HTML preview (mở browser)

  Chọn: 5
  ✅ Đã mở HTML preview trong trình duyệt.

  Chọn: 1
  📤 Đang gửi...
  ✅ Gửi thành công tới: abc@gmail.com
```

### Thêm ví dụ

```
Bạn: phân tích file D:\report.xlsx gửi cho boss@company.com

Bạn: nhắc tôi họp team lúc 15:00 qua email abc@gmail.com

Bạn: danh sách việc cần làm

Bạn: xong việc số 1
```

---

## 📦 NuGet Packages

| Package | Mục đích |
|---------|----------|
| `Microsoft.SemanticKernel` 1.75.0 | AI orchestration framework |
| `Microsoft.SemanticKernel.Agents.Core` 1.75.0 | Chat completion agent |
| `ClosedXML` 0.104.2 | Đọc/ghi Excel .xlsx (MIT license) |

---

## 🗺️ Roadmap

- [ ] Google Sheets API — đọc trực tiếp từ Sheets
- [ ] Attachment support — đính kèm file Excel/PDF
- [ ] Multi-language templates — English, Japanese
- [ ] Database persistence — lưu reminder vào SQLite
- [ ] Web UI — Blazor dashboard thay thế CLI

---

## 📄 License

MIT
