using System.Reflection;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting image content into a document
/// </summary>
public record ImageContentInjectionBuilder(ImageContentInjectionConfig Config)
{
    /// <summary>
    /// Inject content based on an XPath query
    /// </summary>
    /// <param name="xpath">The XPath expression specifying where to inject the content.</param>
    public ImageContentInjectionBuilder At(string xpath) => this with { Config = Config with { XPath = xpath } };

    /// <summary>
    /// Inject content from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the image.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public EmbeddedImageContentInjectionBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
    {
        return new(Config with
        {
            ResourceName = resourceName,
            ResourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly(),
        });
    }

    /// <summary>
    /// Inject content from a URL
    /// </summary>
    /// <param name="url">The URL of the resource to inject.</param>
    public ImageContentInjectionBuilder FromUrl(string url) => this with { Config = Config with { Url = url, ResourceName = null, ResourceAssembly = null } };

    /// <summary>
    /// Set the alt attribute for the injected image
    /// </summary>
    /// <param name="altText">The alt attribute for the image.</param>
    public ImageContentInjectionBuilder WithAlt(string altText) => this with { Config = Config with { Alt = altText } };

    /// <summary>
    /// Set the title attribute for the injected image
    /// </summary>
    /// <param name="title">The title attribute for the image.</param>
    public ImageContentInjectionBuilder WithTitle(string title) => this with { Config = Config with { Title = title } };
}

/// <summary>
/// Fluent builder to configure injecting image content from an embedded resource
/// </summary>
public record EmbeddedImageContentInjectionBuilder(ImageContentInjectionConfig Config) : ImageContentInjectionBuilder(Config)
{
    /// <summary>
    /// Inject the content inline
    /// </summary>
    public EmbeddedImageContentInjectionBuilder Inline() => this with { Config = Config with { Inline = true } };

    /// <summary>
    /// Configure the content type for the injected image
    /// </summary>
    /// <param name="contentType">The content type of the image.</param>
    public EmbeddedImageContentInjectionBuilder AsContentType(string contentType) => this with { Config = Config with { ContentType = contentType, Inline = false } };
}
