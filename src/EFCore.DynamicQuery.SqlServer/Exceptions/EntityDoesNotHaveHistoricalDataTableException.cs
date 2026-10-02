namespace EFCore.DynamicQuery.SqlServer.Exceptions;

public sealed class EntityDoesNotHaveHistoricalDataTableException : Exception
{
    public EntityDoesNotHaveHistoricalDataTableException(Type entityType)
        : base($"Entity '{entityType.Name}' does not have historical (temporal) data - it isn't mapped as a temporal table.")
    {
    }
}
