using EmailAgent.Core.Agents;
using EmailAgent.Core.Services;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace EmailAgent.Core.Plugins
{
    /// <summary>
    /// Human-in-the-Loop Review Gate.
    /// Lấy HTML từ ContentStore bằng emailId — AI không truyền HTML.
    /// </summary>
    public class ReviewPlugin
    {
        private readonly SenderPlugin _senderPlugin;

        public ReviewPlugin(SenderPlugin senderPlugin)
        {
            _senderPlugin = senderPlugin;
        }

        [KernelFunction("ReviewAndSend")]
        [Description("Review email trước khi gửi. Gọi sau WriteEmailContent. Truyền subject, emailId, recipientEmail.")]
        public async Task<string> ReviewAndSendAsync(
            [Description("Tiêu đề email")] string subject,
            [Description("Email ID từ WriteEmailContent (dạng email_...)")] string emailId,
            [Description("Email người nhận")] string recipientEmail)
        {
            var body = ContentStore.Get(emailId);
            if (body == null)
                return "LỖI: Không tìm thấy email. Gọi WriteEmailContent trước.";

            bool isHtml = body.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                          body.Contains("<html", StringComparison.OrdinalIgnoreCase);

            // ── Hiển thị email ──
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║       📧 EMAIL AI ĐÃ SOẠN — VUI LÒNG KIỂM TRA        ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            PrintField("Người nhận", recipientEmail);
            PrintField("Tiêu đề  ", subject);
            PrintField("Định dạng ", isHtml ? "📄 HTML (bảng + biểu đồ)" : "📝 Plain Text");

            if (isHtml)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Nội dung (tóm tắt):");
                Console.ResetColor();
                Console.WriteLine(ExtractText(body));
            }
            else
            {
                PrintField("Nội dung ", "");
                Console.WriteLine(body);
            }

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("══════════════════════════════════════════════════════");
            Console.ResetColor();

            // ── Menu ──
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n  [1] Gửi luôn");
                Console.WriteLine("  [2] Sửa tiêu đề");
                Console.WriteLine("  [3] Sửa nội dung (plain text)");
                Console.WriteLine("  [4] Hủy");
                if (isHtml) Console.WriteLine("  [5] Xem HTML (mở browser)");
                Console.Write("\n  Chọn: ");
                Console.ResetColor();

                var choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        var result = await DoSend(subject, body, recipientEmail, isHtml);
                        ContentStore.Remove(emailId);
                        return result;
                    case "2":
                        Console.Write("  Tiêu đề mới: ");
                        var ns = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(ns)) subject = ns;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  → Cập nhật: {subject}");
                        Console.ResetColor();
                        continue;
                    case "3":
                        Console.WriteLine("  Nhập nội dung (gõ END để kết thúc):");
                        var lines = new List<string>();
                        string? line;
                        while ((line = Console.ReadLine()) != "END") lines.Add(line ?? "");
                        if (lines.Count > 0) { body = string.Join("\n", lines); isHtml = false; }
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  → Đã cập nhật (plain text).");
                        Console.ResetColor();
                        continue;
                    case "4":
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n  ❌ Đã hủy.");
                        Console.ResetColor();
                        ContentStore.Remove(emailId);
                        return "Email đã bị hủy. Không gửi.";
                    case "5" when isHtml:
                        OpenPreview(body);
                        continue;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  ⚠ Không hợp lệ.");
                        Console.ResetColor();
                        continue;
                }
            }
        }

        private async Task<string> DoSend(string subj, string body, string to, bool html)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  📤 Đang gửi...");
            Console.ResetColor();
            await _senderPlugin.SendEmailAsync(subj, body, to, html);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  ✅ Gửi thành công tới: {to}");
            Console.WriteLine($"  📌 Tiêu đề: {subj}");
            Console.ResetColor();
            return $"Email đã gửi thành công tới {to} với tiêu đề: {subj}";
        }

        private static void PrintField(string label, string value)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"  {label}: ");
            Console.ResetColor();
            Console.WriteLine(value);
        }

        private static string ExtractText(string html)
        {
            var t = Regex.Replace(html, @"<style[^>]*>.*?</style>", "", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"</(?:p|tr|div|h[1-6])>", "\n", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"</(?:td|th)>", " | ", RegexOptions.IgnoreCase);
            t = Regex.Replace(t, @"<[^>]+>", "");
            t = t.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&nbsp;", " ");
            t = Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
            var lines = t.Split('\n');
            if (lines.Length > 25) t = string.Join("\n", lines.Take(25)) + "\n  ... (dùng [5] xem đầy đủ)";
            return "  " + t.Replace("\n", "\n  ");
        }

        private static void OpenPreview(string html)
        {
            try
            {
                var path = Path.Combine(Path.GetTempPath(), $"email_{Guid.NewGuid():N}.html");
                File.WriteAllText(path, html, System.Text.Encoding.UTF8);
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  ✅ Đã mở preview.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ❌ Lỗi: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}