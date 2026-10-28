using System.IO;
using System.Threading.Tasks;

using HttpResponseTransformer.Configuration.Enums;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

using StbImageSharp;

using StbImageWriteSharp;

namespace HttpResponseTransformer.Tests.Integration;

[TestFixture]
public class ImageTransformTests
{
    [Test]
    public async Task TransformImage_WithCaching_ServesConvertedImage()
    {
        // Arrange
        using var source = new MemoryStream();
        new ImageWriter().WritePng(new byte[400 * 200 * 4], 400, 200, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, source);
        var png = source.ToArray();
        var transforms = 0;

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformImage(image => image
                            .When(info =>
                            {
                                transforms++;
                                return info.Width > 100;
                            })
                            .Resize(100, 100)
                            .Format(ImageFormat.Jpeg))
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        context.Response.ContentType = "image/png";
                        await context.Response.Body.WriteAsync(png);
                    })))
            .Start();

        var client = host.GetTestClient();
        client.DefaultRequestHeaders.Add("Accept", "image/webp,image/*");

        // Act
        await client.GetAsync("/image.png");
        var response = await client.GetAsync("/image.png");

        // Assert
        var content = await response.Content.ReadAsByteArrayAsync();
        var info = ImageInfo.FromStream(new MemoryStream(content));
        Assert.Multiple(() =>
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("image/jpeg"));
            Assert.That(response.Content.Headers.ContentLength, Is.EqualTo(content.Length));
            Assert.That(info?.Width, Is.EqualTo(100));
            Assert.That(info?.Height, Is.EqualTo(50));
            Assert.That(transforms, Is.EqualTo(1));
        });
    }
}
