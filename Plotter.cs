using Microsoft.Data.Analysis;
using ScottPlot;

namespace SwaggerCoverage;

public class Plotter
{

    /// <summary>
    /// Plots a horizontal bar chart using ScottPlot with counts from the specified CSV file.
    /// </summary>
    /// <param name="csvPath">Path to the CSV file containing the data.</param>
    /// <param name="outputPath">Path where the plot image will be saved.</param>
    public static void PlotHorizontalBarChart(DataFrame df, string outputPath, int width = 1024, int height = 768, bool debug = false)
    {
        var requestColumn = df.Columns["Request"] as StringDataFrameColumn ?? throw new Exception("The 'Request' column is not of type string.");

        string[] requests = [.. requestColumn];

        var countColumn = df.Columns["Count"] as PrimitiveDataFrameColumn<int> ?? throw new Exception("The 'Count' column is not of type integer.");
        
        int[] counts = countColumn.Select(c => c ?? throw new Exception("The 'Count' column should not contain null values.")).ToArray();

        // Convert counts to double for ScottPlot
        double[] countsDouble = counts.Select(c => (double)c).ToArray();

        // Create a new ScottPlot
        var plt = new Plot();

        // Create positions for the bars
        double[] positions = Enumerable.Range(0, requests.Length).Select(i => (double)i).ToArray();

        // Create an array of Bar objects
        var bars = new Bar[requests.Length];
        for (int i = 0; i < requests.Length; i++)
        {
            bars[i] = new Bar
            {
                Position = requests.Length - i, // Reversed positions
                Value = counts[i],
                Label = requests[i]
            };
        }


        // Add bars to the plot
        var barPlot = plt.Add.Bars(bars);
        barPlot.Horizontal = true;

        // Set the right margin to accommodate the labels
        plt.Axes.Margins(right: 0.4); // Adjust the value as needed

        // Set labels and title
        plt.Title("Invocation Counts");
        plt.XLabel("Count");
        plt.YLabel("Request");

        // Automatically adjust axis limits to fit the data
        plt.Axes.AutoScale();

        // Save the plot as an image
        plt.SavePng(outputPath, width, height);
        if (debug)
        {
          Console.WriteLine($"Horizontal bar chart saved to {outputPath}");
        }
    }
}