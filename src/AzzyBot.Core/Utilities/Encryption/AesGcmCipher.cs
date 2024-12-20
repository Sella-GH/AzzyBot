﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;

namespace AzzyBot.Core.Utilities.Encryption;

[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Needed for security")]
public sealed class AesGcmCipher(byte[] nonce, byte[] tag, byte[] cipher)
{
    public byte[] Nonce { get; } = nonce;
    public byte[] Tag { get; } = tag;
    public byte[] Cipher { get; } = cipher;

    public static AesGcmCipher FromBase64String(string data)
    {
        byte[] dataBytes = Convert.FromBase64String(data);

        return new(
            dataBytes[..AesGcm.NonceByteSizes.MaxSize],
            dataBytes[^AesGcm.TagByteSizes.MaxSize..],
            dataBytes[AesGcm.NonceByteSizes.MaxSize..^AesGcm.TagByteSizes.MaxSize]
        );
    }

    public override string ToString()
        => Convert.ToBase64String(Nonce.Concat(Cipher).Concat(Tag).ToArray());
}
