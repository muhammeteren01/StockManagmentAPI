# Stock Management API

Çok şirketli (multi-tenant) stok yönetimi için Clean Architecture tabanlı bir **.NET 10** Web API.

## Özet

Şirketler (`Company`) arasında veri izolasyonu sağlayan bir stok yönetim backend'i. JWT ile kimlik doğrulama, rol bazlı yetkilendirme, EF Core query filter'ları ile `CompanyId` tenant ayrımı ve Serilog ile yapılandırılmış loglama içerir.

## Teknolojiler

| Alan | Stack |
|------|--------|
| Runtime | .NET 10 (`net10.0`) |
| Veri | Entity Framework Core + SQL Server |
| DI | Autofac (Repository / Service modülleri) |
| Validasyon | FluentValidation |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Log | Serilog (console + dosya, correlation id) |
| Test | xUnit, Moq, FluentAssertions (`API.Tests`) |
| API dokümantasyonu | Swagger / OpenAPI (Development) |

Solution dosyası: `Stock_Management_API.slnx`

## Solution yapısı

```
Stock_Management_API/
├── API/                 # Web host, controllers, middleware, JWT, seed, Swagger
├── Core/                # Entities, DTOs, enums, interfaces, validations, settings
├── Repository/          # EF Core DbContext, configurations, migrations, repos, UoW
├── Service/             # İş kuralları / servis implementasyonları
└── API.Tests/           # Controller, service ve validation birim testleri
```

Katman bağımlılıkları (içten dışa): **Core** ← Repository / Service ← **API** ← **API.Tests**

## Özellikler

### Kimlik doğrulama ve roller

- **Auth:** `POST /api/auth/login` (anonim), `POST /api/auth/register` (SuperAdmin / CompanyAdmin), `GET /api/auth/me`, `POST /api/auth/sysmond-token` (anonim; Sysmondax OAuth)
- **Roller:** `SuperAdmin`, `CompanyAdmin`, `Manager`, `Staff`
- Endpoint'ler `[Authorize(Roles = ...)]` ile kısıtlanır (ör. şirket yönetimi SuperAdmin; yazma işlemleri genelde Staff hariç)

### Tenant izolasyonu

- Çoğu domain entity `CompanyId` taşır; SuperAdmin'in `CompanyId`'si null olabilir (sistem geneli).
- EF Core **query filter**'ları ve token'a bağlı şirket kimliği ile şirket verisi ayrılır.
- SuperAdmin yazma işlemlerinde isteğe bağlı / zorunlu `CompanyId` `TenantGuard` ile çözülür.

### Ana domain'ler (API controllers)

| Domain | Controller |
|--------|------------|
| Companies | `CompaniesController` |
| Users | `UsersController` |
| Warehouses | `WarehousesController` |
| Categories | `CategoriesController` |
| Suppliers | `SuppliersController` |
| Products | `ProductsController` |
| Inventories | `InventoriesController` |
| StockTransactions | `StockTransactionsController` |
| StockTransfers | `StockTransfersController` |
| PurchaseOrders | `PurchaseOrdersController` |

Ek: Inventory üzerinde optimistic concurrency (`RowVersion`).

## Kurulum

### Önkoşullar

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (yerel geliştirme için örnek: `localhost\SQLEXPRESS`)
- (İsteğe bağlı) EF Core CLI: `dotnet tool install --global dotnet-ef`

### Restore

```powershell
dotnet restore Stock_Management_API.slnx
```

### Bağlantı dizesi

Varsayılan connection string `API/appsettings.json` içinde `ConnectionStrings:DefaultConnection` anahtarındadır (`StockDb`). Ortamınıza göre düzenleyin veya User Secrets / ortam değişkeni ile ezin.

### User Secrets (Development)

API projesinde User Secrets tanımlıdır. **Gerçek şifreleri repoya yazmayın.**

`API` klasöründen:

```powershell
cd API

# JWT imza anahtarı (yeterince uzun bir secret)
dotnet user-secrets set "JwtSettings:Secret" "YOUR_DEV_JWT_SECRET_HERE"

# SuperAdmin seed (Development'ta SeedSettings:Enabled = true)
dotnet user-secrets set "SeedSettings:Password" "YOUR_DEV_SUPERADMIN_PASSWORD"

# Sysmondax OAuth password grant (gerçek değerleri repoya yazmayın)
dotnet user-secrets set "Sysmond:Username" "YOUR_SYSMOND_USERNAME"
dotnet user-secrets set "Sysmond:Password" "YOUR_SYSMOND_PASSWORD"
dotnet user-secrets set "Sysmond:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "Sysmond:ClientSecret" "YOUR_CLIENT_SECRET"
```

İlgili ayar bölümleri:

- **JwtSettings:** `Secret`, `Issuer`, `Audience`, `ExpirationInMinutes`
- **SeedSettings:** `Enabled`, `Email`, `Password`, `FirstName`, `LastName`  
  - Production'da `Enabled` genelde `false` kalmalı.
  - Development'ta `Enabled: true` iken `Email` + `Password` dolu olmalıdır; aksi halde uygulama açılışta hata verir.
  - Seed varsayılan e-posta örneği: `superadmin@stock.local` (şifreyi yalnızca secrets ile verin).
- **Sysmond:** `BaseUrl`, `Username`, `Password`, `ClientId`, `ClientSecret`, `Scope` (varsayılan `address email phone profile roles offline_access Sysmond`)
  - `POST /api/auth/sysmond-token` yapılandırmadaki kullanıcı/client secret'larla Sysmondax `/connect/token` çağırır (`grant_type=password`).
  - Commit edilen `appsettings.json` içinde secret alanları boş placeholder bırakın.

### Veritabanı / migration

Migration'lar `Repository/Migrations` altında. Design-time için `AppDbContextFactory` kullanılır.

```powershell
# Repo kökünden (örnek)
dotnet ef database update --project Repository/Repository.csproj --startup-project API/API.csproj
```

Uygulama açılışında otomatik `Migrate` çağrılmaz; şemayı yukarıdaki gibi (veya eşdeğer yolla) uygulamanız gerekir.

### API'yi çalıştırma

```powershell
dotnet run --project API/API.csproj
```

Launch profile'lar (`API/Properties/launchSettings.json`):

- HTTP: `http://localhost:5126`
- HTTPS: `https://localhost:7129` (ve HTTP yukarıdaki port)
- Development'ta Swagger: `/swagger`

## Testler

```powershell
dotnet test API.Tests/API.Tests.csproj
```

Testler controller authorize davranışları, servis kuralları ve FluentValidation senaryolarını kapsar (xUnit + Moq + FluentAssertions).

## Lisans / katkı

Özel / staj projesi bağlamında geliştirilmektedir. Commit ve PR akışı ekip sürecine göre ilerler.
