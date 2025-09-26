using System;
using System.Collections.Immutable;

using HttpResponseTransformer.Transforms;

namespace HttpResponseTransformer.Configuration.Builders;

/// <summary>
/// Fluent builder to configure response transformations
/// </summary>
public record ResponseTransformBuilder
{
    private readonly IEmbeddedResourceManager _embeddedResourceManager;

    internal ResponseTransformBuilder(IEmbeddedResourceManager embeddedResourceManager)
    {
        _embeddedResourceManager = embeddedResourceManager;
    }

    public ImmutableArray<IResponseTransform> Transforms { get; init; } = [];

    /// <summary>
    /// Add a response transform to the pipeline
    /// </summary>
    /// <param name="transform">The response transform to add.</param>
    public ResponseTransformBuilder TransformResponse(IResponseTransform transform)
    {
        return this with
        {
            Transforms = Transforms.Add(transform)
        };
    }

    /// <summary>
    /// Add a text response transform to the pipeline
    /// </summary>
    /// <param name="transform">The text response transform to add.</param>
    public ResponseTransformBuilder TransformText(TextResponseTransform transform)
    {
        return TransformResponse(transform);
    }

    /// <summary>
    /// Add an HTML page transform to the pipeline
    /// </summary>
    /// <param name="transform">The HTML response transform to add.</param>
    public ResponseTransformBuilder TransformHtmlPage(HtmlResponseTransform transform)
    {
        return TransformText(transform);
    }

    /// <summary>
    /// Add an content injection transform to the pipeline
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the content injection transform.</param>
    public ResponseTransformBuilder TransformHtmlPage(Func<ContentInjectionConfigBuilder, ContentInjectionConfigBuilder> configure)
    {
        return TransformHtmlPage(new InjectContentResponseTransform(configure(new(new())).Config, _embeddedResourceManager));
    }
}
