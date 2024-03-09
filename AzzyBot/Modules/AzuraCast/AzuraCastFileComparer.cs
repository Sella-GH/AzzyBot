using System;
using System.Collections.Generic;
using AzzyBot.Modules.AzuraCast.Models;

namespace AzzyBot.Modules.AzuraCast;

/// <summary>
/// Compares two FilesModel objects for equality.
/// </summary>
internal sealed class AzuraCastFileComparer : IEqualityComparer<FilesModel>
{
    /// <summary>
    /// Determines whether the specified FilesModel objects are equal.
    /// </summary>
    /// <param name="x">The first FilesModel object to compare.</param>
    /// <param name="y">The second FilesModel object to compare.</param>
    /// <returns>true if the specified FilesModel objects are equal; otherwise, false.</returns>
    public bool Equals(FilesModel? x, FilesModel? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.Unique_Id == y.Unique_Id && x.Song_Id == y.Song_Id;
    }

    /// <summary>
    /// Returns a hash code for the specified FilesModel object.
    /// </summary>
    /// <param name="obj">The FilesModel object for which a hash code is to be returned.</param>
    /// <returns>A hash code for the specified object.</returns>
    public int GetHashCode(FilesModel? obj)
    {
        if (obj is null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = (hash * 23) + (obj.Unique_Id?.GetHashCode(StringComparison.InvariantCultureIgnoreCase) ?? 0);
            hash = (hash * 23) + (obj.Song_Id?.GetHashCode(StringComparison.InvariantCultureIgnoreCase) ?? 0);
            return hash;
        }
    }
}
