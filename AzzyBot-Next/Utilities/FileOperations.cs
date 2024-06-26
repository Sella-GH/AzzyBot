﻿using System;
using System.IO;
using System.Threading.Tasks;

namespace AzzyBot.Utilities;

public static class FileOperations
{
    public static async Task<string> CreateTempFileAsync(string content, string? fileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content, nameof(content));

        string tempFilePath = (!string.IsNullOrWhiteSpace(fileName)) ? Path.Combine(Path.GetTempPath(), fileName) : Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    public static void DeleteTempFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        File.Delete(path);
    }
}
