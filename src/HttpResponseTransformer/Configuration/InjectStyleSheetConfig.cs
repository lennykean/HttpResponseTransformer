using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration;

public sealed record InjectStyleSheetConfig() : InjectPageContentConfig(DocumentLocation.Head, "text/css")
{
    public LinkRel? Relationship { get; init; } = LinkRel.StyleSheet;
    public string? Media { get; init; }
    public string? Title { get; init; }
}
