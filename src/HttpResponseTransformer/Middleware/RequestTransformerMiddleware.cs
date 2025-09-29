using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Middleware;

internal class RequestTransformerMiddleware(
    RequestDelegate next,
    IEnumerable<IResponseTransform> transforms)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var activeTransforms = transforms.Where(t => t.ShouldTransform(context)).ToList();
        if (activeTransforms.Count == 0)
        {
            await next(context);
            return;
        }
        var contentStream = context.Response.Body;
        try
        {
            using var buffer = new MemoryStream();

            context.Response.Body = buffer;
            context.Request.Headers["accept-encoding"] = "identity";

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
            context.Response.Body = contentStream;
        }
    }
}
