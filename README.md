# MiniShop - Basit E-Ticaret

Backend: .NET 10 Web API + SQLite (backend/)
Frontend: React + Vite (frontend/)

## Çalıştırma

1. Backend (http://localhost:5000):
```
cd backend
dotnet watch
```
İlk açılışta `backend/shop.db` oluşur + seedlenir. Mevcut DB varsa veri kaybolmadan yeni tablolara yükseltilir.

2. Frontend (http://localhost:5173):
```
cd frontend
npm run dev
```

## Hesaplar
- Admin: şifre repoda tutulmaz. Admin hesabı ilk açılışta yalnızca `SeedAdmin:Email` + `SeedAdmin:Password` tanımlıysa oluşturulur; DB'de admin varsa seed dokunmaz.
  ```
  cd backend
  dotnet user-secrets set "SeedAdmin:Email" "admin@ornek.com"
  dotnet user-secrets set "SeedAdmin:Password" "guclu-bir-sifre"
  ```
  Canlıda user-secrets yerine `SeedAdmin__Email` / `SeedAdmin__Password` ortam değişkenleri kullanılır.
- Müşteri: sağ üstten Kayıt Ol (email + şifre).

## Müşteri tarafı
- Anasayfa: ürün grid + sol menüde kategoriler
- Ürün detay: sepete ekle + kalp ikonu ile favori (giriş gerekmez)
- Favoriler: girişsiz localStorage'da, giriş yapınca hesaba aktarılır. Sepet: girişsiz cihazda, girişliyken kullanıcı bazlı sunucuda (`Carts`/`CartItems`) saklanır; çıkış/girişte hesaba geri gelir.
- Sağ üst: ikon butonlar (tema, favori, sepet+adet, hesabım). Sol menüde admin için Yönetim Paneli butonu.
- Sepet -> Siparişi Tamamla (/checkout): adres seç + onayla, stok düşer, sipariş no üretilir (SP-2026-0001...)
- Hesabım (ikon): açılır menü — Hesap Bilgileri (/account/profile), Adreslerim (/account/addresses), Siparişlerim (/account/orders), Destek (/account/support), Çıkış Yap
- Destek: konu + açıklama formu, info@shop.com adresine mail şablonu açar
- Siparişlerim (/account/orders): eski siparişler + detay

## Yönetim paneli (/admin, sadece admin)
- Ürünler: liste, **Manuel** (tek tek form) / **Excel** (toplu içe aktarma) butonları, satıra tıkla düzenle, sağ altta sabit Kaydet, sil
- Siparişler: no, ad soyad, adres, adet, toplam, durum; detayda ürünler + adres + durum değiştirme
- Kategoriler / Markalar: liste + + ile ekleme, düzenle, sil
- Müşteriler: üye listesi (sipariş sayısı, harcama), detayda bilgiler + adresler + eski siparişler
- Ayarlar: mağaza adı, duyuru, para birimi, KDV, kargo limiti/ücreti, destek iletişim, bakım modu

## Müşteri yorumları
- Ürün kartlarında ve ürün sayfasında ortalama puan (yıldız) + yorum sayısı.
- Ürün sayfası altında Müşteri Yorumları: puan özeti, dağılım çubukları (tıkla filtrele), sıralama, sadece resimli filtresi, resim galerisi.
- Yorum yazma sadece Siparişlerim -> sipariş detayından, teslim edilmiş ürünlere (ürün başına 1 yorum, en fazla 5 resim). Düzenleme/silme: sipariş detayı veya ürün sayfasındaki kendi yorumu.

## API
- GET /api/products?categoryId=&brandId=&q= (q: ad, kod, barkod arar), GET /api/products/{id}, POST/PUT/DELETE (admin)
  - Ürün: name, description, price, productCode, barcode, vatRate, categoryId, brandId, stock + images:[{id,url}]
- GET /api/products/{id}/images, POST (JSON {url} veya dosya), DELETE /api/product-images/{id} (admin)
- GET/POST/PUT/DELETE /api/categories, /api/brands (yazma admin)
- POST /api/auth/register, /api/auth/login, GET /api/auth/me, PUT /api/auth/profile
- CRUD /api/addresses, /api/favorites (giriş gerekli)
- POST /api/orders/checkout, GET /api/orders/mine(+/{id}), GET /api/orders(+/{id}), PUT /api/orders/{id}/status (admin)
- GET /api/products/{id}/reviews (+/summary, +/eligibility, +/mine), POST (multipart resimli), PUT/DELETE /api/reviews/{id} (sahibi; silmede admin de)
- GET /api/customers(+/{id}) (admin)
- GET /api/settings/public, GET/PUT /api/settings (admin)
- POST /api/upload (admin, dosya max 5MB) -> /uploads/...

Tablolar: Products (productCode, barcode, vatRate, averageRating, reviewCount), ProductImages (productId->url), Reviews (+ReviewImages), Categories, Brands, Users, Addresses, Favorites, Orders, OrderItems, Settings.
