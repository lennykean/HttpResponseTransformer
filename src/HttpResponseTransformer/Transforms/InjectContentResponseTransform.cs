using System.Text;

using HtmlAgilityPack;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Configuration.Enums;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

internal class InjectContentResponseTransform : HtmlResponseTransform
{
    private readonly ContentInjectionConfig _config;
    private readonly IEmbeddedResourceManager _embeddedResourceManager;

    public InjectContentResponseTransform(ContentInjectionConfig config, IEmbeddedResourceManager embeddedResourceManager)
    {
        _config = config;
        _embeddedResourceManager = embeddedResourceManager;

        RegisterEmbeddedResources();
    }

    public override bool ShouldTransform(HttpContext context)
    {
        return base.ShouldTransform(context) && _config.Predicate(context);
    }

    public override void ExecuteTransform(HttpContext context, ref HtmlDocument document)
    {
        foreach (var config in _config.InjectContentConfigs)
        {
            if (config is InjectScriptConfig scriptConfig)
            {
                InjectScript(document, scriptConfig);
            }
            else if (config is InjectStyleSheetConfig styleSheetConfig)
            {
                InjectStyleSheet(document, styleSheetConfig);
            }
        }
    }

    private void RegisterEmbeddedResources()
    {
        foreach (var config in _config.InjectContentConfigs)
        {
            if (config.ResourceAssembly is null || config.ResourceName is null || config.Inline is true)
            {
                continue;
            }
            _embeddedResourceManager.TryAddResource(config.ResourceAssembly, config.ResourceName, config.ContentType, out var _, out _);
        }
    }

    private void InjectScript(HtmlDocument document, InjectScriptConfig config)
    {
        var element = document.CreateElement("script");

        if (config.Url is not null)
        {
            element.SetAttributeValue("src", config.Url);
        }
        else if (config.ResourceAssembly is not null && config.ResourceName is not null)
        {
            if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
            {
                if (config.Inline is true && _embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var script, out var _))
                {
                    element.AppendChild(document.CreateTextNode(Encoding.UTF8.GetString(script)));
                }
                else
                {
                    element.SetAttributeValue("src", $"/_/{namespaceKey}/{resourceKey}");
                }
            }
        }
        if ((config.LoadBehavior & LoadScript.Async) == config.LoadBehavior)
        {
            element.SetAttributeValue("async", "async");
        }
        if ((config.LoadBehavior & LoadScript.Deferred) == config.LoadBehavior)
        {
            element.SetAttributeValue("defer", "defer");
        }
        if ((config.LoadBehavior & LoadScript.Module) == config.LoadBehavior)
        {
            element.SetAttributeValue("module", "module");
        }
        AppendElement(document, element, config.AppendTo);
    }

    private void InjectStyleSheet(HtmlDocument document, InjectStyleSheetConfig config)
    {
        var element = document.CreateElement(config.Inline is true ? "style" : "link");

        if (config.Url is not null)
        {
            element.SetAttributeValue("href", config.Url);
        }
        else if (config.ResourceAssembly is not null && config.ResourceName is not null)
        {
            if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
            {
                if (config.Inline is true && _embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var styleSheet, out var _))
                {
                    element.AppendChild(document.CreateTextNode(Encoding.UTF8.GetString(styleSheet)));
                }
                else
                {
                    element.SetAttributeValue("href", $"/_/{namespaceKey}/{resourceKey}");
                }
            }
        }
        if (config.Relationship is not null)
        {
            element.SetAttributeValue("rel", config.Relationship.ToString()!.ToLower());
        }
        if (config.Media is not null)
        {
            element.SetAttributeValue("media", config.Media);
        }
        if (config.Title is not null)
        {
            element.SetAttributeValue("title", config.Title);
        }
        AppendElement(document, element, config.AppendTo);
    }

    private static void AppendElement(HtmlDocument document, HtmlNode element, DocumentLocation location)
    {
        var target = location switch
        {
            DocumentLocation.Head => document.DocumentNode.SelectSingleNode("//head"),
            DocumentLocation.Body => document.DocumentNode.SelectSingleNode("//body"),
            _ => document.DocumentNode,
        };
        target.AppendChild(element);
    }
}
