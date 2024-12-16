using System;
using System.Text.Json.Serialization;

namespace SwaggerCoverage;

/// <summary>
/// Represents an HTTP request with a method and path.
/// </summary>
public class Request
{
  /// <summary>
  /// The HTTP method (e.g., GET, POST).
  /// </summary>
  [JsonPropertyName("method")]
  public required string Method { get; set; }

  /// <summary>
  /// The request path (e.g., /api/users).
  /// </summary>
  [JsonPropertyName("path")]
  public required string Path { get; set; }

  public override bool Equals(object? obj)
  {
    return obj is Request request &&
            string.Equals(Method, request.Method, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(Path, request.Path, StringComparison.OrdinalIgnoreCase);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Method.ToUpperInvariant(), Path.ToLowerInvariant());
  }

  public override string ToString()
  {
    return $"{Method} {Path}";
  }
}
