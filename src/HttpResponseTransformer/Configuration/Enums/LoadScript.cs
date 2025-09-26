using System;

namespace HttpResponseTransformer.Configuration.Enums;

/// <summary>
/// Flags indicating how the script should be loaded by the browser
/// </summary>
[Flags]
public enum LoadScript
{
    /// <summary>
    /// Load the script normally with blocking behavior
    /// </summary>
    Normal = 0,
    /// <summary>
    /// Load the script asynchronously without blocking
    /// </summary>
    Async = 0x1,
    /// <summary>
    /// Load the script with deferred execution
    /// </summary>
    Deferred = 0x2,
    /// <summary>
    /// Load the script as an ES6 module
    /// </summary>
    Module = 0x4,
}
