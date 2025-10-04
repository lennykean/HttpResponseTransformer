using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
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
            foreach (var transform in activeTransforms)
            {
                transform.ExecuteTransform(context, ref content);
            }
            if (content is null)
            {
                return;
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
