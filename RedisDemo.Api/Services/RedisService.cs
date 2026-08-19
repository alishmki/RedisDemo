using System.Text.Json;
using StackExchange.Redis;

namespace RedisDemo.Api.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly string _instanceName;
    private readonly ILogger<RedisService> _logger;

    public RedisService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _instanceName = configuration["RedisSettings:InstanceName"] ?? "App_";
        _logger = logger;
    }

    private string BuildKey(string key) => $"{_instanceName}{key}";

    // ---------- String ----------
    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            return await _db.StringSetAsync(BuildKey(key), value, expiry);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogError(ex, "Redis connection error while setting key {Key}", key);
            throw;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(BuildKey(key));
        return value.HasValue ? value.ToString() : null;
    }

    // ---------- Object (JSON) ----------
    public async Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        return await _db.StringSetAsync(BuildKey(key), json, expiry);
    }

    public async Task<T?> GetObjectAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(BuildKey(key));
        if (!json.HasValue) return default;
        return JsonSerializer.Deserialize<T>(json!);
    }

    // ---------- Hash ----------
    public async Task<bool> SetHashAsync(string key, string field, string value)
    {
        return await _db.HashSetAsync(BuildKey(key), field, value);
    }

    public async Task<string?> GetHashAsync(string key, string field)
    {
        var value = await _db.HashGetAsync(BuildKey(key), field);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<Dictionary<string, string>> GetAllHashAsync(string key)
    {
        var entries = await _db.HashGetAllAsync(BuildKey(key));
        return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
    }

    // ---------- List ----------
    public async Task<long> AddToListAsync(string key, string value)
    {
        return await _db.ListRightPushAsync(BuildKey(key), value);
    }

    public async Task<List<string>> GetListAsync(string key)
    {
        var values = await _db.ListRangeAsync(BuildKey(key));
        return values.Select(v => v.ToString()).ToList();
    }

    // ---------- Counter ----------
    public async Task<long> IncrementAsync(string key, long value = 1)
    {
        return await _db.StringIncrementAsync(BuildKey(key), value);
    }

    // ---------- Key Management ----------
    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(BuildKey(key));
    }

    public async Task<bool> DeleteKeyAsync(string key)
    {
        return await _db.KeyDeleteAsync(BuildKey(key));
    }

    public async Task<bool> SetExpiryAsync(string key, TimeSpan expiry)
    {
        return await _db.KeyExpireAsync(BuildKey(key), expiry);
    }

    public async Task<TimeSpan?> GetTtlAsync(string key)
    {
        return await _db.KeyTimeToLiveAsync(BuildKey(key));
    }

    // ---------- Pattern Search ----------
    public async Task<List<string>> GetKeysByPatternAsync(string pattern)
    {
        var endpoints = _redis.GetEndPoints();
        var server = _redis.GetServer(endpoints.First());
        var keys = server.Keys(pattern: $"{_instanceName}{pattern}");
        return keys.Select(k => k.ToString()).ToList();
    }
}
