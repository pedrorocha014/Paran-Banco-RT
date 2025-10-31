using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace Worker.Resilience;

public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.RequestTimeout)
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var logger = context.GetLogger();
                    logger?.LogWarning(
                        "Retry {RetryCount} after {Delay}ms. Status: {StatusCode}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Result?.StatusCode.ToString() ?? outcome.Exception?.GetType().Name ?? "Unknown");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.InternalServerError)
            .OrResult(msg => msg.StatusCode == HttpStatusCode.ServiceUnavailable)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (result, duration) =>
                {
                    var logger = result.Exception?.GetType().Name ?? "Unknown";
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds} seconds. Last error: {logger}");
                },
                onReset: () =>
                {
                    Console.WriteLine("Circuit breaker reset.");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    public static IAsyncPolicy<HttpResponseMessage> GetResiliencePolicy()
    {
        return Policy.WrapAsync(
            GetTimeoutPolicy(),
            GetRetryPolicy(),
            GetCircuitBreakerPolicy());
    }
}

public static class ContextExtensions
{
    private static readonly string LoggerKey = "ILogger";

    public static Context WithLogger<T>(this Context context, ILogger<T> logger)
    {
        context[LoggerKey] = logger;
        return context;
    }

    public static ILogger? GetLogger(this Context context)
    {
        if (context.TryGetValue(LoggerKey, out var logger) && logger is ILogger ilogger)
        {
            return ilogger;
        }
        return null;
    }
}

