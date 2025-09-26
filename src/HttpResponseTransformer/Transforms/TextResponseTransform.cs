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
    /// </inheritdoc>
    public virtual bool ShouldTransform(HttpContext context)
    {
        return (
            context.Request.Headers["accept"].Count == 0 ||
            context.Request.Headers["accept"].Any(a => a?.Contains("text/", StringComparison.OrdinalIgnoreCase) is true) is true);
    }

    /// <inheritdoc>
    public void ExecuteTransform(HttpContext context, ref byte[] content)
    {
        if (context.Response.ContentType?.Contains("text/", StringComparison.OrdinalIgnoreCase) is not true)
        {
            return;
        }

        var contentString = Encoding.UTF8.GetString(content);

        ExecuteTransform(context, ref contentString);

        content = Encoding.UTF8.GetBytes(contentString);
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
