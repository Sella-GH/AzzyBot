using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AzzyBot.Data.Extensions;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "https://github.com/dotnet/sdk/issues/51681")]
public static class EFCoreExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> IncludeIf(bool condition, Func<IQueryable<T>, IQueryable<T>> transform)
        {
            ArgumentNullException.ThrowIfNull(transform);

            return (condition) ? transform(query) : query;
        }
    }
}
