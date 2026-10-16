# Değişiklik Günlüğü

## 2026-10-16 — Müşteri Yorumları ve Puanlama

Uçtan uca yorum sistemi eklendi. Yazma **sadece sipariş detayından**
(`Siparişlerim → detay`); ürün sayfası salt-okunur listeler.

### Backend
- `Review` / `ReviewImage` entity'leri (`OrderId` + `OrderItemId` satın alma kanıtı,
  `Status`: Pending/Approved/Rejected, varsayılan Approved; unique `(UserId, ProductId)`).
- `Product`'a denormalize `AverageRating (double)` + `ReviewCount`; CUD ile aynı
  transaction'da güncellenir.
- `OrderStatuses` tek sabiti (`Delivered = "Teslim Edildi"` yorum eşiğidir).
- EF migrations aktif (`Baseline` + `AddProductReviews`); eski `shop.db`'ler
  `DbMigrator` damgasıyla açılır (`Data/DbMigrator.cs`).
- `ReviewsController`: `GET products/{id}/reviews` (sayfa/sort/puan/resimli filtresi),
  `.../summary`, `.../eligibility`, `.../mine`, `POST` (multipart),
  `PUT/DELETE reviews/{id}` (sahibi; silmede admin de). ProblemDetails + kullanıcı
  bazlı rate limiting (dk. 5).
- Resim: 5 adet / 5 MB / jpg-png-webp; uzantı + content-type + magic bytes kontrolü,
  GUID dosya adı, `wwwroot/uploads/reviews` (`IFileStorage` soyutlaması).
- Yorum metni ham saklanır (React escape eder); ürün/favori/sepet DTO'larına
  `averageRating` + `reviewCount` eklendi.

### Frontend
- `StarRating` (0.5 hassas salt-okunur, doluluk puana oranlı), ürün kartlarında puan.
- Ürün sayfası: üst özet + smooth scroll, `Müşteri Yorumları` bölümü
  (dağılım çubukları=filtre, sıralama, resimli filtresi, Daha Fazla Göster,
  lightbox galeri, Skeleton/boş/hata durumları).
- Sipariş detayı: teslim edilmiş ürünlerde `OrderReviewBox` (yaz/düzenle/sil).
