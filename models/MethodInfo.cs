namespace SwaggerCoverage;

/// <summary>
/// Represents information about a method's definition.
/// </summary>
public class MethodInfo : IEquatable<MethodInfo>
{
    /// <summary>
    /// The full file path where the method is defined.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// The name of the class containing the method.
    /// </summary>
    public required string ContainingClass { get; set; }

    /// <summary>
    /// Determines whether the specified MethodInfo is equal to the current MethodInfo.
    /// </summary>
    public bool Equals(MethodInfo? other)
    {
        if (other is null)
            return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(ContainingClass, other.ContainingClass, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as MethodInfo);

    public override int GetHashCode() =>
        HashCode.Combine(FilePath.ToLowerInvariant(), ContainingClass.ToLowerInvariant());
}