namespace HttpResponseTransformer.Transforms;

/// <summary>
/// A decoded image with 8-bit RGBA pixels
/// </summary>
public record RawImage(int Width, int Height, byte[] Pixels);
