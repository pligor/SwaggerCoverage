using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using static SwaggerCoverage.MethodAnalyzer;
using FluentAssertions;
namespace SwaggerCoverage;

public static class FilterUtils
{
  /// <summary>
  /// Filters invocations by Definition's ContainingClass and FilePath suffix, asserts both filters yield the same results,
  /// and returns the filtered dictionary.
  /// </summary>
  /// <param name="results">The JSON deserialized results.</param>
  /// <param name="className">The exact class name to filter by.</param>
  /// <param name="filePathSuffix">The file path suffix to filter by.</param>
  /// <returns>Dictionary of filtered InvocationDefinitionPair lists keyed by method name.</returns>
  public static Dictionary<string, List<InvocationDefinitionPair>> FilterByDefinition(
      Dictionary<string, List<InvocationDefinitionPair>> results,
      string className,
      string filePathSuffix)
  {
    // Filter by exact class name based on Definition
    var filteredByClass = results.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value
            .Where(pair => pair.Definition.ContainingClass.Equals(className, StringComparison.Ordinal))
            .ToList()
    );

    // Filter by file path suffix
    var filteredByFilePath = results.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value
            .Where(pair => pair.Definition.FilePath.EndsWith(filePathSuffix, StringComparison.OrdinalIgnoreCase))
            .ToList()
    );

    // Assert that both filtered dictionaries have equivalent values
    filteredByClass.Should().BeEquivalentTo(filteredByFilePath, "Filtered results by class name and file path suffix do not match.");

    return filteredByClass;
  }

  /// <summary>
  /// Filters out invocations that are in the same definition file and class as specified by <paramref name="className"/> and <paramref name="filePathSuffix"/>. 
  /// Asserts that for all remaining pairs, the invocation class and the definition class do not match.
  /// </summary>
  /// <param name="results">The JSON deserialized results.</param>
  /// <param name="className">The exact class name to filter out by.</param>
  /// <param name="filePathSuffix">The file path suffix to filter out by.</param>
  /// <returns>Dictionary of filtered InvocationDefinitionPair lists keyed by method name.</returns>
  public static Dictionary<string, List<InvocationDefinitionPair>> FilterOutInvocationsInDefinition(
      Dictionary<string, List<InvocationDefinitionPair>> results,
      string className,
      string filePathSuffix)
  {
    // Filter out pairs where the invocation is in the same class and file as provided
    var filteredResults = results.ToDictionary(
        kvp => kvp.Key,
        kvp => kvp.Value
            .Where(pair => !(pair.Invocation.ContainingClass.Equals(className, StringComparison.Ordinal) &&
                             pair.Invocation.FilePath.EndsWith(filePathSuffix, StringComparison.OrdinalIgnoreCase)))
            .ToList()
    );

    // Assert that for all remaining pairs, the invocation class and definition class do not match
    bool allNonMatching = filteredResults.Values
        .SelectMany(list => list)
        .All(pair => !pair.Invocation.ContainingClass.Equals(pair.Definition.ContainingClass, StringComparison.Ordinal));

    allNonMatching.Should().BeTrue("Some invocation classes match their definition classes.");

    // Assert that for all remaining pairs, the invocation file paths and definition file paths do not match
    bool allFilePathsNonMatching = filteredResults.Values
        .SelectMany(list => list)
        .All(pair => !pair.Invocation.FilePath.Equals(pair.Definition.FilePath, StringComparison.OrdinalIgnoreCase));

    allFilePathsNonMatching.Should().BeTrue("Some invocation file paths match their definition file paths.");

    return filteredResults;
  }
}