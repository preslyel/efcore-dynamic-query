using EFCore.DynamicQuery.Filtering;

namespace EFCore.DynamicQuery.Querying;

public static class PagingQueryExtensions
{
    public static IQueryable<T> GetPagedData<T>(this IQueryable<T> query, int pageIndex, int pageSize) =>
        pageSize == 0 ? query : query.Skip((pageIndex == 0 ? pageIndex : pageIndex - 1) * pageSize).Take(pageSize);

    public static IQueryable<T> GetPagedData<T>(this IQueryable<T> query, PageableFilterDataModel request) =>
        query.GetPagedData(request.PageIndex, request.PageSize);

    public static IQueryable<T> TakeIfNotNull<T>(this IQueryable<T> query, int? take) =>
        take.HasValue ? query.Take(take.Value) : query;

    public static IQueryable<T> SkipIfNotNull<T>(this IQueryable<T> query, int? skip) =>
        skip.HasValue ? query.Skip(skip.Value) : query;
}
