using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration;

public sealed record StyleSheetContentInjectionConfig() : ContentInjectionConfig("//head", "text/css", inline: false)
{
    public LinkRel? Relationship { get; init; } = LinkRel.StyleSheet;
    public string? Media { get; init; }
    public string? Title { get; init; }
}
