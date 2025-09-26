using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace HttpResponseTransformer;

internal class EmbeddedResourceManager : IEmbeddedResourceManager
{
    private readonly Dictionary<(string NamespaceKey, string ResourceKey), (byte[] Data, string? ContentType)> _resources = [];

    public bool TryAddResource(Assembly resourceAssembly, string resourceName, string? contentType, out string namespaceKey, out string resourceKey)
    {
        namespaceKey = ComputeHash(resourceAssembly.GetName().FullName);
        resourceKey = $"{ComputeHash(resourceName)}{Path.GetExtension(resourceName)}";

        var stream = resourceAssembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return false;
        }

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);

        return _resources.TryAdd((namespaceKey, resourceKey), (buffer.ToArray(), contentType));
    }

    public bool TryGetResourceKeys(Assembly resourceAssembly, string resourceName, out string namespaceKey, out string resourceKey)
    {
        namespaceKey = ComputeHash(resourceAssembly.GetName().FullName);
        resourceKey = $"{ComputeHash(resourceName)}{Path.GetExtension(resourceName)}";

        return _resources.ContainsKey((namespaceKey, resourceKey));
    }

    public bool TryGetResource(string namespaceKey, string resourceKey, out byte[] data, out string? contentType)
    {
        data = [];
        contentType = null;

        if (_resources.TryGetValue((namespaceKey, resourceKey), out var resource))
        {
            data = resource.Data;
            contentType = resource.ContentType;
            return true;
        }
        return false;
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
