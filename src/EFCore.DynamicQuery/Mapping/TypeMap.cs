using System.Linq.Expressions;
using EFCore.DynamicQuery.Mapping.Mutable;

namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// A one-directional mapping from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>.
/// Every property whose name matches on both sides is mapped automatically when the map is
/// created — call <see cref="ForMember{TProperty}"/> only for the exceptions: a differently-named
/// property, the identity key, or one that needs a runtime transform. Call <see cref="ReverseMap"/>
/// to also register the opposite direction, inheriting every renamed property automatically
/// instead of restating it.
/// </summary>
public sealed class TypeMap<TSource, TDestination> : ITypeMap
    where TSource : class, new()
    where TDestination : class, new()
{
    // Case-insensitive so a caller can resolve a mapped property by whatever casing convention
    // their own field names happen to use (e.g. camelCase over the wire from a JSON request)
    // without having to pre-normalize every incoming name to match this type's own PascalCase.
    private readonly Dictionary<string, PropertyMapping> properties = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<TypeMap<TDestination, TSource>>> reversedBindings = [];
    private readonly Action<ITypeMap> registerMap;

    public Type SourceType { get; } = typeof(TSource);
    public Type DestinationType { get; } = typeof(TDestination);

    internal TypeMap(Action<ITypeMap> registerMap)
    {
        this.registerMap = registerMap;
        AutoMapMatchingProperties();
    }

    /// <summary>
    /// Registers (or overrides an auto-mapped) property explicitly — for a property whose name
    /// differs between source and destination, that needs <see cref="PropertyMapping.IsKey"/>
    /// set, or that needs a <see cref="MutablePropertyMapping{TDestination, TSource, TProperty}.MutableFunction"/>.
    /// </summary>
    public TypeMap<TSource, TDestination> ForMember<TProperty>(
        Expression<Func<TDestination, TProperty>> destinationProperty,
        Expression<Func<TSource, TProperty>> sourceProperty,
        Action<MutablePropertyMapping<TDestination, TSource, TProperty>>? config = null)
    {
        var destinationMemberInfo = ((MemberExpression)destinationProperty.Body).Member;

        var mapping = new MutablePropertyMapping<TDestination, TSource, TProperty>(
            destinationMemberInfo.Name, sourceProperty.Body, destinationMemberInfo, typeof(TProperty));

        config?.Invoke(mapping);
        properties[mapping.PropertyName] = mapping;

        // Remember only the structural correspondence (not this call's own config callback —
        // see ReverseMap's own remarks) so the opposite direction can inherit this rename.
        reversedBindings.Add(reverse => reverse.ForMember(sourceProperty, destinationProperty));

        return this;
    }

    /// <summary>
    /// Registers the opposite direction (<typeparamref name="TDestination"/> to
    /// <typeparamref name="TSource"/>) into the same profile, automatically inheriting every
    /// renamed property configured via <see cref="ForMember{TProperty}"/> on this map — so a
    /// rename is stated once, not once per direction. Deliberately does not carry over each
    /// property's own <c>config</c> callback (e.g. <see cref="PropertyMapping.IsKey"/>) — that
    /// reflects a decision about a specific property on the specific side it was set on, not a
    /// structural fact this method can safely infer and invert. Call <see cref="ForMember{TProperty}"/>
    /// again on the returned map for any property that also needs its own config on this side.
    /// </summary>
    public TypeMap<TDestination, TSource> ReverseMap()
    {
        var reverse = new TypeMap<TDestination, TSource>(registerMap);

        foreach (var applyReversed in reversedBindings)
            applyReversed(reverse);

        registerMap(reverse);
        return reverse;
    }

    private void AutoMapMatchingProperties()
    {
        var sourceParameter = Expression.Parameter(typeof(TSource), "s");

        foreach (var destinationProperty in typeof(TDestination).GetProperties())
        {
            var sourceProperty = typeof(TSource).GetProperty(destinationProperty.Name);
            if (sourceProperty is null) continue;

            var sourceBody = Expression.Property(sourceParameter, sourceProperty);
            properties[destinationProperty.Name] = new MutablePropertyMapping<TDestination, TSource, object>(
                destinationProperty.Name, sourceBody, destinationProperty, destinationProperty.PropertyType);
        }
    }

    public PropertyMapping? GetPropertyMapping(string destinationPropertyName) =>
        properties.TryGetValue(destinationPropertyName, out var mapping) ? mapping : null;

    public IEnumerable<PropertyMapping> GetPropertyMappings() => properties.Values;

    public Expression? GetSourceProperty(string destinationPropertyName) =>
        properties.TryGetValue(destinationPropertyName, out var mapping) ? mapping.PropertyBody : null;

    public IEnumerable<string> GetRequiredFields() =>
        properties.Values
            .Where(m => m.IsKey == true)
            .Select(m => m.PropertyName);
}
