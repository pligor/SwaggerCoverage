#!/bin/bash

# Define the root path and solution file
ROOT_PATH="C:\Users\SFP7ZGX\Downloads\repos\Post.Taf.BFF"
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
