using System.Reflection;

using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting script content into a document
/// </summary>
public record ScriptContentInjectionBuilder(ScriptContentInjectionConfig Config)
{
    /// <summary>
    /// Inject content based on an XPath query
    /// </summary>
    /// <param name="xpath">The XPath expression specifying where to inject the content.</param>
    public ScriptContentInjectionBuilder At(string xpath) => this with { Config = Config with { XPath = xpath } };

    /// <summary>
    /// Inject content from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the script.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public EmbeddedScriptContentInjectionBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
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
    public ScriptContentInjectionBuilder FromUrl(string url) => this with { Config = Config with { Url = url, ResourceName = null, ResourceAssembly = null } };

    /// <summary>
    /// Set the loading behavior for the injected script
    /// </summary>
    /// <param name="loadBehavior">The loading behavior for the script.</param>
    public ScriptContentInjectionBuilder WithLoadingBehavior(LoadScript loadBehavior) => this with { Config = Config with { LoadingBehavior = loadBehavior } };

    /// <summary>
    /// Set the script to load asynchronously
    /// </summary>
    public ScriptContentInjectionBuilder AsAsync() => this with { Config = Config with { LoadingBehavior = Config.LoadingBehavior | LoadScript.Async } };

    /// <summary>
    /// Set the script to deferred loading
    /// </summary>
    public ScriptContentInjectionBuilder AsDeferred() => this with { Config = Config with { LoadingBehavior = Config.LoadingBehavior | LoadScript.Deferred } };

    /// <summary>
    /// Set the script to load as an ES6 module
    /// </summary>
    public ScriptContentInjectionBuilder AsModule() => this with { Config = Config with { LoadingBehavior = Config.LoadingBehavior | LoadScript.Module } };
}

/// <summary>
/// Fluent builder to configure injecting script content from an embedded resource
/// </summary>
public record EmbeddedScriptContentInjectionBuilder(ScriptContentInjectionConfig Config) : ScriptContentInjectionBuilder(Config)
{
    /// <summary>
    /// Inject the content inline
    /// </summary>
    public ScriptContentInjectionBuilder Inline() => this with { Config = Config with { Inline = true, ContentType = null } };

    /// <summary>
    /// Configure the content type for the injected script
    /// </summary>
    /// <param name="contentType">The content type of the script.</param>
    public EmbeddedScriptContentInjectionBuilder AsContentType(string contentType) => this with { Config = Config with { ContentType = contentType, Inline = false } };
}
