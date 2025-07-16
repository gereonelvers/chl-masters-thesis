import pandas as pd
import numpy as np
from scipy import stats
from scipy.stats import friedmanchisquare, wilcoxon
import warnings
warnings.filterwarnings('ignore')

print("=== CORRECTED BRIDGE QUALITY ANALYSIS ===")
print("Using proper statistical methods for repeated measures data")

# Load data
df = pd.read_csv('study-run-results.csv')

# Parse completion time for reference
def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    try:
        parts = time_str.split(':')
        return int(parts[0]) + int(parts[1]) / 60
    except:
        return np.nan

df['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(parse_time)

# Clean bridge quality data
def clean_numeric(value):
    if pd.isna(value) or value == 'INVALID':
        return np.nan
    try:
        return float(str(value).replace(',', ''))
    except:
        return np.nan

# Clean the bridge quality columns
quality_columns = [
    'Bridge Price',
    'Bridge height (max., cm)',
    'Bridge evaluation 1: Safety Factor (min, higher better)',
    'Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)',
    'Bridge evaluation 3: Displacement (max, in mm, smaller better)',
    '# objects (final bridge, start block incl.)',
    '# different objects (final bridge)'
]

for col in quality_columns:
    df[col] = df[col].apply(clean_numeric)

# Rename columns for easier handling
df = df.rename(columns={
    'Bridge Price': 'price',
    'Bridge height (max., cm)': 'height',
    'Bridge evaluation 1: Safety Factor (min, higher better)': 'safety_factor',
    'Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)': 'von_mises_stress',
    'Bridge evaluation 3: Displacement (max, in mm, smaller better)': 'displacement',
    '# objects (final bridge, start block incl.)': 'total_objects',
    '# different objects (final bridge)': 'different_objects'
})

# Create dyad identifiers
df['dyad_id'] = df['Participant 1 ID'].str.split('-').str[0]

# Remove invalid entries (keep only cases with valid structural analysis)
df_valid = df.dropna(subset=['safety_factor', 'von_mises_stress', 'displacement']).copy()

print(f"Total runs: {len(df)}")
print(f"Valid bridge quality data: {len(df_valid)}")
print(f"Failed/Invalid structural analysis: {len(df) - len(df_valid)}")

# Key metrics to analyze
metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price']

print("\n" + "="*60)
print("PROBLEMS WITH ORIGINAL ANALYSIS")
print("="*60)

# Show current problematic approach
for metric in metrics:
    if metric in df_valid.columns and not df_valid[metric].isna().all():
        variant_groups = [group[metric].dropna().values for name, group in df_valid.groupby('Variant')]
        variant_groups = [group for group in variant_groups if len(group) > 0]
        if len(variant_groups) >= 2:
            f_stat, p_val = stats.f_oneway(*variant_groups)
            print(f"INVALID ANOVA {metric}: F={f_stat:.3f}, p={p_val:.3f} (violates independence!)")

print("\n" + "="*60)
print("CORRECTED ANALYSIS: REPEATED MEASURES")
print("="*60)

# Create pivot tables for each metric
results = {}
for metric in metrics:
    if metric in df_valid.columns:
        print(f"\n--- {metric.upper().replace('_', ' ')} ---")
        
        # Create pivot table for repeated measures
        pivot_data = df_valid.pivot_table(
            values=metric, 
            index='dyad_id', 
            columns='Variant', 
            aggfunc='first'
        )
        
        print(f"Pivot structure for {metric}:")
        print(pivot_data.round(3))
        
        # Check complete cases
        complete_cases = pivot_data.dropna()
        print(f"Complete cases: {len(complete_cases)}")
        
        if len(complete_cases) >= 3:
            # Friedman test
            friedman_data = [complete_cases[col].values for col in complete_cases.columns]
            friedman_stat, friedman_p = friedmanchisquare(*friedman_data)
            print(f"Friedman test: χ² = {friedman_stat:.3f}, p = {friedman_p:.3f}")
            
            # Store results
            results[metric] = {
                'friedman_stat': friedman_stat,
                'friedman_p': friedman_p,
                'complete_cases': len(complete_cases)
            }
            
            # Pairwise Wilcoxon tests if significant
            if friedman_p < 0.10:  # Use liberal threshold for post-hoc
                print("Post-hoc Wilcoxon signed-rank tests:")
                variants = complete_cases.columns
                for i, var1 in enumerate(variants):
                    for var2 in variants[i+1:]:
                        stat, p_wilcox = wilcoxon(complete_cases[var1], complete_cases[var2])
                        print(f"  {var1} vs {var2}: W = {stat:.0f}, p = {p_wilcox:.3f}")
        else:
            print(f"Insufficient complete cases for {metric}")
            results[metric] = {'friedman_stat': np.nan, 'friedman_p': np.nan, 'complete_cases': len(complete_cases)}

print("\n" + "="*60)
print("NON-PARAMETRIC ALTERNATIVES")
print("="*60)

# Kruskal-Wallis tests (still problematic but more robust)
print("Kruskal-Wallis tests (more robust than ANOVA):")
for metric in metrics:
    if metric in df_valid.columns and not df_valid[metric].isna().all():
        variant_groups = [group[metric].dropna().values for name, group in df_valid.groupby('Variant')]
        variant_groups = [group for group in variant_groups if len(group) > 0]
        if len(variant_groups) >= 2:
            h_stat, p_val = stats.kruskal(*variant_groups)
            print(f"{metric}: H = {h_stat:.3f}, p = {p_val:.3f}")

print("\n" + "="*60)
print("ASSUMPTION CHECKING")
print("="*60)

# Check normality for each metric by variant
for metric in metrics:
    if metric in df_valid.columns:
        print(f"\nShapiro-Wilk normality tests for {metric}:")
        for variant in df_valid['Variant'].unique():
            group_data = df_valid[df_valid['Variant'] == variant][metric].dropna()
            if len(group_data) >= 3:
                stat, p = stats.shapiro(group_data)
                print(f"  {variant}: W = {stat:.3f}, p = {p:.3f} {'(Normal)' if p > 0.05 else '(Non-normal)'}")

# Check homogeneity of variance
print("\nLevene's tests for equal variances:")
for metric in metrics:
    if metric in df_valid.columns and not df_valid[metric].isna().all():
        variant_groups = [group[metric].dropna().values for name, group in df_valid.groupby('Variant')]
        variant_groups = [group for group in variant_groups if len(group) > 0]
        if len(variant_groups) >= 2:
            levene_stat, levene_p = stats.levene(*variant_groups)
            print(f"  {metric}: W = {levene_stat:.3f}, p = {levene_p:.3f} {'(Equal var)' if levene_p > 0.05 else '(Unequal var)'}")

print("\n" + "="*60)
print("EFFECT SIZES")
print("="*60)

# Calculate effect sizes for significant differences
def cohens_d(x, y):
    pooled_std = np.sqrt(((len(x) - 1) * np.var(x, ddof=1) + (len(y) - 1) * np.var(y, ddof=1)) / (len(x) + len(y) - 2))
    return (np.mean(x) - np.mean(y)) / pooled_std

for metric in metrics:
    if metric in df_valid.columns:
        print(f"\nCohen's d effect sizes for {metric}:")
        variants = df_valid['Variant'].unique()
        for i, var1 in enumerate(variants):
            for var2 in variants[i+1:]:
                group1 = df_valid[df_valid['Variant'] == var1][metric].dropna()
                group2 = df_valid[df_valid['Variant'] == var2][metric].dropna()
                if len(group1) > 0 and len(group2) > 0:
                    d = cohens_d(group1, group2)
                    effect_size = "Small" if abs(d) < 0.5 else "Medium" if abs(d) < 0.8 else "Large"
                    print(f"  {var1} vs {var2}: d = {d:.3f} ({effect_size})")

print("\n" + "="*60)
print("COMPLETION RATE ANALYSIS")
print("="*60)

# Analyze completion rates by variant
completion_stats = df.groupby('Variant').apply(
    lambda x: pd.Series({
        'total_attempts': len(x),
        'successful': len(x.dropna(subset=['safety_factor'])),
        'failed': len(x) - len(x.dropna(subset=['safety_factor'])),
        'completion_rate': len(x.dropna(subset=['safety_factor'])) / len(x)
    })
)

print("Completion rates by variant:")
print(completion_stats)

# Fisher's exact test for completion rate differences
from scipy.stats import fisher_exact

# Compare Timed vs others
timed_success = completion_stats.loc['Timed', 'successful']
timed_total = completion_stats.loc['Timed', 'total_attempts']
other_success = completion_stats.drop('Timed')['successful'].sum()
other_total = completion_stats.drop('Timed')['total_attempts'].sum()

contingency_table = [[timed_success, timed_total - timed_success],
                     [other_success, other_total - other_success]]

odds_ratio, p_fisher = fisher_exact(contingency_table)
print(f"\nFisher's exact test (Timed vs Others completion rate):")
print(f"Odds ratio: {odds_ratio:.3f}, p = {p_fisher:.3f}")

print("\n" + "="*60)
print("SUMMARY OF CORRECTED RESULTS")
print("="*60)

print("BRIDGE QUALITY CORRECTED ANALYSIS:")
for metric, result in results.items():
    if not np.isnan(result['friedman_p']):
        print(f"- {metric}: Friedman χ² = {result['friedman_stat']:.3f}, p = {result['friedman_p']:.3f}")
    else:
        print(f"- {metric}: Insufficient data for repeated measures analysis")

print(f"\nCOMPLETION RATES:")
print(f"- Timed variant: {completion_stats.loc['Timed', 'completion_rate']:.1%} success rate")
print(f"- Other variants: {(other_success/other_total):.1%} success rate")
print(f"- Fisher's exact test: p = {p_fisher:.3f}")

# Save corrected results
with open('corrected_bridge_quality_results.txt', 'w') as f:
    f.write("CORRECTED BRIDGE QUALITY ANALYSIS RESULTS\n")
    f.write("==========================================\n\n")
    f.write("ORIGINAL PROBLEMS:\n")
    f.write("- Used one-way ANOVAs on repeated measures data (independence violation)\n")
    f.write("- Ignored dyadic dependencies\n")
    f.write("- Did not properly handle failed attempts\n\n")
    
    f.write("CORRECTED METHODS:\n")
    f.write("- Friedman tests for repeated measures\n")
    f.write("- Fisher's exact test for completion rates\n")
    f.write("- Proper effect size calculations\n")
    f.write("- Assumption checking\n\n")
    
    f.write("KEY FINDINGS:\n")
    for metric, result in results.items():
        if not np.isnan(result['friedman_p']):
            f.write(f"- {metric}: Friedman χ² = {result['friedman_stat']:.3f}, p = {result['friedman_p']:.3f}\n")
    
    f.write(f"\nCOMPLETION RATE ANALYSIS:\n")
    f.write(f"- Timed variant completion rate: {completion_stats.loc['Timed', 'completion_rate']:.1%}\n")
    f.write(f"- Other variants completion rate: {(other_success/other_total):.1%}\n")
    f.write(f"- Fisher's exact test: p = {p_fisher:.3f}\n")

print(f"\nCorrected results saved to 'corrected_bridge_quality_results.txt'")
print("Use these corrected statistics in your paper!") 