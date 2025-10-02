using System.Net;
using System.Threading.Tasks;

using HtmlAgilityPack;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Moq;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Integration;

[TestFixture]
public class ResponseTransformerMiddlewareTests
{
    [Test]
    public async Task Transform_WithTextTransform_TransformsResponse()
    {
        // Arrange
        var mockTransform = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns((HttpContext ctx) => ctx.Request.Path == "/transform");
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = content.ToUpperInvariant();
            });

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                    services.AddSingleton<IResponseTransform>(mockTransform.Object);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("hello world");
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/plain");

        // Act
        var response = await client.GetAsync("/transform");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("HELLO WORLD"));
    }

    [Test]
    public async Task Transform_WithDocumentTransform_TransformsHtml()
    {
        // Arrange
        var mockTransform = new Mock<DocumentResponseTransform> { CallBase = true };
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<HtmlDocument>.IsAny))
            .Callback((HttpContext ctx, ref HtmlDocument doc) =>
            {
                var h1 = doc.DocumentNode.SelectSingleNode("//h1");
                if (h1 != null)
                {
                    var blink = doc.CreateElement("blink");
                    blink.InnerHtml = h1.InnerHtml;
                    h1.InnerHtml = "";
                    h1.AppendChild(blink);
                }
            });

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                    services.AddSingleton<IResponseTransform>(mockTransform.Object);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("<html><body><h1>Welcome</h1></body></html>");
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/html");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("<blink>Welcome</blink>"));
    }

    [Test]
    public async Task Transform_WithContentInjection_InjectsContent()
    {
        // Arrange
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(builder => builder
                        .TransformDocument(injectionBuilder => injectionBuilder
                            .When(ctx => true)
                            .InjectScript(scriptBuilder => scriptBuilder
                                .FromUrl("https://code.jquery.com/jquery.min.js")
                                .At("//body"))));
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("<html><head></head><body><h1>My Page</h1></body></html>");
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/html");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("<script src=\"https://code.jquery.com/jquery.min.js\">"));
    }

    [Test]
    public async Task Transform_WithNoMatchingTransform_PassesThrough()
    {
        // Arrange
        var mockTransform = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns(false);
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = "SHOULD NOT SEE THIS";
            });

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                    services.AddSingleton<IResponseTransform>(mockTransform.Object);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("original content");
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/plain");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("original content"));
    }

    [Test]
    public async Task Transform_WithEmptyResponse_DoesNotTransform()
    {
        // Arrange
        var mockTransform = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns(true);
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = "SHOULD NOT SEE THIS";
            });

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                    services.AddSingleton<IResponseTransform>(mockTransform.Object);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        context.Response.StatusCode = 204; // No Content
                        await context.Response.CompleteAsync();
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/plain");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.Empty);
    }

    [Test]
    public async Task Transform_WithMultipleTransforms_AppliesInOrder()
    {
        // Arrange
        var mockTransform1 = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform1
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns(true);
        mockTransform1
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = content.Replace("world", "universe");
            });

        var mockTransform2 = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform2
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns(true);
        mockTransform2
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = content.ToUpperInvariant();
            });

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                    services.AddSingleton<IResponseTransform>(mockTransform1.Object);
                    services.AddSingleton<IResponseTransform>(mockTransform2.Object);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync("hello world");
                    });
                });
            });

        using var host = hostBuilder.Start();
        var server = host.GetTestServer();
        var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("Accept", "text/plain");

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("HELLO UNIVERSE"));
    }
}

