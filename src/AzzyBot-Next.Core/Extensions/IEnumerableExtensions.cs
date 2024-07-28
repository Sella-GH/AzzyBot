using System;
using System.Collections.Generic;

namespace AzzyBot.Core.Extensions;

public static class IEnumerableExtensions
{
    public static bool ContainsOneItem<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable, nameof(enumerable));

        using IEnumerator<T> enumerator = enumerable.GetEnumerator();
        return enumerator.MoveNext() && !enumerator.MoveNext();
    }
}
