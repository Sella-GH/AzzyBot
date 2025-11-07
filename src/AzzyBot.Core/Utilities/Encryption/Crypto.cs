using System;
using System.Security.Cryptography;
using System.Text;

namespace AzzyBot.Core.Utilities.Encryption;

public static class Crypto
{
    private static byte[] EncryptionKey = new byte[32];

    public static void SetEncryptionKey(byte[] key)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfLessThan(key.Length, 32);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(key.Length, 32);

        EncryptionKey = key;
    }

    /// <summary>
    /// Encrypts the specified plain text using AES-GCM encryption.
    /// </summary>
    /// <param name="plain">The plain text to encrypt.</param>
    /// <param name="newKey">An optional 32-byte encryption key. If not provided, a default key is used.</param>
    /// <returns>A Base64-encoded string containing the nonce, ciphertext, and authentication tag, separated by colons.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="newKey"/> is provided and is not a 32-byte array.</exception>
    public static string Encrypt(string plain, byte[]? newKey = null)
    {
        byte[] key = newKey ?? EncryptionKey;
        if (key.Length is not 32)
            throw new ArgumentException("Key must be a 32-byte array.", nameof(newKey));

        byte[] nonce = new byte[12]; // 96-bit nonce
        RandomNumberGenerator.Fill(nonce);

        byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tagBytes = new byte[16]; // 128-bit tag

        using AesGcm aes = new(key, 16);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tagBytes);

        string nonceBase64 = Convert.ToBase64String(nonce);
        string cipherBase64 = Convert.ToBase64String(cipherBytes);
        string tagBase64 = Convert.ToBase64String(tagBytes);

        return $"{nonceBase64}:{cipherBase64}:{tagBase64}";
    }

    /// <summary>
    /// Decrypts a cipher text using AES-GCM with the specified key.
    /// </summary>
    /// <param name="cipher">The cipher text to decrypt, in the format 'nonce:cipher:tag'.</param>
    /// <param name="newKey">An optional 32-byte key to use for decryption. If not provided, a default key is used.</param>
    /// <returns>The decrypted plain text as a string.</returns>
    /// <exception cref="FormatException">Thrown if <paramref name="cipher"/> is not in the format 'nonce:cipher:tag'.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="cipher"/> is null, empty, or consists only of white-space characters. Also thrown if
    /// <paramref name="newKey"/> is provided but is not a 32-byte array.</exception>
    public static string Decrypt(string cipher, byte[]? newKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipher);

        string[] parts = cipher.Split(':');
        if (parts.Length is not 3)
            throw new FormatException("Cipher text must be in the format 'nonce:cipher:tag'.");

        byte[] nonce = Convert.FromBase64String(parts[0]);
        byte[] cipherBytes = Convert.FromBase64String(parts[1]);
        byte[] tagBytes = Convert.FromBase64String(parts[2]);
        byte[] key = newKey ?? EncryptionKey;
        if (key.Length is not 32)
            throw new ArgumentException("Key must be a 32-byte array.", nameof(newKey));

        byte[] plainBytes = new byte[cipherBytes.Length];

        using AesGcm aes = new(key, 16);
        aes.Decrypt(nonce, cipherBytes, tagBytes, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }

    // TODO: Remove this method in a future release after enough time has passed since the encryption schema change.
    public static bool CheckIfNewCipherIsUsed(string cipher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipher);

        string[] parts = cipher.Split(':');

        return parts.Length is 3;
    }

    /// <summary>
    /// Migrates a legacy cipher string to a new encryption format.
    /// </summary>
    /// <param name="legacyCipher">The base64-encoded string representing the legacy cipher to be migrated.</param>
    /// <returns>A string containing the newly encrypted cipher in the updated format.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the migration process fails, typically due to an invalid legacy cipher or incorrect encryption key.</exception>
    // TODO: Remove this method in a future release after enough time has passed since the encryption schema change.
    public static string MigrateOldCipherToNew(string legacyCipher)
    {
        try
        {
            AesGcmCipher gcmCipher = AesGcmCipher.FromBase64String(legacyCipher);

            using AesCcm aes = new(EncryptionKey);
            byte[] plainBytes = new byte[gcmCipher.Cipher.Length];
            aes.Decrypt(gcmCipher.Nonce, gcmCipher.Cipher, gcmCipher.Tag, plainBytes);
            string plain = Encoding.UTF8.GetString(plainBytes);

            return Encrypt(plain, EncryptionKey);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Failed to migrate old cipher to new format: Invalid base64 encoding.", ex);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Failed to migrate old cipher to new format: Decryption failed.", ex);
        }
    }
}
