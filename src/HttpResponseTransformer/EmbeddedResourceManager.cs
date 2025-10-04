using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace HttpResponseTransformer;

internal class EmbeddedResourceManager : IEmbeddedResourceManager
{
    private record ResourceReference(Assembly Assembly, string ResourceName, string? ContentType);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ResourceReference>> _resources = [];

    public bool TryAddResource(Assembly resourceAssembly, string resourceName, string? contentType, out string namespaceKey, out string resourceKey)
    {
        namespaceKey = ComputeHash(resourceAssembly.GetName().FullName);
        resourceKey = $"{ComputeHash(resourceName)}{Path.GetExtension(resourceName)}";

        if (resourceAssembly.GetManifestResourceStream(resourceName) is null)
        {
            return false;
        }
        var namespaceResources = _resources.GetOrAdd(namespaceKey, _ => new());

        return namespaceResources.TryAdd(resourceKey, new(resourceAssembly, resourceName, contentType));
    }

    public bool TryGetResourceKeys(Assembly resourceAssembly, string resourceName, out string namespaceKey, out string resourceKey)
    {
        namespaceKey = ComputeHash(resourceAssembly.GetName().FullName);
        resourceKey = $"{ComputeHash(resourceName)}{Path.GetExtension(resourceName)}";

        if (!_resources.TryGetValue(namespaceKey, out var namespaceResources))
        {
            return false;
        }
        return namespaceResources.ContainsKey(resourceKey);
    }

    public bool TryGetResource(string namespaceKey, string resourceKey, out byte[] data, out string? contentType)
    {
        data = [];
        contentType = null;

        if (!_resources.TryGetValue(namespaceKey, out var namespaceResources))
        {
            return false;
        }
        if (!namespaceResources.TryGetValue(resourceKey, out var resource))
        {
            return false;
        }
        using var buffer = new MemoryStream();
        using var stream = resource.Assembly.GetManifestResourceStream(resource.ResourceName);

        if (stream is null)
        {
            return false;
        }
        stream.CopyTo(buffer);

        data = buffer.ToArray();
        contentType = resource.ContentType;

        return true;
    }

    private static string ComputeHash(string input)
    {
        using var sha = MD5.Create();

        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));

        var hexHash = new StringBuilder(hash.Length * 2);
        foreach (var hashByte in hash)
            hexHash.Append($"{hashByte:x2}");

        return hexHash.ToString();
    }
}
