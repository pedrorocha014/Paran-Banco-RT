using System.Threading.Channels;

namespace Core.Shared.Events;

public interface IBackgroundTaskQueue<T>
{
    bool HasItems { get; }
    Task<T> DequeueAsync(CancellationToken cancellationToken);
    Task QueueTaskAsync(T task, CancellationToken cancellationToken = default);
}

public class BackgroundTaskQueue<T> : IBackgroundTaskQueue<T>
{
    private readonly Channel<T> _notificationQueue = Channel.CreateUnbounded<T>();

    public bool HasItems => _notificationQueue.Reader is { CanCount: true, Count: > 0 };

    public async Task<T> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _notificationQueue.Reader.ReadAsync(cancellationToken);
    }

    public async Task QueueTaskAsync(T task, CancellationToken cancellationToken = default)
    {
        await _notificationQueue.Writer.WriteAsync(task, cancellationToken);
    }
}