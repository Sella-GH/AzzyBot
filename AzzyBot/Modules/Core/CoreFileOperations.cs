using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using AzzyBot.Logging;
using AzzyBot.Modules.Core.Enums;
using CsvHelper;
using CsvHelper.Configuration;

namespace AzzyBot.Modules.Core;

internal static class CoreFileOperations
{
    internal static bool CreateDirectory(string dirName, string[] directories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dirName);
        ArgumentNullException.ThrowIfNull(directories);

        string path = GetFileNameAndPath(dirName, directories, string.Empty);
        Directory.CreateDirectory(path);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);

        return true;
    }

    internal static async Task<string> GetFileContentAsync(string fileName, string[] directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directory);

        try
        {
            if (!File.Exists(GetFileNameAndPath(fileName, directory)))
                throw new FileNotFoundException();

            return await File.ReadAllTextAsync(GetFileNameAndPath(fileName, directory));
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Directory not found: {directory}", null);

            if (CreateDirectory(directory[^1], directory[..^1]))
                return await GetFileContentAsync(fileName, directory);

            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"File not found: {fileName}", null);

            if (await CreateTemplateFileAsync(fileName, directory))
                return await GetFileContentAsync(fileName, directory);

            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {fileName} - invalid permissions", null);
            throw;
        }
    }

    internal static async Task<bool> WriteFileContentAsync(string fileName, string[] directories, string directory, Stream stream)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directories);
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            string path = GetFileNameAndPath(fileName, directories, directory);
            await using FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);

            return true;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Directory not found: {directory}", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"File not found: {fileName}", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {fileName} - invalid permissions", null);
            throw;
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    internal static async Task<bool> WriteFileContentAsync(string fileName, string[] directory, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        try
        {
            await File.WriteAllTextAsync(GetFileNameAndPath(fileName, directory), text);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Directory not found: {directory}", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"File not found: {fileName}", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {fileName} - invalid permissions", null);
            throw;
        }
    }

    private static async Task<bool> CreateTemplateFileAsync(string fileName, string[] directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directory);

        try
        {
            string path = GetFileNameAndPath(fileName, directory);
            if (File.Exists(path))
                return true;

            await File.Create(path).DisposeAsync();

            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not create file: {path}");

            return true;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Directory not found: {directory}", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Could not create file: {fileName}", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {fileName} - invalid permissions", null);
            throw;
        }
    }

    internal static async Task<string> CreateTempFileAsync(string content, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        try
        {
            string tempFilePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(name))
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), name);
            }
            else
            {
                tempFilePath = Path.GetTempFileName();
            }

            await File.WriteAllTextAsync(tempFilePath, content);
            return tempFilePath;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Error while creating tempfile: invalid permissions", null);
            throw;
        }
    }

    internal static bool DeleteTempFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            File.Delete(path);
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Directory not found: {path}", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"File not found: {path}", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {path} - invalid permissions", null);
            throw;
        }
    }

    internal static async Task<string> CreateTempCsvFileAsync<T>(List<T> content, string name = "")
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            string tempFilePath = string.Empty;
            if (!string.IsNullOrWhiteSpace(name))
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), name);
            }
            else
            {
                tempFilePath = Path.GetTempFileName();
            }

            await using StreamWriter writer = new(tempFilePath);
            CsvConfiguration config = new(CultureInfo.InvariantCulture)
            {
                Encoding = Encoding.UTF8,
                InjectionOptions = InjectionOptions.Escape
            };
            await using CsvWriter csv = new(writer, config);
            await csv.WriteRecordsAsync(content);

            return tempFilePath;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogError(LoggerBase.GetLogger, "Error while creating csv tempfile: invalid permissions", null);
            throw;
        }
    }

    internal static string CreateZipFile(string fileName, string[] directories)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directories);

        try
        {
            string path = GetFileNameAndPath(fileName, directories, string.Empty);
            string dirPath = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, "ZipFile");
            ZipFile.CreateFromDirectory(dirPath, path, CompressionLevel.NoCompression, false, Encoding.UTF8);

            if (!File.Exists(path))
                throw new FileNotFoundException(path);

            Directory.Delete(dirPath, true);

            return path;
        }
        catch (DirectoryNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, "Directory not found", null);
            throw;
        }
        catch (FileNotFoundException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"File not found: {fileName}", null);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            LoggerBase.LogWarn(LoggerBase.GetLogger, $"Can't access file: {fileName} - invalid permissions", null);
            throw;
        }
    }

    private static string GetFileNameAndPath(string fileName, string[] directories, string directory = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(directories);

        fileName = fileName.Replace("TXT", ".txt", StringComparison.OrdinalIgnoreCase).Replace("JSON", ".json", StringComparison.OrdinalIgnoreCase);
        string[] paths;

        if (directories.Length == 1)
        {
            paths = new string[directories.Length + 1];
        }
        else if (string.IsNullOrWhiteSpace(directory))
        {
            paths = new string[directories.Length + 2];
        }
        else
        {
            paths = new string[directories.Length + 3];
        }

        paths[0] = AppDomain.CurrentDomain.BaseDirectory;

        if (!directories[0].Equals(CoreFileDirectoriesEnum.None))
        {
            for (int i = 0; i < directories.Length; i++)
            {
                paths[i + 1] += directories[i];
            }
        }

        if (!string.IsNullOrWhiteSpace(directory))
            paths[^2] = directory;

        paths[^1] = fileName;

        return Path.Combine(paths);
    }
}
