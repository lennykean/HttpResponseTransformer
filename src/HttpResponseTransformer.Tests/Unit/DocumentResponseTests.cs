using HtmlAgilityPack;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using Moq;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Unit;

[TestFixture]
public class DocumentResponseTransformTests
{
    private Mock<DocumentResponseTransform> _subject = null!;

    [SetUp]
    public void SetUp()
    {
        _subject = new Mock<DocumentResponseTransform> { CallBase = true };
    }

    [Test]
    public void ShouldTransform_WithHtmlAcceptHeader_ReturnsTrue()
    {
        // Arrange
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
        var result = _subject.Object.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldTransform_WithoutAcceptHeader_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var result = _subject.Object.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ShouldTransform_WithoutHtmlAcceptHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HeaderNames.Accept] = "text/plain"
                }
            }
        };

        // Act
        var result = _subject.Object.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ExecuteTransform_WithHtmlContent_TransformsDocument()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Response =
            {
                Headers =
                {
                    [HeaderNames.ContentType] = "text/html; charset=utf-8"
                }
            }
        };

        _subject
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

        var content = "<html><head><title>Welcome to the 90's</title></head><body><h1>UNDER CONSTRUCTION</h1></body></html>";

        // Act
        _subject.Object.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(content, Does.Contain("<blink>UNDER CONSTRUCTION</blink>"));
        _subject.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<HtmlDocument>.IsAny), Times.Once);
    }

    [Test]
    public void ExecuteTransform_WithoutHtmlContent_DoesNotTransform()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Response =
            {
                Headers =
                {
                    [HeaderNames.ContentType] = "text/plain"
                }
            }
        };

        var content = "<html><body>Space Jam</body></html>";

        // Act
        _subject.Object.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(content, Is.EqualTo("<html><body>Space Jam</body></html>"));
        _subject.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<HtmlDocument>.IsAny), Times.Never);
    }
}

