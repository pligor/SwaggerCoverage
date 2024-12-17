using SwaggerCoverage;
using static SwaggerCoverage.MethodAnalyzer;
using Microsoft.Data.Analysis;
using System.Globalization;
using System.CommandLine;
using static SwaggerCoverage.SwaggerCoverageCore;

class Program
{
  static async Task<int> Main(string[] args)
  {
    var rootCommand = BuildRootCommand();
    return await rootCommand.InvokeAsync(args);
  }

  //dotnet run -- "/projs/msTests" "nswag.json" "MyTests.sln"
  private static async Task MySwaggerCoverage(string nswagJsonPath, string solutionPath,
    string sortBy = "Count", string outputCsv = "invocationsCount.csv", string outputPng = "invocationsCount.png", bool debug = false)
  {
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

  private static RootCommand BuildRootCommand()
  {
    var rootOption = new Option<string>("--rootPath", "Path to the root directory of the dotnet solution") { IsRequired = true };
    var nswagJsonOption = new Option<string>("--nswagJson", "Relative path to the nswag.json file") { IsRequired = true };
    var solutionOption = new Option<string>("--solution", "Relative path to the dotnet solution file") { IsRequired = true };
    var sortByOption = new Option<string>("--sortBy", () => "Count", "Column to sort by (e.g. Count, Request)");
    var outputCsvOption = new Option<string>("--outputCsv", () => "invocationsCount.csv", "Path to the output csv file");
    var outputPngOption = new Option<string>("--outputPng", () => "invocationsCount.png", "Path to the output png file");
    var debugOption = new Option<bool>("--debug", () => false, "Enable debug mode");

    var swaggerCoverageCommand = new Command("swaggerCoverage", "Calculates the swagger coverage of the dotnet solution")
    {
        rootOption, nswagJsonOption, solutionOption, sortByOption, outputCsvOption, outputPngOption, debugOption
    };

    swaggerCoverageCommand.SetHandler(
        (string rootPath, string nswagJsonRelativePath, string solutionRelativePath, string sortBy, string outputCsv, string outputPng, bool debug) =>
        {
          string nswagJsonPath = Path.Combine(rootPath, nswagJsonRelativePath);
          string solutionPath = Path.Combine(rootPath, solutionRelativePath);
          return MySwaggerCoverage(nswagJsonPath, solutionPath, sortBy, outputCsv, outputPng, debug);
        },
        rootOption, nswagJsonOption, solutionOption, sortByOption, outputCsvOption, outputPngOption, debugOption
    );

    var rootCommand = new RootCommand("SwaggerCoverage CLI Tool");
    rootCommand.AddCommand(swaggerCoverageCommand);

    return rootCommand;
  }
}