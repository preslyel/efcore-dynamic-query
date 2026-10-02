# EFCore.DynamicQuery

A thin layer over EF Core that turns a JSON filter/sort/page/fields request into a real, translatable `IQueryable<T>` — so "add filtering, sorting, paging, and a DTO shape to this list endpoint" stops being a bespoke feature you build per entity.

It generates no SQL of its own. Every query it builds is ordinary LINQ against EF Core's `IQueryable<T>`, so EF's own provider does the translation — you keep full control of everything else EF already gives you (migrations, providers, tracking).

## Why

Almost every backend ends up with the same shape of endpoint, over and over: take a list of some entity, let the caller filter it, sort it, page it, and shape the response into a DTO that isn't a 1:1 copy of the entity. Usually that means either N near-identical controller actions, or a growing pile of ad-hoc `IQueryable` extension methods per entity.

This library gives you that once. You describe how an entity maps to its DTO — including renamed properties, values computed across a navigation, and runtime transforms — and a generic `IDynamicQueryService` handles the rest: resolving a client-supplied field name back to the real mapped expression, building the filter predicate, applying `Include`s for anything the DTO reaches across a relationship, ordering, paging, and materializing the result.

A new list endpoint becomes a controller action and a mapping profile entry — not a new query.

## Install

```bash
dotnet add package EFCore.DynamicQuery

# optional — SQL Server temporal ("as of a date" / full history) querying
dotnet add package EFCore.DynamicQuery.SqlServer
```

Targets `net10.0`.

## Setup

Three calls in `Program.cs`:

```csharp
builder.Services.AddDbContext<YourDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDynamicMapper(typeof(YourMappingProfile));   // discovers every MappingProfile in that assembly
builder.Services.AddDynamicQueryFilters();                        // built-in filters + a FilterTypeRegistry you can extend
builder.Services.AddDynamicQueryService<YourDbContext>();         // registers IDynamicQueryService

// The library takes no dependency on ASP.NET Core, so wire the filter JSON converter
// into your own JsonOptions yourself:
builder.Services.AddOptions<JsonOptions>()
    .Configure<FilterTypeRegistry>((options, registry) =>
        options.JsonSerializerOptions.Converters.Add(new FilterJsonConverter(registry)));
```

