using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using SwaggerCoverage;
using static SwaggerCoverage.MethodAnalyzer;
using FluentAssertions;
using Microsoft.Data.Analysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Diagnostics;

class Program
{
  static async Task Main(string[] args)
  {
    await MySwaggerCoverage(args);
    // await TestFindMethodsDefinitionAsync(args);
    // await TestFindMethodsInvocationsAsync(args);
    // var results = await TestMapInvocationsToDefinitionsAsync(args);
    // await TestFilterInvocationsAsync(args);
    // await TestExportCoverage(args);
  }

  //dotnet run -- "/projs/msTests" "nswag.json" "MyTests.sln"
  public static async Task MySwaggerCoverage(string[] args,
    string sortBy = "Count", string outputCsv = "invocationsCount.csv", string outputPng = "invocationsCount.png", bool debug = false)
  {
    var (nswagJsonPath, solutionPath) = GetPaths(args);

    var (generatedClientFilePath, className) = ExtractClientInfo(nswagJsonPath, debug);

    var requests = await GetAllRequestsFromSwaggerAsync(nswagJsonPath, debug);

    // Get method name mapping for each request
    var reqToMethodName = GetRequestToMethodNameMapping(requests, generatedClientFilePath, debug);

    //we find the invocations of each method name in the solution
    //but for each invocation we want to know its definition as well
    var methodNameToInvocations = await MapInvocationsToDefinitionsAsync(solutionPath, [.. reqToMethodName.Values]);

    // We will need to filter out the definitions which do not belong to the generated client of the nswag.json file
    var invocationsInDefinition = FilterUtils.FilterByDefinition(methodNameToInvocations, className, generatedClientFilePath);

    // Now we want to filter out any invocations which happen inside the generated client, which we do not care for
    var filteredInvocations = FilterUtils.FilterOutInvocationsInDefinition(invocationsInDefinition, className, generatedClientFilePath);

    // Create dictionary to map the requests to invocations
    var requestToFilteredInvocations = CreateRequestToInvocationsDictionary(reqToMethodName, filteredInvocations, debug);

    var invocationsCount = CalculateInvocationsCount(requestToFilteredInvocations, debug);

    // Convert to DataFrame
    var df = ReqCountDictionaryToDataframe(invocationsCount, debug: debug);

    //Sorting the dataframe by the column specified by sortBy
    df = df.OrderBy(sortBy, ascending: true);

    // Save CSV with the specified culture
    DataFrame.SaveCsv(df, outputCsv,
      //these two symbols need to be different otherwise the csv will not be read correctly
      cultureInfo: new CultureInfo("en-US") { NumberFormat = { NumberDecimalSeparator = "." } },
      separator: ',');

    Plotter.PlotHorizontalBarChart(df, outputPng, debug: debug);
  }

  private static Dictionary<string, int> CalculateInvocationsCount(Dictionary<string, List<InvocationDefinitionPair>> requestsToInvocations, bool debug)
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

  private static async Task<HashSet<Request>> GetAllRequestsFromSwaggerAsync(string nswagJsonPath, bool debug)
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

  private static (string, string) ExtractClientInfo(string nswagJsonPath, bool debug = false)
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

  private static Dictionary<Request, string> GetRequestToMethodNameMapping(HashSet<Request> requests, string generatedClientFilePath, bool debug)
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

  public static (string, string) GetPaths(string[] args)
  {
    // Display usage instructions if arguments are missing
    if (args.Length < 2)
    {
      throw new Exception("Usage: dotnet run -- <rootPath> <NSwagJsonRelativePath> <SolutionRelativePath>");
    }

    string rootPath = args[0];
    string nswagJsonRelativePath = args[1];
    string solutionRelativePath = args[2];

    string nswagJsonPath = Path.Combine(rootPath, nswagJsonRelativePath);
    string solutionPath = Path.Combine(rootPath, solutionRelativePath);

    return (nswagJsonPath, solutionPath);
  }

  private static Dictionary<string, List<InvocationDefinitionPair>> CreateRequestToInvocationsDictionary(
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

  private static DataFrame ReqCountDictionaryToDataframe(Dictionary<string, int> invocationsCount, string requestColName = "Request", string countColName = "Count", bool debug = false)
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