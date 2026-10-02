namespace EFCore.DynamicQuery.Exceptions;

public sealed class TypeMapNotFoundException : Exception
{
    public TypeMapNotFoundException(Type sourceType, Type destinationType)
        : base($"No type map is registered from '{sourceType.Name}' to '{destinationType.Name}'.")
    {
    }

    public TypeMapNotFoundException(Type destinationType)
        : base($"No type map is registered with destination type '{destinationType.Name}'.")
    {
    }
}
