# Redis Demo API (.NET 10 + StackExchange.Redis)

یک نمونه کامل و آماده اجرا از استفاده از Redis در یک پروژه ASP.NET Core Web API با استفاده از پکیج `StackExchange.Redis`.

## پیش‌نیازها

- .NET 10 SDK
- یک نمونه در حال اجرای Redis روی `localhost:6379` (که طبق فرض شما از قبل نصب شده است)

## نحوه اجرا

```bash
cd RedisDemo.Api
dotnet restore
dotnet run
```

سپس مرورگر به صورت خودکار روی آدرس Swagger باز می‌شود (یا به صورت دستی به آدرس زیر مراجعه کنید):

```
https://localhost:5081/swagger
```

## ساختار پروژه

```
RedisDemo/
├── RedisDemo.sln
└── RedisDemo.Api/
    ├── RedisDemo.Api.csproj
    ├── Program.cs                          # تنظیمات و ثبت سرویس‌های Redis
    ├── appsettings.json                    # Connection String و تنظیمات Redis
    ├── Controllers/
    │   └── RedisDemoController.cs          # ۷ مثال کاربردی از عملیات مختلف Redis
    ├── Services/
    │   ├── IRedisService.cs
    │   └── RedisService.cs                 # پیاده‌سازی عملیات String, Hash, List, Counter, ...
    └── Models/
        └── Product.cs
```

## تنظیم آدرس Redis

اگر Redis شما روی پورت یا هاست دیگری اجرا می‌شود، مقدار زیر را در `appsettings.json` یا `appsettings.Development.json` تغییر دهید:

```json
"ConnectionStrings": {
  "Redis": "localhost:6379"
}
```

اگر Redis شما نیاز به Password دارد:

```json
"ConnectionStrings": {
  "Redis": "localhost:6379,password=YOUR_PASSWORD"
}
```

## مثال‌های موجود در Controller

| # | Endpoint | توضیح |
|---|----------|-------|
| 1 | `POST/GET /api/RedisDemo/string/{key}` | ذخیره/بازیابی رشته ساده با انقضا |
| 2 | `POST/GET /api/RedisDemo/product` , `/product/{id}` | الگوی Cache-Aside برای آبجکت (JSON) |
| 3 | `POST/GET /api/RedisDemo/user/{userId}/profile` | ذخیره/بازیابی با ساختار Hash |
| 4 | `POST/GET /api/RedisDemo/activity/{userId}` | افزودن/خواندن با ساختار List |
| 5 | `POST /api/RedisDemo/view/{productId}` | شمارنده اتمیک (Increment) |
| 6 | `DELETE /api/RedisDemo/key/{key}` , `GET .../ttl` , `PUT .../expiry` | مدیریت کلید و TTL |
| 7 | `GET /api/RedisDemo/keys/search?pattern=product:*` | جستجوی کلیدها بر اساس الگو |

## نکات

- `IConnectionMultiplexer` به صورت **Singleton** ثبت شده که طبق توصیه رسمی مستندات StackExchange.Redis است (این کلاس thread-safe است و باید در طول عمر برنامه یک نمونه از آن استفاده شود).
- `AbortOnConnectFail = false` باعث می‌شود در صورت در دسترس نبودن Redis هنگام استارت‌آپ، برنامه کرش نکند.
- علاوه بر سرویس سفارشی `IRedisService`، `IDistributedCache` استاندارد هم در `Program.cs` ثبت شده تا در صورت نیاز به یک لایه Cache قابل تعویض (Provider-agnostic) هم بتوانید استفاده کنید.
