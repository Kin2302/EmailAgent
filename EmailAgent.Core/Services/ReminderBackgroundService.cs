using EmailAgent.Core.Agents;

namespace EmailAgent.Core.Services
{
    /// <summary>
    /// Background service chạy ngầm, kiểm tra mỗi 30 giây.
    /// Khi đến giờ → tự động gửi email nhắc qua SenderPlugin.
    /// </summary>
    public class ReminderBackgroundService
    {
        private readonly SenderPlugin _sender;
        private readonly CancellationTokenSource _cts = new();

        public ReminderBackgroundService(SenderPlugin sender)
        {
            _sender = sender;
        }

        public void Start()
        {
            _ = Task.Run(async () =>
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await CheckAndSendAsync();
                    }
                    catch
                    {
                        // Bỏ qua lỗi — không crash chatbot chính
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), _cts.Token);
                }
            }, _cts.Token);
        }

        public void Stop() => _cts.Cancel();

        private async Task CheckAndSendAsync()
        {
            var due = ReminderPlugin.GetDueReminders();
            if (due.Count == 0) return;

            foreach (var reminder in due)
            {
                var subject = $"⏰ Nhắc việc: {reminder.Title}";
                var body = $"""
                    Xin chào,

                    Đây là email nhắc tự động từ hệ thống AI Agent.

                    📌 Công việc : {reminder.Title}
                    🕐 Thời gian : {reminder.ReminderAt:dd/MM/yyyy HH:mm}
                    {(string.IsNullOrWhiteSpace(reminder.Note) ? "" : $"📝 Ghi chú    : {reminder.Note}")}

                    Vui lòng xử lý công việc này đúng hạn.

                    ──────────────────────────────
                    🤖 AI Agent System — Tự động gửi lúc {DateTime.Now:HH:mm dd/MM/yyyy}
                    """;

                await _sender.SendEmailAsync(subject, body, reminder.NotifyEmail);
                ReminderPlugin.MarkEmailSent(reminder.Id);

                // In ra console để thấy khi demo
                var savedColor = Console.ForegroundColor;
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⏰ [ReminderService] Đến giờ: \"{reminder.Title}\"");
                Console.WriteLine($"   📧 Đã gửi email nhắc tới: {reminder.NotifyEmail}");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Bạn: ");
                Console.ForegroundColor = savedColor;
            }
        }
    }
}