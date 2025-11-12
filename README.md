# Swagger Coverage Analyzer

![Swagger Coverage](./assets/swagger-coverage.png)

## Overview

**Swagger Coverage Analyzer** is a powerful tool designed to evaluate how well your .NET solution leverages the APIs defined in your Swagger (NSwag) specification. By analyzing method invocations within your codebase, this tool provides insightful metrics on API usage, helping you identify coverage gaps and optimize your application's interaction with external services.

## Purpose

In modern software development, ensuring that your application effectively utilizes the defined APIs is crucial for maintainability and scalability. Swagger Coverage Analyzer automates the process of mapping Swagger-defined API requests to their actual usage within your .NET solution. This allows developers and architects to:

- **Identify Unused APIs:** Detect APIs that are defined but not utilized in the codebase.
- **Understand API Utilization:** Gain insights into how frequently each API is invoked.
- **Optimize Codebase:** Refine and optimize interactions with external services based on usage patterns.

## Features

- **Swagger Parsing:** Extracts all API requests defined in your Swagger (`nswag.json`) file.
- **Method Mapping:** Maps each API request to its corresponding method in the generated client.
- **Invocation Analysis:** Scans the entire solution to identify and count method invocations.
- **Data Visualization:** Generates CSV reports and PNG charts to visualize API usage statistics.
- **Debug Mode:** Offers detailed console outputs for deeper insights during analysis.

## Installation

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/yourusername/swagger-coverage-analyzer.git
   ```
2. **Navigate to the Project Directory:**
   ```bash
   cd swagger-coverage-analyzer
   ```
3. **Restore Dependencies:**
   ```bash
   dotnet restore
   ```
4. **Build the Project:**
   ```bash
   dotnet build
   ```

## Usage

Run the analyzer using the `dotnet run` command with the `swaggerCoverage` command and the necessary options:

```bash
dotnet run -- swaggerCoverage --rootPath "<rootPath>" --nswagJson "<nswagJsonRelativePath>" --solution "<SolutionRelativePath>" [options]
```

### Example

```bash
dotnet run -- swaggerCoverage --rootPath "/projs/msTests" --nswagJson "nswag.json" --solution "MyTests.sln" --sortBy "Request" --outputCsv "results/invocations.csv" --outputPng "results/invocationsChart.png" --debug true
```

### Commands and Options

- `swaggerCoverage`: The primary command to execute the Swagger Coverage Analyzer.

#### Required Options:

- `--rootPath`  
  **Description:** Path to the root directory of your .NET solution.  
  **Example:** `"/projs/msTests"`

- `--nswagJson`  
  **Description:** Relative path to the `nswag.json` file.  
  **Example:** `"nswag.json"`

- `--solution`  
  **Description:** Relative path to the `.sln` (solution) file.  
  **Example:** `"MyTests.sln"`

#### Optional Options:

- `--sortBy`  
  **Description:** Column to sort the results by.  
  **Default:** `"Count"`  
  **Options:** `"Count"`, `"Request"`  
  **Example:** `"Request"`

- `--outputCsv`  
  **Description:** Path to the output CSV file.  
  **Default:** `"invocationsCount.csv"`  
  **Example:** `"results/invocations.csv"`

- `--outputPng`  
  **Description:** Path to the output PNG file for the chart.  
  **Default:** `"invocationsCount.png"`  
  **Example:** `"results/invocationsChart.png"`

- `--debug`  
  **Description:** Enable debug mode for detailed console output.  
  **Default:** `false`  
  **Example:** `true`

## How It Works: A Story

Imagine you're a developer working on a large .NET solution with numerous API endpoints defined in your `nswag.json` file. You want to ensure that all these APIs are actively used within your codebase to avoid dead code and optimize performance.

1. **Initialization:** You execute the Swagger Coverage Analyzer with the appropriate paths to your `nswag.json` and solution files.
2. **Client Extraction:** The tool first extracts information about the generated client from the Swagger specification, identifying the file path and class name responsible for API interactions.
3. **Request Extraction:** It then parses the `nswag.json` file to extract all defined API requests.
4. **Method Mapping:** Each extracted API request is mapped to its corresponding method in the generated client class.
5. **Invocation Mapping:** The analyzer scans the entire solution to find all invocations of these mapped methods, determining where and how often each API is called.
6. **Filtering:** It filters out invocations that are part of the generated client itself, focusing only on external usages within your application.
7. **Counting Invocations:** For each API request, the tool counts the number of times it's invoked across the solution.
8. **Data Packaging:** The invocation counts are compiled into a CSV file and visualized as a horizontal bar chart (PNG), providing a clear overview of API usage.
9. **Output:** You receive a detailed report highlighting which APIs are frequently used, which are underutilized, and any potential areas for codebase optimization.

## Expected Results

Upon successful execution, Swagger Coverage Analyzer generates the following outputs:

1. **CSV Report (`invocationsCount.csv`):**
   - **Columns:**
     - **Request:** The API request identifier.
     - **Count:** The number of times the request is invoked within the solution.
   - **Purpose:** Provides a detailed breakdown of API invocation frequencies, which can be used for further analysis or reporting.

2. **Visualization (`invocationsCount.png`):**
   - **Description:** A horizontal bar chart representing the invocation counts for each API request.
   - **Purpose:** Offers a quick visual reference to identify high and low usage APIs at a glance.

### Sample Output

**invocationsCount.csv**
| Request                         | Count |
|---------------------------------|-------|
| GET /api/users                  | 15    |
| POST /api/orders                | 7     |
| DELETE /api/products/{id}       | 3     |
| ...                             | ...   |

**invocationsCount.png**
![Invocation Counts](./assets/invocationsCount.png)

## Coverage Statistics Calculator

The `calculate_coverage.py` script provides aggregate statistics across multiple CSV files generated by the Swagger Coverage Analyzer. This tool is particularly useful when analyzing multiple API specifications or when you need to calculate overall coverage metrics.

### Purpose

After generating multiple CSV files (e.g., from different Swagger specifications), you can use this script to:
- Calculate coverage statistics for each CSV file individually
- Compute aggregate statistics across all CSV files
- Get both per-file and overall coverage metrics

### How It Works

For each CSV file, the script:
1. Counts the number of API requests with non-zero invocation counts (numerator)
2. Counts the total number of API requests (denominator)
3. Calculates the coverage fraction for that file

The script then provides two aggregate statistics:
- **Average of all fractions:** The mean coverage across all CSV files
- **Overall fraction:** The total number of covered APIs divided by the total number of APIs across all files

### Prerequisites

- Python 3.x
- pandas library

Install pandas if needed:
```bash
pip install pandas
```

### Usage

#### Basic Usage (Default Directory)

When run without arguments, the script processes all CSV files in its own directory:

```bash
python calculate_coverage.py
```

#### Custom Directory

To process CSV files from a different directory, provide the directory path as the first argument:

```bash
python calculate_coverage.py /path/to/csv/files
```

#### Examples

```bash
# Process CSV files in the script's directory
python calculate_coverage.py

