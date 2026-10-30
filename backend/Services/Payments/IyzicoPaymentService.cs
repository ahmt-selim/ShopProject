using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.Extensions.Options;

namespace Backend.Services.Payments;

public class IyzicoPaymentService(IOptions<IyzicoOptions> options, ILogger<IyzicoPaymentService> logger) : IPaymentService
{
    private Iyzipay.Options IyzipayOptions()
    {
        var o = options.Value;
        return new Iyzipay.Options
        {
            ApiKey = o.ApiKey,
            SecretKey = o.SecretKey,
            BaseUrl = o.BaseUrl,
        };
    }

    public async Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest req)
    {
        var iyziOpts = IyzipayOptions();
        var request = new CreateCheckoutFormInitializeRequest
        {
            Locale = "tr",
            ConversationId = req.ConversationId,
            Price = Format(req.Price),
            PaidPrice = Format(req.PaidPrice),
            Currency = "TRY",
            BasketId = req.BasketId,
            PaymentGroup = PaymentGroup.PRODUCT.ToString(),
            CallbackUrl = req.CallbackUrl,
            Buyer = new Buyer
            {
                Id = req.Buyer.Id,
                Name = req.Buyer.Name,
                Surname = req.Buyer.Surname,
                Email = req.Buyer.Email,
                GsmNumber = req.Buyer.Phone,
                IdentityNumber = req.Buyer.IdentityNumber,
                Ip = req.Buyer.Ip,
                City = req.Buyer.City,
                Country = req.Buyer.Country,
                RegistrationAddress = req.Buyer.Address,
                LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            },
            ShippingAddress = new Iyzipay.Model.Address
            {
                ContactName = req.Buyer.Name + " " + req.Buyer.Surname,
                City = req.ShippingCity,
                Country = req.ShippingCountry,
                Description = req.ShippingAddress,
                ZipCode = req.ShippingZip,
            },
            BillingAddress = new Iyzipay.Model.Address
            {
                ContactName = req.Buyer.Name + " " + req.Buyer.Surname,
                City = req.BillingCity,
                Country = req.BillingCountry,
                Description = req.BillingAddress,
                ZipCode = req.BillingZip,
            },
            BasketItems = req.Items.Select(i => new BasketItem
            {
                Id = i.Id,
                Name = i.Name,
                Category1 = i.Category,
                ItemType = BasketItemType.PHYSICAL.ToString(),
                Price = Format(i.Price * i.Qty),
            }).ToList(),
        };

        var form = await CheckoutFormInitialize.Create(request, iyziOpts);
        if (form.Status != "success")
        {
            logger.LogWarning("iyzico initialize failed: {Code} {Msg}", form.ErrorCode, form.ErrorMessage);
            return new PaymentInitializeResult(false, null, null, null, form.ErrorCode, form.ErrorMessage);
        }
        return new PaymentInitializeResult(true, form.Token, form.CheckoutFormContent, form.PaymentPageUrl, null, null);
    }

    public async Task<PaymentRetrieveResult> RetrieveAsync(string token)
    {
        var iyziOpts = IyzipayOptions();
        var req = new RetrieveCheckoutFormRequest { Token = token };
        var form = await CheckoutForm.Retrieve(req, iyziOpts);
        if (form.Status != "success")
        {
            logger.LogWarning("iyzico retrieve failed: {Code} {Msg}", form.ErrorCode, form.ErrorMessage);
            return new PaymentRetrieveResult(false, null, form.ConversationId, null, null, null, null, 1, form.ErrorCode, form.ErrorMessage);
        }
        var paid = string.Equals(form.PaymentStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
        int.TryParse(form.Installment?.ToString(), out var inst);
        if (inst <= 0) inst = 1;
        return new PaymentRetrieveResult(paid, form.PaymentId, form.ConversationId, form.PaymentStatus,
            form.CardFamily, form.CardAssociation, form.LastFourDigits, inst, null, null);
    }

    private static string Format(decimal v) => v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
}