using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Middleware;

internal class ResponseTransformerMiddleware(IEnumerable<IResponseTransform> transforms, ResponseTransformerConfig? config) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var activeTransforms = transforms.Where(t => t.ShouldTransform(context)).ToList();
        if (activeTransforms.Count == 0)
        {
            await next(context);
            return;
        }
        var acceptEncoding = context.Request.Headers["accept-encoding"];
        var contentStream = context.Response.Body;
        try
        {
            using var buffer = new MemoryStream();

            if (config?.AllowResponseCompression is not true)
            {
                context.Request.Headers["accept-encoding"] = default;
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
                context.Request.Headers["accept-encoding"] = acceptEncoding;
            }
            context.Response.Body = contentStream;
        }
    }
}
