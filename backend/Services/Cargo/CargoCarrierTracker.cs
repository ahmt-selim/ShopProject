using System.Net.Http;
using Microsoft.Extensions.Logging;
using Backend.Models;

namespace Backend.Services.Cargo;

public interface ICargoCarrierTracker
{
    string CarrierName { get; }
    Task<CargoTrackingResult> TrackAsync(Order order, CancellationToken ct = default);
}

/// <summary>
/// Tum kargo firmalari icin ortak HTTP takip mantigi. Her firma ayri
/// sinif olarak turetilir; gerekiyorsa URL/parse davranisi override edilir.
/// </summary>
public abstract class HttpCargoCarrierTrackerBase(
    IHttpClientFactory httpClientFactory,
    ILogger logger) : ICargoCarrierTracker
{
    public abstract string CarrierName { get; }

    private static readonly string[] DeliveredKeywords =
        ["teslim edildi", "delivered", "teslimat tamam", "teslim alindi", "delivery completed"];
    private static readonly string[] CancelledKeywords =
        ["iptal edildi", "cancelled", "iade edildi", "returned to sender"];

    public virtual async Task<CargoTrackingResult> TrackAsync(Order order, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(order.TrackingNumber))
            return new CargoTrackingResult("Unknown", "Takip numarasi yok");

        var url = ResolveTrackingUrl(order);
        if (string.IsNullOrWhiteSpace(url))
            return new CargoTrackingResult("Unknown", "Takip URL'i yok");

        try
        {
            using var client = httpClientFactory.CreateClient("cargo");
            client.Timeout = TimeSpan.FromSeconds(30);
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return new CargoTrackingResult("Unknown", $"HTTP {(int)response.StatusCode}");

            var body = await response.Content.ReadAsStringAsync(ct);
            var lower = body.ToLowerInvariant();

            if (DeliveredKeywords.Any(k => lower.Contains(k)))
                return new CargoTrackingResult("Delivered", $"{CarrierName}: teslim edildi");

            if (CancelledKeywords.Any(k => lower.Contains(k)))
                return new CargoTrackingResult("Cancelled", $"{CarrierName}: iptal/iade");

            return new CargoTrackingResult("InTransit", $"{CarrierName}: henuz teslim edilmedi");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kargo takip sorgusu basarisiz. Carrier={Carrier} Order={OrderNo}",
                CarrierName, order.OrderNo);
            return new CargoTrackingResult("Unknown", ex.Message);
        }
    }

    protected virtual string? ResolveTrackingUrl(Order order) =>
        !string.IsNullOrWhiteSpace(order.TrackingUrl) ? order.TrackingUrl : null;
}

public sealed class YurticiKargoTracker(IHttpClientFactory f, ILogger<YurticiKargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => "Yurtici Kargo"; }

public sealed class ArasKargoTracker(IHttpClientFactory f, ILogger<ArasKargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => "Aras Kargo"; }

public sealed class MngKargoTracker(IHttpClientFactory f, ILogger<MngKargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => "MNG Kargo"; }

public sealed class PttKargoTracker(IHttpClientFactory f, ILogger<PttKargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => "PTT Kargo"; }

public sealed class SuratKargoTracker(IHttpClientFactory f, ILogger<SuratKargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => "Surat Kargo"; }

/// <summary>
/// Eslesmeyen/ayarlanmamis kargo firmalari icin genel takipci.
/// </summary>
public sealed class GenericCargoTracker(IHttpClientFactory f, ILogger<GenericCargoTracker> l)
    : HttpCargoCarrierTrackerBase(f, l) { public override string CarrierName => ""; }