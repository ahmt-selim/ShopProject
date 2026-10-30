using Backend.Models;

namespace Backend.Services.Payments;

public record PaymentBuyer(
    string Id, string Name, string Surname, string Email, string Phone,
    string IdentityNumber, string Ip, string City, string Country, string Address);

public record PaymentItem(string Id, string Name, string Category, decimal Price, int Qty);

public record PaymentInitializeRequest(
    string ConversationId, string BasketId,
    decimal Price, decimal PaidPrice,
    PaymentBuyer Buyer,
    string ShippingAddressTitle, string ShippingAddress, string ShippingCity, string ShippingCountry, string ShippingZip,
    string BillingAddressTitle, string BillingAddress, string BillingCity, string BillingCountry, string BillingZip,
    List<PaymentItem> Items,
    string CallbackUrl);

public record PaymentInitializeResult(bool Success, string? Token, string? CheckoutFormContent, string? PaymentPageUrl, string? ErrorCode, string? ErrorMessage);

public record PaymentRetrieveResult(bool Success, string? PaymentId, string? ConversationId, string? Status, string? CardFamily, string? CardAssociation, string? CardLast4, int InstallmentCount, string? ErrorCode, string? ErrorMessage);

public interface IPaymentService
{
    Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest req);
    Task<PaymentRetrieveResult> RetrieveAsync(string token);
}