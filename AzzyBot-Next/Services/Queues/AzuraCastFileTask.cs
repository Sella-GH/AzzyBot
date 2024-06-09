using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AzzyBot.Services.Interfaces;

namespace AzzyBot.Services.Queues;

public sealed class AzuraCastFileTask : IQueuedBackgroundTask
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public AzuraCastFileTask(int capacity)
    {
        BoundedChannelOptions options = new(capacity)
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
