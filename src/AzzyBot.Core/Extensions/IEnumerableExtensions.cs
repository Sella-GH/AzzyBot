using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AzzyBot.Core.Extensions;

public static class IEnumerableExtensions
{
    public static bool ContainsOneItem<T>(this IEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        using IEnumerator<T> enumerator = enumerable.GetEnumerator();

        return enumerator.MoveNext() && !enumerator.MoveNext();
    }

    public static async Task<bool> ContainsOneItemAsync<T>(this IAsyncEnumerable<T> enumerable)
    {
        ArgumentNullException.ThrowIfNull(enumerable);

        await using IAsyncEnumerator<T> enumerator = enumerable.GetAsyncEnumerator();

        return await enumerator.MoveNextAsync() && !await enumerator.MoveNextAsync();
    }
}
