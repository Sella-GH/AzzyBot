using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AzzyBot.Utilities.Encryption;

internal static class EncryptionHelper
{
    internal static string Iv = string.Empty;
    internal static string Key = string.Empty;

    internal static string Encrypt(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        using Aes aes = Aes.Create();
        aes.IV = Encoding.UTF8.GetBytes(Iv);
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        ICryptoTransform encryptor = aes.CreateEncryptor();

        using MemoryStream msEncrypt = new();
        using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
        csEncrypt.Write(dataBytes, 0, dataBytes.Length);
        csEncrypt.FlushFinalBlock();

        byte[] encryptedData = msEncrypt.ToArray();

        return Convert.ToBase64String(encryptedData);
    }

    internal static string Decrypt(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return string.Empty;

        byte[] dataBytes = Convert.FromBase64String(data);

        using Aes aes = Aes.Create();
        aes.IV = Encoding.UTF8.GetBytes(Iv);
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        ICryptoTransform decryptor = aes.CreateDecryptor();

        using MemoryStream msDecrypt = new(dataBytes);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);

        byte[] decryptedBytes = new byte[dataBytes.Length];
        int decryptedByteCount = csDecrypt.Read(decryptedBytes, 0, decryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes, 0, decryptedByteCount);
    }
}
