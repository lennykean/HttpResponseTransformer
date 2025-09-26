using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Middleware;

internal class RequestTransformerMiddleware(IEnumerable<IResponseTransform> transforms) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var activeTransforms = transforms.Where(t => t.ShouldTransform(context)).ToList();
        if (activeTransforms.Count == 0)
        {
            await next(context);
            return;
        }

        var responseStream = context.Response.Body;

        using var buffer = new MemoryStream();
        try
        {
            context.Response.Body = buffer;

            await next(context);

            var content = buffer.ToArray();

            foreach (var transform in activeTransforms)
            {
                transform.ExecuteTransform(context, ref content);
            }
            if (content is null)
            {
                return;
            }
            buffer.SetLength(0);
            buffer.Write(content);
            buffer.Seek(0, SeekOrigin.Begin);

            await buffer.CopyToAsync(responseStream);
        }
        finally
        {
            context.Response.Body = responseStream;
        }
    }

}
