using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AzzyBot.Utilities.Records.AzuraCast;

namespace AzzyBot.Utilities;

[SuppressMessage("Roslynator", "RCS1241:Implement non-generic counterpart", Justification = "Not needed")]
public sealed class FileComparer : IEqualityComparer<FilesRecord>
{
    public bool Equals(FilesRecord? x, FilesRecord? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.UniqueId == y.UniqueId && x.SongId == y.SongId;
    }

    public int GetHashCode(FilesRecord? obj)
    {
        if (obj is null)
            return 0;

        unchecked
        {
            int hash = 17;

            hash = (hash * 23) + (obj.UniqueId?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0);
            hash = (hash * 23) + (obj.SongId?.GetHashCode(StringComparison.OrdinalIgnoreCase) ?? 0);

            return hash;
        }
    }
}
