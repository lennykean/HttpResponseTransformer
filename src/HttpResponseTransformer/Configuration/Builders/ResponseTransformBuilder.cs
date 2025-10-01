using System;
using System.Collections.Generic;
using System.Linq;

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

    public ResponseTransformerConfig Config { get; init; } = new();
    public IEnumerable<IResponseTransform> Transforms { get; init; } = [];

    /// <summary>
    /// Allow response compression
    /// </summary>
    /// <remarks>
    /// By default, response compression is bypassed when executing transforms.
    /// Turning this setting on may require transforms to handle compressed content.
    /// </remarks>
    public ResponseTransformBuilder AllowResponseCompression()
    {
        return this with { Config = Config with { AllowResponseCompression = true } };
    }

    /// <summary>
    /// Add a response transform to the pipeline
    /// </summary>
    /// <param name="transform">The response transform to add.</param>
    public ResponseTransformBuilder TransformResponse(IResponseTransform transform)
    {
        return this with
        {
            Transforms = Transforms.Append(transform)
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
    /// Add an HTML document transform to the pipeline
    /// </summary>
    /// <param name="transform">The HTML response transform to add.</param>
    public ResponseTransformBuilder TransformDocument(DocumentResponseTransform transform)
    {
        return TransformText(transform);
    }

    /// <summary>
    /// Add an HTML document transform to the pipeline
    /// </summary>
    /// <param name="configure">The fluent builder function to configure the content injection transform.</param>
    public ResponseTransformBuilder TransformDocument(Func<DocumentInjectionConfigBuilder, DocumentInjectionConfigBuilder> configure)
    {
        return TransformDocument(new InjectContentResponseTransform(configure(new(new())).Config, _embeddedResourceManager));
    }
}
