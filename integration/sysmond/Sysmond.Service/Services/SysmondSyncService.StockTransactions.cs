using Core.Entities;
using Core.Enums;
using Integration.Sysmond.Core.DTOs.StockReceipts;
using Integration.Sysmond.Core.DTOs.Sync;
using Integration.Sysmond.Core.Mappings;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

public partial class SysmondSyncService
{
    private static readonly int[] StockReceiptSyncTypes =
    [
        SysmondStockReceiptTypes.Entry,
        SysmondStockReceiptTypes.Exit,
        SysmondStockReceiptTypes.Transfer
    ];

    /// <inheritdoc />
    public async Task<SysmondStockTransactionSyncResult> SyncStockTransactionsAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var users = await _userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var syncUser = users.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Şirkette kullanıcı yok; StockTransaction.UserId atanamaz (CompanyId={company.Id}).");

        var periods = await _inventoryQuery.GetMyCompanyPeriodsAsync(accessToken, cancellationToken);
        var period = periods.FirstOrDefault(p => p.CompanyId == company.Id && p.IsActive)
            ?? periods.FirstOrDefault(p => p.CompanyId == company.Id)
            ?? throw new InvalidOperationException(
                $"CompanyPeriod bulunamadı (CompanyId={company.Id}). my-company-periods boş veya şirket eşleşmedi.");

        var result = new SysmondStockTransactionSyncResult();
        var errors = new List<string>();
        var remoteExternalIds = new HashSet<Guid>();

        var remoteReceipts = new List<SysmondStockReceiptDto>();
        foreach (var type in StockReceiptSyncTypes)
        {
            var page = await _stockReceiptQuery.GetStockReceiptsAsync(
                accessToken,
                period.Id,
                type,
                isDraft: false,
                cancellationToken);
            remoteReceipts.AddRange(page);
        }

        result.ReceiptsFetched = remoteReceipts.Count;

        foreach (var receipt in remoteReceipts)
        {
            try
            {
                if (receipt.Id == Guid.Empty)
                {
                    result.Failed++;
                    errors.Add("StockReceipt.Id boş; satır atlandı.");
                    continue;
                }

                if (receipt.IsDraft)
                {
                    result.SkippedDraft++;
                    continue;
                }

                if (!StockReceiptSyncTypes.Contains(receipt.Type))
                    continue;

                var items = await _stockReceiptQuery.GetStockReceiptItemsAsync(
                    accessToken, receipt.Id, cancellationToken);
                result.ItemsFetched += items.Count;

                foreach (var item in items)
                {
                    await UpsertReceiptItemAsync(
                        company.Id,
                        syncUser.Id,
                        receipt,
                        item,
                        remoteExternalIds,
                        result,
                        errors,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"ReceiptId={receipt.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond stock-receipt sync hatası: ReceiptId={ReceiptId}", receipt.Id);
            }
        }

        await DeleteOrphanStockTransactionsAsync(
            company.Id, period.Id, remoteExternalIds, result, errors, cancellationToken);

        result.Errors = errors;
        return result;
    }

