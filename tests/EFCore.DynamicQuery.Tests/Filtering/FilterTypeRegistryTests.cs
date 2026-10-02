using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering;

public class FilterTypeRegistryTests
{
    [Theory]
    [InlineData("equal", typeof(EqualFilter))]
    [InlineData("notEqual", typeof(NotEqualFilter))]
    [InlineData("like", typeof(LikeFilter))]
    [InlineData("in", typeof(InFilter))]
    [InlineData("range", typeof(RangeFilter))]
    [InlineData("namedEqual", typeof(NamedEqualFilter))]
    public void Comes_pre_populated_with_the_six_built_in_filters(string key, Type expectedType)
    {
        var registry = new FilterTypeRegistry();

        Assert.Equal(expectedType, registry.Resolve(key));
    }

    [Fact]
    public void Key_lookup_is_case_insensitive()
    {
        var registry = new FilterTypeRegistry();

        Assert.Equal(typeof(EqualFilter), registry.Resolve("EQUAL"));
    }

    [Fact]
    public void Resolve_throws_for_an_unregistered_key()
    {
        var registry = new FilterTypeRegistry();

        Assert.Throws<UnknownFilterKeyException>(() => registry.Resolve("startsWith"));
    }

    [Fact]
    public void A_consumer_can_register_a_brand_new_custom_filter()
    {
        var registry = new FilterTypeRegistry();

        registry.Register<StartsWithFilter>("startsWith");

        Assert.Equal(typeof(StartsWithFilter), registry.Resolve("startsWith"));
    }

    [Fact]
    public void Registering_an_existing_key_again_replaces_it()
    {
        var registry = new FilterTypeRegistry();

        registry.Register<StartsWithFilter>("equal");

        Assert.Equal(typeof(StartsWithFilter), registry.Resolve("equal"));
    }
}
