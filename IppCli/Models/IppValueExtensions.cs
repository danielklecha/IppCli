using SharpIpp.Protocol.Models;

namespace IppCli.Models;

public static class IppValueExtensions
{
    /// <summary>
    /// Safely unwraps an <see cref="IppValue{T}?"/> returning the underlying value if present, or default if null or in the NoValue state.
    /// </summary>
    public static T? Unwrap<T>(this IppValue<T>? value) =>
        value.HasValue && value.Value.IsValue ? value.Value.GetValueOrDefault() : default;

    /// <summary>
    /// Safely unwraps an <see cref="IppValue{T}"/> returning the underlying value if present, or default if in the NoValue state.
    /// </summary>
    public static T? Unwrap<T>(this IppValue<T> value) =>
        value.IsValue ? value.GetValueOrDefault() : default;
}
