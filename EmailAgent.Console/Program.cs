using EmailAgent.Core.Orchestrator;
using EmailAgent.Core.Services;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var orchestrator = new AgentOrchestrator(
    config["Groq:ApiKey"]!,
    config["Groq:WriterModel"]!,
    config["Smtp:Server"]!,
    int.Parse(config["Smtp:Port"]!),
    config["Smtp:SenderEmail"]!,
    config["Smtp:AppPassword"]!
);

orchestrator.Initialize();

var reminderService = new ReminderBackgroundService(orchestrator.SenderPlugin);
reminderService.Start();

Console.Clear();
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║      🤖 AI AGENT - TRỢ LÝ CÔNG VIỆC THÔNG MINH       ║");
Console.WriteLine("║          Powered by Semantic Kernel + Groq           ║");
Console.WriteLine("╠══════════════════════════════════════════════════════╣");
Console.WriteLine("║  📊 Báo cáo doanh thu (CSV, Excel, API)              ║");
Console.WriteLine("║  • gửi báo cáo D:\\data.csv cho a@b.com              ║");
Console.WriteLine("║  • phân tích file D:\\report.xlsx gửi cho a@b.com    ║");
Console.WriteLine("║  ⏰ Nhắc việc / 📧 Email HTML (bảng, biểu đồ SVG)   ║");
Console.WriteLine("║  • exit để thoát                                     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

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

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("🤖 AI: ");
        Console.ResetColor();
        Console.WriteLine(response);
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Lỗi: {ex.Message}");
        Console.ResetColor();
    }

    Console.WriteLine();
}