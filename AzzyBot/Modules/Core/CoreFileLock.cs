using System;
using System.Threading;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.Core.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzzyBot.Modules.Core;

internal sealed class CoreFileLock(string FileName, string[] Directories) : IDisposable
{
    private readonly SemaphoreSlim FileLock = new(1, 1);

    public void Dispose() => FileLock.Dispose();

    internal async Task<string> GetFileContentAsync(CoreFileValuesEnum value = CoreFileValuesEnum.None)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FileName, nameof(FileName));
        ArgumentNullException.ThrowIfNull(Directories, nameof(Directories));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Directories.Length, nameof(Directories));

        await FileLock.WaitAsync();
        try
        {
            string content = await CoreFileOperations.GetFileContentAsync(FileName, Directories);

            if (FileName is not nameof(CoreFileNamesEnum.AzzyBotJSON))
                return content;

            AzzyBotModel? azzyBot = JsonConvert.DeserializeObject<AzzyBotModel>(content) ?? throw new InvalidOperationException("AzzyBot model is null");

            return value switch
            {
                CoreFileValuesEnum.CompileDate => azzyBot.CompileDate,
                CoreFileValuesEnum.Commit => azzyBot.Commit,
                CoreFileValuesEnum.LoC_CS => azzyBot.LoC_CS,
                CoreFileValuesEnum.LoC_JSON => azzyBot.LoC_JSON,
                _ => content,
            };
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
