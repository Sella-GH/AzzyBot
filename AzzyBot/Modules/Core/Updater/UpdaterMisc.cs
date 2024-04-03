using System;
using System.Diagnostics;
using System.IO;

namespace AzzyBot.Modules.Core.Updater;

internal static class UpdaterMisc
{
    internal static void CheckIfDirIsPresent()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater");

        if (Directory.Exists(path))
            return;

        Directory.CreateDirectory(path);

        if (!Directory.Exists(path))
            throw new IOException("Unable to create Directory: Updater");
    }

    internal static void SetFilePermission(string file, string permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(file));

        using Process process = new();
        process.StartInfo = new()
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"chmod {permissions} '{file}'\"",
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        process.Exited += Process_Exited;
        process.Start();
    }

    private static void Process_Exited(object? sender, EventArgs e)
    { }
}
