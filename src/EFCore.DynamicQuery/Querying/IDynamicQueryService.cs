using EFCore.DynamicQuery.Filtering;

namespace EFCore.DynamicQuery.Querying;

/// <summary>
/// A single entry point for "get me data shaped as <typeparamref name="TModel"/>" - no bespoke
/// repository or service needed per model, just <c>GetQueryDataResultAsync&lt;SomeModel&gt;(filter, ct)</c>.
/// </summary>
public interface IDynamicQueryService
{
    Task<QueryDataResultModel<ICollection<TModel>>> GetQueryDataResultAsync<TModel>(
        PageableFilterDataModel filterData, CancellationToken cancellationToken = default)
        where TModel : class, new();

    ICollection<TModel> GetQueryDataResultAsync<TModel>()
        where TModel : class, new();

    ICollection<TModel> GetDataById<TModel, TId>(TId id, bool applyTransforms = false)
        where TModel : class, new();
}
