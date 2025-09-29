using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

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
        using var buffer = new MemoryStream();

        var responseBody = context.Features.Get<IHttpResponseBodyFeature>();

        Debug.Assert(responseBody is not null);
        try
        {
            context.Features.Set(new HttpResponseBodyFeature(buffer, responseBody));
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
            await responseBody.Stream.WriteAsync(content);
            await responseBody.CompleteAsync();
        }
        finally
        {
            context.Features.Set(responseBody);
        }
    }

    private class HttpResponseBodyFeature(MemoryStream buffer, IHttpResponseBodyFeature inner) : IHttpResponseBodyFeature
    {
        public Stream Stream => buffer;

        public PipeWriter Writer => PipeWriter.Create(buffer);

        public Task CompleteAsync() => Task.CompletedTask;

        public void DisableBuffering()
        {
            inner.DisableBuffering();
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
        {
            return SendFileFallback.SendFileAsync(buffer, path, offset, count, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return inner.StartAsync(cancellationToken);
        }
    }
}
