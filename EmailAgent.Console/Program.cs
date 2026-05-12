using EmailAgent.Core.Orchestrator;
using EmailAgent.Core.Services;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

// ══════════════════════════════════════════════
// ĐỌC CẤU HÌNH
// ══════════════════════════════════════════════
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

// ══════════════════════════════════════════════
// KHỞI TẠO ORCHESTRATOR + BACKGROUND SERVICE
// ══════════════════════════════════════════════
var orchestrator = new AgentOrchestrator(
    config["Groq:ApiKey"]!,
    config["Groq:WriterModel"]!,
    config["Smtp:Server"]!,
    int.Parse(config["Smtp:Port"]!),
    config["Smtp:SenderEmail"]!,
    config["Smtp:AppPassword"]!
);

orchestrator.Initialize();

// Khởi động background service kiểm tra reminder mỗi 30 giây
var reminderService = new ReminderBackgroundService(orchestrator.SenderPlugin);
reminderService.Start();

// ══════════════════════════════════════════════
// WELCOME BANNER
// ══════════════════════════════════════════════
Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║      🤖 AI AGENT - TRỢ LÝ CÔNG VIỆC THÔNG MINH     ║");
Console.WriteLine("║          Powered by Semantic Kernel + Groq           ║");
Console.WriteLine("╠══════════════════════════════════════════════════════╣");
Console.WriteLine("║  Gợi ý:                                              ║");
Console.WriteLine("║  • gửi báo cáo doanh thu D:\\data.csv cho a@b.com   ║");
Console.WriteLine("║  • nhắc tôi họp lúc 15:00 qua email abc@gmail.com   ║");
Console.WriteLine("║  • danh sách việc cần làm                            ║");
Console.WriteLine("║  • xong việc số 1                                    ║");
Console.WriteLine("║  • exit để thoát                                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

// ══════════════════════════════════════════════
// CHATBOT LOOP
// ══════════════════════════════════════════════
while (true)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Bạn: ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input)) continue;

    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("thoát", StringComparison.OrdinalIgnoreCase) ||
        input.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        reminderService.Stop();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n🤖 Tạm biệt! Hẹn gặp lại.\n");
        Console.ResetColor();
        break;
    }

    try
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("🤖 Đang xử lý...");
        Console.ResetColor();

        var response = await orchestrator.ChatAsync(input);

        // ── Nếu response chứa email → bước review ──
        if (response.Contains("SUBJECT:", StringComparison.OrdinalIgnoreCase) &&
            response.Contains("BODY:", StringComparison.OrdinalIgnoreCase))
        {
            var (subject, body) = AgentOrchestrator.ParseEmailContent(response);

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║       📧 EMAIL AI ĐÃ SOẠN — VUI LÒNG KIỂM TRA      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Tiêu đề : ");
            Console.ResetColor();
            Console.WriteLine(subject);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Nội dung:");
            Console.ResetColor();
            Console.WriteLine(body);
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("  [1] Gửi luôn");
            Console.WriteLine("  [2] Sửa tiêu đề");
            Console.WriteLine("  [3] Sửa nội dung");
            Console.WriteLine("  [4] Hủy, không gửi");
            Console.Write("\n  Lựa chọn: ");
            Console.ResetColor();

            var choice = Console.ReadLine()?.Trim();

            switch (choice)
            {
                case "2":
                    Console.Write("  Tiêu đề mới: ");
                    subject = Console.ReadLine()?.Trim() ?? subject;
                    goto case "1";

                case "3":
                    Console.WriteLine("  Nội dung mới (gõ END trên dòng riêng để kết thúc):");
                    var lines = new List<string>();
                    string? line;
                    while ((line = Console.ReadLine()) != "END")
                        lines.Add(line ?? "");
                    body = string.Join("\n", lines);
                    goto case "1";

                case "1":
                    Console.Write("\n  Email người nhận: ");
                    var recipient = Console.ReadLine()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(recipient))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine("  📤 Đang gửi...");
                        Console.ResetColor();

                        await orchestrator.SendEmailAsync(subject, body, recipient);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n  ✅ Đã gửi thành công tới: {recipient}");
                        Console.WriteLine($"  📌 Tiêu đề: {subject}");
                        Console.ResetColor();
                    }
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n  ❌ Đã hủy. Email không được gửi.");
                    Console.ResetColor();
                    break;
            }
        }
        else
        {
            // ── Response thông thường ──
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("🤖 AI: ");
            Console.ResetColor();
            Console.WriteLine(response);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Lỗi: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
}