    private async Task UpsertReceiptItemAsync(
        Guid companyId,
        Guid userId,
        SysmondStockReceiptDto receipt,
        SysmondStockReceiptItemDto item,
        HashSet<Guid> remoteExternalIds,
        SysmondStockTransactionSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        if (item.Id == Guid.Empty || item.StockId == Guid.Empty)
        {
            result.Failed++;
            errors.Add($"ReceiptId={receipt.Id}: item id/stockId boş.");
            return;
        }

        var product = await _productRepository.GetByExternalSysmondIdAsync(item.StockId, cancellationToken);
        if (product is null || product.CompanyId != companyId)
        {
            result.SkippedMissingProduct++;
            errors.Add(
                $"ReceiptId={receipt.Id} ItemId={item.Id}: Product ExternalSysmondId={item.StockId} bulunamadı (önce products sync).");
            return;
        }

        if (receipt.Type == SysmondStockReceiptTypes.Transfer)
        {
            var sourceWarehouseId = item.WarehouseId != Guid.Empty ? item.WarehouseId : receipt.WarehouseId;
            var targetWarehouseId = receipt.TargetWarehouseId;

            if (sourceWarehouseId == Guid.Empty || targetWarehouseId is null || targetWarehouseId == Guid.Empty)
            {
                result.SkippedMissingWarehouse++;
                errors.Add($"ReceiptId={receipt.Id} ItemId={item.Id}: Transfer kaynak/hedef depo eksik.");
                return;
            }

            var sourceWh = await _warehouseRepository.GetByExternalSysmondIdAsync(sourceWarehouseId, cancellationToken);
            var targetWh = await _warehouseRepository.GetByExternalSysmondIdAsync(targetWarehouseId.Value, cancellationToken);
            if (sourceWh is null || sourceWh.CompanyId != companyId)
            {
                result.SkippedMissingWarehouse++;
                errors.Add($"ReceiptId={receipt.Id}: kaynak Warehouse ExternalSysmondId={sourceWarehouseId} yok.");
                return;
            }

            if (targetWh is null || targetWh.CompanyId != companyId)
            {
                result.SkippedMissingWarehouse++;
                errors.Add($"ReceiptId={receipt.Id}: hedef Warehouse ExternalSysmondId={targetWarehouseId} yok.");
                return;
            }

            await UpsertSingleMovementAsync(
                companyId,
                userId,
                receipt,
                item,
                product.Id,
                sourceWh.Id,
                item.Id,
                TransactionType.TransferOut,
                isTransferIn: false,
                remoteExternalIds,
                result,
                errors,
                cancellationToken);

            await UpsertSingleMovementAsync(
                companyId,
                userId,
                receipt,
                item,
                product.Id,
                targetWh.Id,
                SysmondStockReceiptMapper.ToTransferInExternalId(item.Id),
                TransactionType.TransferIn,
                isTransferIn: true,
                remoteExternalIds,
                result,
                errors,
                cancellationToken);
            return;
        }

        var warehouseExtId = item.WarehouseId != Guid.Empty ? item.WarehouseId : receipt.WarehouseId;
        if (warehouseExtId == Guid.Empty)
        {
            result.SkippedMissingWarehouse++;
            errors.Add($"ReceiptId={receipt.Id} ItemId={item.Id}: WarehouseId boş.");
            return;
        }

        var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(warehouseExtId, cancellationToken);
        if (warehouse is null || warehouse.CompanyId != companyId)
        {
            result.SkippedMissingWarehouse++;
            errors.Add($"ReceiptId={receipt.Id}: Warehouse ExternalSysmondId={warehouseExtId} bulunamadı.");
            return;
        }

        var txType = SysmondStockReceiptMapper.MapTransactionType(receipt.Type);
            await UpsertSingleMovementAsync(
            companyId,
            userId,
            receipt,
            item,
            product.Id,
            warehouse.Id,
            item.Id,
            txType,
            isTransferIn: false,
            remoteExternalIds,
            result,
            errors,
            cancellationToken);
    }

    private async Task UpsertSingleMovementAsync(
        Guid companyId,
        Guid userId,
        SysmondStockReceiptDto receipt,
        SysmondStockReceiptItemDto item,
        Guid productId,
        Guid warehouseId,
        Guid externalSysmondId,
        TransactionType transactionType,
        bool isTransferIn,
        HashSet<Guid> remoteExternalIds,
        SysmondStockTransactionSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        remoteExternalIds.Add(externalSysmondId);

        var existing = await _stockTransactionRepository.GetByExternalSysmondIdAsync(
            externalSysmondId, cancellationToken);

        if (existing is null)
        {
            var entity = SysmondStockReceiptMapper.ToNewTransaction(
                receipt, item, companyId, productId, warehouseId, userId,
                externalSysmondId, transactionType, isTransferIn);
            await _stockTransactionRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            result.Created++;
            return;
        }

        if (existing.CompanyId != companyId)
        {
            result.Failed++;
            errors.Add(
                $"ExternalSysmondId={externalSysmondId} başka şirkete bağlı (LocalCompanyId={existing.CompanyId}).");
            return;
        }

        SysmondStockReceiptMapper.ApplyToTransaction(
            existing, receipt, item, productId, warehouseId,
            externalSysmondId, transactionType, isTransferIn);
        _stockTransactionRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Updated++;
    }

    private async Task DeleteOrphanStockTransactionsAsync(
        Guid companyId,
        Guid periodId,
        HashSet<Guid> remoteExternalIds,
        SysmondStockTransactionSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var locals = await _stockTransactionRepository.GetSysmondByCompanyPeriodAsync(
            companyId, periodId, cancellationToken);

        foreach (var local in locals)
        {
            if (local.ExternalSysmondId is not Guid ext || remoteExternalIds.Contains(ext))
                continue;

            try
            {
                _stockTransactionRepository.Remove(local);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                result.Deleted++;
            }
            catch (Exception ex)
            {
                result.FailedDeletes++;
                errors.Add($"Orphan delete ExternalSysmondId={ext}: {ex.Message}");
                _logger.LogWarning(ex, "StockTransaction orphan silinemedi: {ExternalId}", ext);
            }
        }
    }
}
