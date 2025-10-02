using System.Text;

using HttpResponseTransformer.Transforms;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using Moq;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Unit;

[TestFixture]
public class TextResponseTransformTests
{
    private Mock<TextResponseTransform> _subject = null!;

    [SetUp]
    public void SetUp()
    {
        _subject = new Mock<TextResponseTransform>
        {
            CallBase = true
        };
    }

    [Test]
    public void ShouldTransform_WithTextAcceptHeader_ReturnsTrue()
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
    public void ShouldTransform_WithoutTextAcceptHeader_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Request =
            {
                Headers =
                {
                    [HeaderNames.Accept] = "image/webp"
                }
            }
        };

        // Act
        var result = _subject.Object.ShouldTransform(context);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void ExecuteTransform_WithTextContent_TransformsContent()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Response =
            {
                Headers =
                {
                    [HeaderNames.ContentType] = "text/plain; charset=utf-8"
                }
            }
        };
        _subject
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content += ", when you've got a library called card!";
            });

        var content = Encoding.UTF8.GetBytes("Having fun isn't hard");

        // Act
        _subject.Object.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(Encoding.UTF8.GetString(content), Is.EqualTo("Having fun isn't hard, when you've got a library called card!"));
        _subject.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Once);
    }

    [Test]
    public void ExecuteTransform_WithCompressedContent_DoesNotTransform()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Response =
            {
                Headers =
                {
                    [HeaderNames.ContentType] = "text/plain",
                    [HeaderNames.ContentEncoding] = "gzip"
                }
            }
        };

        var content = Encoding.UTF8.GetBytes("Nobody here but us chickens");

        // Act
        _subject.Object.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(Encoding.UTF8.GetString(content), Is.EqualTo("Nobody here but us chickens"));
        _subject.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Never);
    }

    [Test]
    public void ExecuteTransform_UsesCorrectEncoding()
    {
        // Arrange
        var context = new DefaultHttpContext
        {
            Response =
            {
                Headers =
                {
                    [HeaderNames.ContentType] = "text/plain; charset=utf-16"
                }
            }
        };
        _subject
            .Setup(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny))
            .Callback((HttpContext ctx, ref string content) =>
            {
                content += ", fruit flies like a banana!";
            });

        var content = Encoding.Unicode.GetBytes("Time flies like an arrow");

        // Act
        _subject.Object.ExecuteTransform(context, ref content);

        // Assert
        Assert.That(Encoding.Unicode.GetString(content), Is.EqualTo("Time flies like an arrow, fruit flies like a banana!"));
        _subject.Verify(t => t.ExecuteTransform(It.IsAny<HttpContext>(), ref It.Ref<string>.IsAny), Times.Once);
    }
}

