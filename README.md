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
- Admin: Şifre repoda tutulmaz. Admin hesabı ilk açılışta yalnızca `SeedAdmin:Email` + `SeedAdmin:Password` tanımlıysa oluşturulur; DB'de admin varsa seed dokunmaz.
  ```
  cd backend
  dotnet user-secrets set "SeedAdmin:Email" "admin@ornek.com"
  dotnet user-secrets set "SeedAdmin:Password" "guclu-bir-sifre"
  ```
  Canlıda user-secrets yerine `SeedAdmin__Email` / `SeedAdmin__Password` ortam değişkenleri kullanılır.
- Müşteri: Sağ üstten Kayıt Ol (email + şifre).

## Müşteri tarafı
- Anasayfa: Ürün grid + sol menüde kategoriler
- Ürün detay: sepete ekle + kalp ikonu ile favori (giriş gerekmez)
- Favoriler: girişsiz localStorage'da, giriş yapınca hesaba aktarılır. Sepet: girişsiz cihazda, girişliyken kullanıcı bazlı sunucuda (`Carts`/`CartItems`) saklanır; çıkış/girişte hesaba geri gelir.
- Sağ üst: ikon butonlar (tema, favori, sepet+adet, hesabım). Sol menüde admin için Yönetim Paneli butonu.
- Sepet -> Siparişi Tamamla (/checkout): adres seç + onayla, stok düşer, sipariş no üretilir (SP-2026-0001...)
- Hesabım (ikon): açılır menü - Hesap Bilgileri (/account/profile), Adreslerim (/account/addresses), Siparişlerim (/account/orders), Destek (/account/support), Çıkış Yap
- Destek: konu + açıklama formu, info@shop.com adresine mail şablonu açar
- Siparişlerim (/account/orders): eski siparişler + detay