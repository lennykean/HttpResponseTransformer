using System.Reflection;

using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting style-sheet content into a document
/// </summary>
public record StyleSheetContentInjectionBuilder(StyleSheetContentInjectionConfig Config)
{
    /// <summary>
    /// Inject content based on an XPath query
    /// </summary>
    /// <param name="xpath">The XPath expression specifying where to inject the content.</param>
    public StyleSheetContentInjectionBuilder At(string xpath) => this with { Config = Config with { XPath = xpath } };

    /// <summary>
    /// Inject content from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the style-sheet.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public EmbeddedStyleSheetContentInjectionBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
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
    public RemoteStyleSheetContentInjectionBuilder FromUrl(string url) => new(Config with { Url = url, ResourceName = null, ResourceAssembly = null });

    /// <summary>
    /// Configure the media attribute for the injected style-sheet
    /// </summary>
    /// <param name="media">The media attribute for the style-sheet.</param>
    public StyleSheetContentInjectionBuilder WithMedia(string media) => this with { Config = Config with { Media = media } };

    /// <summary>
    /// Configure the title attribute for the injected style-sheet
    /// </summary>
    /// <param name="title">The title attribute for the style-sheet.</param>
    public StyleSheetContentInjectionBuilder WithTitle(string title) => this with { Config = Config with { Title = title } };
}

/// <summary>
/// Fluent builder to configure injecting style-sheet content from a remote URL
/// </summary>
public record RemoteStyleSheetContentInjectionBuilder(StyleSheetContentInjectionConfig Config) : StyleSheetContentInjectionBuilder(Config)
{
    /// <summary>
    /// Configure the link relationship for the injected style-sheet
    /// </summary>
    /// <param name="relationship">The link relationship for the style-sheet.</param>
    public RemoteStyleSheetContentInjectionBuilder AsRel(LinkRel relationship) => this with { Config = Config with { Relationship = relationship } };

    /// <summary>
    /// Configure the injected style-sheet to be preloaded by the browser
    /// </summary>
    public RemoteStyleSheetContentInjectionBuilder AsPreload() => this with { Config = Config with { Relationship = LinkRel.Preload } };

    /// <summary>
    /// Configure the injected style-sheet to be prefetched by the browser
    /// </summary>
    public RemoteStyleSheetContentInjectionBuilder AsPrefetch() => this with { Config = Config with { Relationship = LinkRel.Prefetch } };

    /// <summary>
    /// Configure the injected style-sheet as an alternative style-sheet
    /// </summary>
    public RemoteStyleSheetContentInjectionBuilder AsAlternative() => this with { Config = Config with { Relationship = LinkRel.AlternativeStyleSheet } };
}

/// <summary>
/// Fluent builder to configure injecting style-sheet content from an embedded resource
/// </summary>
public record EmbeddedStyleSheetContentInjectionBuilder(StyleSheetContentInjectionConfig Config) : RemoteStyleSheetContentInjectionBuilder(Config)
{
    /// <summary>
    /// Inject the content inline
    /// </summary>
    public StyleSheetContentInjectionBuilder Inline() => this with { Config = Config with { Inline = true, ContentType = null, Relationship = null } };

    /// <summary>
    /// Configure the content type for the injected style-sheet
    /// </summary>
    /// <param name="contentType">The content type of the style-sheet.</param>
    public EmbeddedStyleSheetContentInjectionBuilder AsContentType(string contentType) => this with { Config = Config with { ContentType = contentType, Inline = false } };
}