# Process CSV files from a specific directory
python calculate_coverage.py results/csv

# Process CSV files from an absolute path
python calculate_coverage.py "C:\Users\SFP7ZGX\Downloads\repos\SwaggerCoverage\results\csv"
```

### Output

The script provides detailed output for each CSV file and summary statistics:

```
Processing 7 CSV file(s)...

nswag_bff_auth_v1_invocations.csv:
  Non-zero entries: 9
  Total entries: 42
  Fraction: 0.2143 (9/42)

nswag_bff_auth_v2_invocations.csv:
  Non-zero entries: 9
  Total entries: 13
  Fraction: 0.6923 (9/13)

...

==================================================
STATISTICS:
==================================================
1. Average of all fractions: 0.3876
2. Overall fraction (sum of numerators / sum of denominators):
   0.3750 (42/112)
==================================================
```

### Understanding the Statistics

- **Average of all fractions:** This metric treats each CSV file equally, calculating the mean of individual coverage percentages. Useful when you want to see the average coverage across different API specifications.

- **Overall fraction:** This metric aggregates all APIs from all files, then calculates the coverage. Useful when you want to know the overall coverage across your entire API surface area.

### Notes

- The script automatically excludes lock files (files starting with `.~lock`)
- Files with no data rows are skipped with a warning
- The script handles errors gracefully and continues processing remaining files

## Debugging

For developers seeking deeper insights during the analysis process, enable the debug mode by setting the `--debug` flag to `true` when running the command. This will provide detailed console outputs, including:

- Extracted requests and corresponding method mappings.
- Invocation details and filtered results.
- Serialized JSON outputs of internal states for further examination.

### Example with Debug Mode

```bash
dotnet run -- swaggerCoverage --rootPath "/projs/msTests" --nswagJson "nswag.json" --solution "MyTests.sln" --debug true
```

## Contribution

Contributions are welcome! Please fork the repository and submit pull requests for any enhancements or bug fixes.

## License

This project is licensed under the GNU Affero General Public License v3.0.

## Contact

For any questions or support, please submit an issue in GitHub.

# Acknowledgments

- Inspired by the need for comprehensive API usage analysis in .NET applications.
- Utilizes libraries like [FluentAssertions](https://fluentassertions.com/) and [Microsoft.Data.Analysis](https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.analysis) for robust functionality.

---

Happy Coding! 🚀
