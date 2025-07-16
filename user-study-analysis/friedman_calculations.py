import pandas as pd
import numpy as np
from scipy.stats import friedmanchisquare
import warnings
warnings.filterwarnings('ignore')

print("=== FRIEDMAN TEST CALCULATIONS ===")
print("Calculating corrected statistics for results chapter\n")

# Load data
df = pd.read_csv('study-run-results.csv')

def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    try:
        parts = time_str.split(':')
        return int(parts[0]) + int(parts[1]) / 60
    except:
        return np.nan

# Prepare data
df['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(parse_time)
df['dyad_id'] = df['Participant 1 ID'].str.split('-').str[0]

# Create movement distance total
df['total_movement'] = pd.to_numeric(df['Participant 1 distance (meter)'], errors='coerce') + \
                      pd.to_numeric(df['Participant 2 distance (meter)'], errors='coerce')

# Create movement speed (movement per minute)
df['movement_speed'] = df['total_movement'] / df['completion_time_minutes']

# Parse bridge metrics
df['safety_factor'] = pd.to_numeric(df['Bridge evaluation 1: Safety Factor (min, higher better)'], errors='coerce')
df['von_mises_stress'] = pd.to_numeric(df['Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)'], errors='coerce')
df['displacement'] = pd.to_numeric(df['Bridge evaluation 3: Displacement (max, in mm, smaller better)'], errors='coerce')
df['bridge_price'] = pd.to_numeric(df['Bridge Price'], errors='coerce')

# Construction efficiency calculation
def parse_objects(obj_str):
    if pd.isna(obj_str) or obj_str == 'INVALID':
        return np.nan
    try:
        total = 0
        parts = obj_str.split(', ')
        for part in parts:
            count = int(part.split(' ×')[1])
            total += count
        return total
    except:
        return np.nan

df['spawned_objects'] = df['Spawned objects'].apply(parse_objects)
df['final_objects'] = pd.to_numeric(df['# objects (final bridge, start block incl.)'], errors='coerce')
df['construction_efficiency'] = df['final_objects'] / df['spawned_objects']

# Environment as numeric
df['environment_num'] = pd.to_numeric(df['Environment'], errors='coerce')

print("Data loaded. Calculating Friedman tests...\n")

def calculate_friedman_for_metric(df, metric_name, by_column='Variant'):
    """Calculate Friedman test for a metric grouped by specified column"""
    pivot_data = df.pivot_table(
        values=metric_name,
        index='dyad_id',
        columns=by_column,
        aggfunc='first'
    )
    
    # Remove rows with missing data
    pivot_complete = pivot_data.dropna()
    
    if len(pivot_complete) < 3:
        print(f"Insufficient data for {metric_name} by {by_column}")
        return None, None
    
    try:
        # Extract data for each group
        groups = [pivot_complete[col].values for col in pivot_complete.columns]
        statistic, p_value = friedmanchisquare(*groups)
        return statistic, p_value
    except Exception as e:
        print(f"Error calculating Friedman test for {metric_name}: {e}")
        return None, None

# 1. Completion time (already have: χ² = 12.750, p = 0.005)
chi2, p = calculate_friedman_for_metric(df, 'completion_time_minutes', 'Variant')
print(f"1. Completion time by Variant:")
print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 2. Construction efficiency by Variant  
chi2, p = calculate_friedman_for_metric(df, 'construction_efficiency', 'Variant')
print(f"2. Construction efficiency by Variant:")
if chi2 is not None:
    print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 3. Total movement by Variant
chi2, p = calculate_friedman_for_metric(df, 'total_movement', 'Variant')
print(f"3. Total movement by Variant:")
if chi2 is not None:
    print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 4. Movement speed by Variant
chi2, p = calculate_friedman_for_metric(df, 'movement_speed', 'Variant')
print(f"4. Movement speed by Variant:")
if chi2 is not None:
    print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 5. Completion time by Environment
chi2, p = calculate_friedman_for_metric(df, 'completion_time_minutes', 'Environment')
print(f"5. Completion time by Environment:")
if chi2 is not None:
    print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 6. Completion time by Position (learning effects)
chi2, p = calculate_friedman_for_metric(df, 'completion_time_minutes', 'Position in study')
print(f"6. Completion time by Position (learning effects):")
if chi2 is not None:
    print(f"   χ² = {chi2:.3f}, p = {p:.3f}")

# 7. Bridge quality metrics by Variant
print(f"\n7. Bridge quality by Variant:")
for metric in ['safety_factor', 'von_mises_stress', 'displacement', 'bridge_price']:
    chi2, p = calculate_friedman_for_metric(df, metric, 'Variant')
    if chi2 is not None:
        print(f"   {metric}: χ² = {chi2:.3f}, p = {p:.3f}")

print(f"\nCalculations complete!") 