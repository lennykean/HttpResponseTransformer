namespace HttpResponseTransformer.Configuration;

public record ImageContentInjectionConfig() : ContentInjectionConfig("//body", "application/octet-stream", inline: false)
{
    public string? Alt { get; init; }
    public string? Title { get; init; }
}
