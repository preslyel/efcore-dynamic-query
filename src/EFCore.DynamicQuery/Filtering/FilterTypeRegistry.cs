using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Filtering.Filters;

namespace EFCore.DynamicQuery.Filtering;

/// <summary>
/// Maps the open <see cref="IFilterItem.FilterKey"/> string used on the wire to the concrete
/// <see cref="IFilterItem"/> type <see cref="FilterJsonConverter"/> deserializes into. Comes
/// pre-populated with the six built-in filters; register your own with
/// <see cref="Register{TFilter}"/> - re-registering an existing key replaces it, so a built-in
/// can be swapped out entirely if its default behavior doesn't fit.
/// </summary>
public sealed class FilterTypeRegistry
{
    private readonly Dictionary<string, Type> filterTypesByKey = new(StringComparer.OrdinalIgnoreCase);

    public FilterTypeRegistry()
    {
        Register<EqualFilter>("equal");
        Register<NotEqualFilter>("notEqual");
        Register<LikeFilter>("like");
        Register<InFilter>("in");
        Register<RangeFilter>("range");
        Register<NamedEqualFilter>("namedEqual");
    }

    public FilterTypeRegistry Register<TFilter>(string key) where TFilter : IFilterItem
    {
        filterTypesByKey[key] = typeof(TFilter);
        return this;
    }

    public Type Resolve(string key) =>
        filterTypesByKey.TryGetValue(key, out var type) ? type : throw new UnknownFilterKeyException(key);
}
