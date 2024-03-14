using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using AzzyBot.Settings.MusicStreaming;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingLavalink
{
    private static int? ProcessId;

    internal static async Task<bool> CheckIfJavaIsInstalledAsync()
    {
        try
        {
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "java",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
        catch
        {
            return false;
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
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ProcessId = Process.Start(processStartInfo)?.Id ?? throw new InvalidOperationException("Could not start Lavalink process!");
            if (ProcessId is null or 0)
                throw new InvalidOperationException("Lavalink process is not loaded!");

            return true;
        }
        catch
        {
            return false;
            throw;
        }
    }

    internal static async Task<bool> StopLavalinkAsync()
    {
        try
        {
            if (ProcessId is null or 0)
                throw new InvalidOperationException("Lavalink process is not loaded!");

            int processId = ProcessId.Value;

            Process process = Process.GetProcessById(processId);
            process.Kill();
            await process.WaitForExitAsync();
            process.Dispose();

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "logs");
            if (MusicStreamingSettings.DeleteLavalinkLogs && Directory.Exists(path))
                Directory.Delete(path);

            return true;
        }
        catch
        {
            return false;
            throw;
        }
    }
}
