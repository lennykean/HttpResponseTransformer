namespace HttpResponseTransformer.Configuration.Enums;

/// <summary>
/// Specifies where content should be injected within an HTML document
/// </summary>
public enum DocumentLocation
{
    /// <summary>
    /// Inject content at the document level
    /// </summary>
    Document = 0,
    /// <summary>
    /// Inject content within the HTML head section
    /// </summary>
    Head = 1,
    /// <summary>
    /// Inject content within the HTML body section
    /// </summary>
    Body = 2,
}
