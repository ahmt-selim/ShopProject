namespace Backend.Models;

public static class OrderStatuses
{
    public const string Preparing = "Hazırlanıyor";
    public const string Shipping = "Kargoda";
    // Yorum yazılabilen "tamamlandı" durumu. Yeni durum eklenirse buraya ekleyin,
    // string literali controller/service/seeder içine dağıtmayın.
    public const string Delivered = "Teslim Edildi";
    public const string Cancelled = "İptal Edildi";

    public static readonly string[] All = [Preparing, Shipping, Delivered, Cancelled];
}
