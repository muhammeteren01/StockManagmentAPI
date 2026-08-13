using API.Controllers;
using Core.Abstractions;
using Core.Services;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace API.Tests.Controllers.PurchaseOrders;

/// <summary>PurchaseOrdersController birim testleri için ortak kurulum.</summary>
internal static class PurchaseOrdersControllerTestHelper
{
    public static PurchaseOrdersController CreateSut(
        Mock<IPurchaseOrderService> purchaseOrderService,
        Mock<ISysmondDespatchOrchestrator>? despatchOrchestrator = null,
        Mock<ICurrentUser>? currentUser = null,
        bool sysmondEnabled = false) =>
        new(
            purchaseOrderService.Object,
            (despatchOrchestrator ?? new Mock<ISysmondDespatchOrchestrator>()).Object,
            (currentUser ?? new Mock<ICurrentUser>()).Object,
            Options.Create(new SysmondOptions { Enabled = sysmondEnabled }));
}
