using Microsoft.AspNetCore.Mvc;
using RedisDemo.Api.Models;
using RedisDemo.Api.Services;

namespace RedisDemo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RedisDemoController : ControllerBase
{
    private readonly IRedisService _redisService;

    public RedisDemoController(IRedisService redisService)
    {
        _redisService = redisService;
    }

    // ===================== مثال ۱: String ساده با انقضا =====================

    /// <summary>ذخیره یک مقدار رشته‌ای ساده در Redis با انقضای ۱۰ دقیقه</summary>
    [HttpPost("string/{key}")]
    public async Task<IActionResult> SetString(string key, [FromBody] string value)
    {
        var result = await _redisService.SetStringAsync(key, value, TimeSpan.FromMinutes(10));
        return Ok(new { success = result });
    }

    /// <summary>بازیابی یک مقدار رشته‌ای از Redis</summary>
    [HttpGet("string/{key}")]
    public async Task<IActionResult> GetString(string key)
    {
        var value = await _redisService.GetStringAsync(key);
        if (value is null)
            return NotFound(new { message = "کلید یافت نشد یا منقضی شده است." });

        return Ok(new { key, value });
    }

    // ===================== مثال ۲: Cache-Aside برای آبجکت (Product) =====================

    /// <summary>ذخیره یک محصول در کش به صورت JSON</summary>
    [HttpPost("product")]
    public async Task<IActionResult> CacheProduct([FromBody] Product product)
    {
        var cacheKey = $"product:{product.Id}";
        await _redisService.SetObjectAsync(cacheKey, product, TimeSpan.FromMinutes(30));
        return Ok(new { message = "محصول در کش ذخیره شد.", cacheKey });
    }

    /// <summary>
    /// دریافت یک محصول با الگوی Cache-Aside:
    /// ابتدا کش چک می‌شود، در صورت نبود، از "دیتابیس" (شبیه‌سازی‌شده) خوانده و کش می‌شود.
    /// </summary>
    [HttpGet("product/{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var cacheKey = $"product:{id}";
        var cached = await _redisService.GetObjectAsync<Product>(cacheKey);

        if (cached is not null)
            return Ok(new { source = "cache", data = cached });

        // شبیه‌سازی خواندن از دیتابیس
        var fromDb = new Product
        {
            Id = id,
            Name = $"محصول شماره {id}",
            Price = 1000,
            CreatedAt = DateTime.UtcNow
        };

        await _redisService.SetObjectAsync(cacheKey, fromDb, TimeSpan.FromMinutes(30));
        return Ok(new { source = "database", data = fromDb });
    }

    // ===================== مثال ۳: Hash برای پروفایل کاربر =====================

    /// <summary>ذخیره چند فیلد از پروفایل کاربر در یک Hash</summary>
    [HttpPost("user/{userId}/profile")]
    public async Task<IActionResult> SetUserProfile(string userId, [FromBody] Dictionary<string, string> fields)
    {
        var key = $"user:{userId}:profile";
        foreach (var field in fields)
        {
            await _redisService.SetHashAsync(key, field.Key, field.Value);
        }
        return Ok(new { message = "پروفایل ذخیره شد." });
    }

    /// <summary>دریافت تمام فیلدهای پروفایل کاربر</summary>
    [HttpGet("user/{userId}/profile")]
    public async Task<IActionResult> GetUserProfile(string userId)
    {
        var key = $"user:{userId}:profile";
        var profile = await _redisService.GetAllHashAsync(key);
        return Ok(profile);
    }

    // ===================== مثال ۴: List برای لاگ فعالیت‌ها =====================

    /// <summary>افزودن یک فعالیت جدید به انتهای لیست فعالیت‌های کاربر</summary>
    [HttpPost("activity/{userId}")]
    public async Task<IActionResult> AddActivity(string userId, [FromBody] string activity)
    {
        var key = $"user:{userId}:activities";
        var newLength = await _redisService.AddToListAsync(key, $"{DateTime.UtcNow:u} - {activity}");
        return Ok(new { count = newLength });
    }

    /// <summary>دریافت لیست کامل فعالیت‌های کاربر</summary>
    [HttpGet("activity/{userId}")]
    public async Task<IActionResult> GetActivities(string userId)
    {
        var key = $"user:{userId}:activities";
        var activities = await _redisService.GetListAsync(key);
        return Ok(activities);
    }

    // ===================== مثال ۵: Counter (شمارنده اتمیک) =====================

    /// <summary>افزایش تعداد بازدید یک محصول (مناسب برای شمارنده یا Rate Limiting)</summary>
    [HttpPost("view/{productId:int}")]
    public async Task<IActionResult> IncrementViewCount(int productId)
    {
        var key = $"product:{productId}:views";
        var newCount = await _redisService.IncrementAsync(key);
        return Ok(new { productId, viewCount = newCount });
    }

    // ===================== مثال ۶: مدیریت کلید (حذف / TTL) =====================

    /// <summary>حذف یک کلید از Redis</summary>
    [HttpDelete("key/{key}")]
    public async Task<IActionResult> DeleteKey(string key)
    {
        var deleted = await _redisService.DeleteKeyAsync(key);
        return Ok(new { deleted });
    }

    /// <summary>بررسی وجود کلید و مشاهده زمان باقی‌مانده تا انقضا (TTL)</summary>
    [HttpGet("key/{key}/ttl")]
    public async Task<IActionResult> GetTtl(string key)
    {
        var exists = await _redisService.KeyExistsAsync(key);
        if (!exists)
            return NotFound(new { message = "کلید وجود ندارد." });

        var ttl = await _redisService.GetTtlAsync(key);
        return Ok(new { key, ttlSeconds = ttl?.TotalSeconds, hasExpiry = ttl.HasValue });
    }

    /// <summary>تنظیم مجدد زمان انقضا برای یک کلید موجود</summary>
    [HttpPut("key/{key}/expiry")]
    public async Task<IActionResult> SetExpiry(string key, [FromQuery] int minutes = 10)
    {
        var result = await _redisService.SetExpiryAsync(key, TimeSpan.FromMinutes(minutes));
        return Ok(new { success = result });
    }

    // ===================== مثال ۷: جستجوی کلیدها بر اساس الگو =====================

    /// <summary>
    /// جستجوی تمام کلیدهای منطبق با یک الگو (مثلاً product:* یا user:1:*)
    /// توجه: استفاده از این متد روی دیتاست‌های بزرگ در Production باید با احتیاط انجام شود.
    /// </summary>
    [HttpGet("keys/search")]
    public async Task<IActionResult> SearchKeys([FromQuery] string pattern = "*")
    {
        var keys = await _redisService.GetKeysByPatternAsync(pattern);
        return Ok(keys);
    }
}
