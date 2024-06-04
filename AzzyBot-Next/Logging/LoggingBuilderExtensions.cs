using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string directory)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>(p => new FileLoggerProvider(directory)));

        return builder;
    }
}
