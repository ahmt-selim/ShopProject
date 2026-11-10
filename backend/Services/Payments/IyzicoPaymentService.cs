using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Backend.Data;

namespace Backend.Services.Payments;

public class IyzicoPaymentService(AppDbContext db, ILogger<IyzicoPaymentService> logger) : IPaymentService
{
    private (string ApiKey, string SecretKey, string BaseUrl, string IdentityNumber) ResolveOptions()
    {
        string Get(string key, string fallback = "") =>
            db.Settings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefault() ?? fallback;

        var mode = Get("iyzicoMode", "test").Trim().ToLowerInvariant();
        if (mode != "live") mode = "test";

        string apiKey, secretKey, baseUrl;
        if (mode == "live")
        {
            apiKey = Get("iyzicoLiveApiKey");
            secretKey = Get("iyzicoLiveSecretKey");
            baseUrl = Get("iyzicoLiveBaseUrl", "https://api.iyzipay.com");
        }
        else
        {
            apiKey = Get("iyzicoTestApiKey");
            secretKey = Get("iyzicoTestSecretKey");
            baseUrl = Get("iyzicoTestBaseUrl", "https://sandbox-api.iyzipay.com");
        }
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = mode == "live" ? "https://api.iyzipay.com" : "https://sandbox-api.iyzipay.com";

        var identity = Get("iyzicoIdentityNumber", "74300864791");
        if (string.IsNullOrWhiteSpace(identity)) identity = "74300864791";

        return (apiKey, secretKey, baseUrl, identity);
    }

    private Iyzipay.Options IyzipayOptions()
    {
        var (apiKey, secretKey, baseUrl, _) = ResolveOptions();
        return new Iyzipay.Options
        {
            ApiKey = apiKey,
            SecretKey = secretKey,
            BaseUrl = baseUrl,
        };
    }

    public string TestIdentityNumber => ResolveOptions().IdentityNumber;

    public async Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeRequest req)
    {
        var (apiKey, secretKey, baseUrl, _) = ResolveOptions();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            return new PaymentInitializeResult(false, null, null, null, "CONFIG", "iyzico API bilgileri ayarlardan girilmemiş.");

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
        var (apiKey, secretKey, baseUrl, _) = ResolveOptions();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            return new PaymentRetrieveResult(false, null, null, null, null, null, null, 1, "CONFIG", "iyzico API bilgileri ayarlardan girilmemiş.");

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