using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Query;

namespace AzzyBot.Data.Extensions;

public static class EFCoreExtensions
{
    public static IQueryable<T> IncludeIf<T>(this IQueryable<T> query, bool condition, Func<IQueryable<T>, IQueryable<T>> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        return (condition) ? transform(query) : query;
    }

    public static IQueryable<T> IncludeIf<T, T2>(this IIncludableQueryable<T, T2> query, bool condition, Func<IIncludableQueryable<T, T2>, bool, IQueryable<T>> transform) where T : class
    {
        ArgumentNullException.ThrowIfNull(transform);

        return (condition) ? transform(query, condition) : query;
    }

    public static IQueryable<T> IncludeIf<T, T2>(this IIncludableQueryable<T, IEnumerable<T2>> query, bool condition, Func<IIncludableQueryable<T, IEnumerable<T2>>, bool, IQueryable<T>> transform) where T : class
    {
        ArgumentNullException.ThrowIfNull(transform);

        return (condition) ? transform(query, condition) : query;
    }
}
