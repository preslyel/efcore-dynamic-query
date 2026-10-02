namespace EFCore.DynamicQuery.Filtering;

public sealed class PageableFilterDataModel : FilterDataModel
{
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}
