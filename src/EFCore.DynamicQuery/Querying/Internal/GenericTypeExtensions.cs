namespace EFCore.DynamicQuery.Querying.Internal;

internal static class GenericTypeExtensions
{
    public static bool IsAssignableFromGeneric(this Type genericType, Type givenType)
    {
        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
            return true;

        foreach (var interfaceType in givenType.GetInterfaces())
        {
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType)
                return true;
        }

        return givenType.BaseType is not null && genericType.IsAssignableFromGeneric(givenType.BaseType);
    }
}
