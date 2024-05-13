using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AzzyBot.Utilities.Encryption;

internal static class ModelPropertyEncrypterExtension
{
    public static void UseEncryption(this ModelBuilder modelBuilder)
    {
        // Instantiate the EncryptionConverter
        EncryptionConverter converter = new();

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (IMutableProperty property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(string) && !IsDiscriminator(property))
                {
                    object[]? attributes = property.PropertyInfo?.GetCustomAttributes(typeof(EncryptionAttribute), false);
                    if (attributes?.Length > 0)
                        property.SetValueConverter(converter);
                }
            }
        }
    }

    /// <summary>
    /// A helper function to ignore EF Core Discriminator
    /// </summary>
    private static bool IsDiscriminator(IMutableProperty property)
        => property.Name == "Discriminator" || property.PropertyInfo is null;
}
