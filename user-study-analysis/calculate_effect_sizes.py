import pandas as pd
import numpy as np
from scipy import stats
from scipy.stats import friedmanchisquare, wilcoxon
import warnings
warnings.filterwarnings('ignore')

# Load the data
df = pd.read_csv('study-run-results.csv')

# Clean completion times - convert to minutes and handle INVALID entries
def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    if ':' in str(time_str):
        parts = str(time_str).split(':')
        return float(parts[0]) + float(parts[1])/60
    return float(time_str)

df['completion_time_min'] = df['Completion time (exact, minutes)'].apply(parse_time)

# Clean bridge quality metrics
def safe_float(x):
    if pd.isna(x) or x == 'INVALID':
        return np.nan
    try:
        # Handle comma-separated thousands
        if isinstance(x, str) and ',' in x:
            x = x.replace(',', '')
        return float(x)
    except:
        return np.nan

df['safety_factor'] = df['Bridge evaluation 1: Safety Factor (min, higher better)'].apply(safe_float)
df['von_mises'] = df['Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)'].apply(safe_float)
df['displacement'] = df['Bridge evaluation 3: Displacement (max, in mm, smaller better)'].apply(safe_float)
df['bridge_price'] = df['Bridge Price'].apply(safe_float)

print("=== EFFECT SIZE CALCULATIONS ===\n")

# 1. COMPLETION TIME BY VARIANT - Friedman Test
print("1. COMPLETION TIME BY VARIANT")
print("-" * 40)

# Create dyad-wise data for repeated measures
dyads = []
for dyad_id in range(8):
    dyad_runs = df[df['Run #'].isin([dyad_id*4, dyad_id*4+1, dyad_id*4+2, dyad_id*4+3])]
    if len(dyad_runs) == 4:
        times = []
        for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']:
            time = dyad_runs[dyad_runs['Variant'] == variant]['completion_time_min'].iloc[0]
            times.append(time)
        if not any(pd.isna(times)):  # Only include complete data
            dyads.append(times)

dyads = np.array(dyads)
print(f"Valid dyads for completion time analysis: {len(dyads)}")
print(f"Variants: Open Ended, Silent, Timed, Roleplay")

if len(dyads) > 0:
    # Friedman test
    stat, p = friedmanchisquare(*dyads.T)
    
    # Calculate Kendall's W (effect size for Friedman test)
    N = len(dyads)  # number of subjects (dyads)
    k = 4  # number of conditions (variants)
    kendalls_w = stat / (N * (k - 1))
    
    print(f"Chi-square: {stat:.3f}")
    print(f"p-value: {p:.3f}")
    print(f"Kendall's W: {kendalls_w:.3f}")
    
    # Effect size interpretation
    if kendalls_w < 0.1:
        effect_size = "small"
    elif kendalls_w < 0.3:
        effect_size = "medium" 
    else:
        effect_size = "large"
    print(f"Effect size: {effect_size}")
    
    # Post-hoc Wilcoxon tests for significant pairs
    variants = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    variant_data = {variant: dyads[:, i] for i, variant in enumerate(variants)}
    
    print(f"\nPost-hoc pairwise comparisons:")
    significant_pairs = []
    
    for i, var1 in enumerate(variants):
        for j, var2 in enumerate(variants):
            if i < j:
                data1 = variant_data[var1]
                data2 = variant_data[var2]
                
                # Wilcoxon signed-rank test
                stat, p = wilcoxon(data1, data2)
                
                # Effect size r = Z / sqrt(N)
                # For Wilcoxon, we can estimate Z from the test statistic
                n = len(data1)
                z = stats.norm.ppf(1 - p/2)  # approximate Z from p-value
                r = z / np.sqrt(n)
                
                print(f"  {var1} vs {var2}: W={stat:.1f}, p={p:.3f}, r={abs(r):.2f}")
                
                if p < 0.05:
                    significant_pairs.append((var1, var2, stat, p, abs(r)))

print("\n" + "="*50 + "\n")

