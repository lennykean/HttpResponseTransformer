namespace HttpResponseTransformer.Configuration.Enums;

/// <summary>
/// Specifies how an image is resized
/// </summary>
public enum ImageResizeMode
{
    /// <summary>
    /// Scale down to fit within the bounds, preserving aspect ratio
    /// </summary>
    Fit,
    /// <summary>
    /// Fill the bounds, preserving aspect ratio and cropping the overflow
    /// </summary>
    Fill,
    /// <summary>
    /// Stretch to the bounds, ignoring aspect ratio
    /// </summary>
    Stretch,
}
