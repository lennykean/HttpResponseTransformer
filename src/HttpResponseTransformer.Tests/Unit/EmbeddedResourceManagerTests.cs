using System.Text;

using NUnit.Framework;

namespace HttpResponseTransformer.Tests.Unit;

[TestFixture]
public class EmbeddedResourceManagerTests
{
    private EmbeddedResourceManager _subject = null!;

    [SetUp]
    public void SetUp()
    {
        _subject = new EmbeddedResourceManager();
    }

    [Test]
    public void TryAddResource_WithValidResource_ReturnsTrue()
    {
        // Act
        var result = _subject.TryAddResource(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.a-resource.txt", "text/plain",
            out var namespaceKey,
            out var resourceKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(namespaceKey, Is.Not.Null.And.Not.Empty);
            Assert.That(resourceKey, Does.EndWith(".txt"));
        });
    }

    [Test]
    public void TryAddResource_WithInvalidResource_ReturnsFalse()
    {
        // Act
        var result = _subject.TryAddResource(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.an-invalid-resource.txt", "text/plain",
            out var _,
            out var _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetResourceKeys_WithAddedResource_ReturnsTrue()
    {
        // Arrange
        _subject.TryAddResource(
           GetType().Assembly,
           $"{GetType().Assembly.GetName().Name}.resources.a-resource.txt", "text/plain",
           out var addResourceNamespaceKey,
           out var addResourceKey);

        // Act
        var result = _subject.TryGetResourceKeys(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.a-resource.txt",
            out var getResourceNamespaceKey,
            out var getResourceKey);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(getResourceNamespaceKey, Is.Not.Null.And.Not.Empty.And.EqualTo(addResourceNamespaceKey));
            Assert.That(getResourceKey, Is.EqualTo(addResourceKey).And.EndsWith(".txt"));
        });
    }

    [Test]
    public void TryGetResourceKeys_WithoutAddedResource_ReturnsFalse()
    {
        // Act
        var result = _subject.TryGetResourceKeys(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.an-invalid-resource.txt",
            out var _,
            out var _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetResource_WithAddedResource_ReturnsTrue()
    {
        // Arrange
        _subject.TryAddResource(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.a-resource.txt", "text/plain",
            out var namespaceKey,
            out var resourceKey);

        // Act
        var result = _subject.TryGetResource(
            namespaceKey,
            resourceKey,
            out var data,
            out var contentType);

        // Assert
        var expectedContent = "Help! I'm embedded in this assembly!\n";
        var actualContent = Encoding.UTF8.GetString(data);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(data, Is.Not.Null.And.Not.Empty);
            Assert.That(actualContent, Is.EqualTo(expectedContent));
            Assert.That(contentType, Is.EqualTo("text/plain"));
        });
    }

    [Test]
    public void TryGetResource_WithInvalidKeys_ReturnsFalse()
    {
        // Act
        var result = _subject.TryGetResource(
            "invalid-namespace-key",
            "invalid-resource-key",
            out var _,
            out var _);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TryGetResource_WithInvalidResourceKey_ReturnsFalse()
    {
        // Arrange
        _subject.TryAddResource(
            GetType().Assembly,
            $"{GetType().Assembly.GetName().Name}.resources.a-resource.txt", "text/plain",
            out var namespaceKey,
            out var _);

        // Act
        var result = _subject.TryGetResource(
            namespaceKey,
            "invalid-resource-key.txt",
            out var _,
            out var _);

        // Assert
        Assert.That(result, Is.False);
    }
}

