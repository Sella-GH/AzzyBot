using System;
using System.Linq;
using System.Security.Cryptography;

namespace AzzyBot.Utilities.Encryption;

internal sealed class AesGcmCipher
{
    internal byte[] Nonce { get; }
    internal byte[] Tag { get; }
    internal byte[] Cipher { get; }

    internal static AesGcmCipher FromBase64String(string data)
    {
        byte[] dataBytes = Convert.FromBase64String(data);
        return new(
            dataBytes.Take(AesGcm.NonceByteSizes.MaxSize).ToArray(),
            dataBytes[^AesGcm.TagByteSizes.MaxSize..],
            dataBytes[AesGcm.NonceByteSizes.MaxSize..^AesGcm.TagByteSizes.MaxSize]
        );
    }

    internal AesGcmCipher(byte[] nonce, byte[] tag, byte[] cipher)
    {
        Nonce = nonce;
        Tag = tag;
        Cipher = cipher;
    }

    public override string ToString() => Convert.ToBase64String(Nonce.Concat(Cipher).Concat(Tag).ToArray());
}
