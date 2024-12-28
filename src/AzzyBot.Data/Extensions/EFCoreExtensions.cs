using System;
using System.Linq;

namespace AzzyBot.Data.Extensions;

public static class EFCoreExtensions
{
    public static IQueryable<T> IncludeIf<T>(this IQueryable<T> query, bool condition, Func<IQueryable<T>, IQueryable<T>> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        return (condition) ? transform(query) : query;
    }
}
