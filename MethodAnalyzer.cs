using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace SwaggerCoverage;

public static class MethodAnalyzer
{
    public static void RegisterMSBuild()
    {
        var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
        if (!visualStudioInstances.Any())
        {
            throw new InvalidOperationException("No MSBuild instances found.");
        }

        // Register the latest Visual Studio instance
        MSBuildLocator.RegisterInstance(visualStudioInstances.OrderByDescending(v => v.Version).First());
    }

    /// <summary>
    /// Finds the method name in a specific C# file based on request method and path.
    /// </summary>
    /// <param name="csFilePath">Full file path of the target .cs file.</param>
    /// <param name="requestMethod">Request method string to search within the method body (e.g., "GET", "POST").</param>
    /// <param name="path">Path string to search within the method comments (e.g., "pet/{petId}/uploadImage").</param>
    /// <returns>The name of the matching method.</returns>
    public static string FindMethodName(string csFilePath, string requestMethod, string path)
    {
        path = PreProcessPath(path);

        if (string.IsNullOrWhiteSpace(csFilePath))
            throw new ArgumentException("C# file path cannot be null or empty.", nameof(csFilePath));

        if (!File.Exists(csFilePath))
            throw new FileNotFoundException($"The C# file was not found at path: {csFilePath}");

        string code = File.ReadAllText(csFilePath);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var methods = root.DescendantNodes()
                          .OfType<MethodDeclarationSyntax>()
                          .Where(m => MethodContainsRequestMethod(m, requestMethod) &&
                                      MethodContainsPathInComments(m, path))
                          .ToList();

        if (methods.Count == 0)
            throw new Exception($"No method found matching the specified criteria: {requestMethod} {path}");

        if (methods.Count > 1)
            throw new Exception($"Multiple methods found matching the specified criteria: {requestMethod} {path}. Method names: {string.Join(", ", methods.Select(m => m.Identifier.Text))}");

        return methods.First().Identifier.Text;
    }

    private static string PreProcessPath(string path)
    {
        path = path.StartsWith('/') ? path[1..] : path; // Remove leading slash if it exists
        path = path.EndsWith('/') ? path[..^1] : path; // Remove trailing slash if it exists
        if (path.StartsWith('"') || path.EndsWith('"'))
            throw new Exception($"Path {path} already contains quotes");
        path = '"' + path + '"';
        return path;
    }

    private static bool MethodContainsRequestMethod(MethodDeclarationSyntax method, string requestMethod)
    {
        return method.Body != null &&
               method.Body.DescendantNodes()
                      .OfType<LiteralExpressionSyntax>()
                      .Any(lit => lit.IsKind(SyntaxKind.StringLiteralExpression) &&
                                  lit.Token.ValueText.Equals(requestMethod, StringComparison.OrdinalIgnoreCase));
    }

    private static bool MethodContainsPathInComments(MethodDeclarationSyntax method, string path)
    {
        return method.DescendantTrivia()
                     .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                                      trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
                     .Any(trivia => trivia.ToString().Contains(path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Maps each invocation of the specified methods to their corresponding method definitions.
    /// </summary>
    /// <param name="solutionPath">Full file path of the .sln file.</param>
    /// <param name="methodNames">Array of method names to map.</param>
    /// <returns>A dictionary where each key is a method name and the value is a list of InvocationDefinitionPair instances representing each mapping.</returns>
    public static async Task<Dictionary<string, List<InvocationDefinitionPair>>> MapInvocationsToDefinitionsAsync(string solutionPath, string[] methodNames)
    {
        var invocationDefinitionDict = methodNames.ToDictionary(name => name, name => new List<InvocationDefinitionPair>());
        
        using var workspace = MSBuildWorkspace.Create();
        var solution = await workspace.OpenSolutionAsync(solutionPath);
    
        foreach (var project in solution.Projects)
        {
            var compilation = await project.GetCompilationAsync()
                                  ?? throw new InvalidOperationException($"Compilation for project {project.Name} is null.");
    
            foreach (var document in project.Documents)
            {
                var syntaxTree = await document.GetSyntaxTreeAsync()
                                      ?? throw new InvalidOperationException($"Syntax tree for document {document.Name} is null.");
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
    
                var root = await syntaxTree.GetRootAsync();
                var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();
    
                foreach (var invocation in invocations)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(invocation);
                    var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
    
                    if (methodSymbol != null && invocationDefinitionDict.ContainsKey(methodSymbol.Name))
                    {
                        // Resolve the method definition
                        var definitionReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                        MethodInfo methodInfo;
    
                        if (definitionReference != null)
                        {
                            var definitionSyntaxTree = definitionReference.SyntaxTree;
                            methodInfo = new MethodInfo
                            {
                                FilePath = definitionSyntaxTree.FilePath,
                                ContainingClass = methodSymbol.ContainingType?.Name ?? "Unknown"
                            };
                        }
                        else
                        {
                            methodInfo = new MethodInfo
                            {
                                FilePath = "External or No Source",
                                ContainingClass = methodSymbol.ContainingType?.Name ?? "Unknown"
                            };
                        }
    
                        // Determine the invocation location
                        var location = invocation.GetLocation().GetLineSpan();
                        // Retrieve the containing class for the invocation
                        var containingClass = invocation.Ancestors()
                                                      .OfType<ClassDeclarationSyntax>()
                                                      .FirstOrDefault()?.Identifier.Text ?? "Unknown";
    
                        var invocationLocation = new InvocationLocation
                        {
                            FilePath = syntaxTree.FilePath,
                            ContainingClass = containingClass,
                            LineNumber = location.StartLinePosition.Line + 1,
                            ColumnNumber = location.StartLinePosition.Character + 1
                        };
    
                        // Add the mapping to the dictionary
                        invocationDefinitionDict[methodSymbol.Name].Add(new InvocationDefinitionPair
                        {
                            Invocation = invocationLocation,
                            Definition = methodInfo
                        });
                    }
                }
            }
        }
    
        return invocationDefinitionDict;
    }
}
