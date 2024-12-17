namespace SwaggerCoverage;

using static SwaggerCoverage.MethodAnalyzer;
using FluentAssertions;
using Microsoft.Data.Analysis;
using System.Text.Json;
public static class SwaggerCoverageCore
{
  public static Dictionary<string, int> CalculateInvocationsCount(Dictionary<string, List<InvocationDefinitionPair>> requestsToInvocations, bool debug)
  {
    var invocationsCount = requestsToInvocations.ToDictionary(
      kvp => kvp.Key,
      kvp => kvp.Value.Count
    );
    if (debug)
    {
      string jsonCount = JsonSerializer.Serialize(invocationsCount, new JsonSerializerOptions { WriteIndented = true });
      Console.WriteLine(jsonCount);
    }
    return invocationsCount;
  }

  public static async Task<HashSet<Request>> GetAllRequestsFromSwaggerAsync(string nswagJsonPath, bool debug)
  {
    var extractor = new SwaggerRequestExtractor();
    var requests = await extractor.ExtractRequestsAsync(nswagJsonPath);

    if (debug)
    {
      foreach (var request in requests)
      {
        Console.WriteLine($"Request: {request}");
      }
    }

    return requests;
  }

  public static (string, string) ExtractClientInfo(string nswagJsonPath, bool debug = false)
  {
    var extractor = new SwaggerRequestExtractor();
    var generatedClientFilePath = extractor.GetGeneratedClientFilePath(nswagJsonPath);
    var className = extractor.GetGeneratedClientClassName(nswagJsonPath);

    if (debug)
    {
      Console.WriteLine($"Generated client file path: {generatedClientFilePath}");
      Console.WriteLine($"Generated client class name: {className}");
    }

    return (generatedClientFilePath, className);
  }

  public static Dictionary<Request, string> GetRequestToMethodNameMapping(HashSet<Request> requests, string generatedClientFilePath, bool debug)
  {
    var reqToMethodName = requests.ToDictionary(
      rr => rr,
      rr => FindMethodName(generatedClientFilePath, rr.Method, rr.Path)
    );

    if (debug)
    {
      foreach (var kvp in reqToMethodName)
      {
        Console.WriteLine($"Request: {kvp.Key} => Method Name: {kvp.Value}");
      }
    }

    reqToMethodName.Values.Should().OnlyHaveUniqueItems("Each method name should be uniquely mapped to exactly one request.");
    return reqToMethodName;
  }

  public static Dictionary<string, List<InvocationDefinitionPair>> CreateRequestToInvocationsDictionary(
    Dictionary<Request, string> reqToMethodName,
    Dictionary<string, List<InvocationDefinitionPair>> invocations,
    bool debug)
  {
    var requestToFilteredInvocations = reqToMethodName.ToDictionary(
      kvp => kvp.Key.ToString(),
      kvp => invocations[kvp.Value]
    );

    if (debug)
    {
      Console.WriteLine("\nInvocations\n");
      string jsonOutput = JsonSerializer.Serialize(requestToFilteredInvocations, new JsonSerializerOptions { WriteIndented = true });
      Console.WriteLine(jsonOutput);
    }

    return requestToFilteredInvocations;
  }

  public static DataFrame ReqCountDictionaryToDataframe(Dictionary<string, int> invocationsCount, string requestColName = "Request", string countColName = "Count", bool debug = false)
  {
    var requestColumn = new StringDataFrameColumn(requestColName, invocationsCount.Keys);
    var countColumn = new Int32DataFrameColumn(countColName, invocationsCount.Values);
    var df = new DataFrame(requestColumn, countColumn);

    if (debug)
    {
      Console.WriteLine("DataFrame created with the following data:");
      Console.WriteLine(df);
    }

    return df;
  }
}