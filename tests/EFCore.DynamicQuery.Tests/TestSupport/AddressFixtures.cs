using EFCore.DynamicQuery.Mapping;

namespace EFCore.DynamicQuery.Tests.TestSupport;

public class Address
{
    public int AddressId { get; set; }
    public string Zip { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

public class AddressModel
{
    public int Id { get; set; }
    public string ZipCode { get; set; } = "";
    public string City { get; set; } = "";
    // Deliberately no Country - exercises the "no mapping for this destination property" path.
}

public class AddressProfile : MappingProfile
{
    public AddressProfile()
    {
        CreateMap<Address, AddressModel>()
            .ForMember(dest => dest.Id, src => src.AddressId, cfg => cfg.IsKey = true)
            .ForMember(dest => dest.ZipCode, src => src.Zip)
            .ReverseMap();
    }
}
