namespace HttpResponseTransformer.Configuration;

public record HtmlContentInjectionConfig() : ContentInjectionConfig("//body", contentType: null, inline: true)
{
    public bool? Replace { get; init; }
    public string? Content { get; init; }
}
