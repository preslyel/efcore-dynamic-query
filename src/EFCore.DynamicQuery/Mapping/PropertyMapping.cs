using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// The metadata for one mapped property: its name, CLR type, the reflection/expression handles
/// used to build LINQ expressions against it at runtime, and whether it's the model's own
/// identity/key property. Every real mapping registered by a <see cref="TypeMap{TSource, TDestination}"/>
/// is actually a <see cref="Mutable.MutablePropertyMapping{TDestination, TSource, TProperty}"/> -
/// this base type exists so <see cref="ITypeMap"/> can expose lookups without callers needing to
/// know (or care about) that concrete generic type.
/// </summary>
[DebuggerDisplay("PropertyName: {PropertyName}, PropertyBody: {PropertyBody}")]
public class PropertyMapping
{
    public Expression PropertyBody { get; set; } = default!;
    public MemberInfo PropertyMemberInfo { get; set; } = default!;
    public string PropertyName { get; set; } = default!;
    public Type PropertyType { get; set; } = default!;

    /// <summary>
    /// Whether this property is the model's own identity/key property — used, for
    /// example, to exclude it from a generic "every other property" projection.
    /// </summary>
    public bool? IsKey { get; set; }

    /// <summary>
    /// The body of this property's <see cref="Mutable.MutablePropertyMapping{TDestination, TSource, TProperty}.MutableFunction"/>,
    /// if it has one — populated directly by that setter so callers never need to know the
    /// concrete generic <c>MutablePropertyMapping</c> type to read it back.
    /// </summary>
    public Expression? MutableFunctionBody { get; internal set; }
}
