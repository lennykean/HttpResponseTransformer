using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration;

public sealed record ScriptContentInjectionConfig() : ContentInjectionConfig("//body", "text/javascript", inline: false)
{
    public LoadScript? LoadingBehavior { get; init; } = LoadScript.Normal;
}
