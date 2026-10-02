using System.Linq.Expressions;
using System.Reflection;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Internal;

namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// Discovers every <see cref="MappingProfile"/> in the given assemblies and exposes their
/// registered <see cref="TypeMap{TSource, TDestination}"/>s through lookup, mapping, and
/// partial-update copying.
/// </summary>
public sealed class DynamicMapper : IDynamicMapper
{
    private readonly List<ITypeMap> typeMaps;

    public DynamicMapper(IEnumerable<Assembly> assembliesToScan)
    {
        typeMaps = DiscoverTypeMaps(assembliesToScan).ToList();
    }

    private static IEnumerable<ITypeMap> DiscoverTypeMaps(IEnumerable<Assembly> assemblies) =>
        assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(MappingProfile).IsAssignableFrom(type))
            .Select(type => (MappingProfile)Activator.CreateInstance(type)!)
            .SelectMany(profile => profile.Maps);

    public ITypeMap GetTypeMap(Type sourceType, Type destinationType) =>
        typeMaps.FirstOrDefault(t => t.SourceType == sourceType && t.DestinationType == destinationType)
            ?? throw new TypeMapNotFoundException(sourceType, destinationType);

    public Type GetSourceType<TDestination>() =>
        typeMaps.FirstOrDefault(t => t.DestinationType == typeof(TDestination))?.SourceType
            ?? throw new TypeMapNotFoundException(typeof(TDestination));

    public object Copy(object source, object existing, Type sourceType, Type destinationType)
    {
        var copyMethod = typeof(DynamicMapper)
            .GetMethods()
            .First(m => m.Name == nameof(Copy) && m.IsGenericMethod && m.GetParameters().Length == 2)
            .MakeGenericMethod(sourceType, destinationType);

        return copyMethod.Invoke(this, [source, existing])!;
    }

    public TDestination Copy<TSource, TDestination>(TSource source, TDestination existing)
        where TDestination : class, new()
    {
        var typeMap = GetTypeMap(typeof(TSource), typeof(TDestination));
        var destination = new TDestination();

        foreach (var property in typeof(TDestination).GetProperties())
        {
            var propertyMapping = typeMap.GetPropertyMapping(property.Name);

            var value = propertyMapping is not null
                ? ((MemberExpression)propertyMapping.PropertyBody).Member.GetMemberValue(source!)
                : property.GetValue(existing);

            property.SetMemberValue(destination, value);
        }

        return destination;
    }

    public object Map(object source, Type sourceType, Type destinationType)
    {
        var mapMethod = typeof(DynamicMapper)
            .GetMethods()
            .First(m => m.Name == nameof(Map) && m.IsGenericMethod && m.GetParameters().Length == 1)
            .MakeGenericMethod(sourceType, destinationType);

        return mapMethod.Invoke(this, [source])!;
    }

    public TDestination Map<TSource, TDestination>(TSource source)
        where TSource : class
        where TDestination : class, new() =>
        Map<TSource, TDestination>([source]).First();

    public ICollection<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> sequence, string[]? headers = null)
        where TSource : class
        where TDestination : class, new()
    {
        var typeMap = GetTypeMap(typeof(TSource), typeof(TDestination));

        headers ??= [.. typeof(TDestination).GetProperties().Select(p => p.Name)];

        var createDestination = GetDestinationCreateDelegate<TSource, TDestination>(typeMap, headers);
        return sequence.Select(createDestination).ToList();
    }

    private static Func<TSource, TDestination> GetDestinationCreateDelegate<TSource, TDestination>(
        ITypeMap typeMap, string[] headers)
        where TDestination : class, new()
    {
        var bindings = new List<MemberAssignment>(headers.Length);
        var boundMembers = new HashSet<MemberInfo>();
        var parameter = Expression.Parameter(typeof(TSource), "source");

        foreach (var header in headers)
        {
            var propertyMapping = typeMap.GetPropertyMapping(header);

            // Two headers can resolve to the same member - same header repeated, or (since
            // TypeMap's lookup is case-insensitive) two differently-cased spellings of the same
            // field. Expression.MemberInit throws if the same member is bound twice, so skip it
            // here rather than let every caller worry about de-duplicating their own headers.
            if (propertyMapping is null || !boundMembers.Add(propertyMapping.PropertyMemberInfo)) continue;

            var sourcePropertyExpression = ExpressionParameterHelper.ReplaceRootParameter(parameter, propertyMapping.PropertyBody);
            var convertExpression = Expression.Convert(sourcePropertyExpression, propertyMapping.PropertyType);
            var bindExpression = Expression.Bind(propertyMapping.PropertyMemberInfo, convertExpression);

            bindings.Add(bindExpression);
        }

        var newExpression = Expression.New(typeof(TDestination));
        var memberInitExpression = Expression.MemberInit(newExpression, bindings);

        return Expression.Lambda<Func<TSource, TDestination>>(memberInitExpression, parameter).Compile();
    }
}
