using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AzzyBot.Utilities.Encryption;

internal sealed class EncryptionConverter : ValueConverter<string, string>
{
    internal EncryptionConverter(ConverterMappingHints? hints = null) : base(x => EncryptionHelper.Encrypt(x), x => EncryptionHelper.Decrypt(x), hints)
    { }
}
