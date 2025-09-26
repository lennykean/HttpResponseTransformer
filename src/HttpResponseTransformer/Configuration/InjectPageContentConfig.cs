using System.Reflection;

using HttpResponseTransformer.Configuration.Enums;

namespace HttpResponseTransformer.Configuration;

public abstract record InjectPageContentConfig
{
    protected internal InjectPageContentConfig(DocumentLocation appendLocation, string? contentType)
    {
        AppendTo = appendLocation;
        ContentType = contentType;
    }

    public DocumentLocation AppendTo { get; init; }
    public string? ResourceName { get; init; }
    public Assembly? ResourceAssembly { get; init; }
    public string? ContentType { get; init; }
    public bool? Inline { get; init; }
    public string? Url { get; init; }
}
