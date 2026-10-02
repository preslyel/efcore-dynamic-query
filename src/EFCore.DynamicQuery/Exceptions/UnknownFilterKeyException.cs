namespace EFCore.DynamicQuery.Exceptions;

public sealed class UnknownFilterKeyException : Exception
{
    public UnknownFilterKeyException(string key)
        : base($"No filter is registered for key '{key}'. Register it via FilterTypeRegistry.Register<TFilter>(\"{key}\").")
    {
    }
}
