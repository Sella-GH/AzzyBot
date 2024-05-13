using System;

namespace AzzyBot.Utilities.Encryption;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class EncryptionAttribute : Attribute;
