using System;
using System.Text;

using HtmlAgilityPack;

using HttpResponseTransformer.Configuration;
using HttpResponseTransformer.Configuration.Enums;

using Microsoft.AspNetCore.Http;

namespace HttpResponseTransformer.Transforms;

internal class InjectContentResponseTransform : DocumentResponseTransform
{
    private readonly InjectionConfig _config;
    private readonly IEmbeddedResourceManager _embeddedResourceManager;

    public InjectContentResponseTransform(InjectionConfig config, IEmbeddedResourceManager embeddedResourceManager)
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
        foreach (var config in _config.ContentInjectionConfigs)
        {
            switch (config)
            {
                case ScriptContentInjectionConfig scriptConfig:
                    InjectScript(document, scriptConfig);
                    break;
                case StyleSheetContentInjectionConfig styleSheetConfig:
                    InjectStyleSheet(document, styleSheetConfig);
                    break;
                case HtmlContentInjectionConfig htmlConfig:
                    InjectHtml(document, htmlConfig);
                    break;
                case ImageContentInjectionConfig imageConfig:
                    InjectImage(document, imageConfig);
                    break;
            }
        }
    }

    private void RegisterEmbeddedResources()
    {
        foreach (var config in _config.ContentInjectionConfigs)
        {
            if (config.ResourceAssembly is null || config.ResourceName is null)
            {
                continue;
            }
            _embeddedResourceManager.TryAddResource(config.ResourceAssembly, config.ResourceName, config.ContentType, out var _, out _);
        }
    }

    private void InjectScript(HtmlDocument document, ScriptContentInjectionConfig config)
    {
        var target = document.DocumentNode.SelectSingleNode(config.XPath);
        if (target is null)
        {
            return;
        }

        var element = document.CreateElement("script");
        if (config.Url is not null)
        {
            element.SetAttributeValue("src", config.Url);
        }
        else if (config.ResourceAssembly is not null && config.ResourceName is not null)
        {
            if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
            {
                if (config.Inline is true && _embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var data, out var _))
                {
                    element.AppendChild(document.CreateTextNode(Encoding.UTF8.GetString(data)));
                }
                else
                {
                    element.SetAttributeValue("src", $"/_/{namespaceKey}/{resourceKey}");
                }
            }
        }
        if ((config.LoadingBehavior | LoadScript.Async) == config.LoadingBehavior)
        {
            element.SetAttributeValue("async", "async");
        }
        if ((config.LoadingBehavior | LoadScript.Deferred) == config.LoadingBehavior)
        {
            element.SetAttributeValue("defer", "defer");
        }
        if ((config.LoadingBehavior | LoadScript.Module) == config.LoadingBehavior)
        {
            element.SetAttributeValue("module", "module");
        }
        target.AppendChild(element);
    }

    private void InjectStyleSheet(HtmlDocument document, StyleSheetContentInjectionConfig config)
    {
        var target = document.DocumentNode.SelectSingleNode(config.XPath);
        if (target is null)
        {
            return;
        }

        var element = document.CreateElement(config.Inline is true ? "style" : "link");
        if (config.Url is not null)
        {
            element.SetAttributeValue("href", config.Url);
        }
        else if (config.ResourceAssembly is not null && config.ResourceName is not null)
        {
            if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
            {
                if (config.Inline is true && _embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var data, out var _))
                {
                    element.AppendChild(document.CreateTextNode(Encoding.UTF8.GetString(data)));
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
        target.AppendChild(element);
    }

    private void InjectHtml(HtmlDocument document, HtmlContentInjectionConfig config)
    {
        var target = document.DocumentNode.SelectSingleNode(config.XPath);
        if (target is null)
        {
            return;
        }

        var element = document.CreateTextNode();
        if (config.Content is not null)
        {
            element.InnerHtml = config.Content;
        }
        else
        {
            if (config.ResourceAssembly is not null && config.ResourceName is not null)
            {
                if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
                {
                    if (_embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var data, out var _))
                    {
                        element.InnerHtml = Encoding.UTF8.GetString(data);
                    }
                }
            }
        }
        if (config.Replace is true)
        {
            target.ParentNode.ReplaceChild(element, target);
        }
        else
        {
            target.AppendChild(element);
        }
    }

    private void InjectImage(HtmlDocument document, ImageContentInjectionConfig config)
    {
        var target = document.DocumentNode.SelectSingleNode(config.XPath);
        if (target is null)
        {
            return;
        }

        var element = document.CreateElement("img");
        if (config.Url is not null)
        {
            element.SetAttributeValue("src", config.Url);
        }
        else if (config.ResourceAssembly is not null && config.ResourceName is not null)
        {
            if (_embeddedResourceManager.TryGetResourceKeys(config.ResourceAssembly, config.ResourceName, out var namespaceKey, out var resourceKey))
            {
                string src;
                if (config.Inline is true && _embeddedResourceManager.TryGetResource(namespaceKey, resourceKey, out var data, out var _))
                {
                    src = "data:";
                    if (config.ContentType is not null)
                    {
                        src += $";{config.ContentType}";
                    }
                    src += $",{Convert.ToBase64String(data)}";
                }
                else
                {
                    src = $"/_/{namespaceKey}/{resourceKey}";
                }
                element.SetAttributeValue("src", src);
            }
        }
        if (config.Alt is not null)
        {
            element.SetAttributeValue("alt", config.Alt);
        }
        if (config.Title is not null)
        {
            element.SetAttributeValue("title", config.Title);
        }
        target.AppendChild(element);

    }
}
