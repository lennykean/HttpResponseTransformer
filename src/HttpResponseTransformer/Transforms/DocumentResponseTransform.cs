using System;
using System.IO;
using System.Linq;
using System.Text;

using HtmlAgilityPack;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

/// <summary>
/// A transform that operates on HTML HTTP responses
/// </summary>
public abstract class DocumentResponseTransform : TextResponseTransform
{
    /// <inheritdoc>
    public override bool ShouldTransform(HttpContext context)
    {
        var accept = context.Request?.GetTypedHeaders().Accept;
        return
            base.ShouldTransform(context) && (
            accept?.Any() is not true ||
            accept?.Any(a => a.SubType == "html") is true);
    }

    /// <inheritdoc>
    public sealed override void ExecuteTransform(HttpContext context, ref string content)
    {
        var contentType = context.Response.GetTypedHeaders()?.ContentType;
        if (contentType?.Type != "text" || contentType?.SubType != "html")
        {
            return;
        }
        var document = new HtmlDocument();
        document.LoadHtml(content);

        ExecuteTransform(context, ref document);

        var documentString = new StringBuilder();
        using (var writer = new StringWriter(documentString))
        {
            document.Save(writer);
        }
        content = documentString.ToString();
    }

    /// <summary>
    /// Execute the response HTML transform
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="document">The HTML document to transform.</param>
    public virtual void ExecuteTransform(HttpContext context, ref HtmlDocument document)
    {
    }
}
