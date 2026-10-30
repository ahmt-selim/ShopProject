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

- `Cart` -> `User` (FK `UserId`, cascade delete).
- `CartItem` -> `Cart` (FK `CartId`, cascade delete).
- Modelde navigation property yok; öğeler ayrı sorgulanır.

### EF / Veritabanı (`AppDbContext`)

- `DbSet<Cart> Carts`, `DbSet<CartItem> CartItems`.
- `CartItems.CartId` üzerinde index.