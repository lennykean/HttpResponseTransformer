using System.Reflection;

namespace HttpResponseTransformer.Configuration;

public abstract record ContentInjectionConfig
{
    protected internal ContentInjectionConfig(string xpath, string? contentType = null, bool? inline = null)
    {
        XPath = xpath;
        ContentType = contentType;
        Inline = inline;
    }

    public string XPath { get; init; }
    public string? ContentType { get; init; }
    public bool? Inline { get; init; }
    public string? ResourceName { get; init; }
    public Assembly? ResourceAssembly { get; init; }
    public string? Url { get; init; }
}
