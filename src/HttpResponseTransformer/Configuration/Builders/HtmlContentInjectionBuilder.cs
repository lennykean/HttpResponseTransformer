using System.Reflection;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting HTML content into a document
/// </summary>
public record HtmlContentInjectionBuilder(HtmlContentInjectionConfig Config)
{
    /// <summary>
    /// Inject content based on an XPath query
    /// </summary>
    /// <param name="xpath">The XPath expression specifying where to inject the content.</param>
    public HtmlContentInjectionBuilder At(string xpath) => this with { Config = Config with { XPath = xpath } };

    /// <summary>
    /// Configure the injected content to load from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the script.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public HtmlContentInjectionBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
    {
        return this with
        {
            Config = Config with
            {
                ResourceName = resourceName,
                ResourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly(),
                Content = null
            }
        };
    }

    /// <summary>
    /// Inject the HTML content into the document
    /// </summary>
    /// <param name="content">The HTML content to inject.</param>
    public HtmlContentInjectionBuilder WithContent(string content) => this with { Config = Config with { Content = content, ResourceName = null, ResourceAssembly = null } };

    /// <summary>
    /// Replace the existing content with the injected content
    /// </summary>
    public HtmlContentInjectionBuilder Replace() => this with { Config = Config with { Replace = true } };

    /// <summary>
    /// Append the injected content to the existing content
    /// </summary>
    public HtmlContentInjectionBuilder Append() => this with { Config = Config with { Replace = false } };
}
