using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OdinCore;

public class Cache
{
    internal bool found;

    internal IMemoryCache? MemoryCache { get; set; }

    internal IDistributedCache? DistributedCache { get; set; }

    internal TimeSpan Duration { get; set; }

    internal object Key { get; set; }

    internal bool Sliding { get; set; }

    internal CacheType? Type { get; set; } = CacheType.None;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Cache(
        IMemoryCache memoryCache,
        object key,
        TimeSpan duration,
        bool sliding = false)
    {
        MemoryCache = memoryCache;
        DistributedCache = null;
        Key = key;
        Duration = duration;
        Sliding = sliding;
        Type = CacheType.MemoryCache;
    }

    public Cache(
        IDistributedCache distributedCache,
        string key,
        TimeSpan duration,
        bool sliding = false)
    {
        MemoryCache = null;
        DistributedCache = distributedCache;
        Key = key;
        Duration = duration;
        Sliding = sliding;
        Type = CacheType.DistributedCache;
    }

    public Cache UpdateDuration(TimeSpan duration)
    {
        Duration = duration;
        return this;
    }

    public bool TryGetValue<T>(out T? value)
    {
        switch (Type)
        {
            case CacheType.MemoryCache:

                if (MemoryCache != null &&
                    MemoryCache.TryGetValue(Key, out T? cached))
                {
                    value = cached;
                    return true;
                }

                value = default;
                return false;

            case CacheType.DistributedCache:

                if (DistributedCache != null)
                {
                    var bytes = DistributedCache.Get(Key.ToString()!);

                    if (bytes != null)
                    {
                        value = JsonSerializer.Deserialize<T>(
                            bytes,
                            JsonOptions);

                        return value != null;
                    }
                }

                value = default;
                return false;

            default:

                value = default;
                return false;
        }
    }

    public async Task<T?> GetValueAsync<T>()
    {
        if (Type == CacheType.DistributedCache &&
            DistributedCache != null)
        {
            var bytes = await DistributedCache.GetAsync(
                Key.ToString()!);

            if (bytes == null)
                return default;

            return JsonSerializer.Deserialize<T>(
                bytes,
                JsonOptions);
        }

        if (Type == CacheType.MemoryCache &&
            MemoryCache != null)
        {
            MemoryCache.TryGetValue(Key, out T? cached);
            return cached;
        }

        return default;
    }

    public T GetOrCreate<T>(Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (TryGetValue<T>(out var cached))
            return cached!;

        var value = factory();
        Set(value);
        return value;
    }

    public async Task<T> GetOrCreateAsync<T>(
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var cached = await GetValueAsync<T>();
        if (cached is not null)
            return cached;

        var value = await factory(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await SetAsync(value);
        return value;
    }

    public void Set<T>(T value)
    {
        switch (Type)
        {
            case CacheType.MemoryCache when MemoryCache != null:

                var options = new MemoryCacheEntryOptions();

                if (Sliding)
                    options.SetSlidingExpiration(Duration);
                else
                    options.SetAbsoluteExpiration(Duration);

                MemoryCache.Set(Key, value, options);

                break;

            case CacheType.DistributedCache when DistributedCache != null:

                var bytes = JsonSerializer.SerializeToUtf8Bytes(
                    value,
                    JsonOptions);

                var distOptions = new DistributedCacheEntryOptions();

                if (Sliding)
                    distOptions.SetSlidingExpiration(Duration);
                else
                    distOptions.SetAbsoluteExpiration(Duration);

                DistributedCache.Set(
                    Key.ToString()!,
                    bytes,
                    distOptions);

                break;
        }
    }

    public async Task SetAsync<T>(T value)
    {
        if (Type == CacheType.DistributedCache &&
            DistributedCache != null)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(
                value,
                JsonOptions);

            var distOptions = new DistributedCacheEntryOptions();

            if (Sliding)
                distOptions.SetSlidingExpiration(Duration);
            else
                distOptions.SetAbsoluteExpiration(Duration);

            await DistributedCache.SetAsync(
                Key.ToString()!,
                bytes,
                distOptions);

            return;
        }

        if (Type == CacheType.MemoryCache &&
            MemoryCache != null)
        {
            Set(value);
        }
    }

    public void Remove()
    {
        switch (Type)
        {
            case CacheType.MemoryCache when MemoryCache != null:
                MemoryCache.Remove(Key);
                break;

            case CacheType.DistributedCache when DistributedCache != null:
                DistributedCache.Remove(Key.ToString()!);
                break;
        }
    }

    public async Task RemoveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Type == CacheType.DistributedCache &&
            DistributedCache != null)
        {
            await DistributedCache.RemoveAsync(
                Key.ToString()!,
                cancellationToken);
            return;
        }

        if (Type == CacheType.MemoryCache &&
            MemoryCache != null)
        {
            MemoryCache.Remove(Key);
        }
    }
}
