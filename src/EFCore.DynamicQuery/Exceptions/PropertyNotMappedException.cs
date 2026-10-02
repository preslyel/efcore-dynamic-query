namespace EFCore.DynamicQuery.Exceptions;

public sealed class PropertyNotMappedException : Exception
{
    public PropertyNotMappedException(string propertyName)
        : base($"Property '{propertyName}' is not mapped.")
    {
    }
}
