using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Querying;
using EFCore.DynamicQuery.Sample.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.DynamicQuery.Sample.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IDynamicQueryService dynamicQueryService) : ControllerBase
{
    [HttpPost("query")]
    public async Task<ActionResult<QueryDataResultModel<ICollection<OrderModel>>>> Query(
        [FromBody] PageableFilterDataModel filter, CancellationToken cancellationToken)
    {
        var result = await dynamicQueryService.GetQueryDataResultAsync<OrderModel>(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public ActionResult<OrderModel> GetById(long id)
    {
        var order = dynamicQueryService.GetDataById<OrderModel, long>(id).FirstOrDefault();
        return order is null ? NotFound() : Ok(order);
    }
}
