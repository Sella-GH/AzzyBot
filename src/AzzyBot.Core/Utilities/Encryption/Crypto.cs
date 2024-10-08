﻿using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace AzzyBot.Core.Utilities.Encryption;

[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Well it still does for security.")]
public static class Crypto
{
    public static byte[] EncryptionKey { get; set; } = [];

    public static string Encrypt(string plain, byte[]? newKey = null)
    {
        using AesCcm aes = new(newKey ?? EncryptionKey);

        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return new AesGcmCipher(nonce, tag, cipherBytes).ToString();
    }

    public static string Decrypt(string cipher, byte[]? newKey = null)
    {
        AesGcmCipher gcmCipher = AesGcmCipher.FromBase64String(cipher);

        using AesCcm aes = new(newKey ?? EncryptionKey);
        byte[] plainBytes = new byte[gcmCipher.Cipher.Length];
        aes.Decrypt(gcmCipher.Nonce, gcmCipher.Cipher, gcmCipher.Tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
