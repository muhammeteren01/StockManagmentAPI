using Core.DTOs.Acts;
using Core.Entities;

namespace Core.Mappings;

/// <summary>Act entity ↔ DTO.</summary>
public static class ActMapper
{
    public static ActResponse ToResponse(Act entity) => new()
    {
        Id = entity.Id,
        CompanyId = entity.CompanyId,
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

    public static Act ToEntity(CreateActRequest request, Guid companyId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        ExternalSysmondId = Guid.Empty,
        Type = request.Type <= 0 ? 20 : request.Type,
        Name = Truncate(request.Name, 255),
        Surname = Truncate(request.Surname, 255),
        Title = Truncate(request.Title, 255),
        ActCode = Truncate(request.ActCode, 100),
        VknTckn = Truncate(request.VknTckn, 20),
        CountryId = request.CountryId ?? 1,
        Scenario = 30,
        SyncedAt = DateTime.UtcNow
    };

    public static void ApplyUpdate(Act entity, UpdateActRequest request)
    {
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

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
