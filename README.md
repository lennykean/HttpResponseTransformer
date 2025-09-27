# HttpResponseTransformer

An ASP.NET Core library for injecting customizable transforms into the HTTP response pipeline. This library enables dynamic modification of HTTP responses, with specialized support for HTML document manipulation and content injection.

## Motivation

This library was originally built with Jellyfin plugins in mind, as the standard plugin API doesn't provide UI customization capabilities. However, it is designed as a general-purpose tool for response transformation in any ASP.NET Core application and has no Jellyfin dependencies.

## Installation

```sh
dotnet add package HttpResponseTransformer
```

## Quick Start

Register the services and configure transforms in your `Startup.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddResponseTransforms(builder => builder
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


## Response Pipeline

The library integrates with ASP.NET Core's middleware pipeline to intercept and transform HTTP responses. When `ShouldTransform()` returns true for any registered transform, the response stream is buffered in memory, transformed, and then sent to the client.

> **Important**: When executing a transform, the entire response stream is buffered in memory. Care should be taken when working with large documents or streaming content. `ShouldTransform()` or `TransformDocument(x => x.When(...))` should be used to target specific requests and avoid unnecessary buffering.

### Transform Types

The library provides four types of transforms, each building on the previous level:

- **`IResponseTransform`** - Base interface for all transforms, works with raw byte arrays
- **`TextResponseTransform`** - Base class for text-based responses, handles encoding/decoding automatically
- **`DocumentResponseTransform`** - Specialized for HTML documents, provides parsed DOM access via HtmlAgilityPack
- **`InjectContentResponseTransform`** - Pre-built transform for common content injection scenarios

## Embedded Resource Pipeline

The library includes a built-in embedded resource serving system that works alongside the transform pipeline:

- **Resource Registration**: Embedded resources are automatically registered when using `FromEmbeddedResource()`
- **Automatic Serving**: The library serves embedded resources at generated URLs (e.g., `/_/{namespace-hash}/{resource-hash}`)
- **Content Types**: Proper content-type headers are set based on file extensions

This allows bundling CSS, JavaScript, images, and other assets directly in an assembly and having them automatically served by the application.

## Transform Execution Order

Transforms are executed in the order they are registered. Each transform receives the output of the previous transform, allowing multiple transformations to be chained together.

## Usage Examples

The library provides several levels of response transformation:

### HTML Document Injection

Use the built-in content injection system to add scripts, stylesheets, HTML content, and images to HTML documents using XPath targeting. This is the most common use case and requires no custom code.

```csharp
services.AddResponseTransforms(builder => builder
    .TransformDocument(config => config
        .InjectScript(script => script.FromEmbeddedResource("MyApp.Scripts.analytics.js").At("//body"))
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
```
