using System.Text;

using HtmlAgilityPack;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Configuration.Enums;
using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using Moq;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Unit;

[TestFixture]
public class InjectContentResponseTransformTests
{
    private Mock<IEmbeddedResourceManager> _resourceManager = null!;

    [SetUp]
    public void SetUp()
    {
        _resourceManager = new();
    }

    [Test]
    public void ShouldTransform_WithPredicateTrue_ReturnsTrue()
    {
        // Arrange
        var config = new InjectionConfig { Predicate = _ => true };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HeaderNames.Accept] = "text/html"
                }
            }
        };

        // Act
        var result = subject.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldTransform_WithPredicateFalse_ReturnsFalse()
    {
        // Arrange
        var config = new InjectionConfig { Predicate = _ => false };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HeaderNames.Accept] = "text/html"
                }
            }
        };

        // Act
        var result = subject.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void InjectScript_WithUrl_InjectsScriptTag()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ScriptContentInjectionConfig
                {
                    Url = "https://code.jquery.com/jquery.min.js",
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body><h1>My Page</h1></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var scriptTag = document.DocumentNode.SelectSingleNode("//script[@src]");
        Assert.That(scriptTag, Is.Not.Null);
        Assert.That(scriptTag.GetAttributeValue("src", string.Empty), Is.EqualTo("https://code.jquery.com/jquery.min.js"));
    }

    [Test]
    public void InjectScript_WithAsyncDefer_AddsAttributes()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ScriptContentInjectionConfig
                {
                    Url = "https://code.jquery.com/jquery.min.js",
                    LoadingBehavior = LoadScript.Async | LoadScript.Deferred,
                    XPath = "//head"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><head></head><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var scriptTag = document.DocumentNode.SelectSingleNode("//head/script");
        Assert.Multiple(() =>
        {
            Assert.That(scriptTag, Is.Not.Null);
            Assert.That(scriptTag.GetAttributeValue("async", string.Empty), Is.EqualTo("async"));
            Assert.That(scriptTag.GetAttributeValue("defer", string.Empty), Is.EqualTo("defer"));
        });
    }

    [Test]
    public void InjectScript_WithModule_SetsTypeModule()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ScriptContentInjectionConfig
                {
                    Url = "/js/module.js",
                    LoadingBehavior = LoadScript.Module,
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var scriptTag = document.DocumentNode.SelectSingleNode("//script");
        Assert.That(scriptTag.GetAttributeValue("type", string.Empty), Is.EqualTo("module"));
    }

    [Test]
    public void InjectScript_WithEmbeddedResource_InjectsResourcePath()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = "Assembly.path.file.js";

        _resourceManager
            .Setup(m => m.TryGetResourceKeys(assembly, resourceName, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Callback((System.Reflection.Assembly asm, string name, out string ns, out string key) =>
            {
                ns = "namespace-hash";
                key = "resource-hash.js";
            })
            .Returns(true);

        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ScriptContentInjectionConfig
                {
                    ResourceAssembly = assembly,
                    ResourceName = resourceName,
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body><p></p></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var scriptTag = document.DocumentNode.SelectSingleNode("//script");
        Assert.That(scriptTag.GetAttributeValue("src", string.Empty), Is.EqualTo("/_/namespace-hash/resource-hash.js"));
    }

    [Test]
    public void InjectScript_WithInlineEmbeddedResource_InjectsScriptContent()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = "Assembly.path.file.js";
        var scriptContent = "alert('Meet women in your area!!! Click NOW!!!');";

        _resourceManager
            .Setup(m => m.TryGetResourceKeys(assembly, resourceName, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Callback((System.Reflection.Assembly asm, string name, out string ns, out string key) =>
            {
                ns = "namespace-hash";
                key = "resource-hash.js";
            })
            .Returns(true);

        _resourceManager
            .Setup(m => m.TryGetResource("namespace-hash", "resource-hash.js", out It.Ref<byte[]>.IsAny, out It.Ref<string?>.IsAny))
            .Callback((string ns, string key, out byte[] data, out string? contentType) =>
            {
                data = Encoding.UTF8.GetBytes(scriptContent);
                contentType = "text/javascript";
            })
            .Returns(true);

        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ScriptContentInjectionConfig
                {
                    ResourceAssembly = assembly,
                    ResourceName = resourceName,
                    Inline = true,
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var scriptTag = document.DocumentNode.SelectSingleNode("//script");
        Assert.That(scriptTag.InnerText, Is.EqualTo(scriptContent));
    }

    [Test]
    public void InjectStyleSheet_WithUrl_InjectsLinkTag()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new StyleSheetContentInjectionConfig
                {
                    Url = "https://cdn.hotdog-stand.com/style.css",
                    XPath = "//head"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><head><title>Hot Dog Stand</title></head><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var linkTag = document.DocumentNode.SelectSingleNode("//link[@href]");
        Assert.Multiple(() =>
        {
            Assert.That(linkTag, Is.Not.Null);
            Assert.That(linkTag.GetAttributeValue("href", string.Empty), Is.EqualTo("https://cdn.hotdog-stand.com/style.css"));
            Assert.That(linkTag.GetAttributeValue("rel", string.Empty), Is.EqualTo("stylesheet"));
        });
    }

    [Test]
    public void InjectStyleSheet_WithInline_InjectsStyleTag()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = "Assembly.path.file.css";
        var cssContent = "blink { animation: blink 1s infinite; }";

        _resourceManager
            .Setup(m => m.TryGetResourceKeys(assembly, resourceName, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Callback((System.Reflection.Assembly asm, string name, out string ns, out string key) =>
            {
                ns = "namespace-hash";
                key = "resource-hash.css";
            })
            .Returns(true);

        _resourceManager
            .Setup(m => m.TryGetResource("namespace-hash", "resource-hash.css", out It.Ref<byte[]>.IsAny, out It.Ref<string?>.IsAny))
            .Callback((string ns, string key, out byte[] data, out string? contentType) =>
            {
                data = Encoding.UTF8.GetBytes(cssContent);
                contentType = "text/css";
            })
            .Returns(true);

        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new StyleSheetContentInjectionConfig
                {
                    ResourceAssembly = assembly,
                    ResourceName = resourceName,
                    Inline = true,
                    XPath = "//head"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><head></head><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var styleTag = document.DocumentNode.SelectSingleNode("//style");
        Assert.Multiple(() =>
        {
            Assert.That(styleTag, Is.Not.Null);
            Assert.That(styleTag.InnerText, Is.EqualTo(cssContent));
        });
    }

    [Test]
    public void InjectStyleSheet_WithMediaAndTitle_AddsAttributes()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new StyleSheetContentInjectionConfig
                {
                    Url = "/styles/print.css",
                    Media = "print",
                    Title = "Print Styles",
                    XPath = "//head"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><head></head><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var linkTag = document.DocumentNode.SelectSingleNode("//link");
        Assert.Multiple(() =>
        {
            Assert.That(linkTag.GetAttributeValue("media", string.Empty), Is.EqualTo("print"));
            Assert.That(linkTag.GetAttributeValue("title", string.Empty), Is.EqualTo("Print Styles"));
        });
    }

    [Test]
    public void InjectStyleSheet_WithPreloadRelationship_SetsRelPreload()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new StyleSheetContentInjectionConfig
                {
                    Url = "/styles/critical.css",
                    Relationship = LinkRel.Preload,
                    XPath = "//head"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><head></head><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var linkTag = document.DocumentNode.SelectSingleNode("//link");
        Assert.That(linkTag.GetAttributeValue("rel", string.Empty), Is.EqualTo("preload"));
    }

    [Test]
    public void InjectHtml_WithContent_InjectsHtml()
    {
        // Arrange
        var htmlContent = "<marquee>FREE BITCOIN GIVEAWAY, Enter your credit card to see if you've won!</marquee>";
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new HtmlContentInjectionConfig
                {
                    Content = htmlContent,
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body><h1>Welcome</h1></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var content = document.DocumentNode.OuterHtml;
        Assert.That(content, Does.Contain(htmlContent));
    }

    [Test]
    public void InjectHtml_WithReplace_ReplacesTargetNode()
    {
        // Arrange
        var htmlContent = "<h1>Nobody expects the Spanish Inquisition!</h1>";
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new HtmlContentInjectionConfig
                {
                    Content = htmlContent,
                    Replace = true,
                    XPath = "//h1"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body><h1>The Zen and the Art of Motorcycle Maintenance</h1></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var content = document.DocumentNode.OuterHtml;
        Assert.That(content, Does.Contain(htmlContent));
        Assert.That(content, Does.Not.Contain("The Zen and the Art of Motorcycle Maintenance"));
    }

    [Test]
    public void InjectHtml_WithEmbeddedResource_InjectsResourceContent()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = "Assembly.path.file.html";
        var htmlContent = "<div class='banner'>Laws are threats made by the dominant socioeconomic-ethnic group in a given nation.</div>";

        _resourceManager
            .Setup(m => m.TryGetResourceKeys(assembly, resourceName, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Callback((System.Reflection.Assembly asm, string name, out string ns, out string key) =>
            {
                ns = "namespace-hash";
                key = "resource-hash.html";
            })
            .Returns(true);

        _resourceManager
            .Setup(m => m.TryGetResource("namespace-hash", "resource-hash.html", out It.Ref<byte[]>.IsAny, out It.Ref<string?>.IsAny))
            .Callback((string ns, string key, out byte[] data, out string? contentType) =>
            {
                data = Encoding.UTF8.GetBytes(htmlContent);
                contentType = "text/html";
            })
            .Returns(true);

        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new HtmlContentInjectionConfig
                {
                    ResourceAssembly = assembly,
                    ResourceName = resourceName,
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var content = document.DocumentNode.OuterHtml;
        Assert.That(content, Does.Contain(htmlContent));
    }

    [Test]
    public void InjectImage_WithUrl_InjectsImgTag()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ImageContentInjectionConfig
                {
                    Url = "https://cdn.hotdog-stand.com/banana-dance.gif",
                    Alt = "Banana Dance",
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var imgTag = document.DocumentNode.SelectSingleNode("//img");
        Assert.Multiple(() =>
        {
            Assert.That(imgTag, Is.Not.Null);
            Assert.That(imgTag.GetAttributeValue("src", string.Empty), Does.Contain("banana-dance.gif"));
            Assert.That(imgTag.GetAttributeValue("alt", string.Empty), Is.EqualTo("Banana Dance"));
        });
    }

    [Test]
    public void InjectImage_WithInlineEmbeddedResource_InjectsDataUri()
    {
        // Arrange
        var assembly = GetType().Assembly;
        var resourceName = "Assembly.path.file.gif";
        var imageData = "GIF89a"u8.ToArray();

        _resourceManager
            .Setup(m => m.TryGetResourceKeys(assembly, resourceName, out It.Ref<string>.IsAny, out It.Ref<string>.IsAny))
            .Callback((System.Reflection.Assembly asm, string name, out string ns, out string key) =>
            {
                ns = "namespace-hash";
                key = "resource-hash.gif";
            })
            .Returns(true);

        _resourceManager
            .Setup(m => m.TryGetResource("namespace-hash", "resource-hash.gif", out It.Ref<byte[]>.IsAny, out It.Ref<string?>.IsAny))
            .Callback((string ns, string key, out byte[] data, out string? contentType) =>
            {
                data = imageData;
                contentType = "image/gif";
            })
            .Returns(true);

        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ImageContentInjectionConfig
                {
                    ResourceAssembly = assembly,
                    ResourceName = resourceName,
                    ContentType = "image/gif",
                    Inline = true,
                    Alt = "Banana Dance",
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var imgTag = document.DocumentNode.SelectSingleNode("//img");
        Assert.That(imgTag.GetAttributeValue("src", string.Empty), Does.StartWith("data:image/gif;base64,"));
    }

    [Test]
    public void InjectImage_WithTitleAndClass_AddsAttributes()
    {
        // Arrange
        var config = new InjectionConfig
        {
            ContentInjectionConfigs =
            [
                new ImageContentInjectionConfig
                {
                    Url = "/images/banana-dance.gif",
                    Title = "Banana Dance",
                    CssClass = "barrel-roll",
                    Alt = "Banana Dance",
                    XPath = "//body"
                }
            ]
        };
        var subject = new InjectContentResponseTransform(config, _resourceManager.Object);
        var context = new DefaultHttpContext();

        var document = new HtmlDocument();
        document.LoadHtml("<html><body></body></html>");

        // Act
        subject.ExecuteTransform(context, ref document);

        // Assert
        var imgTag = document.DocumentNode.SelectSingleNode("//img");
        Assert.Multiple(() =>
        {
            Assert.That(imgTag.GetAttributeValue("title", string.Empty), Is.EqualTo("Banana Dance"));
            Assert.That(imgTag.GetAttributeValue("class", string.Empty), Is.EqualTo("barrel-roll"));
        });
    }
}