# 2. BRIDGE QUALITY METRICS - Friedman Tests
print("2. BRIDGE QUALITY BY VARIANT")
print("-" * 40)

quality_metrics = ['safety_factor', 'von_mises', 'displacement', 'bridge_price']
quality_names = ['Safety Factor', 'von Mises Stress', 'Displacement', 'Bridge Price']

for metric, name in zip(quality_metrics, quality_names):
    print(f"\n{name}:")
    
    # Create dyad-wise data for this metric
    dyads_quality = []
    for dyad_id in range(8):
        dyad_runs = df[df['Run #'].isin([dyad_id*4, dyad_id*4+1, dyad_id*4+2, dyad_id*4+3])]
        if len(dyad_runs) == 4:
            values = []
            for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']:
                variant_data = dyad_runs[dyad_runs['Variant'] == variant]
                if len(variant_data) > 0:
                    value = variant_data[metric].iloc[0]
                    values.append(value)
            if len(values) == 4 and not any(pd.isna(values)):
                dyads_quality.append(values)
    
    dyads_quality = np.array(dyads_quality)
    
    if len(dyads_quality) > 0:
        stat, p = friedmanchisquare(*dyads_quality.T)
        N = len(dyads_quality)
        k = 4
        kendalls_w = stat / (N * (k - 1))
        
        if kendalls_w < 0.1:
            effect_size = "small"
        elif kendalls_w < 0.3:
            effect_size = "medium"
        else:
            effect_size = "large"
            
        print(f"  χ²={stat:.3f}, p={p:.3f}, W={kendalls_w:.3f} ({effect_size} effect)")
    else:
        print(f"  Insufficient valid data for analysis")

print("\n" + "="*50 + "\n")

# 3. MOVEMENT CORRELATIONS
print("3. MOVEMENT CORRELATIONS")
print("-" * 40)

# Calculate partner movement correlations by variant
df['distance_1'] = pd.to_numeric(df['Participant 1 distance (meter)'], errors='coerce')
df['distance_2'] = pd.to_numeric(df['Participant 2 distance (meter)'], errors='coerce')

print("Partner movement distance correlations by variant:")
for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']:
    variant_data = df[df['Variant'] == variant].copy()
    
    # Remove rows with invalid data
    valid_data = variant_data.dropna(subset=['distance_1', 'distance_2'])
    
    if len(valid_data) > 2:
        r, p = stats.pearsonr(valid_data['distance_1'], valid_data['distance_2'])
        
        # Effect size interpretation for correlations
        if abs(r) < 0.1:
            effect_size = "small"
        elif abs(r) < 0.3:
            effect_size = "medium"
        else:
            effect_size = "large"
            
        print(f"  {variant}: r={r:.3f}, p={p:.3f}, r²={r**2:.3f} ({effect_size} effect)")
    else:
        print(f"  {variant}: Insufficient data")

print("\n" + "="*50 + "\n")

# 4. COMMUNICATION vs SILENT COMPARISON
print("4. COMMUNICATION vs SILENT COMPARISON")
print("-" * 40)

# Mann-Whitney U test for Silent vs Combined Verbal conditions
silent_times = df[df['Variant'] == 'Silent']['completion_time_min'].dropna()
verbal_times = df[df['Variant'].isin(['Open Ended', 'Roleplay'])]['completion_time_min'].dropna()

if len(silent_times) > 0 and len(verbal_times) > 0:
    stat, p = stats.mannwhitneyu(silent_times, verbal_times, alternative='two-sided')
    
    # Effect size for Mann-Whitney U: r = Z / sqrt(N)
    n1, n2 = len(silent_times), len(verbal_times)
    z = stats.norm.ppf(1 - p/2)
    r = z / np.sqrt(n1 + n2)
    
    print(f"Silent vs Verbal conditions:")
    print(f"  U={stat:.1f}, p={p:.3f}, r={abs(r):.2f}")
    print(f"  Silent mean: {silent_times.mean():.2f} min")
    print(f"  Verbal mean: {verbal_times.mean():.2f} min")

print("\nEffect size calculations complete!") 