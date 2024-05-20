using System;
using System.Linq;
using System.Security.Cryptography;

namespace AzzyBot.Utilities.Encryption;

public sealed class AesGcmCipher
{
    public byte[] Nonce { get; }
    public byte[] Tag { get; }
    public byte[] Cipher { get; }

    public static AesGcmCipher FromBase64String(string data)
    {
        byte[] dataBytes = Convert.FromBase64String(data);
        return new(
            dataBytes.Take(AesGcm.NonceByteSizes.MaxSize).ToArray(),
            dataBytes[^AesGcm.TagByteSizes.MaxSize..],
            dataBytes[AesGcm.NonceByteSizes.MaxSize..^AesGcm.TagByteSizes.MaxSize]
        );
    }

    public AesGcmCipher(byte[] nonce, byte[] tag, byte[] cipher)
    {
        Nonce = nonce;
        Tag = tag;
        Cipher = cipher;
    }

    public override string ToString() => Convert.ToBase64String(Nonce.Concat(Cipher).Concat(Tag).ToArray());
}
