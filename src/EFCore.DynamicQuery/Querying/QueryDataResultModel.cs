namespace EFCore.DynamicQuery.Querying;

public sealed class QueryDataResultModel<T> where T : class
{
    public int TotalCount { get; set; }
    public T? Result { get; set; }
}
