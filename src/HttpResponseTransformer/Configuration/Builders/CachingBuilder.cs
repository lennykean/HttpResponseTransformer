using System;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure caching of transformed output
/// </summary>
public record CachingBuilder(CachingConfig Config)
{
    /// <summary>
    /// Indicate whether the output should be cached based on a predicate
    /// </summary>
    /// <param name="predicate">A function that determines whether the output should be cached.</param>
    public CachingBuilder When(Func<HttpContext, bool> predicate)
    {
        return new(Config with
        {
            Predicate = predicate
        });
    }

    /// <summary>
    /// Set the cache key
    /// </summary>
    /// <param name="key">A function that returns the cache key.</param>
    public CachingBuilder Key(Func<HttpContext, string> key)
    {
        return new(Config with
        {
            Key = key
        });
    }

    /// <summary>
    /// Set the cache entry options
    /// </summary>
    /// <param name="options">The options for each cache entry.</param>
    public CachingBuilder Options(MemoryCacheEntryOptions options)
    {
        return new(Config with
        {
            Options = options
        });
    }
}
