using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

/// <summary>
/// Information about the image being transformed
/// </summary>
public record ImageTransformInfo(HttpContext HttpContext, int Width, int Height, int EncodedBytes);
