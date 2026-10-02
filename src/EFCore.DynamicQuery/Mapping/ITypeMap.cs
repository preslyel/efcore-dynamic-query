using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Mapping;

/// <summary>
/// The read-side contract the query engine uses to resolve a mapped property, without
/// needing to know the concrete <see cref="TypeMap{TSource, TDestination}"/> that produced it.
/// </summary>
public interface ITypeMap
{
    Type SourceType { get; }
    Type DestinationType { get; }
    PropertyMapping? GetPropertyMapping(string destinationPropertyName);
    IEnumerable<PropertyMapping> GetPropertyMappings();
    Expression? GetSourceProperty(string destinationPropertyName);
    IEnumerable<string> GetRequiredFields();
}
