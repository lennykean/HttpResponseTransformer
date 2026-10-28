using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Configuration.Enums;

using Microsoft.AspNetCore.Http;

using StbImageResizeSharp;

using StbImageSharp;

using StbImageWriteSharp;

using ColorComponents = StbImageSharp.ColorComponents;

namespace HttpResponseTransformer.Transforms;

internal class ImageResponseTransform(ImageTransformConfig config) : IResponseTransform
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public bool ShouldTransform(HttpContext context)
    {
        var accept = context.Request?.GetTypedHeaders().Accept;
        return
            accept?.Any() is not true ||
            accept?.Any(a => a.Type == "image") is true;
    }

    public void ExecuteTransform(HttpContext context, ref byte[] content)
    {
        var contentType = context.Response.GetTypedHeaders()?.ContentType;
        if (contentType?.Type != "image" || context.Response.Headers["content-encoding"].Any() is true)
        {
            return;
        }
        var isJpeg = content.AsSpan().StartsWith(JpegSignature);
        if (!isJpeg && !content.AsSpan().StartsWith(PngSignature))
        {
            return;
        }
        var orientation = 1;
        if (isJpeg && !TryReadJpegOrientation(content, out orientation))
        {
            return;
        }
        using var header = new MemoryStream(content, writable: false);
        var info = ImageInfo.FromStream(header);
        if (info is null)
        {
            return;
        }
        var width = orientation >= 5 ? info.Value.Height : info.Value.Width;
        var height = orientation >= 5 ? info.Value.Width : info.Value.Height;
        if (!config.Predicate(new ImageTransformInfo(context, width, height, content.Length)))
        {
            return;
        }
        ImageResult decoded;
        try
        {
            decoded = ImageResult.FromMemory(content, ColorComponents.RedGreenBlueAlpha);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        var image = new RawImage(decoded.Width, decoded.Height, decoded.Data);
        foreach (var operation in config.Operations.Prepend(current => Orient(current, orientation)))
        {
            image = operation(image);
            if (image.Width <= 0 || image.Height <= 0 || image.Pixels.Length != (long)image.Width * image.Height * 4)
            {
                throw new InvalidOperationException("Processed image must have a positive size and Width * Height * 4 bytes of RGBA.");
            }
        }
        var format = config.Format ?? (isJpeg ? ImageFormat.Jpeg : ImageFormat.Png);
        using var output = new MemoryStream();
        string mimeType;
        if (format == ImageFormat.Jpeg)
        {
            new ImageWriter().WriteJpg(image.Pixels, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, output, config.Quality);
            mimeType = "image/jpeg";
        }
        else
        {
            new ImageWriter().WritePng(image.Pixels, image.Width, image.Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, output);
            mimeType = "image/png";
        }
        content = output.ToArray();
        context.Response.ContentType = mimeType;
    }

    internal static RawImage Resize(RawImage image, int width, int height, ImageResizeMode mode)
    {
        if (mode == ImageResizeMode.Fit)
        {
            var scale = Math.Min((double)width / image.Width, (double)height / image.Height);
            if (scale >= 1)
            {
                return image;
            }
            width = Math.Max(1, (int)Math.Round(image.Width * scale));
            height = Math.Max(1, (int)Math.Round(image.Height * scale));
        }
        if (mode == ImageResizeMode.Fill)
        {
            image = CropCenter(image, (double)width / height);
        }
        var pixels = new byte[width * height * 4];
        StbImageResize.stbir_resize_uint8_srgb(image.Pixels, image.Width, image.Height, image.Width * 4, pixels, width, height, width * 4, 4, 3, 0);
        return new RawImage(width, height, pixels);
    }

    private static RawImage CropCenter(RawImage image, double aspectRatio)
    {
        var width = Math.Min(image.Width, Math.Max(1, (int)Math.Round(image.Height * aspectRatio)));
        var height = Math.Min(image.Height, Math.Max(1, (int)Math.Round(image.Width / aspectRatio)));
        var left = (image.Width - width) / 2;
        var top = (image.Height - height) / 2;
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            Array.Copy(image.Pixels, ((top + y) * image.Width + left) * 4, pixels, y * width * 4, width * 4);
        }
        return new RawImage(width, height, pixels);
    }

    private static RawImage Orient(RawImage image, int orientation)
    {
        if (orientation == 1)
        {
            return image;
        }
        var width = orientation >= 5 ? image.Height : image.Width;
        var height = orientation >= 5 ? image.Width : image.Height;
        var pixels = new byte[image.Pixels.Length];
        var source = MemoryMarshal.Cast<byte, uint>(image.Pixels);
        var target = MemoryMarshal.Cast<byte, uint>(pixels);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (sourceX, sourceY) = orientation switch
                {
                    2 => (image.Width - 1 - x, y),
                    3 => (image.Width - 1 - x, image.Height - 1 - y),
                    4 => (x, image.Height - 1 - y),
                    5 => (y, x),
                    6 => (y, image.Height - 1 - x),
                    7 => (image.Width - 1 - y, image.Height - 1 - x),
                    _ => (image.Width - 1 - y, x),
                };
                target[y * width + x] = source[sourceY * image.Width + sourceX];
            }
        }
        return new RawImage(width, height, pixels);
    }

    private static bool TryReadJpegOrientation(byte[] jpeg, out int orientation)
    {
        const byte StartOfScan = 0xDA;
        const byte App1 = 0xE1;

        orientation = 1;

        var position = 2;
        while (position + 4 <= jpeg.Length && jpeg[position] == 0xFF)
        {
            var marker = jpeg[position + 1];
            var payload = position + 4;
            var next = position + 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(position + 2));
            if (next > jpeg.Length)
            {
                return false;
            }
            if (marker == StartOfScan)
            {
                return true;
            }
            if (marker == App1 && payload + 14 <= next && jpeg.AsSpan(payload, 6).SequenceEqual("Exif\0\0"u8))
            {
                orientation = ReadTiffOrientation(jpeg, tiff: payload + 6, next);
            }
            position = next;
        }
        return false;
    }

    private static int ReadTiffOrientation(byte[] data, int tiff, int end)
    {
        const int OrientationTag = 0x0112;
        const int EntrySize = 12;

        var bigEndian = data[tiff] == 'M';
        var directory = tiff + ReadUInt32(data, tiff + 4, bigEndian);
        if (directory + 2 > end)
        {
            return 1;
        }

        var entries = ReadUInt16(data, (int)directory, bigEndian);
        for (var entry = (int)directory + 2; entry + EntrySize <= end && entry < directory + 2 + entries * EntrySize; entry += EntrySize)
        {
            if (ReadUInt16(data, entry, bigEndian) == OrientationTag)
            {
                var orientation = ReadUInt16(data, entry + 8, bigEndian);
                return orientation is >= 1 and <= 8 ? orientation : 1;
            }
        }
        return 1;
    }

    private static int ReadUInt16(byte[] data, int offset, bool bigEndian)
    {
        var bytes = data.AsSpan(offset, 2);
        return bigEndian ? BinaryPrimitives.ReadUInt16BigEndian(bytes) : BinaryPrimitives.ReadUInt16LittleEndian(bytes);
    }

    private static long ReadUInt32(byte[] data, int offset, bool bigEndian)
    {
        var bytes = data.AsSpan(offset, 4);
        return bigEndian ? BinaryPrimitives.ReadUInt32BigEndian(bytes) : BinaryPrimitives.ReadUInt32LittleEndian(bytes);
    }
}
