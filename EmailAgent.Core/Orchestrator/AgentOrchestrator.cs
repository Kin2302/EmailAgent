using EmailAgent.Core.Agents;
using EmailAgent.Core.Filters;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace EmailAgent.Core.Orchestrator
{
    public class AgentOrchestrator
    {
        private readonly string _groqApiKey;
        private readonly string _orchestratorModel;
        private readonly string _smtpServer;
        private readonly int _port;
        private readonly string _senderEmail;
        private readonly string _appPassword;

        // ChatHistory giữ xuyên suốt session — AI nhớ ngữ cảnh
        private readonly ChatHistory _chatHistory = new();
        private ChatCompletionAgent? _agent;
        private Kernel? _kernel;

        // Expose SenderPlugin để ReminderBackgroundService dùng chung
        public SenderPlugin SenderPlugin { get; private set; } = null!;

        private static readonly Uri GroqEndpoint = new("https://api.groq.com/openai/v1");

        public AgentOrchestrator(
            string groqApiKey, string orchestratorModel,
            string smtpServer, int port, string senderEmail, string appPw)
        {
            _groqApiKey = groqApiKey;
            _orchestratorModel = orchestratorModel;
            _smtpServer = smtpServer;
            _port = port;
            _senderEmail = senderEmail;
            _appPassword = appPw;
        }

        /// <summary>
        /// Khởi tạo Kernel + đăng ký tất cả Plugins + tạo Agent.
        /// Gọi 1 lần khi start chatbot.
        /// </summary>
        public void Initialize()
        {
            SenderPlugin = new SenderPlugin(_smtpServer, _port, _senderEmail, _appPassword);

            _kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    modelId: _orchestratorModel,
                    apiKey: _groqApiKey,
                    endpoint: GroqEndpoint)
                .Build();

            // Đăng ký tất cả Plugins — AI tự đọc [Description] và chọn dùng cái nào
            _kernel.Plugins.AddFromObject(new DataPlugin(), "DataPlugin");
            _kernel.Plugins.AddFromObject(new AnalystPlugin(), "AnalystPlugin");
            _kernel.Plugins.AddFromObject(new WriterPlugin(), "WriterPlugin");
            _kernel.Plugins.AddFromObject(SenderPlugin, "SenderPlugin");
            _kernel.Plugins.AddFromObject(new ReminderPlugin(), "ReminderPlugin");

            // SK Filter — log realtime từng Plugin AI gọi
            _kernel.FunctionInvocationFilters.Add(new AgentLoggingFilter());

            _agent = new ChatCompletionAgent
            {
                Name = "WorkflowAssistant",
                Instructions = """
                    Bạn là trợ lý AI thông minh hỗ trợ công việc văn phòng, trả lời bằng tiếng Việt.

                    NHÓM 1 — BÁO CÁO DOANH THU:
                    Khi user muốn gửi báo cáo, phân tích doanh thu:
                    - Bước 1: GetSalesData (hỏi đường dẫn CSV nếu chưa có)
                    - Bước 2: AnalyzeSalesData
                    - Bước 3: WriteEmailContent
                    - Bước 4: Hiển thị email, đợi user xác nhận rồi mới gọi SendEmail

                    NHÓM 2 — NHẮC VIỆC / LỊCH HẸN:
                    - Thêm việc  → AddReminder (cần: title, time HH:mm, email nhận nhắc)
                    - Xem việc   → ListReminders
                    - Xong việc  → CompleteReminder
                    - Xóa việc   → DeleteReminder

                    NGUYÊN TẮC:
                    - Tự hiểu ý định từ ngôn ngữ tự nhiên tiếng Việt
                    - Nếu thiếu thông tin quan trọng thì hỏi ngắn gọn 1 lần
                    - Luôn thân thiện, súc tích
                    - Sau khi xong tác vụ, hỏi user cần gì thêm không
                    - QUAN TRỌNG: Khi Plugin trả về danh sách hoặc kết quả dữ liệu, hãy copy NGUYÊN VĂN toàn bộ nội dung đó vào response, không tóm tắt, không diễn giải lại, không bỏ bớt dòng nào
                    """,
                Kernel = _kernel,
                Arguments = new KernelArguments(
                    new OpenAIPromptExecutionSettings
                    {
                        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                        Temperature = 0.3
                    })
            };
        }

        /// <summary>
        /// Gửi tin nhắn, nhận response AI. History được giữ để AI nhớ ngữ cảnh.
        /// </summary>
        public async Task<string> ChatAsync(string userMessage)
        {
            if (_agent == null)
                throw new InvalidOperationException("Gọi Initialize() trước.");

            _chatHistory.AddUserMessage(userMessage);

            var sb = new System.Text.StringBuilder();
            await foreach (var response in _agent.InvokeAsync(_chatHistory))
            {
                if (response.Message?.Content is string content && !string.IsNullOrWhiteSpace(content))
                    sb.Append(content);
            }

            var result = sb.ToString().Trim();
            if (!string.IsNullOrEmpty(result))
                _chatHistory.AddAssistantMessage(result);

            return result;
        }

        /// <summary>
        /// Gửi email trực tiếp sau khi user review và xác nhận.
        /// </summary>
        public async Task SendEmailAsync(string subject, string body, string recipientEmail)
        {
            await SenderPlugin.SendEmailAsync(subject, body, recipientEmail);
        }

        /// <summary>
        /// Parse SUBJECT và BODY từ output AI.
        /// </summary>
        public static (string subject, string body) ParseEmailContent(string rawEmail)
        {
            var subject = "Báo cáo doanh thu";
            var body = rawEmail;

            var si = rawEmail.IndexOf("SUBJECT:", StringComparison.OrdinalIgnoreCase);
            var bi = rawEmail.IndexOf("BODY:", StringComparison.OrdinalIgnoreCase);

            if (si >= 0 && bi > si)
            {
                subject = rawEmail[(si + 8)..bi].Trim();
                body = rawEmail[(bi + 5)..].Trim();
            }

            return (subject, body);
        }
    }
}