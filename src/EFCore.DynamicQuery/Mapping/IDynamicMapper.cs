namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// The runtime entry point for every registered <see cref="TypeMap{TSource, TDestination}"/> —
/// lookups, object-to-object mapping, and partial-update copying.
/// </summary>
public interface IDynamicMapper
{
    /// <summary>The registered map between two types, or throws if none is registered.</summary>
    ITypeMap GetTypeMap(Type sourceType, Type destinationType);

    /// <summary>The unique source type mapped to <typeparamref name="TDestination"/>.</summary>
    Type GetSourceType<TDestination>();

    /// <summary>
    /// Builds a new <typeparamref name="TDestination"/>, taking each property from
    /// <paramref name="source"/> where a mapping exists, and otherwise preserving
    /// <paramref name="existing"/>'s current value — a partial-update/patch merge.
    /// </summary>
    TDestination Copy<TSource, TDestination>(TSource source, TDestination existing)
        where TDestination : class, new();

    /// <inheritdoc cref="Copy{TSource, TDestination}(TSource, TDestination)"/>
    object Copy(object source, object existing, Type sourceType, Type destinationType);

    /// <summary>Maps a single <typeparamref name="TSource"/> to a new <typeparamref name="TDestination"/>.</summary>
    TDestination Map<TSource, TDestination>(TSource source)
        where TSource : class
        where TDestination : class, new();

    /// <summary>
    /// Maps every item in <paramref name="sequence"/> to a new <typeparamref name="TDestination"/>,
    /// populating only the given <paramref name="headers"/> (destination property names) — or
    /// every mapped property if <c>null</c>.
    /// </summary>
    ICollection<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> sequence, string[]? headers = null)
        where TSource : class
        where TDestination : class, new();

    /// <inheritdoc cref="Map{TSource, TDestination}(TSource)"/>
    object Map(object source, Type sourceType, Type destinationType);
}
