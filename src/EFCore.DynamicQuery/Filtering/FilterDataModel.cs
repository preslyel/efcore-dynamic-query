namespace EFCore.DynamicQuery.Filtering;

public class FilterDataModel
{
    public IFilterItem[] Filters { get; set; } = [];
    public string GroupBy { get; set; } = "";
    public string OrderBy { get; set; } = "";
    public bool Ascending { get; set; }
    public List<string> Fields { get; set; } = [];

    /// <summary>
    /// Whether to apply each mapped property's <see cref="Mapping.Mutable.MutablePropertyMapping{TDestination, TSource, TProperty}.MutableFunction"/>
    /// (if any) before returning results - e.g. converting stored values between units,
    /// currencies, or any other per-property runtime transform.
    /// </summary>
    public bool ApplyTransforms { get; set; }
}
