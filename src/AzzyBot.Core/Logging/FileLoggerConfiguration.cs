using System;
using System.Diagnostics.CodeAnalysis;

namespace AzzyBot.Core.Logging;

[SuppressMessage("Roslynator", "RCS1181:Convert comment to documentation comment", Justification = "No docs needed.")]
public sealed class FileLoggerConfiguration
{
    public string Directory { get; set; } = string.Empty;
    public int MaxFileSize { get; set; } = 26109524; // ~24.9 MB
    public TimeSpan MaxTimeSpan { get; set; } = TimeSpan.FromDays(1);
}
