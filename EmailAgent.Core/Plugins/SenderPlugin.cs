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
        [Description("Gửi email qua SMTP. Hỗ trợ HTML và plain text.")]
        public async Task SendEmailAsync(
            [Description("Tiêu đề email")] string subject,
            [Description("Nội dung email")] string body,
            [Description("Email người nhận")] string recipientEmail,
            [Description("true = HTML, false = plain text")] bool isHtml = true)
        {
            using var client = new SmtpClient(_smtpServer, _port)
            {
                Credentials = new NetworkCredential(_senderEmail, _appPassword),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(_senderEmail),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };
            mail.To.Add(recipientEmail);
            await client.SendMailAsync(mail);
        }
    }
}
