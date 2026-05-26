using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using nietras.SeparatedValues;

namespace AzzyBot.Core.Utilities;

public static class FileOperations
{
    private static readonly char[] _injectionCharacters = ['=', '@', '+', '-', '\t', '\r'];
    private const char InjectionEscapeCharacter = '\'';

    public static async Task<string> CreateCsvFileAsync<T>(IEnumerable<T> content, string? path = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        string filePath = (!string.IsNullOrEmpty(path))
            ? Path.Combine(Path.GetTempPath(), Path.GetFileName(path))
            : Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        IReadOnlyList<CsvColumn> columns = CsvColumnCache<T>.Columns;

        await using SepWriter writer = Sep.New(',').Writer(o => o with
        {
            CultureInfo = CultureInfo.InvariantCulture,
            WriteHeader = true,
            Escape = true
        }).ToFile(filePath);

        foreach (T item in content)
        {
            await using var row = writer.NewRow();
            foreach (CsvColumn column in columns)
            {
                row[column.Header].Set(SanitizeForInjection(FormatValue(column.Getter(item))));
            }
        }

        return filePath;
    }

    private sealed record CsvColumn(string Header, Func<object?, object?> Getter);

    private static class CsvColumnCache<T>
    {
        public static readonly IReadOnlyList<CsvColumn> Columns = BuildColumns(typeof(T), static obj => obj);
    }

    private static IReadOnlyList<CsvColumn> BuildColumns(Type type, Func<object?, object?> parentGetter)
    {
        List<CsvColumn> columns = [];
        foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            Type propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Func<object?, object?> getter = parent =>
            {
                object? owner = parentGetter(parent);
                return (owner is null) ? null : property.GetValue(owner);
            };

            // Mimic CsvHelper auto-mapping: reference types (other than string) are
            // flattened into their own members, everything else becomes a single column.
            if (propertyType == typeof(string) || !propertyType.IsClass)
            {
                columns.Add(new CsvColumn(property.Name, getter));
            }
            else
            {
                columns.AddRange(BuildColumns(propertyType, getter));
            }
        }

        return columns;
    }

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        string s => s,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    private static string SanitizeForInjection(string field)
        => (field.Length > 0 && Array.IndexOf(_injectionCharacters, field[0]) >= 0)
            ? InjectionEscapeCharacter + field
            : field;

    public static async Task<string> CreateTempFileAsync(string content, string? fileName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        string tempFilePath = (!string.IsNullOrEmpty(fileName))
            ? Path.Combine(Path.GetTempPath(), Path.GetFileName(fileName))
            : Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        await File.WriteAllTextAsync(tempFilePath, content);

        return tempFilePath;
    }

    public static async Task CreateZipFileAsync(string zipFileName, string zipFileDir, string filesDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(zipFileDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(filesDir);

        string zipPath = Path.Combine(zipFileDir, zipFileName);
        await using FileStream stream = new(zipPath, FileMode.Create);
        await using ZipArchive zipFile = new(stream, ZipArchiveMode.Create, false, Encoding.UTF8);
        foreach (string file in Directory.EnumerateFiles(filesDir))
        {
            string fileName = Path.GetFileName(file);
            await zipFile.CreateEntryFromFileAsync(file, fileName, CompressionLevel.SmallestSize);
        }

        if (!File.Exists(zipPath))
            throw new FileNotFoundException($"The zip file {zipPath} was not created.");
    }

    public static void DeleteFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        File.Delete(path);
    }

    public static void DeleteFiles(IEnumerable<string> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        foreach (string path in paths)
        {
            File.Delete(path);
        }
    }

    public static void DeleteFiles(string path, string startingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(startingName);

        foreach (string file in Directory.EnumerateFiles(path, $"{startingName}*"))
        {
            File.Delete(file);
        }
    }

    public static Task<byte[]> GetBase64BytesFromFileAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.ReadAllBytesAsync(path);
    }

    public static Task<string> GetFileContentAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return File.ReadAllTextAsync(path);
    }

    public static IEnumerable<string> GetFilesInDirectory(string path, bool latest = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return (!latest) ? Directory.EnumerateFiles(path) : Directory.EnumerateFiles(path).OrderDescending();
    }

    public static async Task WriteToFileAsync(string path, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        await File.WriteAllTextAsync(path, content);
    }
}
