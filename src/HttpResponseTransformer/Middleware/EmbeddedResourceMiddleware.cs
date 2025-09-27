using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Middleware;

internal class EmbeddedResourceMiddleware(IEmbeddedResourceManager embeddedResourceManager) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var path = context.Request.Path.Value?.TrimStart('/');
        if (string.IsNullOrEmpty(path))
        {
            await next(context);
            return;
        }
        var parts = path.Split('/');
        if (parts.Length != 2)
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
