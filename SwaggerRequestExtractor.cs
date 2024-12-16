using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;

namespace SwaggerCoverage;

public class SwaggerRequestExtractor
{
  /// <summary>
  /// Extracts a specified property from a given section in the nswag.json file.
  /// </summary>
  /// <param name="nswagJsonPath">The file path to the nswag.json configuration file.</param>
  /// <param name="sectionPath">The hierarchical path to the desired section (e.g., "codeGenerators.openApiToCSharpClient").</param>
  /// <param name="propertyName">The property name to extract.</param>
  /// <returns>The value of the specified property.</returns>
  /// <exception cref="ArgumentException">Thrown when inputs are invalid.</exception>
  /// <exception cref="FileNotFoundException">Thrown when the nswag.json or the target file does not exist.</exception>
  /// <exception cref="InvalidOperationException">Thrown when the specified section or property is missing.</exception>
  private string ExtractProperty(string nswagJsonPath, string sectionPath, string propertyName)
  {
    if (string.IsNullOrWhiteSpace(nswagJsonPath))
      throw new ArgumentException("The nswagJsonPath cannot be null or empty.", nameof(nswagJsonPath));

    if (!File.Exists(nswagJsonPath))
      throw new FileNotFoundException($"The nswag.json file was not found at path: {nswagJsonPath}");

    // Read and parse the nswag.json file
    string nswagContent = File.ReadAllText(nswagJsonPath);
    try {
      using JsonDocument nswagDoc = JsonDocument.Parse(nswagContent);

      string[] sections = sectionPath.Split('.');
      JsonElement currentElement = nswagDoc.RootElement;

      foreach (var section in sections)
      {
        if (!currentElement.TryGetProperty(section, out JsonElement nextElement))
          throw new InvalidOperationException($"The section '{section}' does not exist in the nswag.json file.");
        currentElement = nextElement;
      }

      if (!currentElement.TryGetProperty(propertyName, out JsonElement propertyElement))
        throw new InvalidOperationException($"The property '{propertyName}' does not exist in the section '{sectionPath}'.");

      string propertyValue = propertyElement.GetString() ?? throw new InvalidOperationException($"The property '{propertyName}' in section '{sectionPath}' is empty.");
      propertyValue.Should().NotBeNullOrWhiteSpace();

      return propertyValue;
    }
    catch (JsonException e)
    {
      throw new InvalidOperationException($"Failed to parse the nswag.json file: {e.Message} with content: {nswagContent}");
    }
  }

  /// <summary>
  /// Retrieves the full file path of the generated C# client from the nswag.json file.
  /// </summary>
  /// <param name="nswagJsonPath">The file path to the nswag.json configuration file.</param>
  /// <returns>The full path to the generated C# client file.</returns>
  public string GetGeneratedClientFilePath(string nswagJsonPath)
  {
    string outputPath = ExtractProperty(nswagJsonPath, "codeGenerators.openApiToCSharpClient", "output");

    // Resolve the output path relative to the nswag.json directory
    string nswagDirectory = Path.GetDirectoryName(nswagJsonPath) ?? throw new InvalidOperationException("Cannot determine the directory of the nswag.json file.");
    string fullOutputPath = Path.GetFullPath(Path.Combine(nswagDirectory, outputPath));

    // Assert that the file exists
    if (!File.Exists(fullOutputPath))
      throw new FileNotFoundException($"The generated C# client file was not found at path: {fullOutputPath}");

    return fullOutputPath;
  }

  /// <summary>
  /// Retrieves the class name of the generated C# client from the nswag.json file.
  /// </summary>
  /// <param name="nswagJsonPath">The file path to the nswag.json configuration file.</param>
  /// <returns>The class name of the generated C# client.</returns>
  public string GetGeneratedClientClassName(string nswagJsonPath)
  {
    return ExtractProperty(nswagJsonPath, "codeGenerators.openApiToCSharpClient", "className");
  }

  /// <summary>
  /// Extracts all unique HTTP methods and paths from a Swagger JSON file specified in the nswag.json configuration.
  /// </summary>
  /// <param name="nswagJsonPath">The file path to the nswag.json configuration file.</param>
  /// <returns>A set of Request objects containing unique method and path combinations.</returns>
  public async Task<HashSet<Request>> ExtractRequestsAsync(string nswagJsonPath)
  {
    if (string.IsNullOrWhiteSpace(nswagJsonPath))
      throw new ArgumentException("The nswagJsonPath cannot be null or empty.", nameof(nswagJsonPath));

    if (!File.Exists(nswagJsonPath))
      throw new FileNotFoundException($"The nswag.json file was not found at path: {nswagJsonPath}");

    // Read and parse the nswag.json file
    string nswagContent = await File.ReadAllTextAsync(nswagJsonPath);
    using JsonDocument nswagDoc = JsonDocument.Parse(nswagContent);

    if (!nswagDoc.RootElement.TryGetProperty("documentGenerator", out JsonElement documentGenerator))
      throw new InvalidOperationException("The nswag.json file does not contain a 'documentGenerator' section.");

    if (!documentGenerator.TryGetProperty("fromDocument", out JsonElement fromDocument))
      throw new InvalidOperationException("The 'documentGenerator' section does not contain a 'fromDocument' section.");

    if (!fromDocument.TryGetProperty("url", out JsonElement urlElement))
      throw new InvalidOperationException("The 'fromDocument' section does not contain a 'url' property.");

    string swaggerUrl = urlElement.GetString() ?? throw new InvalidOperationException("The 'url' property in nswag.json is empty.");

    if (string.IsNullOrWhiteSpace(swaggerUrl))
      throw new InvalidOperationException("The 'url' property in nswag.json is empty.");

    // Download the Swagger JSON
    string swaggerJson;
    using (HttpClient httpClient = new HttpClient())
    {
      swaggerJson = await httpClient.GetStringAsync(swaggerUrl);
    }

    // Parse the Swagger JSON
    using JsonDocument swaggerDoc = JsonDocument.Parse(swaggerJson);

    if (!swaggerDoc.RootElement.TryGetProperty("paths", out JsonElement pathsElement))
      throw new InvalidOperationException("The Swagger JSON does not contain a 'paths' section.");

    HashSet<Request> requests = new HashSet<Request>();

    foreach (JsonProperty pathProperty in pathsElement.EnumerateObject())
    {
      string path = pathProperty.Name;
      JsonElement methodsElement = pathProperty.Value;

      foreach (JsonProperty methodProperty in methodsElement.EnumerateObject())
      {
        string method = methodProperty.Name.ToUpperInvariant();

        // Only consider standard HTTP methods
        if (IsValidHttpMethod(method))
        {
          requests.Add(new Request
          {
            Method = method,
            Path = path
          });
        }
      }
    }

    return requests;
  }

  /// <summary>
  /// Validates if the provided method is a standard HTTP method.
  /// </summary>
  /// <param name="method">The HTTP method to validate.</param>
  /// <returns>True if valid; otherwise, false.</returns>
  private bool IsValidHttpMethod(string method)
  {
    return method == "GET" ||
            method == "POST" ||
            method == "PUT" ||
            method == "DELETE" ||
            method == "PATCH" ||
            method == "OPTIONS" ||
            method == "HEAD";
  }
}
