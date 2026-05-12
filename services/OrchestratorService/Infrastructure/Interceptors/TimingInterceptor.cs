using System.Diagnostics;
using System.Reflection;
using Castle.DynamicProxy;

namespace OrchestratorService.Infrastructure.Interceptors;

/// <summary>
/// Logs execution time for every proxied method call.
/// Registered as singleton — wired into DI via ProxyGenerator for all service interfaces.
/// </summary>
public class TimingInterceptor : IInterceptor
{
    private readonly ILogger<TimingInterceptor> _logger;

    public TimingInterceptor(ILogger<TimingInterceptor> logger) => _logger = logger;

    public void Intercept(IInvocation invocation)
    {
        var sw = Stopwatch.StartNew();
        invocation.Proceed();

        var returnType = invocation.Method.ReturnType;

        if (returnType == typeof(Task))
        {
            invocation.ReturnValue = WrapVoidAsync((Task)invocation.ReturnValue!, invocation, sw);
        }
        else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var wrapper = typeof(TimingInterceptor)
                .GetMethod(nameof(WrapResultAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
                .MakeGenericMethod(resultType);
            invocation.ReturnValue = wrapper.Invoke(this, [invocation.ReturnValue, invocation, sw]);
        }
        else
        {
            LogTiming(invocation, sw.ElapsedMilliseconds);
        }
    }

    private async Task WrapVoidAsync(Task task, IInvocation invocation, Stopwatch sw)
    {
        await task;
        LogTiming(invocation, sw.ElapsedMilliseconds);
    }

    private async Task<T> WrapResultAsync<T>(Task<T> task, IInvocation invocation, Stopwatch sw)
    {
        var result = await task;
        LogTiming(invocation, sw.ElapsedMilliseconds);
        return result;
    }

    private void LogTiming(IInvocation invocation, long elapsedMs) =>
        _logger.LogInformation(
            "[{Class}.{Method}] {ElapsedMs}ms",
            invocation.TargetType.Name, invocation.Method.Name, elapsedMs);
}
