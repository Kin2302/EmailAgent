using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;

namespace EmailAgent.Core.Agents
{
    public class SenderPlugin
    {
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _appPassword;

        public SenderPlugin(string server, int port, string email, string password)
        {
            _smtpServer = server;
            _port = port;
            _senderEmail = email;
            _appPassword = password;
        }

        [KernelFunction("SendEmail")]
        [Description("Gửi email thực tế qua hệ thống SMTP. Hãy gọi hàm này ở bước cuối cùng sau khi đã soạn xong nội dung email.")]
        public async Task SendEmailAsync(
            [Description("Tiêu đề của email (Subject)")] string subject,
            [Description("Nội dung chi tiết của email (Body)")] string body,
            [Description("Địa chỉ email của người nhận")] string recipientEmail)
        {
            using var client = new SmtpClient(_smtpServer, _port)
            {
                Credentials = new NetworkCredential(_senderEmail, _appPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            mailMessage.To.Add(recipientEmail);

            await client.SendMailAsync(mailMessage);
        }
    }
}
