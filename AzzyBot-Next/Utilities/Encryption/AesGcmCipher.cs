using System;
using System.Linq;
using System.Security.Cryptography;

namespace AzzyBot.Utilities.Encryption;

internal sealed class AesGcmCipher(byte[] nonce, byte[] tag, byte[] cipher)
{
    internal byte[] Nonce { get; } = nonce;
    internal byte[] Tag { get; } = tag;
    internal byte[] Cipher { get; } = cipher;

    internal static AesGcmCipher FromBase64String(string data)
    {
        byte[] dataBytes = Convert.FromBase64String(data);
        return new(
            dataBytes.Take(AesGcm.NonceByteSizes.MaxSize).ToArray(),
            dataBytes[^AesGcm.TagByteSizes.MaxSize..],
            dataBytes[AesGcm.NonceByteSizes.MaxSize..^AesGcm.TagByteSizes.MaxSize]
        );
    }

    public override string ToString() => Convert.ToBase64String(Nonce.Concat(Cipher).Concat(Tag).ToArray());
}
