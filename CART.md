# Sepet (Cart) Yapısı Özeti

Bu doküman, ShopProject'teki sepet (cart) akışını backend + frontend boyunca özetler.

## 1. Genel Mimari

- **İki modlu sepet:**
  - **Misafir (giriş yapmamış):** sepet `localStorage`'da `cart` anahtarı altında JSON dizi olarak tutulur.
  - **Giriş yapmış kullanıcı:** sepet veritabanında kullanıcı bazlı kalıcı olarak saklanır (`Carts` / `CartItems` tabloları).
- Giriş yapıldığında misafir sepeti hesaba **bir kez** aktarılır (merge), sonra misafir sepeti temizlenir.
- Frontend tek kaynak: `src/store/CartContext.jsx` (`CartProvider` / `useCart`). Backend: `Controllers/CartController.cs`.

## 2. Backend

### Modeller (`backend/Models/Cart.cs`)

```csharp
public class Cart
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;  // User.Id (string)
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Qty { get; set; }
}
```

- `Cart` → `User` (FK `UserId`, cascade delete).
- `CartItem` → `Cart` (FK `CartId`, cascade delete).
- Modelde navigation property yok; öğeler ayrı sorgulanır.

### EF / Veritabanı (`AppDbContext`)

- `DbSet<Cart> Carts`, `DbSet<CartItem> CartItems`.
- `CartItems.CartId` üzerinde index.
- Tablolar: `Carts`, `CartItems`.

### Controller (`/api/cart`, `[Authorize]`)

Tüm endpoint'ler kimlik doğrulama gerektirir (`[Authorize]`).

| Method | Route | Açıklama |
|---|---|---|
| GET | `/api/cart` | Kullanıcının sepetini getir (yoksa oluştur). |
| POST | `/api/cart/items` | `{ productId, qty }` — ekle; ürün varsa adet **toplanır**. |
| PUT | `/api/cart/items/{productId}` | `{ productId, qty }` — adedi **ayarla**; `qty <= 0` ise öğe silinir. |
| DELETE | `/api/cart/items/{productId}` | Öğeyi kaldır. |
| DELETE | `/api/cart` | Sepeti tamamen temizle. |

Kurallar:
- `dto.Qty <= 0` POST'ta hata (`BadRequest`).
- Ürün varlığı kontrolü (`db.Products.Any`); yoksa `NotFound`.
- `GetOrCreate()`: kullanıcıya ait sepet yoksa oluşturur (kullanıcı başına tek sepet).

### DTO Çıktısı (`ToDto`)

```json
{
  "id": <cartId>,
  "items": [
    {
      "id": <cartItemId>,
      "productId": <productId>,
      "name": "...",
      "price": 0.0,
      "imageUrl": "<ilk ürün görseli url veya ''>",
      "averageRating": 0.0,
      "reviewCount": 0,
      "qty": 0
    }
  ]
}
```

- `imageUrl` backend'de `db.PrimaryImageUrl(productId)` ile doldurulur.
- Silinmiş/eksik ürünler filtrelenir (`.Where(i => products.ContainsKey(...))`).

## 3. Frontend

### API Katmanı (`src/api/client.js`)

```js
getCart: () => request('/api/cart'),
addCartItem: (productId, qty) => request('/api/cart/items', { method: 'POST', body: JSON.stringify({ productId, qty }) }),
setCartItem: (productId, qty) => request(`/api/cart/items/${productId}`, { method: 'PUT', body: JSON.stringify({ productId, qty }) }),
removeCartItem: (productId) => request(`/api/cart/items/${productId}`, { method: 'DELETE' }),
clearCart: () => request('/api/cart', { method: 'DELETE' }),
```

### State Yönetimi (`src/store/CartContext.jsx`)

Sağlanan değerler: `{ items, add, remove, setQty, clear, totalCount, totalPrice }`.

- `serverItems` (state): giriş yapmış kullanıcı için sunucu sepeti.
- `guest` (state): misafir sepeti; başlangıçta `localStorage.getItem('cart')`'tan okunur; değişince `localStorage`'a yazılır.
- `mergedFor` (ref): hangi kullanıcı için merge yapıldığını takip eder (tekrar merge'i engeller).

**Giriş sonrası merge:**
```js
if (guest.length > 0 && mergedFor.current !== user.id) {
  mergedFor.current = user.id;
  // her misafir öğesini api.addCartItem ile sunucuya ekle
  // guest'ı temizle, loadServer()
}
```

**Türetilmiş değerler:**
```js
const items = user ? serverItems : guest;
const totalCount = items.reduce((s, i) => s + i.qty, 0);
const totalPrice = items.reduce((s, i) => s + i.qty * i.price, 0);
```

**Misafir öğe şekli:**
```js
{ id: product.id, name, price, imageUrl: product.imageUrl || api.primaryImage(product), qty }
```
Not: misafirde `id` = ürün id; sunucuda `id` = `CartItem.Id`, ayrıca `productId` alanı var.

**Metotlar:**
- `add(product, qty=1)`: giriş varsa API çağırıp `serverItems` günceller; yoksa guest state'i günceller (varsa adet toplar).
- `remove(id)`: giriş varsa API; yoksa filtreler.
- `setQty(id, qty)`: giriş varsa API; yoksa `qty <= 0` ise siler, değilse günceller.
- `clear()`: giriş varsa API + `serverItems=[]`; yoksa `guest=[]`.

### Bileşenler / Sayfalar

- `src/components/AddToCartButton.jsx`: `useCart().add` kullanır; loading → added (1 sn) animasyonu.
- `src/pages/Cart.jsx`: sepet listesi, adet +/-, kaldır, toplam, "Siparişi Tamamla" (giriş varsa `/checkout`, yoksa `/login`).
- `src/pages/Checkout.jsx`: adres seçimi + sipariş özeti; `api.checkout(selected, items.map(i => ({ productId: i.id, qty: i.qty })))`; başarılıysa `clear()` ve sipariş sayfasına yönlendirir.
- `src/components/Header.jsx`: `totalCount` ile sepet ikonu rozeti.

### Route / Provider (`src/App.jsx`)

`CartProvider`, uygulamanın kökünde (auth provider'ın altında) sarar; `/cart` rotası `Cart` sayfasına.

## 4. Akış Özeti

```
Misafir:  Ürün ekle → localStorage 'cart' → Cart sayfası → /login
Giriş:    Login → misafir sepeti API'ye merge → serverItems → Cart/Checkout
Checkout: items → /api/orders/checkout → stok düşer → sipariş oluşur → clear()
```

## 5. Notlar / Dikkat Edilecekler

- `CartContext`'teki state quirks (AGENTS.md): misafir→sunucu merge **login başına bir kez** (`mergedFor` ref). "Sadeleştir" denmemeli.
- Sunucu sepet öğesinde `id` (CartItem id) ile `productId` (ürün id) farklı; Checkout şu an `productId: i.id` kullanıyor — sunucu sepetinde bu `CartItem.Id`'dir, ürün id değil. Doğru kullanım `i.productId` olmalı (potansiyel bug).
- Görsel URL'leri her zaman `api.imageUrl()` ile prefix'lenmeli; inline URL logic yazılmamalı.
- Backend sepet endpoint'leri `[Authorize]` (admin değil, her giriş yapmış kullanıcı kendi sepetini yönetir).
- Sepet öğeleri ürün silinirse ToDto'da filtrelenir.