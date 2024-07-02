using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace AzzyBot.Services.Interfaces;

[SuppressMessage("Roslynator", "RCS0036:Remove blank line between single-line declarations of same kind", Justification = "Code Style")]
public interface IQueuedBackgroundTask
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
