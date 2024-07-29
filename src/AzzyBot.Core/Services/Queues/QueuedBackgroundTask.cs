using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AzzyBot.Core.Services.Interfaces;

namespace AzzyBot.Core.Services.Queues;

public sealed class QueuedBackgroundTask : IQueuedBackgroundTask
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public QueuedBackgroundTask()
    {
        BoundedChannelOptions options = new(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem, nameof(workItem));

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
