﻿using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AzzyBot.ExceptionHandling;
using AzzyBot.Modules.Core;
using AzzyBot.Settings.MusicStreaming;
using Microsoft.Extensions.Logging;

namespace AzzyBot.Modules.MusicStreaming;

internal static class MusicStreamingLavalinkHandler
{
    private static Process? LavalinkProcess;

    internal static async Task<bool> CheckIfJavaIsInstalledAsync()
    {
        ExceptionHandler.LogMessage(LogLevel.Debug, "Checking if OpenJDK JRE is installed");

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
        ExceptionHandler.LogMessage(LogLevel.Debug, "Checking if Lavalink config is correct");

        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "application.yml");

        if (!File.Exists(path))
            return false;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(path);

            foreach (string line in lines)
            {
                string newLine = line.Trim();
                if (newLine.StartsWith("geniusApiKey:", StringComparison.OrdinalIgnoreCase) && newLine.Contains("\"Your Genius Client Access Token\"", StringComparison.OrdinalIgnoreCase))
                {
                    ExceptionHandler.LogMessage(LogLevel.Error, "You forgot to set your genius api key!");
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
        ExceptionHandler.LogMessage(LogLevel.Debug, "Checking if Lavalink files are present");

        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files");

        bool directoryExists = Directory.Exists(path);
        bool lavalinkJarExists = File.Exists(Path.Combine(path, "Lavalink.jar"));
        bool applicationYmlExists = File.Exists(Path.Combine(path, "application.yml"));

        return directoryExists && lavalinkJarExists && applicationYmlExists;
    }

    internal static async Task<string> GetLavalinkPasswordAsync()
    {
        ExceptionHandler.LogMessage(LogLevel.Debug, "Getting Lavalink password");
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules", "MusicStreaming", "Files", "application.yml");

        if (!File.Exists(path))
            return string.Empty;

        try
        {
            string[] lines = await File.ReadAllLinesAsync(path);
            string lineBefore = string.Empty;
            string lineAfter = string.Empty;
            string password = string.Empty;

            for (int i = 0; lines.Length > i; i++)
            {
                string line = lines[i].Trim();
                if (i > 1)
                    lineBefore = lines[i - 1].Trim();

                lineAfter = lines[i + 1].Trim();

                if (line.StartsWith("password:", StringComparison.OrdinalIgnoreCase) && lineBefore is "#######################################" && lineAfter is "#######################################")
                {
                    password = line.Split(':')[1].Trim().Trim('\"');
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("The password couldn't be found");

            return password;
        }
        catch (Exception)
        {
            throw;
        }
    }

    internal static async Task<bool> StartLavalinkAsync()
    {
        ExceptionHandler.LogMessage(LogLevel.Debug, "Starting Lavalink");

        try
        {
            if (!CheckIfLavalinkIsThere())
            {
                ExceptionHandler.LogMessage(LogLevel.Error, "Lavalink files are missing!");
                return false;
            }

            if (MusicStreamingSettings.ActivateLyrics && !await CheckIfLavalinkConfigIsRightAsync())
                return false;

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
        ExceptionHandler.LogMessage(LogLevel.Debug, "Stopping Lavalink");

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
        catch (IOException)
        {
            ExceptionHandler.LogMessage(LogLevel.Warning, "Could not delete lavalink log path");
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
