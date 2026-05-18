using EmailAgent.Core.Agents;
using EmailAgent.Core.Templates;

namespace EmailAgent.Core.Services
{
    /// <summary>
    /// Background service kiểm tra mỗi 15s, gửi email HTML nhắc việc khi đến giờ.
    /// </summary>
    public class ReminderBackgroundService
    {
        private readonly SenderPlugin _sender;
        private readonly EmailTemplateBuilder _templateBuilder = new();
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
                    try { await CheckAndSendAsync(); }
                    catch { /* không crash chatbot */ }
                    await Task.Delay(TimeSpan.FromSeconds(15), _cts.Token);
                }
            }, _cts.Token);
        }

        public void Stop() => _cts.Cancel();

        private async Task CheckAndSendAsync()
        {
            var due = ReminderPlugin.GetDueReminders();
            if (due.Count == 0) return;

            foreach (var r in due)
            {
                var subject = $"⏰ Nhắc việc: {r.Title}";
                var html = _templateBuilder.BuildReminderEmail(r.Title, r.ReminderAt, r.Note);

                await _sender.SendEmailAsync(subject, html, r.NotifyEmail, isHtml: true);
                ReminderPlugin.MarkEmailSent(r.Id);

                var saved = Console.ForegroundColor;
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⏰ [Reminder] Đến giờ: \"{r.Title}\"");
                Console.WriteLine($"   📧 Đã gửi email HTML tới: {r.NotifyEmail}");
                Console.Write("Bạn: ");
                Console.ForegroundColor = saved;
            }
        }
    }
}