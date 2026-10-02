using System.Text.Json;
using EFCore.DynamicQuery.Filtering;
using EFCore.DynamicQuery.Filtering.Filters;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Filtering;

public class FilterJsonConverterTests
{
    private static JsonSerializerOptions OptionsWith(FilterTypeRegistry registry) => new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FilterJsonConverter(registry) }
    };

    [Fact]
    public void Deserializes_an_equal_filter_by_its_filter_key()
    {
        var options = OptionsWith(new FilterTypeRegistry());
        var json = """[{"filter":"equal","name":"City","value":"NYC"}]""";

        var filters = JsonSerializer.Deserialize<IFilterItem[]>(json, options)!;

        var filter = Assert.IsType<EqualFilter>(Assert.Single(filters));
        Assert.Equal("City", filter.Name);
        Assert.Equal("NYC", ((JsonElement)filter.Value!).GetString());
    }

    [Fact]
    public void Deserializes_multiple_filters_of_different_kinds_in_one_array()
    {
        var options = OptionsWith(new FilterTypeRegistry());
        var json = """
        [
            {"filter":"equal","name":"City","value":"NYC"},
            {"filter":"range","name":"Quantity","fromValue":1,"toValue":10}
        ]
        """;

        var filters = JsonSerializer.Deserialize<IFilterItem[]>(json, options)!;

        Assert.IsType<EqualFilter>(filters[0]);
        Assert.IsType<RangeFilter>(filters[1]);
    }

    [Fact]
    public void Throws_for_an_unregistered_filter_key()
    {
        var options = OptionsWith(new FilterTypeRegistry());
        var json = """[{"filter":"startsWith","name":"City","value":"NY"}]""";

        Assert.Throws<EFCore.DynamicQuery.Exceptions.UnknownFilterKeyException>(
            () => JsonSerializer.Deserialize<IFilterItem[]>(json, options));
    }

    [Fact]
    public void Resolves_a_custom_filter_registered_by_the_consumer()
    {
        var registry = new FilterTypeRegistry();
        registry.Register<StartsWithFilter>("startsWith");
        var options = OptionsWith(registry);

        var json = """[{"filter":"startsWith","name":"City","value":"NY"}]""";
        var filters = JsonSerializer.Deserialize<IFilterItem[]>(json, options)!;

        var filter = Assert.IsType<StartsWithFilter>(Assert.Single(filters));
        Assert.Equal("City", filter.Name);
        Assert.Equal("NY", filter.Value);
    }

    [Fact]
    public void Round_trips_through_write_then_read()
    {
        var options = OptionsWith(new FilterTypeRegistry());
        IFilterItem[] original = [new EqualFilter { Name = "City", Value = JsonSerializer.SerializeToElement("NYC") }];

        var json = JsonSerializer.Serialize(original, options);
        var roundTripped = JsonSerializer.Deserialize<IFilterItem[]>(json, options)!;

        var filter = Assert.IsType<EqualFilter>(Assert.Single(roundTripped));
        Assert.Equal("City", filter.Name);
        Assert.Equal("NYC", ((JsonElement)filter.Value!).GetString());
    }
}
