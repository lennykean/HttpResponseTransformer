using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration;

public sealed record InjectScriptConfig() : InjectPageContentConfig(DocumentLocation.Body, "text/javascript")
{
    public LoadScript? LoadBehavior { get; init; } = LoadScript.Normal;
}
