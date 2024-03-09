using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.Core;

internal sealed class CoreFileLock(string FileName, string[] Directories) : IDisposable
{
    private readonly SemaphoreSlim FileLock = new(1, 1);

    public void Dispose() => FileLock.Dispose();

    internal async Task<string> GetFileContentAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName, nameof(FileName));
        ArgumentNullException.ThrowIfNull(Directories, nameof(Directories));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Directories.Length, nameof(Directories));

        await FileLock.WaitAsync();
        try
        {
            return await CoreFileOperations.GetFileContentAsync(FileName, Directories);
        }
        catch (Exception)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, "Can not get file content");
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }

    internal async Task<bool> SetFileContentAsync(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName, nameof(FileName));
        ArgumentNullException.ThrowIfNull(Directories, nameof(Directories));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Directories.Length, nameof(Directories));
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        await FileLock.WaitAsync();
        try
        {
            await CoreFileOperations.WriteFileContentAsync(FileName, Directories, content);
            return true;
        }
        catch (Exception)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, "Can not set file content");
            throw;
        }
        finally
        {
            FileLock.Release();
        }
    }
}