That's the entire setup surface. The only thing you'll typically extend later is the filter registry (see [Custom filters](#custom-filters)).

## Define a mapping profile

One `MappingProfile` per bounded set of entities, one `CreateMap<TEntity, TModel>()` per DTO shape you expose:

```csharp
public sealed class ProjectForgeMappingProfile : MappingProfile
{
    public ProjectForgeMappingProfile()
    {
        CreateMap<Order, OrderModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true)
            .ForMember(dest => dest.TotalItems, src => src.OrderItems.Count)
            .ForMember(dest => dest.ItemsList, src => string.Join(", ", src.OrderItems.Select(oi => oi.ProductName).Distinct()))
            .ForMember(dest => dest.CustomerName, src => src.Customer != null ? src.Customer.Name : null);

        CreateMap<OrderItem, OrderItemModel>()
            .ForMember(dest => dest.Id, src => src.Id, cfg => cfg.IsKey = true)
            // Stored as thousands in this schema — a caller that asks for transforms
            // (ApplyTransforms: true) sees/filters in real units instead.
            .ForMember(dest => dest.Quantity, src => src.Quantity, cfg => cfg.MutableFunction = q => q * 1000);
    }
}
```

Every property whose name matches on both sides is mapped automatically when the map is created. `ForMember` is only for the exceptions:

- a differently-named or computed property (`TotalItems`, `ItemsList`, `CustomerName` above),
- the identity/key property (`cfg.IsKey = true`),
- a runtime value transform (`cfg.MutableFunction = ...`) — e.g. unit or currency conversion, applied when the caller opts in via `ApplyTransforms: true`.

`ReverseMap()` registers the opposite direction, inheriting every rename automatically.

## Use it

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController(IDynamicQueryService dynamicQueryService) : ControllerBase
{
    [HttpPost("query")]
    public async Task<ActionResult<QueryDataResultModel<ICollection<OrderModel>>>> Query(
        [FromBody] PageableFilterDataModel filter, CancellationToken cancellationToken) =>
        Ok(await dynamicQueryService.GetQueryDataResultAsync<OrderModel>(filter, cancellationToken));

    [HttpGet("{id:long}")]
    public ActionResult<OrderModel> GetById(long id) =>
        dynamicQueryService.GetDataById<OrderModel, long>(id).FirstOrDefault() is { } order ? Ok(order) : NotFound();
}
```

Request body for `POST /api/orders/query` (assuming the default ASP.NET Core camelCase JSON policy):

```json
{
  "filters": [
    { "filter": "equal", "name": "status", "value": 2 },
    { "filter": "range", "name": "orderDate", "fromValue": "2026-01-01", "toValue": "2026-01-31" }
  ],
  "orderBy": "orderDate",
  "ascending": false,
  "pageIndex": 0,
  "pageSize": 25,
  "fields": ["id", "orderDate", "amount", "customerName"]
}
```

Response:

```json
{
  "totalCount": 42,
  "result": [
    { "id": 101, "orderDate": "2026-01-18T00:00:00", "amount": 340.00, "customerName": "Acme Corp" }
  ]
}
```

`customerName` isn't a real column on `Order` — it's mapped across the `Customer` navigation in the profile above, and the service adds the right `.Include()` for it automatically, whether it's requested via `fields` or filtered/sorted on directly.

## Built-in filters

| `filter` key | Shape | Behavior |
|---|---|---|
| `equal` | `{ "name", "value" }` | `property == value` |
| `notEqual` | `{ "name", "value" }` | `property != value` |
| `like` | `{ "name", "value" }` | SQL `LIKE` via `EF.Functions.Like` |
| `in` | `{ "name", "values": [...] }` | `property` is one of `values` |
| `range` | `{ "name", "fromValue", "toValue" }` | `fromValue <= property <= toValue` |
| `namedEqual` | `{ "name", "nameValues": [{ "name", "value" }, ...] }` | ANDs equality across several sibling properties of the same object (e.g. a composite key) |

`name` always refers to the DTO's property name, not the entity's — it's resolved through the mapping profile.

## Custom filters

Implement `FilterItem`, give it a wire key, and register it:

```csharp
public sealed class StartsWithFilter : FilterItem
{
    public required string Value { get; set; }
    public override string FilterKey => "startsWith";

    public override Expression GetBinaryExpression(Expression property)
    {
        var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
            nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])!;
        var functions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

        var call = Expression.Call(null, likeMethod, functions, property, Expression.Constant(Value + "%"));
        return Expression.Equal(Expression.Constant(true), call);
    }
}
```

```csharp
builder.Services.AddDynamicQueryFilters(registry =>
    registry.Register<StartsWithFilter>("startsWith"));
```

Registering an existing key (e.g. `"equal"`) replaces the built-in filter entirely — useful if its default behavior doesn't fit.

## Optional: SQL Server temporal queries (`EFCore.DynamicQuery.SqlServer`)

For entities configured as [SQL Server temporal tables](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/temporal-tables):

```csharp
using EFCore.DynamicQuery.SqlServer.Querying;

var asOfLastMonth = dbContext.Set<Order>().UseHistory(dbContext, someDateTime); // null date -> current data
var everyVersionEver = dbContext.Set<Order>().UseAllHistory(dbContext);
```

Throws `EntityDoesNotHaveHistoricalDataTableException` if the entity isn't actually configured as temporal — fails fast instead of silently returning current-only data.

## What this library deliberately doesn't do

- It doesn't generate SQL or replace EF Core's LINQ provider — every query it builds is a normal `IQueryable<T>` EF Core translates itself.
- It doesn't let you define new entity types or relationships at runtime — every `TEntity`/`TModel` in a mapping profile is a real, compile-time CLR type known to your `DbContext` at startup.
- It's read/query-focused — it has nothing to do with writes, change tracking, or persistence.
