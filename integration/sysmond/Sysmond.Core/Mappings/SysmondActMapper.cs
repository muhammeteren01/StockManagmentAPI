using System.Security.Cryptography;
using System.Text;
using Integration.Sysmond.Core.DTOs;
using Core.Entities;

namespace Integration.Sysmond.Core.Mappings;

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

    private static readonly Guid SyntheticAddressNamespace =
        Guid.Parse("a3b8c2d1-5e6f-4789-a012-3c4d5e6f7890");

    private static Guid CreateSyntheticAddressId(Guid actId)
    {
        var input = Encoding.UTF8.GetBytes($"{SyntheticAddressNamespace:D}:{actId:D}");
        var hash = MD5.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        return new Guid(hash);
    }

    /// <summary>act-address boşsa act-query by-id actFullAddress alanından sentetik adres.</summary>
    public static SysmondActAddressDto? CreateFallbackAddressFromAct(SysmondActDto act)
    {
        if (act.Id == Guid.Empty)
            return null;

        if (string.IsNullOrWhiteSpace(act.ActFullAddress))
            return null;

        return new SysmondActAddressDto
        {
            Id = CreateSyntheticAddressId(act.Id),
            ActId = act.Id,
            Type = 10,
            CountryId = act.CountryId ?? 1,
            CityId = act.CityId,
            CityOther = FirstNonEmpty(act.CityOther, act.CityName),
            Street = act.ActFullAddress,
            CountryName = act.CountryName,
            CityName = act.CityName,
            IsDisabled = false
        };
    }

    /// <summary>Yerel Act + update isteği → Sysmond ActUpdateDto.</summary>
    public static SysmondActUpdateDto ToActUpdateDto(
        Act entity,
        SysmondUpdateActRequest request,
        Guid sysmondCompanyId,
        Guid sysmondActId)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(request);

        return new SysmondActUpdateDto
        {
            Id = sysmondActId,
            CompanyId = sysmondCompanyId,
            Type = request.Type ?? entity.Type,
            Name = CoalesceTrim(request.Name, entity.Name),
            Surname = CoalesceTrim(request.Surname, entity.Surname),
            Title = CoalesceTrim(request.Title, entity.Title),
            ActCode = CoalesceTrim(request.ActCode, entity.ActCode),
            VknTckn = CoalesceTrim(request.VknTckn, entity.VknTckn),
            TaxOfficeName = CoalesceTrim(request.TaxOfficeName, entity.TaxOfficeName),
            ActFullAddress = CoalesceTrim(request.ActFullAddress, entity.ActFullAddress),
            CountryId = request.CountryId ?? entity.CountryId,
            CityId = request.CityId ?? entity.CityId,
            CityOther = CoalesceTrim(request.CityOther, entity.CityOther),
            MainCurrencyId = request.MainCurrencyId,
            IsCommunityCompany = request.IsCommunityCompany ?? false,
            IsAbroadCustomer = request.IsAbroadCustomer ?? entity.IsAbroadCustomer,
            Scenario = request.Scenario ?? entity.Scenario,
            IsDisabled = request.IsDisabled ?? entity.IsDisabled
        };
    }

    /// <summary>Create isteği → Sysmond ActCreateDto.</summary>
    public static SysmondActCreateDto ToActCreateDto(SysmondCreateActRequest request, Guid companyId) =>
        new()
        {
            Type = request.Type <= 0 ? 20 : request.Type,
            CompanyId = companyId,
            Name = request.Name?.Trim(),
            ActCode = request.ActCode?.Trim(),
            VknTckn = request.VknTckn?.Trim(),
            CountryId = request.CountryId ?? 1,
            MainCurrencyId = request.MainCurrencyId
        };

    /// <summary>Create sonrası yerel Act entity.</summary>
    public static Act ToNewActFromCreate(
        SysmondCreateActRequest request,
        Guid localCompanyId,
        Guid sysmondActId)
    {
        var syncedAt = DateTime.UtcNow;
        return new Act
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            ExternalSysmondId = sysmondActId,
            Type = request.Type <= 0 ? 20 : request.Type,
            Name = Truncate(request.Name, 255),
            Surname = Truncate(request.Surname, 255),
            Title = Truncate(request.Title, 255),
            ActCode = Truncate(request.ActCode, 100),
            VknTckn = Truncate(request.VknTckn, 20),
            CountryId = request.CountryId ?? 1,
            Scenario = 30,
            SyncedAt = syncedAt
        };
    }

    /// <summary>Update isteğini yerel Act'a uygular.</summary>
    public static void ApplyUpdateFromRequest(Act entity, SysmondUpdateActRequest request)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Type is int type)
            entity.Type = type;
        if (!string.IsNullOrWhiteSpace(request.Name))
            entity.Name = Truncate(request.Name, 255);
        if (request.Surname is not null)
            entity.Surname = Truncate(request.Surname, 255);
        if (request.Title is not null)
            entity.Title = Truncate(request.Title, 255);
        if (request.ActCode is not null)
            entity.ActCode = Truncate(request.ActCode, 100);
        if (request.VknTckn is not null)
            entity.VknTckn = Truncate(request.VknTckn, 20);
        if (request.TaxOfficeName is not null)
            entity.TaxOfficeName = Truncate(request.TaxOfficeName, 150);
        if (request.ActFullAddress is not null)
            entity.ActFullAddress = Truncate(request.ActFullAddress, 500);
        if (request.CountryId is not null)
            entity.CountryId = request.CountryId;
        if (request.CityId is not null)
            entity.CityId = request.CityId;
        if (request.CityOther is not null)
            entity.CityOther = Truncate(request.CityOther, 100);
        if (request.Scenario is int scenario)
            entity.Scenario = scenario;
        if (request.IsDisabled is bool disabled)
            entity.IsDisabled = disabled;
        if (request.IsAbroadCustomer is bool abroad)
            entity.IsAbroadCustomer = abroad;
        entity.SyncedAt = DateTime.UtcNow;
    }

    public static SysmondActResponse ToResponse(Act entity) => new()
    {
        Id = entity.Id,
        ExternalSysmondId = entity.ExternalSysmondId,
        Type = entity.Type,
        Name = entity.Name,
        Surname = entity.Surname,
        Title = entity.Title,
        ActCode = entity.ActCode,
        VknTckn = entity.VknTckn,
        TaxOfficeName = entity.TaxOfficeName,
        ActFullAddress = entity.ActFullAddress,
        CountryId = entity.CountryId,
        CityId = entity.CityId,
        CityOther = entity.CityOther,
        Scenario = entity.Scenario,
        IsDisabled = entity.IsDisabled,
        IsAbroadCustomer = entity.IsAbroadCustomer,
        SyncedAt = entity.SyncedAt
    };

    private static string? CoalesceTrim(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
            return preferred.Trim();
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
    }

    public static void EnrichFromDetail(SysmondActDto target, SysmondActDto detail)
    {
        if (string.IsNullOrWhiteSpace(target.ActFullAddress) && !string.IsNullOrWhiteSpace(detail.ActFullAddress))
            target.ActFullAddress = detail.ActFullAddress;
        if (target.CityId is null && detail.CityId is not null)
            target.CityId = detail.CityId;
        if (string.IsNullOrWhiteSpace(target.CityOther) && !string.IsNullOrWhiteSpace(detail.CityOther))
            target.CityOther = detail.CityOther;
        if (string.IsNullOrWhiteSpace(target.CityName) && !string.IsNullOrWhiteSpace(detail.CityName))
            target.CityName = detail.CityName;
        if (string.IsNullOrWhiteSpace(target.CountryName) && !string.IsNullOrWhiteSpace(detail.CountryName))
            target.CountryName = detail.CountryName;
        if (target.CountryId is null && detail.CountryId is not null)
            target.CountryId = detail.CountryId;
        target.CanAccessAddressAndContactInfo = detail.CanAccessAddressAndContactInfo;
    }
}
