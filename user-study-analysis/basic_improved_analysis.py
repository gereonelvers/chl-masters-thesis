import pandas as pd
import numpy as np
from scipy import stats
import warnings
warnings.filterwarnings('ignore')

print("=== BASIC IMPROVED STATISTICAL ANALYSIS ===")
print("Showing problems with current ANOVA approach and better alternatives")

# Load and prepare data
df = pd.read_csv('study-run-results.csv')

def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    try:
        parts = time_str.split(':')
        return int(parts[0]) + int(parts[1]) / 60
    except:
        return np.nan

df['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(parse_time)
df_valid = df.dropna(subset=['completion_time_minutes']).copy()

# Create dyad identifiers
df_valid['dyad_id'] = df_valid['Participant 1 ID'].str.split('-').str[0]

print(f"Total runs: {len(df)}")
print(f"Valid completion times: {len(df_valid)}")
print(f"Number of dyads: {df_valid['dyad_id'].nunique()}")
print(f"Failed/Invalid runs: {len(df) - len(df_valid)}")

print("\n" + "="*60)
print("PROBLEM 1: INDEPENDENCE ASSUMPTION VIOLATED")
print("="*60)

# Show the data structure problem
print("Data structure showing repeated measures:")
for dyad in df_valid['dyad_id'].unique()[:3]:  # Show first 3 dyads
    dyad_data = df_valid[df_valid['dyad_id'] == dyad][['dyad_id', 'Variant', 'completion_time_minutes']]
    print(f"\nDyad {dyad}:")
    print(dyad_data.to_string(index=False))

print(f"\nThe problem: Each dyad contributes {len(df_valid['Variant'].unique())} data points")
print("One-way ANOVA assumes all observations are independent!")
print("This violates the independence assumption.")

print("\n" + "="*60)
print("CURRENT ANALYSIS (PROBLEMATIC)")
print("="*60)

# Current approach (problematic)
variant_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Variant')]
f_stat, p_val = stats.f_oneway(*variant_groups)
print(f"One-way ANOVA: F = {f_stat:.3f}, p = {p_val:.3f}")
print("*** WARNING: This is INVALID due to non-independence! ***")

print("\n" + "="*60)
print("BETTER APPROACH 1: NON-PARAMETRIC TESTS")
print("="*60)

# Kruskal-Wallis (still has independence issues but more robust)
h_stat, p_kw = stats.kruskal(*variant_groups)
print(f"Kruskal-Wallis test: H = {h_stat:.3f}, p = {p_kw:.3f}")
print("More robust to outliers and non-normality, but still has independence issues")

print("\n" + "="*60)
print("BETTER APPROACH 2: PAIRED ANALYSIS")
print("="*60)

# Create a proper repeated measures structure
pivot_data = df_valid.pivot_table(
    values='completion_time_minutes', 
    index='dyad_id', 
    columns='Variant', 
    aggfunc='first'
)

print("Repeated measures structure:")
print(pivot_data)

# Check how many complete cases we have
complete_cases = pivot_data.dropna()
print(f"\nComplete cases (dyads with all 4 variants): {len(complete_cases)}")

if len(complete_cases) >= 3:
    # Friedman test for repeated measures
    from scipy.stats import friedmanchisquare
    
    friedman_data = [complete_cases[col].values for col in complete_cases.columns]
    friedman_stat, friedman_p = friedmanchisquare(*friedman_data)
    print(f"Friedman test (proper repeated measures): χ² = {friedman_stat:.3f}, p = {friedman_p:.3f}")
    
    # Pairwise Wilcoxon signed-rank tests
    from scipy.stats import wilcoxon
    print("\nPairwise comparisons (Wilcoxon signed-rank tests):")
    variants = complete_cases.columns
    for i, var1 in enumerate(variants):
        for var2 in variants[i+1:]:
            stat, p_wilcox = wilcoxon(complete_cases[var1], complete_cases[var2])
            print(f"{var1} vs {var2}: W = {stat:.0f}, p = {p_wilcox:.3f}")

print("\n" + "="*60)
print("ASSUMPTION CHECKING")
print("="*60)

# Test normality
print("Shapiro-Wilk normality tests by variant:")
for variant in df_valid['Variant'].unique():
    group_data = df_valid[df_valid['Variant'] == variant]['completion_time_minutes']
    if len(group_data) >= 3:
        stat, p = stats.shapiro(group_data)
        print(f"{variant}: W = {stat:.3f}, p = {p:.3f} {'(Normal)' if p > 0.05 else '(Non-normal)'}")

# Test equal variances
levene_stat, levene_p = stats.levene(*variant_groups)
print(f"\nLevene's test for equal variances: W = {levene_stat:.3f}, p = {levene_p:.3f}")
print("Equal variances" if levene_p > 0.05 else "Unequal variances - ANOVA assumption violated")

print("\n" + "="*60)
print("EFFECT SIZES")
print("="*60)

# Cohen's d for pairwise comparisons
def cohens_d(x, y):
    pooled_std = np.sqrt(((len(x) - 1) * np.var(x, ddof=1) + (len(y) - 1) * np.var(y, ddof=1)) / (len(x) + len(y) - 2))
    return (np.mean(x) - np.mean(y)) / pooled_std

print("Cohen's d effect sizes (pairwise comparisons):")
variants = df_valid['Variant'].unique()
for i, var1 in enumerate(variants):
    for var2 in variants[i+1:]:
        group1 = df_valid[df_valid['Variant'] == var1]['completion_time_minutes']
        group2 = df_valid[df_valid['Variant'] == var2]['completion_time_minutes']
        d = cohens_d(group1, group2)
        effect_size = "Small" if abs(d) < 0.5 else "Medium" if abs(d) < 0.8 else "Large"
        print(f"{var1} vs {var2}: d = {d:.3f} ({effect_size})")

# Eta-squared
ss_between = sum([len(group) * (np.mean(group) - np.mean(df_valid['completion_time_minutes']))**2 for group in variant_groups])
ss_total = np.sum((df_valid['completion_time_minutes'] - np.mean(df_valid['completion_time_minutes']))**2)
eta_squared = ss_between / ss_total
print(f"\nEta-squared (η²): {eta_squared:.3f}")

print("\n" + "="*60)
print("DESCRIPTIVE STATISTICS WITH CONFIDENCE INTERVALS")
print("="*60)

def bootstrap_ci(data, n_bootstrap=1000, confidence_level=0.95):
    """Calculate bootstrap confidence interval for the mean"""
    bootstrap_means = []
    for _ in range(n_bootstrap):
        sample = np.random.choice(data, size=len(data), replace=True)
        bootstrap_means.append(np.mean(sample))
    
    alpha = 1 - confidence_level
    lower = np.percentile(bootstrap_means, 100 * alpha/2)
    upper = np.percentile(bootstrap_means, 100 * (1 - alpha/2))
    return lower, upper

print("Variant statistics with bootstrap 95% confidence intervals:")
for variant in variants:
    group_data = df_valid[df_valid['Variant'] == variant]['completion_time_minutes']
    mean_val = np.mean(group_data)
    std_val = np.std(group_data, ddof=1)
    lower_ci, upper_ci = bootstrap_ci(group_data)
    print(f"{variant:12}: Mean = {mean_val:5.2f}, SD = {std_val:5.2f}, 95% CI = [{lower_ci:5.2f}, {upper_ci:5.2f}]")

print("\n" + "="*60)
print("RECOMMENDATIONS FOR THE PAPER")
print("="*60)

print("1. REMOVE all one-way ANOVAs - they violate independence assumptions")
print("2. USE Friedman test for repeated measures comparisons")
print("3. REPORT Wilcoxon signed-rank tests for pairwise comparisons")
print("4. ALWAYS report effect sizes (Cohen's d, eta-squared)")
print("5. CHECK and REPORT assumption violations")
print("6. USE bootstrap confidence intervals instead of assuming normality")

# Save results
with open('statistical_analysis_corrections.txt', 'w') as f:
    f.write("STATISTICAL ANALYSIS CORRECTIONS\n")
    f.write("===============================\n\n")
    f.write("ORIGINAL PROBLEM:\n")
    f.write(f"- One-way ANOVA used: F = {f_stat:.3f}, p = {p_val:.3f}\n")
    f.write("- This is INVALID due to repeated measures (non-independence)\n")
    f.write("- Each dyad contributes 4 data points, violating independence assumption\n\n")
    
    f.write("CORRECTED ANALYSIS:\n")
    f.write(f"- Friedman test (repeated measures): χ² = {friedman_stat:.3f}, p = {friedman_p:.3f}\n")
    f.write(f"- Kruskal-Wallis test (robust): H = {h_stat:.3f}, p = {p_kw:.3f}\n")
    f.write(f"- Effect size (eta-squared): {eta_squared:.3f}\n")
    f.write(f"- Equal variances assumption: {'Met' if levene_p > 0.05 else 'Violated'} (p = {levene_p:.3f})\n\n")
    
    f.write("PAIRWISE COMPARISONS (Wilcoxon signed-rank tests):\n")
    if len(complete_cases) >= 3:
        for i, var1 in enumerate(variants):
            for var2 in variants[i+1:]:
                stat, p_wilcox = wilcoxon(complete_cases[var1], complete_cases[var2])
                f.write(f"{var1} vs {var2}: W = {stat:.0f}, p = {p_wilcox:.3f}\n")

print(f"\nResults saved to 'statistical_analysis_corrections.txt'")
print("Use these corrected statistics in your paper!") 