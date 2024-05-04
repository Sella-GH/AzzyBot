using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core;
using AzzyBot.Modules.Core.Enums;
using AzzyBot.Modules.MusicStreaming.Settings;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MsLavalinkHandler
{
    private static Process? LavalinkProcess;
    private static readonly string LavalinkPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameof(CoreFileDirectoriesEnum.Modules), nameof(CoreFileDirectoriesEnum.MusicStreaming), nameof(CoreFileDirectoriesEnum.Files));

    internal static async Task<bool> CheckIfJavaIsInstalledAsync()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Checking if OpenJDK JRE is installed", null);

        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "java",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process? process = Process.Start(processStartInfo);

            if (process is null)
                return false;

            string? output = await process.StandardOutput.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(output))
                return false;

            string[] javaString = output.Split(' ')[1].Split('.');

            return javaString[0] is "17" or "21";
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static async Task<bool> CheckIfLavalinkConfigIsRightAsync()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Checking if Lavalink config is correct", null);

        string path = Path.Combine(LavalinkPath, "application.yml");

        if (!File.Exists(path))
            return false;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(path);

            const string lineStarts = "geniusApiKey:";
            const string lineContains = "\"Your Genius Client Access Token\"";

            foreach (string line in lines)
            {
                string newLine = line.Trim();
                if (newLine.StartsWith(lineStarts, StringComparison.OrdinalIgnoreCase) && newLine.Contains(lineContains, StringComparison.OrdinalIgnoreCase))
                {
                    LoggerBase.LogError(LoggerBase.GetLogger, "You forgot to set your genius api key!", null);
                    return false;
                }
            }

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static bool CheckIfLavalinkIsThere()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Checking if Lavalink files are present", null);

        bool directoryExists = Directory.Exists(LavalinkPath);
        bool lavalinkJarExists = File.Exists(Path.Combine(LavalinkPath, "Lavalink.jar"));
        bool applicationYmlExists = File.Exists(Path.Combine(LavalinkPath, "application.yml"));

        return directoryExists && lavalinkJarExists && applicationYmlExists;
    }

    internal static async Task<bool> StartLavalinkAsync()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Starting Lavalink", null);

        try
        {
            if (!CheckIfLavalinkIsThere())
            {
                LoggerBase.LogCrit(LoggerBase.GetLogger, "Lavalink files are missing!", null);
                return false;
            }

            if (MsSettings.ActivateLyrics && !await CheckIfLavalinkConfigIsRightAsync())
                return false;

            ProcessStartInfo processStartInfo = new()
            {
                FileName = "java",
                Arguments = $"-jar {Path.Combine(LavalinkPath, "Lavalink.jar")}",
                WorkingDirectory = Path.Combine(LavalinkPath),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            LavalinkProcess = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Could not start Lavalink process!");

            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    internal static async Task<bool> StopLavalinkAsync()
    {
        LoggerBase.LogInfo(LoggerBase.GetLogger, "Stopping Lavalink", null);

        try
        {
            if (LavalinkProcess is null)
                throw new InvalidOperationException("No Lavalink process was created!");

            if (CoreMisc.CheckIfLinuxOs())
            {
                int errorCode = sys_kill(LavalinkProcess.Id, 19);

                LavalinkProcess.Dispose();

                return errorCode is 0;
            }

            LavalinkProcess.Kill();
            await LavalinkProcess.WaitForExitAsync();
            LavalinkProcess.Dispose();

            string path = Path.Combine(LavalinkPath, nameof(CoreFileDirectoriesEnum.logs));
            if (MsSettings.DeleteLavalinkLogs && Directory.Exists(path))
            {
                await Task.Delay(3000);
                Directory.Delete(path, true);
            }

            return true;
        }
        catch (IOException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, "Could not delete lavalink log path", null);
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    [SuppressMessage("SYSLIB", "SYSLIB1054:Use LibraryImportAttribute instead of DllImportAttribute to generate p/invoke marshalling code at compile time.", Justification = "No use of unsafe code")]
    [SuppressMessage("Security", "CA5392:Use DefaultDllImportSearchPaths attribute for P/Invokes.", Justification = "This is linux")]
    [DllImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static extern int sys_kill(int pid, int sig);
}
