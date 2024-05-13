using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AzzyBot.Utilities.Encryption;

internal static class EncryptionHelper
{
    internal static string Key = string.Empty;

    internal static string Encrypt(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        byte[] encryptedData;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(Key);
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor();

            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new(csEncrypt))
            {
                sw.Write(data);
            }

            encryptedData = msEncrypt.ToArray();
        }

        return Convert.ToBase64String(encryptedData);
    }

    internal static string Decrypt(string data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(data, nameof(data));

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(Key);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        ICryptoTransform decryptor = aes.CreateDecryptor();

        byte[] encryptedData = Convert.FromBase64String(data);
        using MemoryStream msDecrypt = new(encryptedData);
        using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new(csDecrypt);

        return sr.ReadToEnd();
    }
}
