using Core.DTOs.Sysmond;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Sysmond ActDto / ActAddressDto → yerel Act / ActAddress.</summary>
public static class SysmondActMapper
{
    public static Act ToNewAct(SysmondActDto remote, Guid localCompanyId, DateTime syncedAtUtc)
    {
        var entity = new Act
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            ExternalSysmondId = remote.Id,
            SyncedAt = syncedAtUtc
        };
        ApplyToAct(entity, remote, syncedAtUtc);
        return entity;
    }

    public static void ApplyToAct(Act entity, SysmondActDto remote, DateTime syncedAtUtc)
    {
        entity.ExternalSysmondId = remote.Id;
        entity.Type = remote.Type;
        entity.Name = Truncate(remote.Name, 255);
        entity.Surname = Truncate(remote.Surname, 255);
        entity.Title = Truncate(remote.Title ?? remote.LegalName, 255);
        entity.ActCode = Truncate(remote.ActCode, 100);
        entity.VknTckn = Truncate(remote.VknTckn, 20);
        entity.TaxOfficeName = Truncate(remote.TaxOfficeName, 150);
        entity.ActFullAddress = Truncate(remote.ActFullAddress, 500);
        entity.CountryId = remote.CountryId;
        entity.CityId = remote.CityId;
        entity.CityOther = Truncate(remote.CityOther, 100);
        entity.Scenario = remote.Scenario;
        entity.IsDisabled = remote.IsDisabled;
        entity.IsLocked = remote.IsLocked;
        entity.IsAbroadCustomer = remote.IsAbroadCustomer;
        entity.ParentActExternalId = remote.ParentActId;
        entity.SyncedAt = syncedAtUtc;
    }

    public static ActAddress ToNewAddress(
        SysmondActAddressDto remote,
        Guid localActId,
        Guid localCompanyId,
        DateTime syncedAtUtc)
    {
        var entity = new ActAddress
        {
            Id = Guid.NewGuid(),
            ActId = localActId,
            CompanyId = localCompanyId,
            ExternalSysmondId = remote.Id,
            SyncedAt = syncedAtUtc
        };
        ApplyToAddress(entity, remote, syncedAtUtc);
        return entity;
    }

    public static void ApplyToAddress(ActAddress entity, SysmondActAddressDto remote, DateTime syncedAtUtc)
    {
        entity.ExternalSysmondId = remote.Id;
        entity.Type = remote.Type;
        entity.CountryId = remote.CountryId <= 0 ? 1 : remote.CountryId;
        entity.CityId = remote.CityId;
        entity.CityOther = Truncate(remote.CityOther, 100);
        entity.DistrictId = remote.DistrictId;
        entity.DistrictOther = Truncate(remote.DistrictOther, 100);
        entity.Street = Truncate(remote.Street, 255);
        entity.BuildingNumber = Truncate(remote.BuildingNumber, 50);
        entity.BuildingName = Truncate(remote.BuildingName, 100);
        entity.Room = Truncate(remote.Room, 50);
        entity.Floor = Truncate(remote.Floor, 50);
        entity.PostalZone = Truncate(remote.PostalZone, 20);
        entity.Note = Truncate(remote.Note, 500);
        entity.CountryName = Truncate(remote.CountryName, 100);
        entity.CityName = Truncate(remote.CityName, 100);
        entity.DistrictName = Truncate(remote.DistrictName, 100);
        entity.IsDisabled = remote.IsDisabled;
        entity.ContactFirstName = Truncate(remote.ContactInfo?.FirstName, 100);
        entity.ContactLastName = Truncate(remote.ContactInfo?.LastName, 100);
        entity.ContactEmail = Truncate(remote.ContactInfo?.Email, 150);
        entity.ContactMainPhone = Truncate(remote.ContactInfo?.MainPhone, 50);
        entity.ContactMainCellPhone = Truncate(remote.ContactInfo?.MainCellPhone, 50);
        entity.SyncedAt = syncedAtUtc;
    }

    /// <summary>Act-address: 20 Delivery → 10 Invoice → ilk dolu kayıt.</summary>
    public static SysmondActAddressDto? SelectPreferredAddress(IReadOnlyList<SysmondActAddressDto> addresses)
    {
        if (addresses.Count == 0)
            return null;

        var enabled = addresses.Where(a => !a.IsDisabled).ToList();
        var pool = enabled.Count > 0 ? enabled : addresses.ToList();

        static bool HasLocation(SysmondActAddressDto a) =>
            a.CityId is not null
            || !string.IsNullOrWhiteSpace(a.CityOther)
            || !string.IsNullOrWhiteSpace(a.CityName)
            || !string.IsNullOrWhiteSpace(a.Street);

        return pool.FirstOrDefault(a => a.Type == 20 && HasLocation(a))
               ?? pool.FirstOrDefault(a => a.Type == 10 && HasLocation(a))
               ?? pool.FirstOrDefault(HasLocation)
               ?? pool.FirstOrDefault(a => a.Type == 20)
               ?? pool.FirstOrDefault(a => a.Type == 10)
               ?? pool[0];
    }

    public static SysmondActAddressDto FromEntity(ActAddress entity) =>
        new()
        {
            Id = entity.ExternalSysmondId,
            ActId = entity.Act?.ExternalSysmondId ?? Guid.Empty,
            Type = entity.Type,
            CountryId = entity.CountryId,
            CityId = entity.CityId,
            CityOther = entity.CityOther,
            DistrictId = entity.DistrictId,
            DistrictOther = entity.DistrictOther,
            Street = entity.Street,
            BuildingNumber = entity.BuildingNumber,
            BuildingName = entity.BuildingName,
            Room = entity.Room,
            Floor = entity.Floor,
            PostalZone = entity.PostalZone,
            Note = entity.Note,
            CountryName = entity.CountryName,
            CityName = entity.CityName,
            DistrictName = entity.DistrictName,
            IsDisabled = entity.IsDisabled,
            ContactInfo = string.IsNullOrWhiteSpace(entity.ContactFirstName)
                && string.IsNullOrWhiteSpace(entity.ContactLastName)
                && string.IsNullOrWhiteSpace(entity.ContactEmail)
                    ? null
                    : new SysmondActAddressContactDto
                    {
                        FirstName = entity.ContactFirstName,
                        LastName = entity.ContactLastName,
                        Email = entity.ContactEmail,
                        MainPhone = entity.ContactMainPhone,
                        MainCellPhone = entity.ContactMainCellPhone
                    }
        };

    public static SysmondDespatchDeliveryAddressCreateDto ToDeliveryAddressCreateDto(SysmondActAddressDto address)
    {
        var addressType = address.Type switch
        {
            10 => 10,
            20 => 20,
            _ => 30
        };

        SysmondContactInfoCreateDto? contact = null;
        if (address.ContactInfo is not null)
        {
            contact = new SysmondContactInfoCreateDto
            {
                FirstName = address.ContactInfo.FirstName,
                LastName = address.ContactInfo.LastName,
                Email = address.ContactInfo.Email,
                MainPhone = address.ContactInfo.MainPhone,
                MainCellPhone = address.ContactInfo.MainCellPhone
            };
        }

        return new SysmondDespatchDeliveryAddressCreateDto
        {
            Address = new SysmondAddressCreateDto
            {
                Type = addressType,
                CountryId = address.CountryId <= 0 ? 1 : address.CountryId,
                CityId = address.CityId,
                CityOther = FirstNonEmpty(address.CityOther, address.CityName),
                DistrictId = address.DistrictId,
                DistrictOther = FirstNonEmpty(address.DistrictOther, address.DistrictName),
                Street = address.Street,
                BuildingNumber = address.BuildingNumber,
                BuildingName = address.BuildingName,
                Room = address.Room,
                Floor = address.Floor,
                PostalZone = address.PostalZone,
                Note = address.Note,
                IsDisabled = address.IsDisabled
            },
            Contact = contact
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
