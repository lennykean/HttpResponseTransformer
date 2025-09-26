using System.Reflection;

using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting a style-sheet into an HTML document
/// </summary>
public record InjectStyleSheetConfigBuilder(InjectStyleSheetConfig Config)
{
    /// <summary>
    /// Inject a style-sheet from a URL
    /// </summary>
    /// <param name="url">The URL of the style-sheet to inject.</param>
    public InjectRemoteStyleSheetConfigBuilder FromUrl(string url)
    {
        return new(Config with
        {
            Url = url,
            ResourceAssembly = null,
            ResourceName = null,
            Inline = false
        });
    }

    /// <summary>
    /// Inject a style-sheet from an embedded resource
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the style-sheet.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public InjectEmbeddedStyleSheetConfigBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
    {
        return new(Config with
        {
            ResourceName = resourceName,
            ResourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly(),
            Url = null
        });
    }

    /// <summary>
    ///  a style-sheet with a media attribute
    /// </summary>
    /// <param name="media">The media attribute for the style-sheet link.</param>
    public InjectStyleSheetConfigBuilder WithMedia(string media)
    {
        return new(Config with
        {
            Media = media
        });
    }

    /// <summary>
    /// Configure the title for the injected style-sheet
    /// </summary>
    /// <param name="title">The title attribute for the style-sheet link.</param>
    public InjectStyleSheetConfigBuilder WithTitle(string title)
    {
        return new(Config with
        {
            Title = title
        });
    }
}

/// <summary>
/// Fluent builder to configure injecting an embedded style-sheet
/// </summary>
public record InjectEmbeddedStyleSheetConfigBuilder(InjectStyleSheetConfig Config) : InjectStyleSheetConfigBuilder(Config)
{
    /// <summary>
    /// Configure the style-sheet to be injected directly into the HTML document
    /// </summary>
    public InjectEmbeddedStyleSheetConfigBuilder Inline()
    {
        return new(Config with
        {
            Inline = true
        });
    }

    /// <summary>
    /// Configure the injected style-sheet to be loaded as a reference
    /// </summary>
    public InjectEmbeddedStyleSheetConfigBuilder AsReference()
    {
        return new(Config with
        {
            Inline = false
        });
    }

    /// <summary>
    /// Configure the content type for the injected style-sheet
    /// </summary>
    /// <param name="contentType">The content type for the style-sheet.</param>
    public InjectEmbeddedStyleSheetConfigBuilder AsContentType(string? contentType)
    {
        return new(Config with
        {
            ContentType = contentType
        });
    }
}

/// <summary>
/// Fluent builder to configure injecting a remote style-sheet
/// </summary>
public record InjectRemoteStyleSheetConfigBuilder(InjectStyleSheetConfig Config) : InjectStyleSheetConfigBuilder(Config)
{
    /// <summary>
    /// Configure the link relationship for the injected style-sheet
    /// </summary>
    /// <param name="relationship">The link relationship type.</param>
    public InjectRemoteStyleSheetConfigBuilder AsRel(LinkRel relationship)
    {
        return new(Config with
        {
            Relationship = relationship
        });
    }

    /// <summary>
    /// Configure the injected style-sheet to be preloaded by the browser
    /// </summary>
    public InjectRemoteStyleSheetConfigBuilder AsPreload()
    {
        return AsRel(LinkRel.Preload);
    }

    /// <summary>
    /// Configure the injected style-sheet to be prefetched by the browser
    /// </summary>
    public InjectRemoteStyleSheetConfigBuilder AsPrefetch()
    {
        return AsRel(LinkRel.Prefetch);
    }

    /// <summary>
    /// Configure the injected style-sheet as an alternative style-sheet
    /// </summary>
    public InjectRemoteStyleSheetConfigBuilder AsAlternative()
    {
        return AsRel(LinkRel.AlternativeStyleSheet);
    }
}
