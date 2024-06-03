using Microsoft.Extensions.Logging;

namespace AzzyBot.Logging;

public static class LoggingBuilderExtensions
{
    public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
    {
        using FileLoggerProvider fileLoggerProvider = new(filePath);
        builder.AddProvider(fileLoggerProvider);

        return builder;
    }
}