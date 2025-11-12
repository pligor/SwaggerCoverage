#!/usr/bin/env python3
"""Calculate API coverage statistics from CSV files."""

import os
import sys
import pandas as pd
from pathlib import Path
from typing import List, Tuple


def process_csv_file(csv_path: Path) -> Tuple[int, int]:
  """
  Process a single CSV file and return numerator and denominator.
  
  Args:
    csv_path: Path to the CSV file
    
  Returns:
    Tuple of (numerator, denominator) where:
    - numerator: count of rows with non-zero Count values
    - denominator: total number of rows (excluding header)
  """
  df = pd.read_csv(csv_path)
  
  # Count rows with non-zero values in the Count column
  non_zero_count = (df['Count'] != 0).sum()
  
  # Total number of rows (excluding header)
  total_rows = len(df)
  
  return (non_zero_count, total_rows)


def main() -> None:
  """Main function to process all CSV files and calculate statistics."""
  # Get the directory from command line argument or use script's directory
  if len(sys.argv) > 1:
    csv_dir = Path(sys.argv[1])
    if not csv_dir.exists():
      print(f"Error: Directory '{csv_dir}' does not exist.")
      return
    if not csv_dir.is_dir():
      print(f"Error: '{csv_dir}' is not a directory.")
      return
  else:
    csv_dir = Path(__file__).parent
  
  # Find all CSV files in the directory (excluding lock files)
  csv_files = [
    f for f in csv_dir.glob('*.csv')
    if not f.name.startswith('.~lock')
  ]
  
  if not csv_files:
    print("No CSV files found in the directory.")
    return
  
  numerators: List[int] = []
  denominators: List[int] = []
  fractions: List[float] = []
  
  print(f"Processing {len(csv_files)} CSV file(s)...\n")
  
  # Process each CSV file
  for csv_file in sorted(csv_files):
    try:
      numerator, denominator = process_csv_file(csv_file)
      
      if denominator == 0:
        print(f"Warning: {csv_file.name} has no data rows, skipping.")
        continue
      
      fraction = numerator / denominator
      
      numerators.append(numerator)
      denominators.append(denominator)
      fractions.append(fraction)
      
      print(f"{csv_file.name}:")
      print(f"  Non-zero entries: {numerator}")
      print(f"  Total entries: {denominator}")
      print(f"  Fraction: {fraction:.4f} ({numerator}/{denominator})")
      print()
      
    except Exception as e:
      print(f"Error processing {csv_file.name}: {e}")
      continue
  
  if not fractions:
    print("No valid data found in any CSV files.")
    return
  
  # Calculate statistics
  average_of_fractions = sum(fractions) / len(fractions) if fractions else 0.0
  total_numerator = sum(numerators)
  total_denominator = sum(denominators)
  overall_fraction = total_numerator / total_denominator if total_denominator > 0 else 0.0
  
  # Print results
  print("=" * 50)
  print("STATISTICS:")
  print("=" * 50)
  print(f"1. Average of all fractions: {average_of_fractions:.4f}")
  print(f"2. Overall fraction (sum of numerators / sum of denominators):")
  print(f"   {overall_fraction:.4f} ({total_numerator}/{total_denominator})")
  print("=" * 50)


if __name__ == '__main__':
  main()

