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

    [SetUp]
    public void SetUp()
    {
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddResponseTransformer(builder => builder
                        .TransformDocument(document => document
                            .InjectHtml(html => html
                                .WithContent("<body>Never Gonna Give You Up!</body>")
                                .Replace())));
                });
                webHost.Configure(app =>
                {
                    app.Run(async context =>
                    {
                        context.Response.ContentType = "text/html";
                        context.Response.StatusCode = 200;

                        await context.Response.WriteAsync("<html><body>You should not see this</body></html>");
                    });
                });
            });

        _host = hostBuilder.Start();
        _server = _host.GetTestServer();
    }

    [TearDown]
    public void TearDown()
    {
        _host?.Dispose();
    }


    [Test]
    public async Task Get_Document()
    {
        // Arrange
        var client = _server.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(content, Is.EqualTo("<html><body>Never Gonna Give You Up!</body></html>"));
        });
    }
}

