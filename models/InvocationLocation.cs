namespace SwaggerCoverage;

/// <summary>
/// Represents the location of a method invocation within the codebase.
/// </summary>
public class InvocationLocation : IEquatable<InvocationLocation>
{
    /// <summary>
    /// The full file path where the invocation occurs.
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    /// The name of the class containing the invocation.
    /// </summary>
    public required string ContainingClass { get; set; }

    /// <summary>
    /// The line number where the invocation occurs.
    /// </summary>
    public required int LineNumber { get; set; }

    /// <summary>
    /// The column number where the invocation occurs.
    /// </summary>
    public required int ColumnNumber { get; set; }

    public bool Equals(InvocationLocation? other)
    {
        if (other is null)
            return false;

        return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(ContainingClass, other.ContainingClass, StringComparison.OrdinalIgnoreCase) &&
                LineNumber == other.LineNumber &&
                ColumnNumber == other.ColumnNumber;
    }

    public override bool Equals(object? obj) => Equals(obj as InvocationLocation);

    public override int GetHashCode() =>
        HashCode.Combine(FilePath.ToLowerInvariant(), ContainingClass.ToLowerInvariant(), LineNumber, ColumnNumber);
}
