namespace Integration.Sysmond.Core.DTOs.Company;

/// <summary>Sysmond <c>CompanyDocNoTemplateDto</c> (şablon listesi).</summary>
public class SysmondCompanyDocNoTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }

    /// <summary>DocNoTemplateTypes: 10 PaperInvoice … 40 EDespatch, 50 PaperDespatch.</summary>
    public int Type { get; set; }

    public string? TypeName { get; set; }
    public string? Name { get; set; }
    public string? DocNoPrefix { get; set; }
    public bool IsDefault { get; set; }
    public int DocNoYear { get; set; }
    public int DocNoCounter { get; set; }
}

public class SysmondCompanyDocNoTemplatePagedResult
{
    public IReadOnlyList<SysmondCompanyDocNoTemplateDto>? Items { get; set; }
    public long TotalCount { get; set; }
    public object? Status { get; set; }
}

/// <summary>act-query despatch-scenarios-by-act-id satırı.</summary>
public class SysmondDespatchScenarioTypeMapDto
{
    public SysmondIdNameIntDto? Scenario { get; set; }
    public IReadOnlyList<SysmondIdNameIntDto>? Types { get; set; }
}

public class SysmondIdNameIntDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class SysmondDespatchScenarioListResult
{
    public IReadOnlyList<SysmondDespatchScenarioTypeMapDto>? Data { get; set; }
    public object? Status { get; set; }
}
