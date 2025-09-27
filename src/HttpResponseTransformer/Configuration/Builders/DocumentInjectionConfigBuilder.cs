using System;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure injecting content into a document
/// </summary>
public record DocumentInjectionConfigBuilder(InjectionConfig Config)
{
    /// <summary>
    /// Indicate whether content should be injected based on a predicate
    /// </summary>
    /// <param name="predicate">A function that determines whether the content should be injected.</param>
    public DocumentInjectionConfigBuilder When(Func<HttpContext, bool> predicate)
    {
        return new(Config with
        {
            Predicate = predicate
        });
    }

    /// <summary>
    /// Inject a script into the HTML page
    /// </summary>
    /// <param name="config">The configuration for the script injection.</param>
    public DocumentInjectionConfigBuilder InjectScript(ScriptContentInjectionConfig config)
    {
        return new(Config with
        {
            ContentInjectionConfigs = Config.ContentInjectionConfigs.Add(config)
        });
    }

    /// <summary>
    /// Inject a script into the HTML page
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the script injection.</param>
    public DocumentInjectionConfigBuilder InjectScript(Func<ScriptContentInjectionBuilder, ScriptContentInjectionBuilder> configure)
    {
        return InjectScript(configure(new(new())).Config);
    }

    /// <summary>
    /// Inject a style-sheet into the HTML page
    /// </summary>
    /// <param name="config">The configuration for the style-sheet injection.</param>
    public DocumentInjectionConfigBuilder InjectStyleSheet(StyleSheetContentInjectionConfig config)
    {
        return new(Config with
        {
            ContentInjectionConfigs = Config.ContentInjectionConfigs.Add(config)
        });
    }

    /// <summary>
    /// Inject a style-sheet into the HTML page
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the style-sheet injection.</param>
    public DocumentInjectionConfigBuilder InjectStyleSheet(Func<StyleSheetContentInjectionBuilder, StyleSheetContentInjectionBuilder> configure)
    {
        return InjectStyleSheet(configure(new(new())).Config);
    }

    /// <summary>
    /// Inject HTML into the document
    /// </summary>
    /// <param name="config">The configuration for the HTML injection.</param>
    public DocumentInjectionConfigBuilder InjectHtml(HtmlContentInjectionConfig config)
    {
        return new(Config with { ContentInjectionConfigs = Config.ContentInjectionConfigs.Add(config) });
    }

    /// <summary>
    /// Inject HTML into the document
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the HTML injection.</param>
    public DocumentInjectionConfigBuilder InjectHtml(Func<HtmlContentInjectionBuilder, HtmlContentInjectionBuilder> configure)
    {
        return InjectHtml(configure(new(new())).Config);
    }

    /// <summary>
    /// Inject an image into the document
    /// </summary>
    /// <param name="config">The configuration for the image injection.</param>
    public DocumentInjectionConfigBuilder InjectImage(ImageContentInjectionConfig config)
    {
        return new(Config with { ContentInjectionConfigs = Config.ContentInjectionConfigs.Add(config) });
    }

    /// <summary>
    /// Inject an image into the document
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the image injection.</param>
    public DocumentInjectionConfigBuilder InjectImage(Func<ImageContentInjectionBuilder, ImageContentInjectionBuilder> configure)
    {
        return InjectImage(configure(new(new())).Config);
    }
}
