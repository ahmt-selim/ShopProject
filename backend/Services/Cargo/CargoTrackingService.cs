using Microsoft.Extensions.Logging;
using Backend.Models;

namespace Backend.Services.Cargo;

public sealed record CargoTrackingResult(string Status, string? Detail = null);

public interface ICargoTrackingService
{
    Task<CargoTrackingResult> TrackAsync(Order order, CancellationToken ct = default);
}

/// <summary>
/// Siparisin Carrier alanina gore ilgili kargo firmasi takipcisini secer.
/// Eslesme yoksa genel takipci kullanilir.
/// </summary>
public sealed class CargoTrackingService(
    IEnumerable<ICargoCarrierTracker> trackers,
    GenericCargoTracker fallback,
    ILogger<CargoTrackingService> logger) : ICargoTrackingService
{
    public Task<CargoTrackingResult> TrackAsync(Order order, CancellationToken ct = default)
    {
        var tracker = trackers.FirstOrDefault(t =>
            !string.IsNullOrWhiteSpace(t.CarrierName) &&
            string.Equals(t.CarrierName, order.Carrier, StringComparison.OrdinalIgnoreCase))
            ?? fallback;

        logger.LogDebug("Kargo takipci secildi: {Tracker} (Carrier={Carrier})",
            tracker.GetType().Name, order.Carrier);

        return tracker.TrackAsync(order, ct);
    }
}