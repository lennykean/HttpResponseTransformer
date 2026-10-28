using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace HttpResponseTransformer.Middleware;

internal abstract class ResponseTransformerMiddleware(
    IEnumerable<IResponseTransform> transforms,
    ResponseTransformerConfig? config = default)
{
    protected async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var activeTransforms = transforms?.Where(t => t.ShouldTransform(context)).ToList();
        if (activeTransforms?.Any() is not true)
        {
            await next(context);
            return;
        }
        var acceptEncoding = context.Request.Headers[HeaderNames.AcceptEncoding];
        var contentStream = context.Response.Body;
        try
        {
            using var buffer = new MemoryStream();

            if (config?.AllowResponseCompression is not true)
            {
                context.Request.Headers[HeaderNames.AcceptEncoding] = default;
            }
            context.Response.Body = buffer;

            await next(context);

            var content = buffer.ToArray();
            if (content.Length == 0)
            {
                return;
            }
            string? cacheKey = null;
            var caching = config?.Caching;
            if (caching is not null && !context.Response.Headers.ContainsKey(HeaderNames.ContentEncoding) && caching.Predicate(context))
            {
                var activeMask = string.Concat(transforms.Select(transform => activeTransforms.Contains(transform) ? '1' : '0'));
                cacheKey = $"{activeMask} {caching.Key(context)}";
            }
            var cache = cacheKey is null ? null : context.RequestServices.GetRequiredService<IMemoryCache>();
            if (cache is not null && cache.TryGetValue((this, cacheKey), out (byte[] Content, string? ContentType) cached))
            {
                context.Response.ContentType = cached.ContentType;
                context.Response.ContentLength = cached.Content.Length;

                using var cachedContent = new MemoryStream(cached.Content, writable: false);
                await cachedContent.CopyToAsync(contentStream);
                await contentStream.FlushAsync();
                return;
            }
            foreach (var transform in activeTransforms)
            {
                transform.ExecuteTransform(context, ref content);
            }
            if (content is null)
            {
                return;
            }
            if (cache is not null && caching is not null)
            {
                using var entry = cache.CreateEntry((this, cacheKey));
                entry.SetOptions(caching.Options);
                entry.Size ??= content.Length;
                entry.Value = (content, context.Response.ContentType);
            }
            context.Response.ContentLength = content.Length;

            await contentStream.WriteAsync(content);
            await contentStream.FlushAsync();
        }
        finally
        {
            if (config?.AllowResponseCompression is not true)
            {
                context.Request.Headers[HeaderNames.AcceptEncoding] = acceptEncoding;
            }
            context.Response.Body = contentStream;
        }
    }
}

internal class ScopedResponseTransformerMiddleware(
    RequestDelegate next,
    IEnumerable<IResponseTransform> transforms,
    ResponseTransformerConfig? config = default) : ResponseTransformerMiddleware(transforms, config)
{
    public Task InvokeAsync(HttpContext context)
    {
        return InvokeAsync(context, next);
    }
}

internal class GlobalResponseTransformerMiddleware(
    IEnumerable<IResponseTransform> transforms,
    ResponseTransformerConfig? config = default) : ResponseTransformerMiddleware(transforms, config), IMiddleware
{
    Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
    {
        return InvokeAsync(context, next);
    }
}
