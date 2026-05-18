using EmailAgent.Core.Agents;
using EmailAgent.Core.Filters;
using EmailAgent.Core.Plugins;
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
            _kernel.Plugins.AddFromObject(new ReviewPlugin(SenderPlugin), "ReviewPlugin");
            _kernel.Plugins.AddFromObject(new ReminderPlugin(), "ReminderPlugin");

            // SK Filter — log realtime từng Plugin AI gọi
            _kernel.FunctionInvocationFilters.Add(new AgentLoggingFilter());

            _agent = new ChatCompletionAgent
            {
                Name = "WorkflowAssistant",
                Instructions = """
                    Bạn là trợ lý AI văn phòng, trả lời tiếng Việt.

                    BÁO CÁO DOANH THU (4 bước tuần tự):
                    1. Lấy dữ liệu: .csv→GetSalesData, .xlsx→GetDataFromExcel, URL→GetDataFromApi
                    2. AnalyzeSalesData(csvContent)
                    3. WriteEmailContent(analysisSummary, recipientTitle)
                    4. ReviewAndSend(subject, emailId, recipientEmail) — emailId lấy từ kết quả bước 3

                    NHẮC VIỆC: AddReminder, ListReminders, CompleteReminder, DeleteReminder

                    NGUYÊN TẮC:
                    - Gọi TỪNG hàm một, chờ kết quả rồi mới gọi tiếp
                    - In TOÀN BỘ output Plugin, không tóm tắt
                    - Thiếu thông tin → hỏi ngắn 1 lần
                    - KHÔNG truyền JSON schema vào tham số hàm
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

            // 1. Thêm câu hỏi của user vào lịch sử
            _chatHistory.AddUserMessage(userMessage);

            var sb = new System.Text.StringBuilder();

            // 2. Duyệt qua TẤT CẢ các phản hồi của AI (bao gồm cả các bước gọi Plugin ẩn)
            await foreach (var response in _agent.InvokeAsync(_chatHistory))
            {
                _chatHistory.Add(response);

                if (!string.IsNullOrWhiteSpace(response.Message?.Content))
                {
                    sb.Append(response.Message?.Content);
                }
            }

            return sb.ToString().Trim();
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