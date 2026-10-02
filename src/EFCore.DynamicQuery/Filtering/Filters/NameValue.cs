namespace EFCore.DynamicQuery.Filtering.Filters;

public sealed class NameValue
{
    public required string Name { get; set; }
    public object? Value { get; set; }
}
