using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Mapping;

public class TypeMapTests
{
    private static readonly Address Address = new() { AddressId = 42, Zip = "90210", City = "Beverly Hills", Country = "USA" };
    private static readonly AddressModel Model = new() { Id = 42, ZipCode = "90210", City = "Beverly Hills" };

    private readonly AddressProfile profile = new();

    private TypeMap<Address, AddressModel> Forward => profile.Maps.OfType<TypeMap<Address, AddressModel>>().Single();
    private TypeMap<AddressModel, Address> Reverse => profile.Maps.OfType<TypeMap<AddressModel, Address>>().Single();

    [Fact]
    public void CreateMap_registers_exactly_the_forward_map_before_ReverseMap_is_called()
    {
        var soloProfile = new SoloForwardProfile();

        var map = Assert.Single(soloProfile.Maps);
        Assert.Equal(typeof(Address), map.SourceType);
        Assert.Equal(typeof(AddressModel), map.DestinationType);
    }

    [Fact]
    public void ReverseMap_registers_a_second_map_with_source_and_destination_swapped()
    {
        Assert.Equal(2, profile.Maps.Count());
        Assert.Equal(typeof(AddressModel), Reverse.SourceType);
        Assert.Equal(typeof(Address), Reverse.DestinationType);
    }

    [Theory]
    [InlineData("Id", 42)]
    [InlineData("ZipCode", "90210")]
    [InlineData("City", "Beverly Hills")]
    public void Forward_map_resolves_each_destination_property_from_the_source_instance(string destinationProperty, object expected)
    {
        var propertyBody = Forward.GetSourceProperty(destinationProperty);
        Assert.NotNull(propertyBody);

        var actual = ExpressionTestHelpers.Evaluate(propertyBody!, Address);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("AddressId", 42)]
    [InlineData("Zip", "90210")]
    [InlineData("City", "Beverly Hills")]
    public void ReverseMap_inherits_every_ForMember_rename_in_the_opposite_direction(string destinationProperty, object expected)
    {
        var propertyBody = Reverse.GetSourceProperty(destinationProperty);
        Assert.NotNull(propertyBody);

        var actual = ExpressionTestHelpers.Evaluate(propertyBody!, Model);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetRequiredFields_reflects_IsKey_set_via_ForMember()
    {
        Assert.Equal(["Id"], Forward.GetRequiredFields());
    }

    [Fact]
    public void ReverseMap_does_not_carry_over_IsKey_or_any_other_ForMember_config()
    {
        // Deliberate scope boundary: ReverseMap only inherits the structural rename
        // (which destination property reads from which source property), never the
        // config callback - see TypeMap.ReverseMap's own remarks.
        Assert.Empty(Reverse.GetRequiredFields());
    }

    [Fact]
    public void Unmapped_destination_property_returns_null()
    {
        // AddressModel has no property that would map to Address.Country, so the
        // reverse map (Address as destination) has no registration to read it from.
        Assert.Null(Reverse.GetPropertyMapping("Country"));
    }

    [Theory]
    [InlineData("zipCode")]
    [InlineData("ZIPCODE")]
    [InlineData("ZipCode")]
    public void GetPropertyMapping_resolves_regardless_of_casing(string requestedName)
    {
        // A JSON request typically names fields in camelCase; the mapped destination property
        // is PascalCase C#. Lookups must not require the caller to pre-normalize casing.
        Assert.NotNull(Forward.GetPropertyMapping(requestedName));
        Assert.Equal("ZipCode", Forward.GetPropertyMapping(requestedName)!.PropertyName);
    }

    private sealed class SoloForwardProfile : MappingProfile
    {
        public SoloForwardProfile()
        {
            CreateMap<Address, AddressModel>();
        }
    }
}
