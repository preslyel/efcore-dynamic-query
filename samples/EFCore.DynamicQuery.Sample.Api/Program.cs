using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Sample.Api.Data;
using EFCore.DynamicQuery.Sample.Api.Mapping;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ProjectForgeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ProjectForge")));

// Full DI wiring for the library:
builder.Services.AddDynamicMapper(typeof(ProjectForgeMappingProfile));
builder.Services.AddDynamicQueryFilters();
builder.Services.AddDynamicQueryService<ProjectForgeDbContext>();

builder.Services.AddControllers();

// FilterJsonConverter needs the same FilterTypeRegistry AddDynamicQueryFilters registered
// above - resolved from the real container when MVC's JsonOptions are actually built, rather
// than calling BuildServiceProvider() here to get it early.
builder.Services.AddOptions<JsonOptions>()
    .Configure<FilterTypeRegistry>((options, registry) =>
        options.JsonSerializerOptions.Converters.Add(new FilterJsonConverter(registry)));

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
