using System.Linq.Expressions;

namespace EFCore.DynamicQuery.Filtering;

/// <summary>
/// One filter condition against a single mapped property. <see cref="FilterKey"/> is the open
/// discriminator used both to resolve the concrete type during JSON deserialization
/// (<see cref="FilterJsonConverter"/>/<see cref="FilterTypeRegistry"/>) and to identify it on
/// the wire - there is no closed enum of filter kinds, so adding a new one never requires
/// touching this library's own source.
/// </summary>
public interface IFilterItem
{
    string FilterKey { get; }
    string Name { get; set; }
    Expression GetBinaryExpression(Expression property);
}
