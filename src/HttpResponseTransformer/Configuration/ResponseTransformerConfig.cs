namespace HttpResponseTransformer.Configuration;

public record ResponseTransformerConfig(bool AllowResponseCompression = false)
{
    internal CachingConfig? Caching { get; init; }
}
