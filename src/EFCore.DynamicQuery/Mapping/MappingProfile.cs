namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// A container for one or more <see cref="TypeMap{TSource, TDestination}"/> declarations. Every
/// map is independent — group them into one profile however makes sense for the consuming
/// application (typically one profile per entity, with one <see cref="CreateMap{TSource, TDestination}"/>
/// call per direction/DTO shape needed).
/// </summary>
public abstract class MappingProfile
{
    private readonly List<ITypeMap> maps = [];
    public IEnumerable<ITypeMap> Maps => maps;

    protected TypeMap<TSource, TDestination> CreateMap<TSource, TDestination>()
        where TSource : class, new()
        where TDestination : class, new()
    {
        var map = new TypeMap<TSource, TDestination>(RegisterMap);
        maps.Add(map);
        return map;
    }

    private void RegisterMap(ITypeMap map) => maps.Add(map);
}
