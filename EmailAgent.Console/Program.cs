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

        // ReviewPlugin tự xử lý toàn bộ review + gửi bên trong SK
        // Program.cs chỉ hiển thị response cuối của AI
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