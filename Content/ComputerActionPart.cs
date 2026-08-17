using SaaFarr.AI.Prompt.Interfaces;
using SaaFarr.AI.Prompt.Models;

namespace SaaFarr.AI.Prompt.Content;

/// <summary>
/// A computer-use / UI agent action (click, type, scroll, etc.).
/// Execution is outside this library; this models the protocol payload.
/// </summary>
public sealed class ComputerActionPart : IContentPart
{
    /// <inheritdoc />
    public string Type => "computer_action";

    /// <summary>Gets the action name (e.g. <c>click</c>, <c>type</c>).</summary>
    public string Action { get; }

    /// <summary>Gets optional JSON arguments for the action.</summary>
    public string? ArgumentsJson { get; }

    /// <inheritdoc />
    public CacheControl? CacheControl { get; }

    /// <summary>Creates a computer-action part.</summary>
    /// <param name="action">Action name.</param>
    /// <param name="argumentsJson">Optional JSON arguments.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    public ComputerActionPart(string action, string? argumentsJson = null, CacheControl? cacheControl = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        Action = action.Trim();
        ArgumentsJson = argumentsJson;
        CacheControl = cacheControl;
    }

    /// <summary>Creates a computer-action part.</summary>
    /// <param name="action">Action name.</param>
    /// <param name="argumentsJson">Optional JSON arguments.</param>
    /// <param name="cacheControl">Optional cache control.</param>
    /// <returns>A new <see cref="ComputerActionPart"/>.</returns>
    public static ComputerActionPart Create(string action, string? argumentsJson = null, CacheControl? cacheControl = null) =>
        new(action, argumentsJson, cacheControl);
}
