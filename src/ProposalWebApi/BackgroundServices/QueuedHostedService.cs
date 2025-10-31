using Core.Shared.Events;

namespace ProposalWebApi.BackgroundServices;

public class QueuedHostedService<TEvent>(
    IBackgroundTaskQueue<TEvent> notificationQueue,
    IEventDispatcher eventDispatcher) : BackgroundService where TEvent : class, IEvent
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                TEvent backgroundTask = await notificationQueue.DequeueAsync(cancellationToken);

                await eventDispatcher.HandleDispatchAsync(backgroundTask, cancellationToken);
            }
            catch (Exception e)
            {
                // To Do: Tratar / logar depois
            }
        }
    }
}