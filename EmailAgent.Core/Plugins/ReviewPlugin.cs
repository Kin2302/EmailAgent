using EmailAgent.Core.Agents;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EmailAgent.Core.Plugins
{
    /// <summary>
    /// SK Plugin: Human-in-the-Loop Review Gate.
    /// AI gọi hàm này sau khi WriteEmailContent hoàn tất.
    /// Hàm block lại, hiển thị email cho user xem/sửa, 
    /// rồi mới gọi SenderPlugin nếu user xác nhận.
    /// SenderPlugin KHÔNG đăng ký vào Kernel — chỉ được gọi từ đây.
    /// </summary>
    public class ReviewPlugin
    {
        private readonly SenderPlugin _senderPlugin;

        public ReviewPlugin(SenderPlugin senderPlugin)
        {
            _senderPlugin = senderPlugin;
        }

        [KernelFunction("ReviewAndSend")]
        [Description("Hiển thị email cho user xem và chỉnh sửa trước khi gửi. " +
                     "LUÔN LUÔN gọi hàm này sau WriteEmailContent để user review. " +
                     "Hàm sẽ tự xử lý việc gửi sau khi user xác nhận.")]
        public async Task<string> ReviewAndSendAsync(
            [Description("Tiêu đề email đã soạn từ WriteEmailContent")] string subject,
            [Description("Nội dung email đã soạn từ WriteEmailContent")] string body,
            [Description("Địa chỉ email người nhận")] string recipientEmail)
        {
            // ── Hiển thị email để user review ──
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║       📧 EMAIL AI ĐÃ SOẠN — VUI LÒNG KIỂM TRA      ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Người nhận : ");
            Console.ResetColor();
            Console.WriteLine(recipientEmail);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("  Tiêu đề   : ");
            Console.ResetColor();
            Console.WriteLine(subject);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("  Nội dung  :");
            Console.ResetColor();
            Console.WriteLine(body);

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("══════════════════════════════════════════════════════");
            Console.ResetColor();

            // ── Menu lựa chọn ──
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\n  [1] Gửi luôn");
                Console.WriteLine("  [2] Sửa tiêu đề");
                Console.WriteLine("  [3] Sửa nội dung");
                Console.WriteLine("  [4] Hủy, không gửi");
                Console.Write("\n  Lựa chọn của bạn: ");
                Console.ResetColor();

                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        return await SendEmailAsync(subject, body, recipientEmail);

                    case "2":
                        Console.Write("  Tiêu đề mới: ");
                        var newSubject = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(newSubject))
                            subject = newSubject;
                        // Hiển thị lại để confirm
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"  → Tiêu đề đã cập nhật: {subject}");
                        Console.ResetColor();
                        continue; // Quay lại menu

                    case "3":
                        Console.WriteLine("  Nội dung mới (gõ END trên dòng riêng để kết thúc):");
                        var lines = new List<string>();
                        string? line;
                        while ((line = Console.ReadLine()) != "END")
                            lines.Add(line ?? "");
                        if (lines.Count > 0)
                            body = string.Join("\n", lines);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("  → Nội dung đã cập nhật.");
                        Console.ResetColor();
                        continue; // Quay lại menu

                    case "4":
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n  ❌ Đã hủy. Email không được gửi.");
                        Console.ResetColor();
                        return "Email đã bị hủy theo yêu cầu của người dùng. Không có email nào được gửi.";

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("  ⚠ Lựa chọn không hợp lệ. Vui lòng nhập 1, 2, 3 hoặc 4.");
                        Console.ResetColor();
                        continue;
                }
            }
        }

        private async Task<string> SendEmailAsync(string subject, string body, string recipient)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  📤 Đang gửi email...");
            Console.ResetColor();

            await _senderPlugin.SendEmailAsync(subject, body, recipient);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n  ✅ Email đã gửi thành công tới: {recipient}");
            Console.WriteLine($"  📌 Tiêu đề: {subject}");
            Console.ResetColor();

            return $"Email đã gửi thành công tới {recipient} với tiêu đề: {subject}";
        }
    }
}