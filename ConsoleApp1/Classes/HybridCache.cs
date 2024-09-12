using ConsoleApp1.Interface;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace ConsoleApp1.Classes;

public class HybridCache : IHybridCache
{
  private readonly IMemoryCache _memoryCache;
  private readonly IDistributedCache _distributedCache;
  private static readonly object _lock = new object();  // Lock for thread-safety

  public HybridCache(IMemoryCache memoryCache, IDistributedCache distributedCache)
  {
    _memoryCache = memoryCache;
    _distributedCache = distributedCache;
  }

  public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan duration)
  {
    T value;

    // Check MemoryCache first
    lock (_lock)
    {
      if (_memoryCache.TryGetValue(key, out value))
      {
        string valueString = value.ToString();
        valueString = ReplaceSourceInfo(valueString, "MemoryCache");
        value = (T)Convert.ChangeType(valueString, typeof(T));
        return value;
      }
    }

    // Check Distributed Cache (Redis)
    var cachedData = await _distributedCache.GetStringAsync(key);
    if (cachedData != null)
    {
      value = System.Text.Json.JsonSerializer.Deserialize<T>(cachedData);

      string valueString = value.ToString();
      valueString = ReplaceSourceInfo(valueString, "Distributed Cache");
      value = (T)Convert.ChangeType(valueString, typeof(T));

      // Set the value in MemoryCache
      lock (_lock)
      {
        _memoryCache.Set(key, value, absoluteExpirationRelativeToNow: duration);
      }

      return value;
    }

    // Query the database if not found in any cache
    value = await factory();

    // Save the data to both MemoryCache and Redis
    string newValue = ReplaceSourceInfo(value.ToString(), "Database");
    value = (T)Convert.ChangeType(newValue, typeof(T));

    var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);
    await _distributedCache.SetStringAsync(key, serializedValue, new DistributedCacheEntryOptions
    {
      AbsoluteExpirationRelativeToNow = duration  // Set expiration time for Redis
    });

    lock (_lock)
    {
      _memoryCache.Set(key, value, duration);  // Set expiration time for MemoryCache
    }

    return value;
  }

  public void InvalidateCache(string key)
  {
    // Remove key from MemoryCache
    lock (_lock)
    {
      _memoryCache.Remove(key);
    }

    // Remove key from Distributed Cache (Redis)
    _distributedCache.Remove(key);
  }

  private string ReplaceSourceInfo(string originalValue, string source)
  {
    string patternToRemove = @"\(Source:.*?\)";
    string updatedValue = System.Text.RegularExpressions.Regex.Replace(originalValue, patternToRemove, "");
    return $"{updatedValue} (Source: {source})";
  }

  public async Task<T> GetAsync<T>(string key)
  {
    // Try getting the value from MemoryCache first
    lock (_lock)
    {
      if (_memoryCache.TryGetValue(key, out T value))
      {
        return value;
      }
    }

    // If not found, get the value from Distributed Cache
    var cachedData = await _distributedCache.GetStringAsync(key);
    return cachedData != null ? System.Text.Json.JsonSerializer.Deserialize<T>(cachedData) : default;
  }
}
