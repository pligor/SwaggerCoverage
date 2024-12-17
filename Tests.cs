using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Analysis;
using static SwaggerCoverage.MethodAnalyzer;

namespace SwaggerCoverage;

public class Tests
{
  public static async Task TestExportCoverage(string sortBy = "Request")
  {
    string jsonPath = "jsons/req_invocs.json";
    if (!File.Exists(jsonPath))
    {
      Console.WriteLine($"Invocations JSON file not found at path: {jsonPath}");
      return;
    }

    string jsonContent = await File.ReadAllTextAsync(jsonPath);
    Dictionary<string, List<InvocationDefinitionPair>> invocations = JsonSerializer.Deserialize<
      Dictionary<string, List<InvocationDefinitionPair>>>(jsonContent) ?? throw new Exception("Failed to deserialize invocations JSON");

    var invocationsCount = invocations.ToDictionary(
      kvp => kvp.Key,
      kvp => kvp.Value.Count
    );

    string jsonOutput = JsonSerializer.Serialize(invocationsCount, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(jsonOutput);

    var requestColumn = new StringDataFrameColumn("Request", invocationsCount.Keys);
    var countColumn = new Int32DataFrameColumn("Count", invocationsCount.Values);
    var df = new DataFrame(requestColumn, countColumn);

    // Start Generation Here
    df = df.OrderBy(sortBy, ascending: true);

    // Define a culture with a different decimal separator
    var culture = new CultureInfo("en-US") { NumberFormat = { NumberDecimalSeparator = "." } };
    // Save CSV with the specified culture
    DataFrame.SaveCsv(df, "invocationsCount.csv", cultureInfo: culture);

    Plotter.PlotHorizontalBarChart(df, "invocationsCount.png");
  }
  
  public static async Task TestFilterInvocationsAsync(string nswagJsonPath)
  {
    string invocationsJsonPath = "jsons/invocations.json";
    if (!File.Exists(invocationsJsonPath))
    {
      Console.WriteLine($"Invocations JSON file not found at path: {invocationsJsonPath}");
      return;
    }

    var extractor = new SwaggerRequestExtractor();
    var generatedClientFilePath = extractor.GetGeneratedClientFilePath(nswagJsonPath);
    var className = extractor.GetGeneratedClientClassName(nswagJsonPath);

    string invocationsJson = await File.ReadAllTextAsync(invocationsJsonPath);
    Dictionary<string, List<InvocationDefinitionPair>> invocations = JsonSerializer.Deserialize<
      Dictionary<string, List<InvocationDefinitionPair>>>(invocationsJson) ??
        throw new Exception("Failed to deserialize invocations JSON");

    var filteredInvocations = FilterUtils.FilterByDefinition(invocations, className, generatedClientFilePath);
    string jsonOutput = JsonSerializer.Serialize(filteredInvocations, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(jsonOutput);

    Console.WriteLine("\nFiltering out invocations in definition\n");

    var filteredInvocationsNonMatching = FilterUtils.FilterOutInvocationsInDefinition(filteredInvocations, className, generatedClientFilePath);
    string jsonOutputNonMatching = JsonSerializer.Serialize(filteredInvocationsNonMatching, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(jsonOutputNonMatching);
  }

  public static async Task TestMapInvocationsToDefinitionsAsync(string solutionPath)
  {
    string[] methodNames = ["FindPetsByStatusAsync"];

    var results = await MapInvocationsToDefinitionsAsync(solutionPath, methodNames);

    string jsonOutput = JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine(jsonOutput);
  }

  public static async Task TestSwaggerExtractor(string nswagJsonPath)
  {
    var extractor = new SwaggerRequestExtractor();
    var requests = await extractor.ExtractRequestsAsync(nswagJsonPath);

    Console.WriteLine($"Found {requests.Count} unique API endpoints:");
    foreach (var request in requests)
    {
      Console.WriteLine($"{request.Method,-7} {request.Path}");
    }
  }
}