using System;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;

namespace HttpResponseTransformer.Configuration;

public record CachingConfig
{
    internal Func<HttpContext, bool> Predicate { get; init; } = context =>
        HttpMethods.IsGet(context.Request.Method) &&
        context.Response.StatusCode == StatusCodes.Status200OK &&
        context.User.Identity?.IsAuthenticated is not true;

    internal Func<HttpContext, string> Key { get; init; } = context =>
        $"{context.Request.GetEncodedUrl()} {context.Request.Headers[HeaderNames.Accept]}";

    internal MemoryCacheEntryOptions Options { get; init; } = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) };
}
