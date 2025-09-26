namespace HttpResponseTransformer.Configuration.Enums;

/// <summary>
/// Specifies the relationship of the linked resource
/// </summary>
public enum LinkRel
{
    /// <summary>
    /// Indicate the linked resource is a style-sheet
    /// </summary>
    StyleSheet = 0,
    /// <summary>
    /// Indicate the linked resource should be preloaded
    /// </summary>
    Preload = 1,
    /// <summary>
    /// Indicate the linked resource should be prefetched
    /// </summary>
    Prefetch = 2,
    /// <summary>
    /// Indicate the linked resource is an alternative style-sheet
    /// </summary>
    AlternativeStyleSheet = 3,
}
