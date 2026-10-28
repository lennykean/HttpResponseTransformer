using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

using HttpResponseTransformer.Configuration.Builders;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;

using Moq;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Integration;

[TestFixture]
public class TransformCachingTests
{
    private static readonly TestCaseData[] CustomCaching =
    [
        new TestCaseData(new Func<CachingBuilder, CachingBuilder>(cache => cache.Key(ctx => "shared")), "POST", null, "<P>HELLO WORLD 2</P>")
            .SetName("WithCaching_WithCustomKey_KeepsDefaultFilter"),
        new TestCaseData(new Func<CachingBuilder, CachingBuilder>(cache => cache.When(ctx => true)), "POST", null, "<P>HELLO WORLD 1</P>")
            .SetName("WithCaching_WithCustomFilter_ReplacesDefaultFilter"),
        new TestCaseData(new Func<CachingBuilder, CachingBuilder>(cache => cache.When(ctx => true)), "GET", "gzip", "<p>hello world 2</p>")
            .SetName("WithCaching_WithCompressedResponse_DoesNotCache"),
    ];

    private Mock<TextResponseTransform> _mockTransform = null!;
    private int _requests;

    [SetUp]
    public void SetUp()
    {
        _requests = 0;
        _mockTransform = new Mock<TextResponseTransform> { CallBase = true };
        _mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = content.ToUpperInvariant();
            });
    }

    [TestCase("/page?id=1", "text/html", "<P>HELLO WORLD 1</P>")]
    [TestCase("/page?id=2", "text/html", "<P>HELLO WORLD 2</P>")]
    [TestCase("/page?id=1", "text/plain", "<P>HELLO WORLD 2</P>")]
    public async Task WithCaching_DefaultKey_UsesUrlAndAcceptHeader(string secondUrl, string secondAccept, string expected)
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();
        using var first = new HttpRequestMessage(HttpMethod.Get, "/page?id=1") { Headers = { { "Accept", "text/html" } } };
        using var second = new HttpRequestMessage(HttpMethod.Get, secondUrl) { Headers = { { "Accept", secondAccept } } };

        // Act
        await client.SendAsync(first);
        var response = await client.SendAsync(second);

        // Assert
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(expected));
        Assert.That(response.Content.Headers.ContentLength, Is.EqualTo(expected.Length));
        Assert.That(_requests, Is.EqualTo(2));
    }

    [TestCase("POST", 200, false)]
    [TestCase("GET", 404, false)]
    [TestCase("GET", 200, true)]
    public async Task WithCaching_DefaultFilter_SkipsUncacheableResponses(string method, int statusCode, bool authenticated)
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.StatusCode = statusCode;
                        if (authenticated)
                        {
                            context.User = new ClaimsPrincipal(new ClaimsIdentity("test"));
                        }
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/"));
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/"));

        // Assert
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo("<P>HELLO WORLD 2</P>"));
        _mockTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Exactly(2));
    }

    [TestCaseSource(nameof(CustomCaching))]
    public async Task WithCaching_WithCustomConfiguration(Func<CachingBuilder, CachingBuilder> configure, string method, string? contentEncoding, string expected)
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching(configure)))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        if (contentEncoding is not null)
                        {
                            context.Response.Headers.ContentEncoding = contentEncoding;
                        }
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/"));
        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/"));

        // Assert
        Assert.That(await response.Content.ReadAsStringAsync(), Is.EqualTo(expected));
    }

    [Test]
    public async Task WithCaching_ExpiresAfterTenMinutesByDefault()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var clock = new Mock<ISystemClock>();
        clock.Setup(c => c.UtcNow).Returns(() => now);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddMemoryCache(options => options.Clock = clock.Object)
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.GetStringAsync("/");
        now += TimeSpan.FromMinutes(9);
        var beforeExpiry = await client.GetStringAsync("/");
        now += TimeSpan.FromMinutes(2);
        var afterExpiry = await client.GetStringAsync("/");

        // Assert
        Assert.That(beforeExpiry, Is.EqualTo("<P>HELLO WORLD 1</P>"));
        Assert.That(afterExpiry, Is.EqualTo("<P>HELLO WORLD 3</P>"));
    }

    [Test]
    public async Task WithCaching_WithKeyAndOptions_SharesEntryUntilSlidingExpiry()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var clock = new Mock<ISystemClock>();
        clock.Setup(c => c.UtcNow).Returns(() => now);

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddMemoryCache(options => options.Clock = clock.Object)
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching(cache => cache
                            .Key(ctx => "shared")
                            .Options(new MemoryCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) }))))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.GetStringAsync("/a");
        var sharedKey = await client.GetStringAsync("/b");
        now += TimeSpan.FromMinutes(2);
        var afterSlidingExpiry = await client.GetStringAsync("/a");

        // Assert
        Assert.That(sharedKey, Is.EqualTo("<P>HELLO WORLD 1</P>"));
        Assert.That(afterSlidingExpiry, Is.EqualTo("<P>HELLO WORLD 3</P>"));
    }

    [Test]
    public async Task WithCaching_PreservesTransformedContentType()
    {
        // Arrange
        var mockTransform = new Mock<IResponseTransform>();
        mockTransform
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns(true);
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<byte[]>.IsAny))
            .Callback((HttpContext ctx, ref byte[] content) =>
            {
                content = Encoding.UTF8.GetBytes($"{{\"html\":\"{Encoding.UTF8.GetString(content)}\"}}");
                ctx.Response.ContentType = "application/json";
            });

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformResponse(mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.GetAsync("/");
        var response = await client.GetAsync("/");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(content, Is.EqualTo("{\"html\":\"<p>hello world 1</p>\"}"));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
        Assert.That(response.Content.Headers.ContentLength, Is.EqualTo(content.Length));
    }

    [Test]
    public async Task WithCaching_PreservesTransformOrder()
    {
        // Arrange
        var mockTransform = new Mock<TextResponseTransform> { CallBase = true };
        mockTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content = content.Replace("world", "universe");
            });

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(mockTransform.Object)
                        .TransformDocument(config => config
                            .InjectHtml(html => html.WithContent("<b>world</b>").At("//p")))
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        var miss = await client.GetStringAsync("/");
        var hit = await client.GetStringAsync("/");

        // Assert
        Assert.That(miss, Is.EqualTo("<p>hello universe 1<b>world</b></p>"));
        Assert.That(hit, Is.EqualTo(miss));
        mockTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Once);
    }

    [Test]
    public async Task WithCaching_KeepsSeparatePipelinesApart()
    {
        // Arrange
        var innerTransform = new Mock<TextResponseTransform> { CallBase = true };
        innerTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content += "!";
            });

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching())
                    .AddResponseTransformer(builder => builder
                        .TransformText(innerTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        var miss = await client.GetStringAsync("/");
        var hit = await client.GetStringAsync("/");

        // Assert
        Assert.That(miss, Is.EqualTo("<P>HELLO WORLD 1</P>!"));
        Assert.That(hit, Is.EqualTo(miss));
        _mockTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Once);
        innerTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Once);
    }

    [Test]
    public async Task WithCaching_SameTransformRegisteredTwice()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .TransformText(_mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        var miss = await client.GetStringAsync("/");
        var hit = await client.GetStringAsync("/");

        // Assert
        Assert.That(miss, Is.EqualTo("<P>HELLO WORLD 1</P>"));
        Assert.That(hit, Is.EqualTo(miss));
        _mockTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Exactly(2));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AddResponseTransformer_WithAppMemoryCache_UsesAppMemoryCache(bool registeredFirst)
    {
        // Arrange
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var services = new ServiceCollection();

        // Act
        if (registeredFirst)
        {
            services.AddSingleton<IMemoryCache>(cache).AddResponseTransformer();
        }
        else
        {
            services.AddResponseTransformer().AddSingleton<IMemoryCache>(cache);
        }

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IMemoryCache>(), Is.SameAs(cache));
    }

    [Test]
    public async Task WithCaching_WithSizeLimitedCache_CachesOutput()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddMemoryCache(options => options.SizeLimit = 1024)
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        await client.GetStringAsync("/");
        var hit = await client.GetStringAsync("/");

        // Assert
        Assert.That(hit, Is.EqualTo("<P>HELLO WORLD 1</P>"));
    }

    [Test]
    public async Task WithCaching_WithConditionalTransform_CachesEachTransformSet()
    {
        // Arrange
        var betaTransform = new Mock<TextResponseTransform> { CallBase = true };
        betaTransform
            .Setup(t => t.ShouldTransform(It.IsAny<HttpContext>()))
            .Returns((HttpContext ctx) => ctx.Request.Headers.ContainsKey("X-Beta"));
        betaTransform
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content += " beta";
            });

        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)
                        .TransformText(betaTransform.Object)
                        .WithCaching()))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();
        using var beta = new HttpRequestMessage(HttpMethod.Get, "/") { Headers = { { "X-Beta", "true" } } };
        using var betaAgain = new HttpRequestMessage(HttpMethod.Get, "/") { Headers = { { "X-Beta", "true" } } };

        // Act
        await client.SendAsync(beta);
        var regular = await client.GetStringAsync("/");
        var betaHit = await (await client.SendAsync(betaAgain)).Content.ReadAsStringAsync();

        // Assert
        Assert.That(regular, Is.EqualTo("<P>HELLO WORLD 2</P>"));
        Assert.That(betaHit, Is.EqualTo("<P>HELLO WORLD 1</P> beta"));
    }

    [Test]
    public async Task WithoutCaching_TransformsEveryResponse()
    {
        // Arrange
        using var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services => services
                    .AddResponseTransformer(builder => builder
                        .TransformText(_mockTransform.Object)))
                .Configure(app => app
                    .Run(async context =>
                    {
                        _requests++;
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync($"<p>hello world {_requests}</p>");
                    })))
            .Start();

        var client = host.GetTestClient();

        // Act
        var first = await client.GetStringAsync("/");
        var second = await client.GetStringAsync("/");

        // Assert
        Assert.That(first, Is.EqualTo("<P>HELLO WORLD 1</P>"));
        Assert.That(second, Is.EqualTo("<P>HELLO WORLD 2</P>"));
        _mockTransform.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Exactly(2));
    }
}
