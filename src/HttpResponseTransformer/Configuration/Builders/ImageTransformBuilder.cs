using System;
using System.Linq;

using HttpResponseTransformer.Configuration.Enums;
using HttpResponseTransformer.Transforms;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure an image transform
/// </summary>
public record ImageTransformBuilder(ImageTransformConfig Config)
{
    /// <summary>
    /// Indicate whether the image should be transformed based on a predicate
    /// </summary>
    /// <param name="predicate">A function that determines whether the image should be transformed.</param>
    public ImageTransformBuilder When(Func<ImageTransformInfo, bool> predicate)
    {
        return new(Config with
        {
            Predicate = predicate
        });
    }

    /// <summary>
    /// Resize the image
    /// </summary>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    /// <param name="mode">The resize mode.</param>
    public ImageTransformBuilder Resize(int width, int height, ImageResizeMode mode = ImageResizeMode.Fit)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Must be positive.");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Must be positive.");
        }
        return new(Config with
        {
            Operations = Config.Operations.Append(image => ImageResponseTransform.Resize(image, width, height, mode))
        });
    }

    /// <summary>
    /// Process the decoded image
    /// </summary>
    /// <param name="process">A function that returns the processed image.</param>
    public ImageTransformBuilder Process(Func<RawImage, RawImage> process)
    {
        return new(Config with
        {
            Operations = Config.Operations.Append(process)
        });
    }

    /// <summary>
    /// Strip metadata from the image
    /// </summary>
    /// <remarks>
    /// Transformed images are always encoded without metadata.
    /// </remarks>
    public ImageTransformBuilder StripMetadata()
    {
        return this;
    }

    /// <summary>
    /// Set the output format
    /// </summary>
    /// <param name="format">The output format.</param>
    public ImageTransformBuilder Format(ImageFormat format)
    {
        return new(Config with
        {
            Format = format
        });
    }

    /// <summary>
    /// Set the JPEG quality
    /// </summary>
    /// <param name="quality">The quality, from 1 to 100.</param>
    public ImageTransformBuilder Quality(int quality)
    {
        if (quality < 1 || quality > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(quality), "Must be from 1 to 100.");
        }
        return new(Config with
        {
            Quality = quality
        });
    }
}
