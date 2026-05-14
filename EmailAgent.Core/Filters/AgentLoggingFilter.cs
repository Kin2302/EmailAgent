using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace EmailAgent.Core.Filters
{
    // Implement IFunctionInvocationFilter
    // Chạy trước/sau mỗi lần AI gọi KernelFunction
    public class AgentLoggingFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            // TRƯỚC: log AI đang gọi hàm gì
            Console.WriteLine($"[SK Filter] → AI gọi: {context.Function.PluginName}.{context.Function.Name}");
            var sw = Stopwatch.StartNew();

            await next(context); // ← gọi hàm thật

            // SAU: log kết quả, thời gian
            sw.Stop();
            Console.WriteLine($"[SK Filter] ✓ Hoàn tất: {context.Function.Name} ({sw.ElapsedMilliseconds}ms)");
        }
    }
}
