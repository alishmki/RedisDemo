using RedisDemo.Api.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Redis Demo API",
        Version = "v1",
        Description = "نمونه کامل استفاده از StackExchange.Redis در ASP.NET Core Web API (.NET 10)"
    });
});

// ===================== تنظیمات اتصال به Redis =====================
var redisConnectionString = builder.Configuration.GetConnectionString("Redis")
    ?? throw new InvalidOperationException("Redis connection string ('ConnectionStrings:Redis') در appsettings.json یافت نشد.");

// ثبت IConnectionMultiplexer به صورت Singleton (طبق توصیه رسمی StackExchange.Redis)
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();

    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // اگر Redis موقتاً در دسترس نبود، اپ در استارت‌آپ کرش نکند
    configuration.ConnectRetry = 3;
    configuration.ConnectTimeout = 5000;
    configuration.ReconnectRetryPolicy = new ExponentialRetry(1000);

    var connection = ConnectionMultiplexer.Connect(configuration);

    connection.ConnectionFailed += (_, args) =>
        logger.LogError("اتصال به Redis قطع شد: {EndPoint} - {FailureType}", args.EndPoint, args.FailureType);

    connection.ConnectionRestored += (_, args) =>
        logger.LogInformation("اتصال به Redis برقرار شد: {EndPoint}", args.EndPoint);

    return connection;
});

// سرویس سفارشی برای عملیات مختلف روی Redis (String, Hash, List, Counter, ...)
builder.Services.AddScoped<IRedisService, RedisService>();

// (اختیاری) ثبت Redis به عنوان IDistributedCache استاندارد .NET
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "MyApp_";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Redis Demo API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
