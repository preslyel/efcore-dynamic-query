using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.DynamicQuery.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddDynamicMapper_registers_a_working_singleton_IDynamicMapper()
    {
        var services = new ServiceCollection();
        services.AddDynamicMapper(typeof(AddressProfile));

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IDynamicMapper>();

        var model = mapper.Map<Address, AddressModel>(new Address { AddressId = 1, Zip = "12345", City = "Springfield" });

        Assert.Equal(1, model.Id);
        Assert.Equal("12345", model.ZipCode);
        Assert.Same(mapper, provider.GetRequiredService<IDynamicMapper>());
    }

    [Fact]
    public void AddDynamicMapper_deduplicates_marker_types_from_the_same_assembly()
    {
        var services = new ServiceCollection();

        // Both marker types live in this test assembly - should scan it exactly once,
        // not fail or double-register anything.
        services.AddDynamicMapper(typeof(AddressProfile), typeof(Address));

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IDynamicMapper>();

        Assert.Equal(typeof(Address), mapper.GetSourceType<AddressModel>());
    }

    [Fact]
    public void AddDynamicQueryFilters_registers_registry_and_converter_prepopulated_with_built_ins()
    {
        var services = new ServiceCollection();
        services.AddDynamicQueryFilters();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FilterTypeRegistry>();

        Assert.NotNull(provider.GetRequiredService<FilterJsonConverter>());
        Assert.Equal(typeof(EFCore.DynamicQuery.Filtering.Filters.EqualFilter), registry.Resolve("equal"));
    }

    [Fact]
    public void AddDynamicQueryFilters_configure_callback_can_register_custom_filters()
    {
        var services = new ServiceCollection();
        services.AddDynamicQueryFilters(registry => registry.Register<StartsWithFilter>("startsWith"));

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<FilterTypeRegistry>();

        Assert.Equal(typeof(StartsWithFilter), registry.Resolve("startsWith"));
    }
}
