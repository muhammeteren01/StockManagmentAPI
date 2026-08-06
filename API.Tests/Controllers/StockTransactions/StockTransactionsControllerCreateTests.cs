using API.Controllers;
using Core.Exceptions;
using Core.Services;
using Core.Validations;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace API.Tests.Controllers.StockTransactions;

/// <summary>StockTransactionsController.Create birim testleri.</summary>
public class StockTransactionsControllerCreateTests
{
    private readonly Mock<IStockTransactionService> _stockTransactionService = new();
    private readonly StockTransactionsController _sut;

    public StockTransactionsControllerCreateTests()
    {
        _sut = new StockTransactionsController(_stockTransactionService.Object);
    }

    /// <summary>
    /// Create: servis başarılı StockTransactionResponse → CreatedAtAction(201), route GetById.
    /// </summary>
    [Fact]
    public async Task Create_WhenServiceSucceeds_ReturnsCreatedAtActionWithTransactionResponse()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest();
        var expected = StockTransactionsTestHelper.CreateTransactionResponse(
            productId: request.ProductId,
            warehouseId: request.WarehouseId,
            transactionType: request.TransactionType,
            quantity: request.Quantity,
            referenceNo: request.ReferenceNo,
            notes: request.Notes);
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(StockTransactionsController.GetById));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(expected.Id);
        created.Value.Should().BeEquivalentTo(expected);
        _stockTransactionService.Verify(s => s.CreateAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Create: Quantity ≤ 0 → ValidationException iletilir (controller yakalamaz).
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_WhenQuantityNotPositive_ThrowsValidationException(int quantity)
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest(quantity: quantity);
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(
            [
                new ValidationFailure(nameof(request.Quantity), "'Quantity' must be greater than '0'.")
            ]));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    /// <summary>
    /// Create: ürün yok → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenProductNotFound_ThrowsInvalidOperationException()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest();
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Ürün bulunamadı: {request.ProductId}"));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Ürün bulunamadı: {request.ProductId}");
    }

    /// <summary>
    /// Create: ürün ve depo farklı şirket → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenProductAndWarehouseDifferentCompany_ThrowsInvalidOperationException()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest();
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Ürün ve depo aynı şirkete ait olmalıdır."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Ürün ve depo aynı şirkete ait olmalıdır.");
    }

    /// <summary>
    /// Create: yetersiz stok → InvalidOperationException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenInsufficientStock_ThrowsInvalidOperationException()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest(quantity: 50);
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Yetersiz stok."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Yetersiz stok.");
    }

    /// <summary>
    /// Create: eşzamanlılık çakışması → ConflictException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenConcurrencyConflict_ThrowsConflictException()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest();
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
    }

    /// <summary>
    /// Create: şirket erişimi yok → ForbiddenException iletilir.
    /// </summary>
    [Fact]
    public async Task Create_WhenCompanyAccessDenied_ThrowsForbiddenException()
    {
        var request = StockTransactionsTestHelper.CreateCreateRequest();
        _stockTransactionService
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Bu şirkete erişim yetkiniz yok."));

        var act = async () => await _sut.Create(request, CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Bu şirkete erişim yetkiniz yok.");
    }
}
