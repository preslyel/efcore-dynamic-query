using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Querying;
using Microsoft.EntityFrameworkCore;

// Deliberately in the Microsoft.Extensions.DependencyInjection namespace (not
// EFCore.DynamicQuery.DependencyInjection) - the same convention AutoMapper, MediatR, and
// Serilog use for their own IServiceCollection extensions, so AddDynamicMapper shows up
// wherever services.Add... is already being called, with no extra using required.
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="IDynamicMapper"/> that discovers every
    /// <see cref="MappingProfile"/> in the assemblies containing <paramref name="profileAssemblyMarkerTypes"/>.
    /// Pass any one type from each assembly you want scanned - typically one of your own
    /// profile classes, e.g. <c>services.AddDynamicMapper(typeof(AddressProfile))</c>.
    /// </summary>
    public static IServiceCollection AddDynamicMapper(this IServiceCollection services, params Type[] profileAssemblyMarkerTypes)
    {
        var assemblies = profileAssemblyMarkerTypes.Select(t => t.Assembly).Distinct().ToArray();

        services.AddSingleton<IDynamicMapper>(_ => new DynamicMapper(assemblies));

        return services;
    }

    /// <summary>
    /// Registers a singleton <see cref="FilterTypeRegistry"/> (pre-populated with the six
    /// built-in filters) and a matching <see cref="FilterJsonConverter"/>, optionally extended
    /// via <paramref name="configure"/> - e.g. <c>registry.Register&lt;StartsWithFilter&gt;("startsWith")</c>.
    /// Wire the converter into your own JSON pipeline yourself (this library takes no dependency
    /// on ASP.NET Core), e.g. <c>services.Configure&lt;JsonOptions&gt;(o =>
    /// o.JsonSerializerOptions.Converters.Add(sp.GetRequiredService&lt;FilterJsonConverter&gt;()))</c>.
    /// </summary>
    public static IServiceCollection AddDynamicQueryFilters(this IServiceCollection services, Action<FilterTypeRegistry>? configure = null)
    {
        var registry = new FilterTypeRegistry();
        configure?.Invoke(registry);

        services.AddSingleton(registry);
        services.AddSingleton(new FilterJsonConverter(registry));

        return services;
    }

    /// <summary>
    /// Registers a scoped <see cref="IDynamicQueryService"/> backed by a plain, untracked
    /// <see cref="DbContextQueryableProvider"/> over <typeparamref name="TContext"/> - so a
    /// controller/service can call <c>GetQueryDataResultAsync&lt;SomeModel&gt;(filter, ct)</c>
    /// for any model with a registered <see cref="MappingProfile"/>, without a bespoke
    /// repository or service per model. Requires <typeparamref name="TContext"/> and
    /// <see cref="IDynamicMapper"/> (<see cref="AddDynamicMapper"/>) to already be registered.
    /// Register your own <see cref="IQueryableProvider"/> instead if you need scoping beyond a
    /// plain DbSet query (soft-delete, multi-tenancy, etc.).
    /// </summary>
    public static IServiceCollection AddDynamicQueryService<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IQueryableProvider>(sp => new DbContextQueryableProvider(sp.GetRequiredService<TContext>()));
        services.AddScoped<IDynamicQueryService, DynamicQueryService>();

        return services;
    }
}
