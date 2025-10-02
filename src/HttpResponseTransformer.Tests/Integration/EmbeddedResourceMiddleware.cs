using System.Net;
using System.Threading.Tasks;

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
    private IEmbeddedResourceManager _resourceManager = null!;

    [SetUp]
    public void SetUp()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(_ => _);
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not Found");
                    });
                });
            });

        _host = hostBuilder.Start();
        _server = _host.GetTestServer();
        _resourceManager = _server.Services.GetRequiredService<IEmbeddedResourceManager>();
    }

    [TearDown]
    public void TearDown()
    {
        _host?.Dispose();
    }

    [Test]
    public async Task Get_WithValidEmbeddedResource_ReturnsResource()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = $"{assembly.GetName().Name}.resources.a-resource.txt";
        _resourceManager.TryAddResource(assembly, resourceName, "text/plain", out var namespaceKey, out var resourceKey);

        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync($"/_/{namespaceKey}/{resourceKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/plain"));

        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Does.Contain("Help! I'm embedded in this assembly!"));
    }

    [Test]
    public async Task Get_WithInvalidNamespaceKey_ReturnsNotFound()
    {
        // Arrange
        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync("/_/invalid-namespace/invalid-resource.txt");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Get_WithInvalidPath_ReturnsNotFound()
    {
        // Arrange
        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync("/_/only-one-segment");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Get_WithoutEmbeddedResourcePrefix_PassesThrough()
    {
        // Arrange
        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync("/regular/path");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("Not Found"));
    }

    [Test]
    public async Task Get_WithDefaultContentType_ReturnsOctetStream()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = $"{assembly.GetName().Name}.resources.a-resource.txt";
        _resourceManager.TryAddResource(assembly, resourceName, null, out var namespaceKey, out var resourceKey);

        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync($"/_/{namespaceKey}/{resourceKey}");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/octet-stream"));
    }
}

