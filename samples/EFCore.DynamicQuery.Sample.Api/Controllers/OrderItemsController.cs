using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Sample.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.DynamicQuery.Sample.Api.Controllers;

[ApiController]
[Route("api/order-items")]
public class OrderItemsController(IDynamicQueryService dynamicQueryService) : ControllerBase
{
    [HttpPost("query")]
    public async Task<ActionResult<QueryDataResultModel<ICollection<OrderItemModel>>>> Query(
        [FromBody] PageableFilterDataModel filter, CancellationToken cancellationToken)
    {
        var result = await dynamicQueryService.GetQueryDataResultAsync<OrderItemModel>(filter, cancellationToken);
        return Ok(result);
    }
}
