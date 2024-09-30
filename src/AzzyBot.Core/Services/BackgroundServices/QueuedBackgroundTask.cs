using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AzzyBot.Core.Services.BackgroundServices;

public sealed class QueuedBackgroundTask
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

    public QueuedBackgroundTask()
    {
        BoundedChannelOptions options = new(1024)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        => await _queue.Reader.ReadAsync(cancellationToken);
}
