using System.Linq;
using System.Net;
using System.Threading.Tasks;

using HtmlAgilityPack;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Integration;

[TestFixture]
public class EmbeddedResourceMiddlewareTests
{
    private IHost _host = null!;
    private TestServer _server = null!;

    [SetUp]
    public void SetUp()
    {
        var assembly = GetType().Assembly;

        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformDocument(document => document
                            .When(ctx => true)
                            .InjectScript(script => script
                                .FromEmbeddedResource($"{assembly.GetName().Name}.resources.a-resource.txt", assembly)
                                .At("//body")))))
                .Configure(app => app
                    .Run(async context =>
                    {
                        context.Response.ContentType = "text/html";
                        context.Response.StatusCode = 200;

                        await context.Response.WriteAsync("<html><head></head><body><h1>Test Page</h1></body></html>");
                    })));

        _host = hostBuilder.Start();
        _server = _host.GetTestServer();
    }

    [TearDown]
    public void TearDown()
    {
        _host?.Dispose();
    }


    [Test]
    public async Task EmbeddedResourceMiddleware_ServesEmbeddedResource()
    {
        // Arrange
        var client = _server.CreateClient();
        var response = await client.GetAsync("/");
        var htmlContent = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();

        doc.LoadHtml(htmlContent);

        var scriptTag = doc.DocumentNode.SelectSingleNode("//script");
        var scriptSrc = scriptTag.GetAttributeValue("src", null);

        // Act
        var resourceResponse = await client.GetAsync(scriptSrc);

        // Assert
        var resourceContent = await resourceResponse.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(resourceResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(resourceContent, Does.Contain("Help! I'm embedded in this assembly!"));
        });
    }
}

