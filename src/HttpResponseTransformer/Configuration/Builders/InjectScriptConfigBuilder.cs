using System.Reflection;

using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting a script into an HTML document
/// </summary>
public record InjectScriptConfigBuilder(InjectScriptConfig Config)
{
    /// <summary>
    /// Configure the injected script to load from a URL
    /// </summary>
    /// <param name="url">The URL of the script to inject.</param>
    public InjectScriptConfigBuilder FromUrl(string url)
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
    /// Configure the injected script to load from an embedded resource.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource containing the script.</param>
    /// <param name="resourceAssembly">The assembly containing the resource.</param>
    /// <remarks>If <paramref name="resourceAssembly"/> is not provided, the calling assembly will be used.</remarks>
    public InjectEmbeddedScriptConfigBuilder FromEmbeddedResource(string resourceName, Assembly? resourceAssembly = null)
    {
        return new(Config with
        {
            ResourceName = resourceName,
            ResourceAssembly = resourceAssembly ?? Assembly.GetCallingAssembly(),
            Url = null
        });
    }

    /// <summary>
    /// Configure how the injected script should be loaded in the browser
    /// </summary>
    /// <param name="loadBehavior">The loading behavior for the script.</param>
    public InjectScriptConfigBuilder LoadAs(LoadScript loadBehavior)
    {
        return new(Config with
        {
            LoadBehavior = loadBehavior
        });
    }

    /// <summary>
    /// Configure the injected script to load with deferred execution
    /// </summary>
    public InjectScriptConfigBuilder AsDeferred()
    {
        return LoadAs((Config.LoadBehavior ?? LoadScript.Normal) | LoadScript.Deferred);
    }

    /// <summary>
    /// Configure the injected script to load asynchronously
    /// </summary>
    public InjectScriptConfigBuilder AsAsync()
    {
        return LoadAs((Config.LoadBehavior ?? LoadScript.Normal) | LoadScript.Async);
    }

    /// <summary>
    /// Configure the injected script to be treated as an ES6 module
    /// </summary>
    public InjectScriptConfigBuilder AsModule()
    {
        return LoadAs((Config.LoadBehavior ?? LoadScript.Normal) | LoadScript.Module);
    }
}

/// <summary>
/// Fluent builder to configuring injecting an embedded script
/// </summary>
public record InjectEmbeddedScriptConfigBuilder(InjectScriptConfig Config) : InjectScriptConfigBuilder(Config)
{
    /// <summary>
    /// Configure the script resource to be injected directly into the HTML document
    /// </summary>
    public InjectEmbeddedScriptConfigBuilder Inline()
    {
        return new(Config with
        {
            Inline = true
        });
    }

    /// <summary>
    /// Configure injected script to be loaded as a reference
    /// </summary>
    public InjectEmbeddedScriptConfigBuilder AsReference()
    {
        return new(Config with
        {
            Inline = false
        });
    }

    /// <summary>
    /// Configure the content type for the injected script
    /// </summary>
    /// <param name="contentType">The content type for the script.</param>
    public InjectEmbeddedScriptConfigBuilder AsContentType(string? contentType)
    {
        return new(Config with
        {
            ContentType = contentType
        });
    }
}
