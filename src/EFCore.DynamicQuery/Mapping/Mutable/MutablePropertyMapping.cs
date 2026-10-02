using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.DynamicQuery.Mapping.Mutable;

/// <summary>
/// A <see cref="PropertyMapping"/> typed to the concrete destination/source/property triple it
/// belongs to, with an optional runtime transform (<see cref="MutableFunction"/>) — e.g.
/// converting a stored value between units, currencies, or any other computed transform.
/// Every real mapping is one of these, whether or not it actually sets a mutable function.
/// </summary>
public class MutablePropertyMapping<TDestination, TSource, TProperty> : PropertyMapping
{
    private Expression<Func<TProperty, TProperty>>? mutableFunction;

    public Expression<Func<TProperty, TProperty>>? MutableFunction
    {
        get => mutableFunction;
        set
        {
            mutableFunction = value;
            MutableFunctionBody = value?.Body;
        }
    }

    public MutablePropertyMapping(string propertyName, Expression propertyBody, MemberInfo propertyMemberInfo, Type propertyType)
    {
        PropertyName = propertyName;
        PropertyBody = propertyBody;
        PropertyMemberInfo = propertyMemberInfo;
        PropertyType = propertyType;
    }
}
