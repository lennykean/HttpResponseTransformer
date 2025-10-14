# HttpResponseTransformer

[![Build](https://github.com/lennykean/HttpResponseTransformer/actions/workflows/build.yml/badge.svg)](https://github.com/lennykean/HttpResponseTransformer/actions/workflows/build.yml)
[![Publish](https://github.com/lennykean/HttpResponseTransformer/actions/workflows/publish.yml/badge.svg)](https://github.com/lennykean/HttpResponseTransformer/actions/workflows/publish.yml)
[![NuGet Version](https://img.shields.io/nuget/v/HttpResponseTransformer)](https://www.nuget.org/packages/HttpResponseTransformer)

An ASP.NET Core library for injecting customizable transforms into the HTTP response pipeline. This library enables dynamic modification of HTTP responses, with specialized support for HTML document manipulation and content injection.

## Motivation

This library was built with Jellyfin plugins in mind, since the plugin API doesn't expose any UI capabilities. However, it is a general-purpose tool for use in any ASP.NET Core application and has no Jellyfin dependencies.

> **Note:** Modifying the HTML files directly in the filesystem is not recommended, especially if being deployed via docker.

## Installation

```sh
dotnet add package HttpResponseTransformer
```

## Quick Start

Add the response transformer to the `IServiceCollection`, and configure any transforms.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddResponseTransformer(builder => builder
        .TransformDocument(config => config
            .InjectScript(script => script
                .FromUrl("https://cdn.example.com/script.js")
                .At("//head")
                .AsAsync())
            .InjectStyleSheet(css => css
                .FromUrl("https://cdn.example.com/styles.css")
                .At("//head"))));
}
```

### Jellyfin Plugins

To use HttpResponseTransformer in a Jellyfin plugin, implement `IPluginServiceRegistrator` and add response transformer to the service collection.

```csharp
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddResponseTransformer(config => config
            .TransformDocument(injectPage => injectPage
                .When(ctx => ctx.Request.Path == "/web")
                .InjectScript(script => script
                    .FromEmbeddedResource($"{GetType().Namespace}.scripts.my-custom-script.js", GetType().Assembly)
                    .AsDeferred())));
    }
}
```

## Response Pipeline

HttpResponseTransformer integrates into the ASP.NET Core's middleware pipeline to intercept and transform HTTP responses. Filters determine whether transforms should run (`IResponseTransformer.ShouldTransform`, or `builder.When()` for injection transforms). When a transform executes, the response stream is buffered in memory and transformed by `IResponseTransformer.ExecuteTransform()` before being returned to the client.

> **Important**:  Transforms buffer the entire response stream in memory. Care should be taken when working with large documents or streaming content. `ShouldTransform()` or `builder.When()` should be used to target specific requests and avoid unnecessary buffering.

### Response Compression

By default, HttpResponseTransformer bypasses response compression when executing a transform by temporarily clearing the `Accept-Encoding` HTTP header.

This behavior can be disabled by calling `AllowResponseCompression()`.

```csharp
services.AddResponseTransformer(builder => builder
    .AllowResponseCompression()
    .TransformDocument(config => config
        ...
    ));
```

> **Warning**: When response compression is enabled, transforms should be prepared to handle compressed content themselves. Built-in transforms will fail if they receive compressed data.

### Types of Transforms

The library provides four types of transforms, each building on the previous level:

- **`InjectContentResponseTransform`** - Pre-built transform for common content injection scenarios
- **`DocumentResponseTransform`** - Specialized for HTML documents, provides parsed DOM access via HtmlAgilityPack
- **`TextResponseTransform`** - Base class for text-based responses, handles encoding/decoding automatically
- **`IResponseTransform`** - Base interface for all transforms, works with raw byte arrays

## Embedded Resource Pipeline

The library includes a built-in embedded resource serving system that works alongside the transform pipeline:

- **Resource Registration**: Embedded resources are automatically registered when using `FromEmbeddedResource()`
- **Automatic Serving**: The library serves embedded resources at generated URLs (e.g., `/_/{namespace-hash}/{resource-hash}`)
- **Content Types**: Proper content-type headers are set based on file extensions

This allows for bundling CSS, JavaScript, images, and other assets directly in an assembly and having them automatically served by the application.

## Transform Execution Order

Transforms are executed in the order they are registered. Each transform receives the output of the previous transform, allowing multiple transformations to be chained together.

## Usage

### HTML Document Injection

Use the built-in content injection system to add scripts, stylesheets, HTML content, and images to HTML documents using XPath targeting. This is the most common use case and requires no custom code.

```csharp
services.AddResponseTransformer(builder => builder
    .TransformDocument(config => config
        .InjectScript(script => script.FromEmbeddedResource("MyApp.Scripts.analytics.js", typeof(Program).Assembly).At("//body"))
        .InjectStyleSheet(css => css.FromUrl("/css/styles.css").At("//head"))
        .InjectImage(img => img.FromUrl("/images/logo.png").At("//div[@id='header']"))
        .InjectHtml(html => html.WithContent("<div>Welcome!</div>").At("//body"))));
```

### Custom HTML Transforms

Implement `DocumentResponseTransform` to create custom HTML document manipulations with full access to the parsed HTML DOM via [HtmlAgilityPack](https://html-agility-pack.net/).

```csharp
public class MetaTagTransform : DocumentResponseTransform
{
    public override bool ShouldTransform(HttpContext context) =>
        context.Request.Path.StartsWithSegments("/static/html");

    public override void ExecuteTransform(HttpContext context, ref HtmlDocument document)
    {
        var head = document.DocumentNode.SelectSingleNode("//head");
        var metaTag = document.CreateElement("meta");
        metaTag.SetAttributeValue("name", "generated-at");
        metaTag.SetAttributeValue("content", DateTime.UtcNow.ToString());
        head?.AppendChild(metaTag);
    }
}

...

// Register the transform
services.AddTransient<IResponseTransform, MetaTagTransform>();
```

### Custom Text Transforms

Implement `TextResponseTransform` to perform string-based transformations on any text-based HTTP responses (HTML, JSON, XML, etc.).

```csharp
public class TokenReplacementTransform : TextResponseTransform
{
    public override bool ShouldTransform(HttpContext context) =>
        context.Response.ContentType?.Contains("text/html") == true;

    public override void ExecuteTransform(HttpContext context, ref string content)
    {
        content = content.Replace("{{USER_NAME}}", context.User.Identity?.Name ?? "Guest");
    }
}

...

// Register the transform
services.AddTransient<IResponseTransform, TokenReplacementTransform>();
```

### Custom Binary Transforms

Implement `IResponseTransform` directly to work with raw byte arrays for complete control over any response type.

```csharp
public class ResizeImageTransform : IResponseTransform
{
    public bool ShouldTransform(HttpContext context) =>
        context.Response.ContentType?.StartsWith("image/") == true;

    public void ExecuteTransform(HttpContext context, ref byte[] content)
    {
        // Resize image logic here
        content = ResizeImage(content, maxWidth: 800, maxHeight: 600);
    }
}

...

// Register the transform
services.AddTransient<IResponseTransform, ResizeImageTransform>();
```
