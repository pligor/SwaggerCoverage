#!/bin/bash

# Check if required parameter is provided
if [ $# -lt 1 ]; then
    echo "Usage: $0 <ROOT_PATH>"
    echo ""
    echo "Parameters:"
    echo "  ROOT_PATH    Path to the root directory of the project (e.g., C:\\Users\\SFP7ZGX\\Downloads\\repos\\Post.Taf.BFF)"
    echo ""
    echo "Example:"
    echo "  $0 \"C:\\Users\\SFP7ZGX\\Downloads\\repos\\Post.Taf.BFF\""
    exit 1
fi

# Get ROOT_PATH from command line argument
ROOT_PATH="$1"
# Solution file is hardcoded
SOLUTION="Post.Taf.BFF.sln"

# Create directories for CSV and PNG results if they don't exist
mkdir -p results/csv
mkdir -p results/png

# Array of nswag json files
NSWAG_FILES=(
    "nswag_bff_auth_v1.json"
    "nswag_bff_auth_v2.json"
    "nswag_bff_auth_v3.json"
    "nswag_bff_auth_v4.json"
    "nswag_bff_public_v1.json"
    "nswag_bff_public_v2.json"
    "nswag_bff_public_v3.json"
)

# Process each nswag json file
for json_file in "${NSWAG_FILES[@]}"; do
    # Extract base name without extension
    base_name=$(basename "$json_file" .json)
    
    echo "Processing $json_file..."
    
    # Execute the command with specific output files
    dotnet run -- swaggerCoverage \
        --rootPath "$ROOT_PATH" \
        --nswagJson "src/Keywords/$json_file" \
        --solution "$SOLUTION" \
        --outputCsv "results/csv/${base_name}_invocations.csv" \
        --outputPng "results/png/${base_name}_invocationsChart.png"
        
    echo "Completed processing $json_file"
    echo "----------------------------------------"
done

echo "All files processed successfully!"
