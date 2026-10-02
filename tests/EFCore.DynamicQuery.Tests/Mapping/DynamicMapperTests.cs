using System.Reflection;
using EFCore.DynamicQuery.Exceptions;
using EFCore.DynamicQuery.Mapping;
using EFCore.DynamicQuery.Tests.TestSupport;

namespace EFCore.DynamicQuery.Tests.Mapping;

public class DynamicMapperTests
{
    private readonly DynamicMapper mapper = new([Assembly.GetExecutingAssembly()]);

    [Fact]
    public void GetTypeMap_resolves_a_registered_pair()
    {
        var typeMap = mapper.GetTypeMap(typeof(Address), typeof(AddressModel));

        Assert.Equal(typeof(Address), typeMap.SourceType);
        Assert.Equal(typeof(AddressModel), typeMap.DestinationType);
    }

    [Fact]
    public void GetTypeMap_throws_for_an_unregistered_pair()
    {
        Assert.Throws<TypeMapNotFoundException>(() => mapper.GetTypeMap(typeof(string), typeof(int)));
    }

    [Fact]
    public void GetSourceType_resolves_the_unique_source_for_a_destination()
    {
        Assert.Equal(typeof(Address), mapper.GetSourceType<AddressModel>());
    }

    [Fact]
    public void GetSourceType_throws_for_an_unmapped_destination()
    {
        Assert.Throws<TypeMapNotFoundException>(() => mapper.GetSourceType<Guid>());
    }

    [Fact]
    public void Map_populates_every_mapped_property_when_no_headers_are_given()
    {
        var address = new Address { AddressId = 42, Zip = "90210", City = "Beverly Hills" };

        var model = mapper.Map<Address, AddressModel>(address);

        Assert.Equal(42, model.Id);
        Assert.Equal("90210", model.ZipCode);
        Assert.Equal("Beverly Hills", model.City);
    }

    [Fact]
    public void Map_with_headers_populates_only_the_requested_properties()
    {
        var address = new Address { AddressId = 42, Zip = "90210", City = "Beverly Hills" };

        var model = mapper.Map<Address, AddressModel>([address], ["ZipCode"]).Single();

        Assert.Equal("90210", model.ZipCode);
        Assert.Equal(0, model.Id);
        Assert.Equal("", model.City);
    }

    [Fact]
    public void Map_resolves_headers_case_insensitively()
    {
        var address = new Address { AddressId = 42, Zip = "90210", City = "Beverly Hills" };

        var model = mapper.Map<Address, AddressModel>([address], ["zipCode", "CITY"]).Single();

        Assert.Equal("90210", model.ZipCode);
        Assert.Equal("Beverly Hills", model.City);
    }

    [Fact]
    public void Map_does_not_throw_when_two_headers_resolve_to_the_same_property()
    {
        // "ZipCode" and "zipcode" both resolve to the same destination member - MemberInit
        // would throw if it were bound twice, so DynamicMapper must de-duplicate by the
        // resolved member, not by the raw header string.
        var address = new Address { AddressId = 42, Zip = "90210", City = "Beverly Hills" };

        var model = mapper.Map<Address, AddressModel>([address], ["ZipCode", "zipcode", "ZIPCODE"]).Single();

        Assert.Equal("90210", model.ZipCode);
    }

    [Fact]
    public void Non_generic_Map_matches_the_generic_overload()
    {
        var address = new Address { AddressId = 42, Zip = "90210", City = "Beverly Hills" };

        var result = (AddressModel)mapper.Map(address, typeof(Address), typeof(AddressModel));

        Assert.Equal(42, result.Id);
        Assert.Equal("90210", result.ZipCode);
    }

    [Fact]
    public void Copy_takes_mapped_properties_from_source_and_preserves_unmapped_ones_from_existing()
    {
        var model = new AddressModel { Id = 42, ZipCode = "10001", City = "New York" };
        var existingAddress = new Address { AddressId = 999, Zip = "00000", City = "Old City", Country = "Canada" };

        var copied = mapper.Copy<AddressModel, Address>(model, existingAddress);

        Assert.Equal(42, copied.AddressId);
        Assert.Equal("10001", copied.Zip);
        Assert.Equal("New York", copied.City);
        // Country has no counterpart on AddressModel - must fall through to existingAddress's value.
        Assert.Equal("Canada", copied.Country);
    }

    [Fact]
    public void Non_generic_Copy_matches_the_generic_overload()
    {
        var model = new AddressModel { Id = 42, ZipCode = "10001", City = "New York" };
        var existingAddress = new Address { AddressId = 999, Zip = "00000", City = "Old City", Country = "Canada" };

        var result = (Address)mapper.Copy(model, existingAddress, typeof(AddressModel), typeof(Address));

        Assert.Equal(42, result.AddressId);
        Assert.Equal("Canada", result.Country);
    }
}
