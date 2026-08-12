using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Mappings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

public partial class SysmondSyncService
{
    /// <inheritdoc />
    public async Task<SysmondActSyncResult> SyncActsAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        // Müşteri / tedarikçi / her ikisi; Carrier (40) irsaliye create için opsiyonel — ayrı sync gerekir diye dahil.
        int[] types = [10, 20, 30, 40];
        var remoteActs = await _actQuery.GetAllActsAsync(accessToken, company.Id, types, cancellationToken);

        var result = new SysmondActSyncResult { ActsFetched = remoteActs.Count };
        var errors = new List<string>();
        var syncedAt = DateTime.UtcNow;
        var remoteActIds = new HashSet<Guid>();

        foreach (var remote in remoteActs)
        {
            try
            {
                if (remote.Id == Guid.Empty)
                {
                    result.Failed++;
                    errors.Add("Act.Id boş; satır atlandı.");
                    continue;
                }

                remoteActIds.Add(remote.Id);

                var existing = await _actRepository.GetByExternalSysmondIdAsync(remote.Id, cancellationToken);
                Act localAct;
                if (existing is null)
                {
                    localAct = SysmondActMapper.ToNewAct(remote, company.Id, syncedAt);
                    await _actRepository.AddAsync(localAct, cancellationToken);
                    result.ActsCreated++;
                }
                else
                {
                    if (existing.CompanyId != company.Id)
                    {
                        result.Failed++;
                        errors.Add(
                            $"Act ExternalSysmondId={remote.Id} başka şirkete bağlı (LocalCompanyId={existing.CompanyId}).");
                        continue;
                    }

                    SysmondActMapper.ApplyToAct(existing, remote, syncedAt);
                    _actRepository.Update(existing);
                    localAct = existing;
                    result.ActsUpdated++;
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                IReadOnlyList<SysmondActAddressDto> remoteAddresses;
                try
                {
                    remoteAddresses = await _actQuery.GetActAddressesAsync(
                        accessToken,
                        remote.Id,
                        includeDisabled: true,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    errors.Add($"ActAddress fetch ActId={remote.Id}: {ex.Message}");
                    _logger.LogWarning(ex, "Sysmond act-address çekilemedi: ActId={ActId}", remote.Id);
                    continue;
                }

                result.AddressesFetched += remoteAddresses.Count;
                var thisActRemoteAddressIds = new HashSet<Guid>();

                foreach (var remoteAddr in remoteAddresses)
                {
                    if (remoteAddr.Id == Guid.Empty)
                    {
                        result.Failed++;
                        errors.Add($"ActAddress.Id boş (ActId={remote.Id}).");
                        continue;
                    }

                    thisActRemoteAddressIds.Add(remoteAddr.Id);

                    var existingAddr = await _actAddressRepository.GetByExternalSysmondIdAsync(
                        remoteAddr.Id,
                        cancellationToken);

                    if (existingAddr is null)
                    {
                        var created = SysmondActMapper.ToNewAddress(
                            remoteAddr,
                            localAct.Id,
                            company.Id,
                            syncedAt);
                        await _actAddressRepository.AddAsync(created, cancellationToken);
                        result.AddressesCreated++;
                    }
                    else
                    {
                        existingAddr.ActId = localAct.Id;
                        existingAddr.CompanyId = company.Id;
                        SysmondActMapper.ApplyToAddress(existingAddr, remoteAddr, syncedAt);
                        _actAddressRepository.Update(existingAddr);
                        result.AddressesUpdated++;
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Bu cariye ait local adreslerden remote'da olmayanları sil
                var localAddresses = await _actAddressRepository.GetByActIdAsync(localAct.Id, cancellationToken);
                foreach (var localAddr in localAddresses)
                {
                    if (thisActRemoteAddressIds.Contains(localAddr.ExternalSysmondId))
                        continue;

                    try
                    {
                        var tracked = await _actAddressRepository.GetByIdAsync(localAddr.Id, cancellationToken);
                        if (tracked is null)
                            continue;

                        _actAddressRepository.Remove(tracked);
                        result.AddressesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedDeletes++;
                        errors.Add($"Delete ActAddress ExternalId={localAddr.ExternalSysmondId}: {ex.Message}");
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"Act ExternalSysmondId={remote.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond act senkron başarısız: ActId={ActId}", remote.Id);
            }
        }

        // Orphan act + cascade addresses
        var localActs = await _actRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        foreach (var local in localActs)
        {
            if (remoteActIds.Contains(local.ExternalSysmondId))
                continue;

            try
            {
                var tracked = await _actRepository.GetByIdAsync(local.Id, cancellationToken);
                if (tracked is null)
                    continue;

                _actRepository.Remove(tracked);
                result.ActsDeleted++;
            }
            catch (Exception ex)
            {
                result.FailedDeletes++;
                errors.Add($"Delete Act ExternalId={local.ExternalSysmondId}: {ex.Message}");
                _logger.LogWarning(
                    ex,
                    "Sysmond act orphan silinemedi: ActId={ActId}, ExternalSysmondId={ExternalId}",
                    local.Id,
                    local.ExternalSysmondId);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Errors = errors;
        return result;
    }
}
