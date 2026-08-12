using System.Text.Json;
using System.Text.Json.Serialization;
using Core.DTOs.Sysmond;

namespace Core.Mappings;

/// <summary>Sysmond act-address GET yanıtını parse eder (status/data/items sarmalayıcıları).</summary>
public static class SysmondActAddressResponseParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public static IReadOnlyList<SysmondActAddressDto> Parse(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<SysmondActAddressDto>();

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var fromArrays = CollectFromArrays(root);
            if (fromArrays.Count > 0)
                return fromArrays;

            var fromSingles = CollectSingleObjects(root);
            if (fromSingles.Count > 0)
                return fromSingles;

            var fallback = JsonSerializer.Deserialize<SysmondActAddressListResult>(body, JsonOptions);
            var resolved = fallback?.ResolveItems() ?? Array.Empty<SysmondActAddressDto>();
            if (resolved.Count > 0)
                return resolved;
        }
        catch (JsonException)
        {
            return Array.Empty<SysmondActAddressDto>();
        }

        return Array.Empty<SysmondActAddressDto>();
    }

    private static List<SysmondActAddressDto> CollectFromArrays(JsonElement root)
    {
        var list = new List<SysmondActAddressDto>();
        foreach (var array in FindAddressArrays(root))
            list.AddRange(DeserializeArray(array));

        return Deduplicate(list);
    }

    private static List<SysmondActAddressDto> CollectSingleObjects(JsonElement root)
    {
        var list = new List<SysmondActAddressDto>();
        foreach (var obj in FindAddressObjects(root))
        {
            var dto = DeserializeElement(obj);
            if (dto is not null && dto.Id != Guid.Empty)
                list.Add(dto);
        }

        return Deduplicate(list);
    }

    private static List<SysmondActAddressDto> Deduplicate(IReadOnlyList<SysmondActAddressDto> list)
    {
        if (list.Count <= 1)
            return list.ToList();

        return list
            .GroupBy(a => a.Id)
            .Select(g => g.First())
            .ToList();
    }

    private static IEnumerable<JsonElement> FindAddressArrays(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            yield return element;
            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var name in new[] { "data", "items", "result", "value" })
        {
            if (!TryGetProperty(element, name, out var prop))
                continue;

            switch (prop.ValueKind)
            {
                case JsonValueKind.Array:
                    yield return prop;
                    break;
                case JsonValueKind.Object when !LooksLikeAddressDto(prop):
                    foreach (var nested in FindAddressArrays(prop))
                        yield return nested;
                    break;
            }
        }
    }

    private static IEnumerable<JsonElement> FindAddressObjects(JsonElement element)
    {
        if (LooksLikeAddressDto(element))
        {
            yield return element;
            yield break;
        }

        if (element.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var name in new[] { "data", "items", "result", "value" })
        {
            if (!TryGetProperty(element, name, out var prop) || prop.ValueKind != JsonValueKind.Object)
                continue;

            if (LooksLikeAddressDto(prop))
                yield return prop;
            else
            {
                foreach (var nested in FindAddressObjects(prop))
                    yield return nested;
            }
        }
    }

    private static bool LooksLikeAddressDto(JsonElement obj)
    {
        if (obj.ValueKind != JsonValueKind.Object)
            return false;

        if (!TryGetGuid(obj, "id", out var id) || id == Guid.Empty)
            return false;

        return TryGetProperty(obj, "actId", out _)
               || TryGetProperty(obj, "street", out _)
               || TryGetProperty(obj, "countryId", out _);
    }

    private static IReadOnlyList<SysmondActAddressDto> DeserializeArray(JsonElement array)
    {
        if (array.ValueKind != JsonValueKind.Array)
            return Array.Empty<SysmondActAddressDto>();

        var list = new List<SysmondActAddressDto>();
        foreach (var element in array.EnumerateArray())
        {
            var dto = DeserializeElement(element);
            if (dto is not null && dto.Id != Guid.Empty)
                list.Add(dto);
        }

        return list;
    }

    private static SysmondActAddressDto? DeserializeElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        try
        {
            return JsonSerializer.Deserialize<SysmondActAddressDto>(element.GetRawText(), JsonOptions);
        }
        catch (JsonException)
        {
            return MapElementManually(element);
        }
    }

    private static SysmondActAddressDto? MapElementManually(JsonElement element)
    {
        if (!TryGetGuid(element, "id", out var id) || id == Guid.Empty)
            return null;

        TryGetGuid(element, "actId", out var actId);

        return new SysmondActAddressDto
        {
            Id = id,
            ActId = actId,
            Type = ReadAddressType(element),
            CountryId = ReadInt(element, "countryId") ?? 1,
            CityId = ReadInt(element, "cityId"),
            CityOther = ReadString(element, "cityOther"),
            DistrictId = ReadInt(element, "districtId"),
            DistrictOther = ReadString(element, "districtOther"),
            Street = ReadString(element, "street"),
            BuildingNumber = ReadString(element, "buildingNumber"),
            BuildingName = ReadString(element, "buildingName"),
            Room = ReadString(element, "room"),
            Floor = ReadString(element, "floor"),
            PostalZone = ReadString(element, "postalZone"),
            Note = ReadString(element, "note"),
            CountryName = ReadString(element, "countryName"),
            CityName = ReadString(element, "cityName"),
            DistrictName = ReadString(element, "districtName"),
            IsDisabled = ReadBool(element, "isDisabled"),
            ContactInfo = ReadContactInfo(element)
        };
    }

    private static SysmondActAddressContactDto? ReadContactInfo(JsonElement element)
    {
        if (!TryGetProperty(element, "contactInfo", out var contact) || contact.ValueKind != JsonValueKind.Object)
            return null;

        return new SysmondActAddressContactDto
        {
            FirstName = ReadString(contact, "firstName"),
            LastName = ReadString(contact, "lastName"),
            Email = ReadString(contact, "email"),
            MainPhone = ReadString(contact, "mainPhone"),
            MainCellPhone = ReadString(contact, "mainCellPhone")
        };
    }

    private static int ReadAddressType(JsonElement element)
    {
        if (!TryGetProperty(element, "type", out var typeEl))
            return 0;

        return typeEl.ValueKind switch
        {
            JsonValueKind.Number when typeEl.TryGetInt32(out var n) => n,
            JsonValueKind.String => typeEl.GetString() switch
            {
                "10" or "Invoice" => 10,
                "20" or "Delivery" => 20,
                "30" or "Contact" => 30,
                _ when int.TryParse(typeEl.GetString(), out var parsed) => parsed,
                _ => 0
            },
            _ => 0
        };
    }

    private static bool ReadBool(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => bool.TryParse(value.GetString(), out var parsed) && parsed,
            _ => false
        };
    }

    private static int? ReadInt(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n))
            return n;

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
            return parsed;

        return null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;

        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static bool TryGetGuid(JsonElement element, string name, out Guid value)
    {
        value = Guid.Empty;
        if (!TryGetProperty(element, name, out var prop))
            return false;

        if (prop.ValueKind == JsonValueKind.String)
            return Guid.TryParse(prop.GetString(), out value);

        return false;
    }

    private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.TryGetProperty(name, out value))
            return true;

        foreach (var prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
