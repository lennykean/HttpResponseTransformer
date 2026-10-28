using System;
using System.IO;
using System.Linq;

using HttpResponseTransformer.Configuration.Builders;
using HttpResponseTransformer.Configuration.Enums;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using NUnit.Framework;

using StbImageSharp;

using StbImageWriteSharp;

using ColorComponents = StbImageSharp.ColorComponents;
using WriterColorComponents = StbImageWriteSharp.ColorComponents;

namespace HttpResponseTransformer.Tests.Unit;

[TestFixture]
public class ImageResponseTransformTests
{
    private static readonly byte[] Red = [255, 0, 0, 255];
    private static readonly byte[] Blue = [0, 0, 255, 255];
    private static readonly byte[] Lime = [0, 255, 0, 255];
    private static readonly byte[] Yellow = [255, 255, 0, 255];

    private byte[] _png = null!;
    private byte[] _jpeg = null!;

    [SetUp]
    public void SetUp()
    {
        byte[][] bands = [Lime, Red, Red, Blue];
        var pixels = Enumerable.Range(0, 400 * 200).SelectMany(i => bands[i % 400 / 100]).ToArray();

        using var png = new MemoryStream();
        new ImageWriter().WritePng(pixels, 400, 200, WriterColorComponents.RedGreenBlueAlpha, png);
        _png = png.ToArray();

        using var jpeg = new MemoryStream();
        new ImageWriter().WriteJpg(pixels, 400, 200, WriterColorComponents.RedGreenBlueAlpha, jpeg, 100);
        _jpeg = jpeg.ToArray();
    }

