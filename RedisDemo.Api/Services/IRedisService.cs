namespace RedisDemo.Api.Services;

public interface IRedisService
{
    // String operations
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetStringAsync(string key);

    // Object (JSON) operations
    Task<bool> SetObjectAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetObjectAsync<T>(string key);

    // Hash operations
    Task<bool> SetHashAsync(string key, string field, string value);
    Task<string?> GetHashAsync(string key, string field);
    Task<Dictionary<string, string>> GetAllHashAsync(string key);

    // List operations
    Task<long> AddToListAsync(string key, string value);
    Task<List<string>> GetListAsync(string key);

    // Counter operations
    Task<long> IncrementAsync(string key, long value = 1);

    // Key management
    Task<bool> KeyExistsAsync(string key);
    Task<bool> DeleteKeyAsync(string key);
    Task<bool> SetExpiryAsync(string key, TimeSpan expiry);
    Task<TimeSpan?> GetTtlAsync(string key);

    // Pattern search
    Task<List<string>> GetKeysByPatternAsync(string pattern);
}
