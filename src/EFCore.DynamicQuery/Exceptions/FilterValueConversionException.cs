namespace EFCore.DynamicQuery.Exceptions;

public sealed class FilterValueConversionException : Exception
{
    public FilterValueConversionException(Type propertyType, string? propertyName)
        : base(propertyName is null
            ? $"The provided filter value is not supported for type '{propertyType.Name}'."
            : $"The provided filter value is not supported for '{propertyName}' (type '{propertyType.Name}').")
    {
    }
}
