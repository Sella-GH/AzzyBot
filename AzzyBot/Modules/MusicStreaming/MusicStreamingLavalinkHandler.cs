using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzzyBot.Modules.Core;
using AzzyBot.Settings.MusicStreaming;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingLavalinkHandler
{
    private static Process? LavalinkProcess;

    internal static async Task<bool> CheckIfJavaIsInstalledAsync()
    {
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

    internal static bool StartLavalink()
    {
        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "java",
                Arguments = $"-jar {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "Lavalink.jar")}",
                WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files"),
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

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "logs");
            if (MusicStreamingSettings.DeleteLavalinkLogs && Directory.Exists(path))
                Directory.Delete(path, true);

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
