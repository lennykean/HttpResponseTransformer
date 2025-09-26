using System;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting content into an HTML document
/// </summary>
public record ContentInjectionConfigBuilder(ContentInjectionConfig Config)
{
    /// <summary>
    /// Indicate whether content should be injected based based on a predicate
    /// </summary>
    /// <param name="predicate">A function that determines whether the content should be injected.</param>
    public ContentInjectionConfigBuilder When(Func<HttpContext, bool> predicate)
    {
        return new(Config with
        {
            Predicate = predicate
        });
    }

    /// <summary>
    /// Inject a script into the HTML page
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the script injection.</param>
    public ContentInjectionConfigBuilder InjectScript(Func<InjectScriptConfigBuilder, InjectScriptConfigBuilder> configure)
    {
        return InjectScript(configure(new(new())).Config);
    }

    /// <summary>
    /// Inject a script into the HTML page
    /// </summary>
    /// <param name="config">The configuration for the script injection.</param>
    public ContentInjectionConfigBuilder InjectScript(InjectScriptConfig config)
    {
        return new(Config with
        {
            InjectContentConfigs = Config.InjectContentConfigs.Add(config)
        });
    }

    /// <summary>
    /// Inject a style-sheet into the HTML page
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the style-sheet injection.</param>
    public ContentInjectionConfigBuilder InjectStyleSheet(Func<InjectStyleSheetConfigBuilder, InjectStyleSheetConfigBuilder> configure)
    {
        return InjectStyleSheet(configure(new(new())).Config);
    }

    /// <summary>
    /// Inject a style-sheet into the HTML page
    /// </summary>
    /// <param name="config">The configuration for the style-sheet injection.</param>
    public ContentInjectionConfigBuilder InjectStyleSheet(InjectStyleSheetConfig config)
    {
        return new(Config with
        {
            InjectContentConfigs = Config.InjectContentConfigs.Add(config)
        });
    }
}
