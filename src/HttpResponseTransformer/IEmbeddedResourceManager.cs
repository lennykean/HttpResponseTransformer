using System.Reflection;

namespace HttpResponseTransformer;

internal interface IEmbeddedResourceManager
{
    bool TryAddResource(Assembly resourceAssembly, string resourceKey, string? contentType, out string resourceNamespaceKey, out string resourceNameKey);
    bool TryGetResourceKeys(Assembly resourceAssembly, string resourceName, out string resourceNamespaceKey, out string resourceNameKey);
    bool TryGetResource(string namespaceKey, string resourceNameKey, out byte[] data, out string? contentType);
}
