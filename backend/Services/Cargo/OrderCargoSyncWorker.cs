using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Backend.Data;
using Backend.Models;

namespace Backend.Services.Cargo;

/// <summary>
/// Her saat calisir: kargoda (Shipping) durumundaki siparislerin takip
/// numaralarini ilgili kargo servisinden sorgular ve durumu gunceller.
/// </summary>
public sealed class OrderCargoSyncWorker(
    IServiceScopeFactory scopeFactory,
    ICargoTrackingService trackingService,
    ILogger<OrderCargoSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Uygulama baslar baslamaz ilk senkronizasyonu yapma; kisa bir bekleme.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var orders = await db.Orders
                .Where(o => o.Status == OrderStatuses.Shipping
                            && !string.IsNullOrWhiteSpace(o.TrackingNumber))
                .ToListAsync(ct);

            if (orders.Count == 0) return;

            logger.LogInformation("Kargo durum senkronizasyonu basladi: {Count} siparis", orders.Count);

            foreach (var order in orders)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var result = await trackingService.TrackAsync(order, ct);

                    if (result.Status == "Delivered")
                    {
                        var fromStatus = order.Status;
                        order.Status = OrderStatuses.Delivered;
                        if (order.DeliveredAt is null)
                            order.DeliveredAt = DateTime.UtcNow;

                        db.OrderStatusHistories.Add(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            FromStatus = fromStatus,
                            ToStatus = order.Status,
                            FromPaymentStatus = order.PaymentStatus,
                            ToPaymentStatus = order.PaymentStatus,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = "system:cargo-sync",
                            Note = $"Kargo servisinden guncellendi: {result.Detail}"
                        });

                        await db.SaveChangesAsync(ct);
                        logger.LogInformation("Siparis teslim edildi olarak guncellendi: {OrderNo}", order.OrderNo);
                    }
                    else if (result.Status == "Cancelled")
                    {
                        var fromStatus = order.Status;
                        order.Status = OrderStatuses.Cancelled;

                        db.OrderStatusHistories.Add(new OrderStatusHistory
                        {
                            OrderId = order.Id,
                            FromStatus = fromStatus,
                            ToStatus = order.Status,
                            FromPaymentStatus = order.PaymentStatus,
                            ToPaymentStatus = order.PaymentStatus,
                            ChangedAt = DateTime.UtcNow,
                            ChangedByUserId = "system:cargo-sync",
                            Note = $"Kargo servisinden guncellendi: {result.Detail}"
                        });

                        await db.SaveChangesAsync(ct);
                        logger.LogInformation("Siparis iptal olarak guncellendi: {OrderNo}", order.OrderNo);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Siparis kargo durumu guncellenemedi: {OrderNo}", order.OrderNo);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kargo durum senkronizasyonu hatasi");
        }
    }
}