    [TestCase(null, true)]
    [TestCase("image/webp,*/*", true)]
    [TestCase("text/html", false)]
    public void ShouldTransform_ChecksAcceptHeader(string? accept, bool expected)
    {
        // Arrange
        var context = new DefaultHttpContext();
        if (accept is not null)
        {
            context.Request.Headers[HeaderNames.Accept] = accept;
        }
        var subject = new ImageResponseTransform(new());

        // Act
        var result = subject.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("png", "image/png", false)]
    [TestCase("png", "text/html", true)]
    [TestCase("gif", "image/gif", true)]
    [TestCase("truncated png", "image/png", true)]
    [TestCase("truncated jpeg", "image/jpeg", true)]
    public void ExecuteTransform_WithoutMatchingImage_DoesNotTransform(string input, string contentType, bool predicate)
    {
        // Arrange
        var original = input switch
        {
            "gif" => "GIF89a"u8.ToArray(),
            "truncated png" => _png[..(_png.Length / 2)],
            "truncated jpeg" => _jpeg[..10],
            _ => _png,
        };
        var content = original;
        var context = new DefaultHttpContext();
        context.Response.ContentType = contentType;
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new()).When(info => predicate).Resize(10, 10).Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(content, Is.SameAs(original));
    }

    [Test]
    public void When_ReceivesImageInfo()
    {
        // Arrange
        var content = _png;
        ImageTransformInfo? seen = null;
        var context = new DefaultHttpContext();
        context.Response.ContentType = "image/png";
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new())
            .When(info =>
            {
                seen = info;
                return true;
            })
            .Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(seen, Is.EqualTo(new ImageTransformInfo(context, 400, 200, _png.Length)));
    }

    [TestCase(100, 100, ImageResizeMode.Fit, 100, 50, "00FF00", "0000FF")]
    [TestCase(1000, 1000, ImageResizeMode.Fit, 400, 200, "00FF00", "0000FF")]
    [TestCase(100, 100, ImageResizeMode.Fill, 100, 100, "FF0000", "FF0000")]
    [TestCase(100, 100, ImageResizeMode.Stretch, 100, 100, "00FF00", "0000FF")]
    public void Resize_ScalesToBounds(int width, int height, ImageResizeMode mode, int expectedWidth, int expectedHeight, string expectedLeft, string expectedRight)
    {
        // Arrange
        var content = _png;
        var context = new DefaultHttpContext();
        context.Response.ContentType = "image/png";
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new()).Resize(width, height, mode).Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        var result = ImageResult.FromMemory(content, ColorComponents.RedGreenBlueAlpha);
        var left = (expectedHeight / 2 * expectedWidth + 1) * 4;
        var right = (expectedHeight / 2 * expectedWidth + expectedWidth - 2) * 4;
        Assert.That(result.Width, Is.EqualTo(expectedWidth));
        Assert.That(result.Height, Is.EqualTo(expectedHeight));
        Assert.That(result.Data[left..(left + 3)], Is.EqualTo(Convert.FromHexString(expectedLeft)).Within(40));
        Assert.That(result.Data[right..(right + 3)], Is.EqualTo(Convert.FromHexString(expectedRight)).Within(40));
    }

    [TestCase(1, 40, 20, "FF0000", "0000FF")]
    [TestCase(2, 40, 20, "0000FF", "FF0000")]
    [TestCase(3, 40, 20, "FFFF00", "00FF00")]
    [TestCase(4, 40, 20, "00FF00", "FFFF00")]
    [TestCase(5, 20, 40, "FF0000", "00FF00")]
    [TestCase(6, 20, 40, "00FF00", "FF0000")]
    [TestCase(7, 20, 40, "FFFF00", "0000FF")]
    [TestCase(8, 20, 40, "0000FF", "FFFF00")]
    public void ExecuteTransform_WithExifOrientation_OrientsAndStripsExif(int orientation, int width, int height, string expectedTopLeft, string expectedTopRight)
    {
        // Arrange
        var pixels = Enumerable.Range(0, 40 * 20).SelectMany(i => (i % 40 < 20, i / 40 < 10) switch
        {
            (true, true) => Red,
            (false, true) => Blue,
            (true, false) => Lime,
            _ => Yellow,
        }).ToArray();
        using var jpeg = new MemoryStream();
        new ImageWriter().WriteJpg(pixels, 40, 20, WriterColorComponents.RedGreenBlueAlpha, jpeg, 100);
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            .. "Exif\0\0"u8,
            .. "MM"u8, 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x01,
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, (byte)orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];
        byte[] content = [.. jpeg.ToArray()[..2], .. exif, .. jpeg.ToArray()[2..]];

        ImageTransformInfo? seen = null;
        var context = new DefaultHttpContext();
        context.Response.ContentType = "image/jpeg";
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new())
            .When(info =>
            {
                seen = info;
                return true;
            })
            .StripMetadata()
            .Format(ImageFormat.Png)
            .Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        var result = ImageResult.FromMemory(content, ColorComponents.RedGreenBlueAlpha);
        var topLeft = (4 * width + 4) * 4;
        var topRight = (4 * width + width - 5) * 4;
        Assert.That(seen?.Width, Is.EqualTo(width));
        Assert.That(seen?.Height, Is.EqualTo(height));
        Assert.That(result.Width, Is.EqualTo(width));
        Assert.That(result.Height, Is.EqualTo(height));
        Assert.That(result.Data[topLeft..(topLeft + 3)], Is.EqualTo(Convert.FromHexString(expectedTopLeft)).Within(40));
        Assert.That(result.Data[topRight..(topRight + 3)], Is.EqualTo(Convert.FromHexString(expectedTopRight)).Within(40));
        Assert.That(content.AsSpan().IndexOf("Exif\0\0"u8), Is.EqualTo(-1));
    }

    [TestCase(ImageFormat.Png, ImageFormat.Jpeg, "image/jpeg", "FFD8FF")]
    [TestCase(ImageFormat.Jpeg, ImageFormat.Png, "image/png", "89504E47")]
    [TestCase(ImageFormat.Jpeg, null, "image/jpeg", "FFD8FF")]
    [TestCase(ImageFormat.Png, null, "image/png", "89504E47")]
    public void Format_EncodesOutput(ImageFormat input, ImageFormat? format, string expectedContentType, string expectedSignature)
    {
        // Arrange
        var content = input == ImageFormat.Jpeg ? _jpeg : _png;
        var context = new DefaultHttpContext();
        context.Response.ContentType = input == ImageFormat.Jpeg ? "image/jpeg" : "image/png";
        var builder = new ImageTransformBuilder(new());
        if (format is not null)
        {
            builder = builder.Format(format.Value);
        }
        var subject = new ImageResponseTransform(builder.Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        var signature = Convert.FromHexString(expectedSignature);
        Assert.That(context.Response.ContentType, Is.EqualTo(expectedContentType));
        Assert.That(content[..signature.Length], Is.EqualTo(signature));
    }

    [TestCase(ImageFormat.Jpeg, true)]
    [TestCase(ImageFormat.Png, false)]
    public void Quality_AffectsJpegOnly(ImageFormat format, bool expectSmaller)
    {
        // Arrange
        var low = _png;
        var high = _png;
        var context = new DefaultHttpContext();
        var lowQuality = new ImageResponseTransform(new ImageTransformBuilder(new()).Format(format).Quality(10).Config);
        var highQuality = new ImageResponseTransform(new ImageTransformBuilder(new()).Format(format).Quality(100).Config);

        // Act
        context.Response.ContentType = "image/png";
        lowQuality.ExecuteTransform(context, ref low);
        context.Response.ContentType = "image/png";
        highQuality.ExecuteTransform(context, ref high);

        // Assert
        Assert.That(low.Length < high.Length, Is.EqualTo(expectSmaller));
    }

    [Test]
    public void Process_AfterResize_ReceivesResizedImage()
    {
        // Arrange
        var content = _png;
        RawImage? seen = null;
        var context = new DefaultHttpContext();
        context.Response.ContentType = "image/png";
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new())
            .Resize(100, 100)
            .Process(image =>
            {
                seen = image;
                return new RawImage(1, 1, [.. Blue]);
            })
            .Config);

        // Act
        subject.ExecuteTransform(context, ref content);

        // Assert
        var result = ImageResult.FromMemory(content, ColorComponents.RedGreenBlueAlpha);
        Assert.That(seen?.Width, Is.EqualTo(100));
        Assert.That(seen?.Height, Is.EqualTo(50));
        Assert.That(seen?.Pixels, Has.Length.EqualTo(100 * 50 * 4));
        Assert.That(result.Data, Is.EqualTo(Blue));
    }

    [TestCase(1600, 200, 400 * 200 * 4)]
    [TestCase(-1, -1, 4)]
    [TestCase(65536, 16384, 0)]
    public void Process_WithInvalidImage_Throws(int width, int height, int pixelBytes)
    {
        // Arrange
        var content = _png;
        var context = new DefaultHttpContext();
        context.Response.ContentType = "image/png";
        var subject = new ImageResponseTransform(new ImageTransformBuilder(new())
            .Process(image => new RawImage(width, height, new byte[pixelBytes]))
            .Resize(10, 10, ImageResizeMode.Stretch)
            .Config);

        // Act & Assert
        Assert.That(() => subject.ExecuteTransform(context, ref content), Throws.InvalidOperationException);
    }

    [TestCase(0, 10)]
    [TestCase(10, -1)]
    public void Resize_WithNonPositiveSize_Throws(int width, int height)
    {
        // Arrange
        var builder = new ImageTransformBuilder(new());

        // Act & Assert
        Assert.That(() => builder.Resize(width, height), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [TestCase(0)]
    [TestCase(101)]
    public void Quality_WithOutOfRangeValue_Throws(int quality)
    {
        // Arrange
        var builder = new ImageTransformBuilder(new());

        // Act & Assert
        Assert.That(() => builder.Quality(quality), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }
}
