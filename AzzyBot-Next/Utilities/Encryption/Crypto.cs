using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace AzzyBot.Utilities.Encryption;

[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "This is an application not a library")]
public static class Crypto
{
    public static byte[] EncryptionKey = [];

    public static string Encrypt(string plain)
    {
        using AesCcm aes = new(EncryptionKey);

        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);
        byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize];
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        return new AesGcmCipher(nonce, tag, cipherBytes).ToString();
    }

    public static string Decrypt(string cipher)
    {
        AesGcmCipher gcmCipher = AesGcmCipher.FromBase64String(cipher);

        using AesCcm aes = new(EncryptionKey);
        byte[] plainBytes = new byte[gcmCipher.Cipher.Length];
        aes.Decrypt(gcmCipher.Nonce, gcmCipher.Cipher, gcmCipher.Tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
