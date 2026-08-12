using Core.Mappings;
using FluentAssertions;

namespace API.Tests.Mappings;

public class SysmondActAddressResponseParserTests
{
    private static readonly Guid ActId = Guid.Parse("1d15a962-3a17-f783-f15d-3a22e2d1b4b5");
    private static readonly Guid AddressId = Guid.Parse("8849b16c-cb0b-e039-fe00-3a22008d01af");

    [Fact]
    public void Parse_WhenSuccessAndDataArray_ReturnsAddresses()
    {
        var body = $$"""
            {
              "success": true,
              "data": [
                {
                  "id": "{{AddressId}}",
                  "actId": "{{ActId}}",
                  "type": 10,
                  "street": "Atatürk Bulvarı No:123",
                  "countryId": 1,
                  "cityId": 6,
                  "isDisabled": false
                }
              ]
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Id.Should().Be(AddressId);
        items[0].ActId.Should().Be(ActId);
        items[0].Street.Should().Be("Atatürk Bulvarı No:123");
    }

    [Fact]
    public void Parse_WhenStatusAndDataArray_ReturnsAddresses()
    {
        var body = $$"""
            {
              "status": { "success": true, "message": null, "code": null },
              "data": [
                {
                  "id": "{{AddressId}}",
                  "actId": "{{ActId}}",
                  "type": 20,
                  "street": "çınarönü",
                  "countryId": 1,
                  "cityId": 16,
                  "districtOther": "yıldırım",
                  "isDisabled": false
                }
              ]
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Type.Should().Be(20);
        items[0].DistrictOther.Should().Be("yıldırım");
    }

    [Fact]
    public void Parse_WhenStatusAndItemsAtRoot_ReturnsAddresses()
    {
        var body = $$"""
            {
              "status": { "success": true },
              "totalCount": 1,
              "items": [
                {
                  "id": "{{AddressId}}",
                  "actId": "{{ActId}}",
                  "type": 10,
                  "street": "Deneme Cad.",
                  "countryId": 1
                }
              ]
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Street.Should().Be("Deneme Cad.");
    }

    [Fact]
    public void Parse_WhenDataObjectContainsItems_ReturnsAddresses()
    {
        var body = $$"""
            {
              "status": { "success": true },
              "data": {
                "totalCount": 1,
                "items": [
                  {
                    "id": "{{AddressId}}",
                    "actId": "{{ActId}}",
                    "type": 10,
                    "street": "Nested Items",
                    "countryId": 1
                  }
                ]
              }
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Street.Should().Be("Nested Items");
    }

    [Fact]
    public void Parse_WhenRootArray_ReturnsAddresses()
    {
        var body = $$"""
            [
              {
                "id": "{{AddressId}}",
                "actId": "{{ActId}}",
                "type": 10,
                "street": "Root Array",
                "countryId": 1
              }
            ]
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Street.Should().Be("Root Array");
    }

    [Fact]
    public void Parse_WhenSandboxEarsivResponse_ReturnsTwoAddresses()
    {
        var body = """
            {
              "status": { "success": true, "message": null, "code": null },
              "data": [
                {
                  "id": "8601d689-1e44-0ad3-65a6-3a22e2d1b4f4",
                  "actId": "1d15a962-3a17-f783-f15d-3a22e2d1b4b5",
                  "type": 10,
                  "street": "test",
                  "countryId": 1,
                  "countryName": "Turkey",
                  "cityId": 3,
                  "cityName": "Afyonkarahisar",
                  "districtName": "",
                  "districtOther": "test",
                  "postalZone": "00000",
                  "isDisabled": false
                },
                {
                  "id": "78c26ce3-b049-94ff-b469-3a22e2d1b5d0",
                  "actId": "1d15a962-3a17-f783-f15d-3a22e2d1b4b5",
                  "type": 30,
                  "street": "test",
                  "countryId": 1,
                  "cityId": 3,
                  "districtOther": "test",
                  "postalZone": "00000",
                  "isDisabled": false
                }
              ]
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(2);
        items[0].Street.Should().Be("test");
        items[0].DistrictOther.Should().Be("test");
        items[0].PostalZone.Should().Be("00000");
        items[0].CityName.Should().Be("Afyonkarahisar");
    }

    [Fact]
    public void Parse_WhenDataIsSingleAddressObject_ReturnsOne()
    {
        var body = $$"""
            {
              "status": { "success": true },
              "data": {
                "id": "{{AddressId}}",
                "actId": "{{ActId}}",
                "type": 10,
                "street": "Single Object",
                "countryId": 1
              }
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Street.Should().Be("Single Object");
    }

    [Fact]
    public void Parse_WhenTypeIsStringEnumName_ReturnsAddresses()
    {
        var body = $$"""
            {
              "success": true,
              "data": [
                {
                  "id": "{{AddressId}}",
                  "actId": "{{ActId}}",
                  "type": "Delivery",
                  "street": "Enum Type",
                  "countryId": 1
                }
              ]
            }
            """;

        var items = SysmondActAddressResponseParser.Parse(body);

        items.Should().HaveCount(1);
        items[0].Type.Should().Be(20);
        items[0].Street.Should().Be("Enum Type");
    }
}
