using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Middleware;

internal class EmbeddedResourceMiddleware(RequestDelegate next, IEmbeddedResourceManager embeddedResourceManager)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/_", out var path))
        {
            await next(context);
            return;
        }
        var parts = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts?.Length != 2)
        {
            await next(context);
            return;
        }
        var namespaceKey = parts[0];
        var resourceKey = parts[1];

        if (!embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var data, out var contentType))
        {
            await next(context);
            return;
        }
        context.Response.ContentType = contentType ?? "application/octet-stream";

        await context.Response.Body.WriteAsync(data);
    }
}
