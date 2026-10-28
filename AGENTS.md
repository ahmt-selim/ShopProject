# AGENTS.md

Two independent packages, no root workspace, no CI, no tests. Work in `backend/` or `frontend/` separately.

## Run

- Backend (http://localhost:5000): `cd backend && dotnet watch` — `Properties/launchSettings.json` fixes port 5000; CORS only allows `localhost:5173` and `localhost:3000`.
- Frontend (http://localhost:5173): `cd frontend && npm run dev` — API base is `VITE_API_URL` in `frontend/.env` (defaults to `http://localhost:5000` in `src/api/client.js`).
- Always run backend before frontend; no proxy in `vite.config.js`, frontend calls backend directly.
- Swagger: `/swagger` and `/openapi/v1.json` in Development only.

## Verify

- Backend: `cd backend && dotnet build`. No test project, no lint/typecheck.
- Frontend: `npm run lint` (eslint, `--max-warnings 0`), `npm run build`. No tests, no typecheck (plain JSX).
- No single command covers both; verify the touched package only.

## Backend (`backend/`, .NET 10 + SQLite)

- Entrypoint: `Program.cs` → `Controllers/`; EF context `Data/AppDbContext.cs`.
- **EF migrations aktif.** Startup `DbMigrator.EnsureMigrated()` (`Data/DbMigrator.cs`) çağırır: history'si olmayan eski `shop.db` idempotent `ShopDbSeeder.EnsureNewSchema` raw SQL'iyle yükseltilip migration'lar history'ye damgalanır, sıfır DB'ye `Migrate()` uygulanır. Yeni entity'de `dotnet tool run dotnet-ef migrations add <Ad>` kullan, `EnsureCreated()`'a dönme. `dotnet-tools.json` local `dotnet-ef` içerir.
- Reset DB: stop backend, delete `shop.db*`, restart — reseeds demo customer (`test@musteri.com` / `Test123!`), brands, settings. Admin is seeded only when `SeedAdmin:Email` + `SeedAdmin:Password` are set (dev: user-secrets, prod: `SeedAdmin__Email` / `SeedAdmin__Password`); never commit real credentials. Don't commit `shop.db*`, `logs/`, `wwwroot/uploads/`.
- `dotnet watch` ignores `wwwroot/uploads/**`, `logs/**`, `*.db*` (see `Backend.csproj` `<Watch Remove>`). Don't remove that.
- Auth: JWT Bearer, `RoleClaimType = ClaimTypes.Role`; write endpoints on products/categories/brands/settings/upload + order status are admin-only. Default dev JWT key lives in `appsettings.json`.
- Images: `Products` has no image columns; all images live in `ProductImages(productId→url)`. Legacy `ImageUrl`/`Images` columns are auto-migrated and dropped on startup — don't re-add them to the model.
- Uploads: `POST /api/upload` (max 5MB) → `/uploads/...`; product-file upload also via `POST /api/products/{id}/images` (multipart).
- Excel import uses ClosedXML (`ProductImportController` + `ImportTemplatesController`); match key defaults to `productCode`.

## Frontend (`frontend/`, React 18 + Vite 5, react-router v7)

- API layer: `src/api/client.js` only — token from `localStorage.token`, throws `Error(data.error)`. Image helper `api.imageUrl()` prefixes `/uploads/` with `BASE_URL`; `api.primaryImage(p)` falls back to picsum. Reuse these, don't inline URL logic.
- State quirks (don't "simplify"): `CartContext` keeps guests in `localStorage.cart` and logged-in users in server `Carts`/`CartItems`, merging guest→server once per login. Favorites: `localStorage` when anonymous, `POST /api/favorites` on login. Route guards are inline per page, no central guard component.
- Routes: `src/App.jsx` — `/admin/*` under `AdminLayout`, account pages under `/account/*`.
