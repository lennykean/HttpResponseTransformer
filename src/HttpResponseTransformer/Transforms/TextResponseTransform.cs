using System;
using System.Linq;
using System.Text;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

/// <summary>
/// A transform that operates on text-based HTTP responses
/// </summary>
public class TextResponseTransform : IResponseTransform
{
    /// <inheritdoc/>
    public virtual bool ShouldTransform(HttpContext context)
    {
        return context.Request?.GetTypedHeaders().Accept?.Any(a => a.Type == "text") is true;
    }

    /// <inheritdoc>
    public void ExecuteTransform(HttpContext context, ref byte[] content)
    {
        var contentType = context.Response.GetTypedHeaders()?.ContentType;
        if (contentType?.Type != "text")
        {
            return;
        }
        if (context.Response.Headers["content-encoding"].Any() is true)
        {
            return;
        }
        var encoding = contentType?.Encoding ?? Encoding.Default;
        var contentString = encoding.GetString(content);

        ExecuteTransform(context, ref contentString);

        content = encoding.GetBytes(contentString);
    }

    /// <summary>
    /// Execute the response text transform
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="content">The string content to transform.</param>
    public virtual void ExecuteTransform(HttpContext context, ref string content)
    {
    }
}
