using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

/// <summary>
/// Represents a transform that can be applied to an HTTP response
/// </summary>
public interface IResponseTransform
{
    /// <summary>
    /// Determine whether the response should be transformed for the given HTTP context
    /// </summary>
    /// <param name="context">The HTTP context to evaluate.</param>
    bool ShouldTransform(HttpContext context);

    /// <summary>
    /// Execute the response transform
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="content">The content to transform.</param>
    void ExecuteTransform(HttpContext context, ref byte[] content);
}
