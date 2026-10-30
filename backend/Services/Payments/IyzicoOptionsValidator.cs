using Microsoft.Extensions.Options;

namespace Backend.Services.Payments;

public class IyzicoOptionsValidator : IValidateOptions<IyzicoOptions>
{
    public ValidateOptionsResult Validate(string? name, IyzicoOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return ValidateOptionsResult.Fail("Iyzico:ApiKey boş. user-secrets veya ortam değişkeni ile tanımlayın.");
        if (string.IsNullOrWhiteSpace(options.SecretKey))
            return ValidateOptionsResult.Fail("Iyzico:SecretKey boş. user-secrets veya ortam değişkeni ile tanımlayın.");
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            return ValidateOptionsResult.Fail("Iyzico:BaseUrl boş.");

        var isSandboxKey = options.ApiKey.StartsWith("sandbox-", StringComparison.OrdinalIgnoreCase);
        var isSandboxUrl = options.BaseUrl.Contains("sandbox-api.iyzipay.com", StringComparison.OrdinalIgnoreCase);
        var isLiveUrl = options.BaseUrl.Contains("api.iyzipay.com", StringComparison.OrdinalIgnoreCase) && !isSandboxUrl;

        if (isSandboxKey && !isSandboxUrl)
            return ValidateOptionsResult.Fail("Iyzico uyuşmazlığı: sandbox anahtar kullanılıyor ama BaseUrl sandbox değil (sandbox-api.iyzipay.com olmalı).");
        if (!isSandboxKey && isSandboxUrl)
            return ValidateOptionsResult.Fail("Iyzico uyuşmazlığı: canlı anahtar kullanılıyor ama BaseUrl sandbox (api.iyzipay.com olmalı).");

        return ValidateOptionsResult.Success;
    }
}