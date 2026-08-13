using Core.DTOs.PurchaseOrders;
using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;
using Integration.Sysmond.Core.Mappings;
using Core.Mappings;
using Core.Validations;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

public partial class SysmondSyncService
{
    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> CreateIncomingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<ValidationFailure>();
        if (request.ActId == Guid.Empty)
            failures.Add(new ValidationFailure("actId", "actId zorunludur."));
        if (request.Items is null || request.Items.Count == 0)
            failures.Add(new ValidationFailure("items", "En az bir kalem zorunludur."));
        else
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (item.StockId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].stockId", "stockId zorunludur."));
                if (item.WarehouseId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].warehouseId", "warehouseId zorunludur."));
                if (item.MeasureUnitId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].measureUnitId", "measureUnitId zorunludur."));
                if (item.Quantity <= 0)
                    failures.Add(new ValidationFailure($"items[{i}].quantity", "quantity > 0 olmalı."));
            }
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        var items = request.Items!;

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        ValidateIncomingBuyerCompany(company);

        var users = await _userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var syncUser = users.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Şirkette User yok; belge UserId zorunlu (CompanyId={company.Id}).");

        var companyPeriodId = request.CompanyPeriodId;
        if (companyPeriodId is null || companyPeriodId == Guid.Empty)
        {
            var periods = await _inventoryQuery.GetMyCompanyPeriodsAsync(accessToken, cancellationToken);
            var period =
                periods.FirstOrDefault(p => p.CompanyId == company.Id && p.IsActive)
                ?? periods.FirstOrDefault(p => p.CompanyId == company.Id);

            if (period is null || period.Id == Guid.Empty)
                throw new InvalidOperationException(
                    $"Aktif CompanyPeriod bulunamadı (CompanyId={company.Id}).");

            companyPeriodId = period.Id;
        }

        var issueDate = ResolveDespatchDateTime(request.IssueDate);
        var actualDate = request.ActualDespatchDate is null
            ? issueDate
            : ResolveDespatchDateTime(request.ActualDespatchDate);
        var currencyId = request.CurrencyId is > 0 ? request.CurrencyId : 949;

        await EnsureDespatchItemStockPriceIdsAsync(
            accessToken, company.Id, items, preferPurchasePrice: true, cancellationToken);

        var carrierId = await ResolveIncomingCarrierIdAsync(
            accessToken, company.Id, request.CarrierId, cancellationToken);

        var deliveryAddress = await ResolveIncomingDeliveryAddressAsync(
            accessToken, company.Id, request, cancellationToken);
        var sellerParty = BuildIncomingSellerParty(request, deliveryAddress);
        var buyerParties = BuildIncomingBuyerParties(company, deliveryAddress);

        var draftBody = new SysmondIncomingDespatchCreateDto
        {
            CompanyPeriodId = companyPeriodId.Value,
            Scenario = request.Scenario <= 0 ? 30 : request.Scenario,
            Type = request.Type <= 0 ? 10 : request.Type,
            DocNo = request.DocNo,
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            CarrierId = carrierId,
            Description = request.Description,
            CurrencyId = currencyId,
            CurrencyExchangeRate = request.CurrencyExchangeRate <= 0 ? 1 : request.CurrencyExchangeRate,
            DeliveryAddressCreateDto = deliveryAddress,
            DespatchPartyCreateDtos = [sellerParty, .. buyerParties]
        };

        var despatchId = await _despatchCommand.CreateIncomingDraftAsync(accessToken, draftBody, cancellationToken);

        var createdItemIds = new List<Guid>();
        foreach (var item in items)
        {
            var itemId = await _despatchCommand.CreateIncomingItemAsync(
                accessToken,
                new SysmondDespatchItemCreateDto
                {
                    DespatchId = despatchId,
                    StockId = item.StockId,
                    WarehouseId = item.WarehouseId,
                    MeasureUnitId = item.MeasureUnitId,
                    StockPriceId = item.StockPriceId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VatPercent = item.VatPercent,
                    Name = item.Name,
                    Code = item.Code,
                    Description = item.Description
                },
                cancellationToken);
            createdItemIds.Add(itemId);
        }

        await _despatchCommand.SaveIncomingAsync(
            accessToken,
            new SysmondIncomingDespatchSaveDto
            {
                CompanyId = company.Id,
                DespatchId = despatchId,
                Recalculate = true
            },
            cancellationToken);

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            DocumentType = PurchaseOrderDocumentType.IncomingDespatch,
            Direction = DespatchDirection.Incoming,
            UserId = syncUser.Id,
            OrderNumber = BuildCreatedOrderNumber(request.DocNo, despatchId),
            Status = PurchaseOrderStatus.Saved,
            ExternalSysmondId = despatchId,
            ExternalSysmondCompanyPeriodId = companyPeriodId,
            ActName = Truncate(request.ActName, 255),
            ActVknTckn = Truncate(request.ActVknTckn, 20),
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            CreatedAt = DateTime.UtcNow,
            Items = new List<PurchaseOrderItem>()
        };
        SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, deliveryAddress);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var product = await _productRepository.GetByExternalSysmondIdAsync(item.StockId, cancellationToken);
            if (product is null || product.CompanyId != company.Id)
            {
                throw new InvalidOperationException(
                    $"Kalem Product bulunamadı (StockId={item.StockId}). Önce product sync yapın.");
            }

            Guid? warehouseId = null;
            var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(
                item.WarehouseId, cancellationToken);
            if (warehouse is not null && warehouse.CompanyId == company.Id)
                warehouseId = warehouse.Id;

            order.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                ProductId = product.Id,
                WarehouseId = warehouseId,
                ExternalSysmondId = createdItemIds[i],
                MeasureUnitId = item.MeasureUnitId,
                StockPriceId = item.StockPriceId,
                Name = Truncate(item.Name, 255),
                Code = Truncate(item.Code, 100),
                Quantity = SysmondDespatchMapper.MapQuantity(item.Quantity),
                UnitPrice = (decimal)item.UnitPrice,
                VatPercent = (decimal)item.VatPercent,
                ReceivedQuantity = 0
            });
        }

        SysmondDespatchPurchaseOrderMapper.RecalcTotal(order);
        await _purchaseOrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sysmond incoming despatch create OK: DespatchId={DespatchId}, LocalOrderId={OrderId}, Items={ItemCount}",
            despatchId,
            order.Id,
            order.Items.Count);

        return PurchaseOrderMapper.ToResponse(order);
    }

    /// <summary>
    /// Sysmond incoming draft <c>carrierId</c> zorunlu (50009).
    /// Types=40 → tüm cariler → lokal DB; yoksa sandbox taşıyıcı act oluşturulur.
    /// </summary>
    private async Task<Guid> ResolveIncomingCarrierIdAsync(
        string accessToken,
        Guid companyId,
        Guid? requestedCarrierId,
        CancellationToken cancellationToken)
    {
        var carriers = await ListIncomingCarrierCandidatesAsync(
            accessToken, companyId, cancellationToken);

        if (requestedCarrierId is Guid requested && requested != Guid.Empty)
        {
            var match = carriers.FirstOrDefault(c => c.Id == requested);
            if (match is not null)
                return match.Id;

            _logger.LogWarning(
                "Incoming carrierId şirket taşıyıcı listesinde yok; alternatif aranacak. Requested={Requested}",
                requested);
        }

        if (carriers.Count > 0)
        {
            var pick = carriers[0];
            _logger.LogInformation(
                "Incoming carrierId resolved: CarrierId={CarrierId}, Name={Name}",
                pick.Id,
                pick.Name ?? pick.Title);
            return pick.Id;
        }

        var suffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var createdId = await _actQuery.CreateActAsync(
            accessToken,
            new SysmondActCreateDto
            {
                Type = 40,
                CompanyId = companyId,
                Name = "Taşıyıcı",
                ActCode = $"TAS-{suffix}",
                VknTckn = "11111111111",
                CountryId = 1,
                MainCurrencyId = 949
            },
            cancellationToken);

        _logger.LogInformation(
            "Incoming carrier act auto-created: CarrierId={CarrierId}, CompanyId={CompanyId}",
            createdId,
            companyId);
        return createdId;
    }

    private async Task<List<SysmondActDto>> ListIncomingCarrierCandidatesAsync(
        string accessToken,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        static bool IsActiveCarrier(SysmondActDto c) =>
            c.Id != Guid.Empty && c.Type == 40 && !c.IsDisabled && !c.IsLocked;

        var typed = await _actQuery.GetAllActsAsync(accessToken, companyId, [40], cancellationToken);
        var active = typed.Where(IsActiveCarrier).ToList();
        if (active.Count > 0)
            return active;

        var local = await _actRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        var localCarriers = local
            .Where(a => a.Type == 40)
            .Select(a => new SysmondActDto
            {
                Id = a.ExternalSysmondId,
                CompanyId = a.CompanyId,
                Type = a.Type,
                Name = a.Name,
                Title = a.Title,
                VknTckn = a.VknTckn
            })
            .Where(c => c.Id != Guid.Empty)
            .ToList();
        if (localCarriers.Count > 0)
            return localCarriers;

        var all = await _actQuery.GetAllActsAsync(accessToken, companyId, types: null, cancellationToken);
        return all.Where(IsActiveCarrier).ToList();
    }

    /// <summary>
    /// Sysmond incoming draft teslimat adresi zorunlu (50001).
    /// Önce istek; yoksa act-address (Delivery/Invoice); sonra lokal sync; son çare istek party alanları.
    /// </summary>
    private async Task<SysmondDespatchDeliveryAddressCreateDto> ResolveIncomingDeliveryAddressAsync(
        string accessToken,
        Guid companyId,
        SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeliveryAddress?.Address is not null)
            return request.DeliveryAddress;

        SysmondActAddressDto? pick = null;

        if (request.ActId != Guid.Empty)
        {
            var remote = await _actQuery.GetActAddressesAsync(
                accessToken,
                request.ActId,
                companyId,
                cancellationToken: cancellationToken);
            pick = SysmondActMapper.SelectPreferredAddress(remote);
            if (pick is not null)
            {
                _logger.LogInformation(
                    "Incoming delivery address from act-address: ActId={ActId}, AddressId={AddressId}, Type={Type}",
                    request.ActId,
                    pick.Id,
                    pick.Type);
            }
        }

        if (pick is null)
        {
            var localAct = await _actRepository.GetByExternalSysmondIdAsync(request.ActId, cancellationToken);
            if (localAct is not null)
            {
                var localAddresses = await _actAddressRepository.GetByActIdAsync(localAct.Id, cancellationToken);
                var localDtos = localAddresses.Select(SysmondActMapper.FromEntity).ToList();
                pick = SysmondActMapper.SelectPreferredAddress(localDtos);
                if (pick is not null)
                {
                    _logger.LogInformation(
                        "Incoming delivery address from local act-address: ActId={ActId}, AddressId={AddressId}",
                        request.ActId,
                        pick.Id);
                }
            }
        }

        if (pick is not null)
            return SysmondActMapper.ToDeliveryAddressCreateDto(pick);

        if (!string.IsNullOrWhiteSpace(request.Street)
            || request.CityId is not null
            || !string.IsNullOrWhiteSpace(request.CityOther))
        {
            return new SysmondDespatchDeliveryAddressCreateDto
            {
                Address = new SysmondAddressCreateDto
                {
                    Type = 30,
                    CountryId = request.CountryId <= 0 ? 1 : request.CountryId,
                    CityId = request.CityId,
                    CityOther = request.CityOther,
                    Street = request.Street
                }
            };
        }

        throw new ValidationException(
        [
            new ValidationFailure(
                "deliveryAddress",
                $"Satıcı cari teslimat adresi bulunamadı (ActId={request.ActId}). " +
                "Sysmond act-address tanımlayın, sync/acts çalıştırın veya deliveryAddress gönderin.")
        ]);
    }

    private static SysmondDespatchPartyCreateDto BuildIncomingSellerParty(
        SysmondCreateIncomingDespatchRequest request,
        SysmondDespatchDeliveryAddressCreateDto deliveryAddress)
    {
        var addr = deliveryAddress.Address;
        return new SysmondDespatchPartyCreateDto
        {
            Type = 30, // SellerSupplier
            ActId = request.ActId,
            ActName = request.ActName,
            ActVknTckn = request.ActVknTckn,
            ActTaxOfficeName = request.ActTaxOfficeName,
            CountryId = addr?.CountryId > 0 ? addr.CountryId : request.CountryId <= 0 ? 1 : request.CountryId,
            CityId = addr?.CityId ?? request.CityId,
            CityOther = FirstNonEmpty(addr?.CityOther, request.CityOther),
            DistrictId = addr?.DistrictId,
            DistrictOther = addr?.DistrictOther,
            Street = FirstNonEmpty(addr?.Street, request.Street),
            BuildingNumber = addr?.BuildingNumber,
            BuildingName = addr?.BuildingName,
            PostalZone = addr?.PostalZone
        };
    }

    /// <summary>Gelen irsaliye alıcısı = bizim şirket. Sysmond 50004: DeliveryCustomer (10) + BuyerCustomer (20).</summary>
    private static List<SysmondDespatchPartyCreateDto> BuildIncomingBuyerParties(
        Company company,
        SysmondDespatchDeliveryAddressCreateDto deliveryAddress)
    {
        var addr = deliveryAddress.Address;
        var name = string.IsNullOrWhiteSpace(company.Name) ? null : company.Name.Trim();
        var vkn = string.IsNullOrWhiteSpace(company.TaxNumber) ? null : company.TaxNumber.Trim();
        var taxOffice = FirstNonEmpty(company.TaxOffice, "Ankara VD");

        // Alıcı taraf adresi: şirket kaydı öncelikli (satıcı act-address değil).
        var countryId = 1;
        var cityId = addr?.CityId;
        var cityOther = addr?.CityOther;
        var districtOther = addr?.DistrictOther;
        var street = FirstNonEmpty(company.Address, addr?.Street);
        var buildingNumber = addr?.BuildingNumber;
        var postalZone = addr?.PostalZone;

        SysmondDespatchPartyCreateDto MakeParty(int type) => new()
        {
            Type = type,
            ActName = name,
            ActVknTckn = vkn,
            ActTaxOfficeName = taxOffice,
            CountryId = countryId,
            CityId = cityId,
            CityOther = cityOther,
            DistrictOther = districtOther,
            Street = street,
            BuildingNumber = buildingNumber,
            PostalZone = postalZone,
            Phone = Truncate(company.Phone, 50),
            Email = Truncate(company.Email, 150)
        };

        return [MakeParty(10), MakeParty(20)];
    }

    private static void ValidateIncomingBuyerCompany(Company company)
    {
        var failures = new List<ValidationFailure>();
        if (string.IsNullOrWhiteSpace(company.Name))
            failures.Add(new ValidationFailure("company.name", "Gelen irsaliye alıcısı için şirket adı zorunlu."));
        if (string.IsNullOrWhiteSpace(company.TaxNumber))
            failures.Add(new ValidationFailure("company.taxNumber", "Gelen irsaliye alıcısı için şirket VKN/TCKN zorunlu (50004)."));

        if (failures.Count > 0)
            throw new ValidationException(failures);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> CreateOutgoingDespatchAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        var failures = new List<ValidationFailure>();
        if (request.ActId == Guid.Empty)
            failures.Add(new ValidationFailure("actId", "actId zorunludur."));
        if (request.CompanyAddressId == Guid.Empty)
            failures.Add(new ValidationFailure("companyAddressId", "companyAddressId zorunludur."));
        if (request.DeliveryAddress?.Address is null)
            failures.Add(new ValidationFailure("deliveryAddress", "deliveryAddress.address zorunludur."));
        else if (request.DeliveryAddress.Address.CountryId <= 0)
            failures.Add(new ValidationFailure("deliveryAddress.address.countryId", "countryId zorunludur."));
        if (request.Items is null || request.Items.Count == 0)
            failures.Add(new ValidationFailure("items", "En az bir kalem zorunludur."));
        else
        {
            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                if (item.StockId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].stockId", "stockId zorunludur."));
                if (item.WarehouseId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].warehouseId", "warehouseId zorunludur."));
                if (item.MeasureUnitId == Guid.Empty)
                    failures.Add(new ValidationFailure($"items[{i}].measureUnitId", "measureUnitId zorunludur."));
                if (item.Quantity <= 0)
                    failures.Add(new ValidationFailure($"items[{i}].quantity", "quantity > 0 olmalı."));
            }
        }

        if (failures.Count > 0)
            throw new ValidationException(failures);

        var items = request.Items!;

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var users = await _userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var syncUser = users.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Şirkette User yok; belge UserId zorunlu (CompanyId={company.Id}).");

        var companyPeriodId = await ResolveCompanyPeriodIdAsync(
            company.Id, accessToken, request.CompanyPeriodId, cancellationToken);

        var issueDate = ResolveDespatchDateTime(request.IssueDate);
        var actualDate = request.ActualDespatchDate is null
            ? issueDate
            : ResolveDespatchDateTime(request.ActualDespatchDate);
        var currencyId = request.CurrencyId is > 0 ? request.CurrencyId : 949;

        var buyerParties = await BuildOutgoingBuyerPartiesAsync(request, cancellationToken);
        var parties = new List<SysmondDespatchPartyCreateDto>
        {
            // Giden: satıcı = bizim şirket (Sysmond 50003)
            new()
            {
                Type = 30, // SellerSupplier
                ActName = string.IsNullOrWhiteSpace(company.Name) ? null : company.Name.Trim(),
                ActVknTckn = string.IsNullOrWhiteSpace(company.TaxNumber) ? null : company.TaxNumber.Trim(),
                ActTaxOfficeName = string.IsNullOrWhiteSpace(company.TaxOffice) ? null : company.TaxOffice.Trim(),
                CountryId = 1,
                Street = string.IsNullOrWhiteSpace(company.Address) ? null : company.Address.Trim()
            }
        };
        parties.AddRange(buyerParties);

        var primaryBuyer = buyerParties.FirstOrDefault(p => p.Type == 20) ?? buyerParties.FirstOrDefault();
        if (primaryBuyer is null
            || (string.IsNullOrWhiteSpace(primaryBuyer.ActName) && string.IsNullOrWhiteSpace(primaryBuyer.ActVknTckn)))
        {
            throw new ValidationException(
            [
                new ValidationFailure(
                    "actId",
                    "Alıcı adı veya VKN/TCKN çözülemedi. actName/actVknTckn gönderin veya adı olan cari seçin.")
            ]);
        }

        var (scenario, despatchType) = await ResolveOutgoingScenarioAsync(
            accessToken, request, cancellationToken);
        var templateId = await ResolveOutgoingTemplateIdAsync(
            accessToken, company.Id, request.TemplateId, scenario, cancellationToken);

        var draftBody = new SysmondOutgoingDespatchCreateDto
        {
            CompanyPeriodId = companyPeriodId,
            Scenario = scenario,
            Type = despatchType,
            TemplateId = templateId,
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            Description = request.Description,
            CurrencyId = currencyId,
            CurrencyExchangeRate = request.CurrencyExchangeRate <= 0 ? 1 : request.CurrencyExchangeRate,
            CompanyAddressId = request.CompanyAddressId,
            DeliveryAddressCreateDto = request.DeliveryAddress,
            DespatchPartyCreateDtos = parties
        };

        _logger.LogInformation(
            "Outgoing draft resolve: Scenario={Scenario}, Type={Type}, TemplateId={TemplateId}",
            scenario,
            despatchType,
            templateId);

        await EnsureDespatchItemStockPriceIdsAsync(accessToken, company.Id, items, preferPurchasePrice: false, cancellationToken);

        var despatchId = await _despatchCommand.CreateOutgoingDraftAsync(accessToken, draftBody, cancellationToken);

        var createdItemIds = new List<Guid>();
        foreach (var item in items)
        {
            var itemId = await _despatchCommand.CreateOutgoingItemAsync(
                accessToken,
                new SysmondDespatchItemCreateDto
                {
                    DespatchId = despatchId,
                    StockId = item.StockId,
                    WarehouseId = item.WarehouseId,
                    MeasureUnitId = item.MeasureUnitId,
                    StockPriceId = item.StockPriceId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VatPercent = item.VatPercent,
                    Name = item.Name,
                    Code = item.Code,
                    Description = item.Description
                },
                cancellationToken);
            createdItemIds.Add(itemId);
        }

        await _despatchCommand.SaveOutgoingAsync(
            accessToken,
            new SysmondIncomingDespatchSaveDto
            {
                CompanyId = company.Id,
                DespatchId = despatchId,
                Recalculate = true
            },
            cancellationToken);

        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            DocumentType = PurchaseOrderDocumentType.OutgoingDespatch,
            Direction = DespatchDirection.Outgoing,
            UserId = syncUser.Id,
            OrderNumber = BuildCreatedOrderNumber(request.DocNo, despatchId),
            Status = PurchaseOrderStatus.Saved,
            ExternalSysmondId = despatchId,
            ExternalSysmondCompanyPeriodId = companyPeriodId,
            ExternalSysmondCompanyAddressId = request.CompanyAddressId,
            ActName = Truncate(request.ActName, 255),
            ActVknTckn = Truncate(request.ActVknTckn, 20),
            IssueDate = issueDate,
            ActualDespatchDate = actualDate,
            CreatedAt = DateTime.UtcNow,
            Items = new List<PurchaseOrderItem>()
        };
        SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, request.DeliveryAddress);

        await FillLocalOrderItemsAsync(order, company.Id, items, createdItemIds, cancellationToken);

        SysmondDespatchPurchaseOrderMapper.RecalcTotal(order);
        await _purchaseOrderRepository.AddAsync(order, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Sysmond outgoing despatch create OK: DespatchId={DespatchId}, LocalOrderId={OrderId}, Items={ItemCount}",
            despatchId,
            order.Id,
            order.Items.Count);

        return PurchaseOrderMapper.ToResponse(order);
    }

    private async Task<Guid> ResolveCompanyPeriodIdAsync(
        Guid companyId,
        string accessToken,
        Guid? requestedPeriodId,
        CancellationToken cancellationToken)
    {
        if (requestedPeriodId is Guid id && id != Guid.Empty)
            return id;

        var periods = await _inventoryQuery.GetMyCompanyPeriodsAsync(accessToken, cancellationToken);
        var period =
            periods.FirstOrDefault(p => p.CompanyId == companyId && p.IsActive)
            ?? periods.FirstOrDefault(p => p.CompanyId == companyId);

        if (period is null || period.Id == Guid.Empty)
            throw new InvalidOperationException($"Aktif CompanyPeriod bulunamadı (CompanyId={companyId}).");

        return period.Id;
    }

    /// <summary>
    /// Scenario &gt; 0: istek değerine sadık kal (cari listesinde olmasa da Paper 30 zorlanabilir).
    /// Scenario == 0: despatch-scenarios-by-act-id ile otomatik seç.
    /// Tip, mümkünse eşleşen senaryo types listesinden doğrulanır.
    /// </summary>
    private async Task<(int Scenario, int Type)> ResolveOutgoingScenarioAsync(
        string accessToken,
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        var requestedScenario = request.Scenario;
        var requestedType = request.Type <= 0 ? 10 : request.Type;
        var honorExplicit = requestedScenario > 0;
        var fallbackScenario = honorExplicit ? requestedScenario : 30;

        if (request.ActId == Guid.Empty)
            return (fallbackScenario, requestedType);

        try
        {
            var maps = await _actQuery.GetDespatchScenariosByActIdAsync(
                accessToken, request.ActId, cancellationToken);
            if (maps.Count == 0)
                return (fallbackScenario, requestedType);

            SysmondDespatchScenarioTypeMapDto? preferred;
            if (honorExplicit)
            {
                // İstenen senaryo cari listesinde yoksa bile maps[0]'a (çoğu zaman e-irsaliye) düşme —
                // aksi halde 50008 Posta Kutusu hatası gelir.
                preferred = maps.FirstOrDefault(m => m.Scenario?.Id == requestedScenario);
                if (preferred is null)
                {
                    _logger.LogInformation(
                        "Outgoing scenario forced from request (not in act map): Scenario={Scenario}, Type={Type}, ActId={ActId}",
                        requestedScenario,
                        requestedType,
                        request.ActId);
                    return (requestedScenario, requestedType);
                }
            }
            else
            {
                preferred = maps.FirstOrDefault(m => m.Scenario?.Id == 30) ?? maps[0];
            }

            var scenarioId = preferred.Scenario?.Id is > 0 ? preferred.Scenario.Id : fallbackScenario;
            var typeIds = preferred.Types?.Select(t => t.Id).Where(id => id > 0).ToList() ?? [];
            var typeId = typeIds.Contains(requestedType)
                ? requestedType
                : typeIds.Contains(10)
                    ? 10
                    : typeIds.Count > 0 ? typeIds[0] : 10;

            return (scenarioId, typeId <= 0 ? 10 : typeId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Despatch scenarios resolve failed; Scenario={Scenario}", requestedScenario);
            return (fallbackScenario, requestedType);
        }
    }

    /// <summary>
    /// Sysmond despatch/item <c>stockPriceId</c> zorunlu olabilir.
    /// İstekte yoksa stock-query IncludePrice ile fiyat seçilir (gelen: alış, giden: satış).
    /// </summary>
    private async Task EnsureDespatchItemStockPriceIdsAsync(
        string accessToken,
        Guid companyId,
        IReadOnlyList<SysmondCreateIncomingDespatchItemRequest> items,
        bool preferPurchasePrice,
        CancellationToken cancellationToken)
    {
        var missingStockIds = items
            .Where(i => i.StockPriceId is null || i.StockPriceId == Guid.Empty)
            .Select(i => i.StockId)
            .Distinct()
            .ToList();

        if (missingStockIds.Count == 0)
            return;

        var stocks = await _stockQuery.GetAllStocksAsync(accessToken, companyId, cancellationToken);
        var byId = stocks.ToDictionary(s => s.Id);

        foreach (var item in items)
        {
            if (item.StockPriceId is Guid existing && existing != Guid.Empty)
                continue;

            if (!byId.TryGetValue(item.StockId, out var stock))
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        "stockPriceId",
                        $"Stok bulunamadı; stockPriceId çözülemedi (StockId={item.StockId}). Önce product sync veya geçerli stockId kullanın.")
                ]);
            }

            var priceId = SelectPreferredStockPriceId(stock, preferPurchasePrice);
            if (priceId is null)
            {
                throw new ValidationException(
                [
                    new ValidationFailure(
                        "stockPriceId",
                        $"Stok fiyatı yok (StockId={item.StockId}). Sysmond'ta fiyatsız stok için stockPriceId gönderin veya fiyat tanımlayın.")
                ]);
            }

            item.StockPriceId = priceId;
            _logger.LogInformation(
                "Despatch item stockPriceId auto-resolved: StockId={StockId}, StockPriceId={StockPriceId}, PreferPurchase={PreferPurchase}",
                item.StockId,
                priceId,
                preferPurchasePrice);
        }
    }

    private static Guid? SelectPreferredStockPriceId(SysmondStockDto stock, bool preferPurchase)
    {
        var prices = stock.Prices;
        if (prices is null || prices.Count == 0)
            return null;

        static bool IsSale(string? name) =>
            !string.IsNullOrEmpty(name)
            && (name.Contains("sale", StringComparison.OrdinalIgnoreCase)
                || name.Contains("satış", StringComparison.OrdinalIgnoreCase)
                || name.Contains("satis", StringComparison.OrdinalIgnoreCase));

        static bool IsPurchase(string? name) =>
            !string.IsNullOrEmpty(name)
            && (name.Contains("purchase", StringComparison.OrdinalIgnoreCase)
                || name.Contains("alış", StringComparison.OrdinalIgnoreCase)
                || name.Contains("alis", StringComparison.OrdinalIgnoreCase)
                || name.Contains("alım", StringComparison.OrdinalIgnoreCase)
                || name.Contains("alim", StringComparison.OrdinalIgnoreCase));

        SysmondStockPriceDto? preferred = preferPurchase
            ? prices.FirstOrDefault(p => IsPurchase(p.StockPriceTypeName))
            : prices.FirstOrDefault(p => IsSale(p.StockPriceTypeName));

        if (preferred is not null && preferred.Id != Guid.Empty)
            return preferred.Id;

        if (!preferPurchase)
        {
            var sale = prices.FirstOrDefault(p => IsSale(p.StockPriceTypeName));
            if (sale is not null && sale.Id != Guid.Empty)
                return sale.Id;
        }
        else
        {
            var purchase = prices.FirstOrDefault(p => IsPurchase(p.StockPriceTypeName));
            if (purchase is not null && purchase.Id != Guid.Empty)
                return purchase.Id;
        }

        var def = prices.FirstOrDefault(p => p.IsDefault && p.Id != Guid.Empty);
        if (def is not null)
            return def.Id;

        var first = prices.FirstOrDefault(p => p.Id != Guid.Empty);
        return first?.Id;
    }

    /// <summary>
    /// DocNoTemplateTypes: 50 PaperDespatch (KAĞIT irsaliye), 40 EDespatch.
    /// </summary>
    private async Task<Guid?> ResolveOutgoingTemplateIdAsync(
        string accessToken,
        Guid companyId,
        Guid? requestedTemplateId,
        int scenario,
        CancellationToken cancellationToken)
    {
        if (requestedTemplateId is Guid tid && tid != Guid.Empty)
            return tid;

        // Paper senaryosu → PaperDespatch (50); aksi halde EDespatch (40) tercih.
        var preferredType = scenario == 30 ? 50 : 40;

        try
        {
            var templates = await _actQuery.GetCompanyDocNoTemplatesAsync(
                accessToken, companyId, cancellationToken);

            var pick =
                templates.FirstOrDefault(t => t.Type == preferredType && t.IsDefault)
                ?? templates.FirstOrDefault(t => t.Type == preferredType)
                ?? templates.FirstOrDefault(t => t.Type is 50 or 40 && t.IsDefault)
                ?? templates.FirstOrDefault(t => t.Type is 50 or 40);

            if (pick is null)
            {
                throw new InvalidOperationException(
                    "Şirkette İRSALİYE (PaperDespatch/EDespatch) şablonu yok. Sysmond'ta irsaliye serisi tanımlayın.");
            }

            _logger.LogInformation(
                "Outgoing template selected: Id={Id}, Type={Type}, Name={Name}, IsDefault={IsDefault}",
                pick.Id,
                pick.Type,
                pick.Name,
                pick.IsDefault);
            return pick.Id;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"İrsaliye şablonu çözülemedi (50007). company-invoice-template kontrol edin: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Alıcı partileri: DeliveryCustomer (10) + BuyerCustomer (20).
    /// Sysmond 50004 genelde DeliveryCustomer eksikliğinden; ada sahip caride ActId bağlanır.
    /// </summary>
    private async Task<List<SysmondDespatchPartyCreateDto>> BuildOutgoingBuyerPartiesAsync(
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken)
    {
        Act? localAct = null;
        if (request.ActId != Guid.Empty)
            localAct = await _actRepository.GetByExternalSysmondIdAsync(request.ActId, cancellationToken);

        ActAddress? localAddress = null;
        if (localAct is not null)
        {
            var addresses = await _actAddressRepository.GetByActIdAsync(localAct.Id, cancellationToken);
            localAddress = addresses.FirstOrDefault(a => !a.IsDisabled) ?? addresses.FirstOrDefault();
        }

        var delivery = request.DeliveryAddress?.Address;
        var name = FirstNonEmpty(
            request.ActName,
            localAct?.Name,
            localAct?.Title,
            localAct is null ? null : $"{localAct.Name} {localAct.Surname}".Trim());
        var vkn = FirstNonEmpty(request.ActVknTckn, localAct?.VknTckn);
        var taxOffice = FirstNonEmpty(request.ActTaxOfficeName, localAct?.TaxOfficeName, "Ankara VD");

        var countryId = request.CountryId > 0
            ? request.CountryId
            : localAddress?.CountryId > 0
                ? localAddress.CountryId
                : delivery?.CountryId > 0
                    ? delivery.CountryId
                    : 1;

        var cityId = request.CityId ?? localAddress?.CityId ?? delivery?.CityId;
        var cityOther = FirstNonEmpty(request.CityOther, localAddress?.CityOther, localAddress?.CityName, delivery?.CityOther);
        var street = FirstNonEmpty(request.Street, localAddress?.Street, delivery?.Street);
        var districtOther = FirstNonEmpty(localAddress?.DistrictOther, localAddress?.DistrictName, delivery?.DistrictOther);
        var postalZone = FirstNonEmpty(localAddress?.PostalZone, delivery?.PostalZone);
        var buildingNumber = FirstNonEmpty(localAddress?.BuildingNumber, delivery?.BuildingNumber);

        // Adı olan cari → ActId bağla; adı boşsa serbest metin (50004)
        Guid? linkActId = null;
        if (localAct is not null
            && (!string.IsNullOrWhiteSpace(localAct.Name) || !string.IsNullOrWhiteSpace(localAct.Title)))
        {
            linkActId = localAct.ExternalSysmondId;
        }

        _logger.LogInformation(
            "Outgoing buyer parties: ActId={ActId}, LinkActId={LinkActId}, Name={Name}, Vkn={Vkn}, TaxOffice={TaxOffice}, Street={Street}",
            request.ActId,
            linkActId,
            name,
            vkn,
            taxOffice,
            street);

        SysmondDespatchPartyCreateDto MakeParty(int type) => new()
        {
            Type = type,
            ActId = linkActId,
            ActName = name,
            ActVknTckn = vkn,
            ActTaxOfficeName = taxOffice,
            CountryId = countryId,
            CityId = cityId,
            CityOther = cityOther,
            DistrictOther = districtOther,
            Street = street,
            BuildingNumber = buildingNumber,
            PostalZone = postalZone
        };

        // 10 DeliveryCustomer (teslim alıcı) + 20 BuyerCustomer (cari alıcı)
        return [MakeParty(10), MakeParty(20)];
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

    private async Task FillLocalOrderItemsAsync(
        PurchaseOrder order,
        Guid companyId,
        List<SysmondCreateIncomingDespatchItemRequest> items,
        List<Guid> createdItemIds,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var product = await _productRepository.GetByExternalSysmondIdAsync(item.StockId, cancellationToken);
            if (product is null || product.CompanyId != companyId)
            {
                throw new InvalidOperationException(
                    $"Kalem Product bulunamadı (StockId={item.StockId}). Önce product sync yapın.");
            }

            Guid? warehouseId = null;
            var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(
                item.WarehouseId, cancellationToken);
            if (warehouse is not null && warehouse.CompanyId == companyId)
                warehouseId = warehouse.Id;

            order.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = order.Id,
                ProductId = product.Id,
                WarehouseId = warehouseId,
                ExternalSysmondId = createdItemIds[i],
                MeasureUnitId = item.MeasureUnitId,
                StockPriceId = item.StockPriceId,
                Name = Truncate(item.Name, 255),
                Code = Truncate(item.Code, 100),
                Quantity = SysmondDespatchMapper.MapQuantity(item.Quantity),
                UnitPrice = (decimal)item.UnitPrice,
                VatPercent = (decimal)item.VatPercent,
                ReceivedQuantity = 0
            });
        }
    }

    private static string BuildCreatedOrderNumber(string? docNo, Guid despatchId)
    {
        var doc = string.IsNullOrWhiteSpace(docNo) ? "DESPATCH" : docNo.Trim();
        var suffix = despatchId.ToString("N")[..8];
        var combined = $"{doc}-{suffix}";
        return combined.Length <= 100 ? combined : combined[..100];
    }

    /// <summary>
    /// İstekte tarih yoksa UTC şimdi; yalnızca gün (00:00:00) gönderilmişse o güne şimdiki saat yazılır.
    /// </summary>
    private static DateTime ResolveDespatchDateTime(DateTime? requested)
    {
        var now = DateTime.UtcNow;
        if (requested is null)
            return now;

        var value = requested.Value;
        if (value.Kind == DateTimeKind.Unspecified)
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return value.TimeOfDay == TimeSpan.Zero
            ? value.Date.Add(now.TimeOfDay)
            : value;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
