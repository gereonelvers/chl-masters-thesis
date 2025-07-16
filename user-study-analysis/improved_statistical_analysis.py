import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from scipy import stats
from scipy.stats import friedmanchisquare, wilcoxon
import warnings
import os
warnings.filterwarnings('ignore')

# Advanced statistical packages
try:
    import statsmodels.api as sm
    from statsmodels.formula.api import mixedlm, ols
    from statsmodels.stats.multicomp import pairwise_tukeyhsd
    HAS_STATSMODELS = True
except ImportError:
    print("Warning: statsmodels not available. Mixed-effects models will not be run.")
    HAS_STATSMODELS = False

try:
    import pingouin as pg
    HAS_PINGOUIN = True
except ImportError:
    print("Warning: pingouin not available. Some advanced tests will not be run.")
    HAS_PINGOUIN = False

# Create output directory
output_dir = '../../assets/06'
os.makedirs(output_dir, exist_ok=True)

print("=== IMPROVED STATISTICAL ANALYSIS ===")
print("Addressing issues with independence assumptions in repeated measures data\n")

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

# Create participant identifiers and dyad information
df_valid['dyad_id'] = df_valid['Participant 1 ID'].str.split('-').str[0]
df_valid['participant_id'] = df_valid['Participant 1 ID'] + '_' + df_valid['Participant 2 ID']

print(f"Total runs: {len(df)}")
print(f"Valid completion times: {len(df_valid)}")
print(f"Number of dyads: {df_valid['dyad_id'].nunique()}")
print(f"Failed/Invalid runs: {len(df) - len(df_valid)}")

# ===============================
# 1. MIXED-EFFECTS MODELS
# ===============================
print("\n=== 1. MIXED-EFFECTS MODELS ===")
print("Accounting for repeated measures and dyadic dependencies")

if HAS_STATSMODELS:
    # Mixed-effects model for completion time
    # Fixed effect: Variant, Random effect: Dyad
    try:
        model = mixedlm("completion_time_minutes ~ C(Variant)", 
                       df_valid, 
                       groups=df_valid["dyad_id"])
        result = model.fit()
        print("\nMixed-Effects Model Results (Completion Time ~ Variant | Dyad):")
        print("=" * 60)
        print(result.summary().tables[1])
        
        # Calculate effect sizes
        # Marginal R² (fixed effects only)
        fixed_pred = result.fittedvalues
        marginal_r2 = np.corrcoef(df_valid['completion_time_minutes'], fixed_pred)[0,1]**2
        print(f"\nMarginal R² (fixed effects): {marginal_r2:.3f}")
        
        # Model comparison against null
        null_model = mixedlm("completion_time_minutes ~ 1", 
                           df_valid, 
                           groups=df_valid["dyad_id"])
        null_result = null_model.fit()
        
        # Likelihood ratio test
        lr_stat = 2 * (result.llf - null_result.llf)
        df_diff = len(result.params) - len(null_result.params)
        p_lr = 1 - stats.chi2.cdf(lr_stat, df_diff)
        print(f"Likelihood Ratio Test: χ²({df_diff}) = {lr_stat:.3f}, p = {p_lr:.3f}")
        
    except Exception as e:
        print(f"Mixed-effects model failed: {e}")

# ===============================
# 2. NON-PARAMETRIC TESTS
# ===============================
print("\n=== 2. NON-PARAMETRIC TESTS ===")
print("For data that may violate normality assumptions")

# Friedman test for repeated measures (non-parametric alternative to repeated measures ANOVA)
if HAS_PINGOUIN:
    try:
        # Reshape data for Friedman test
        pivot_data = df_valid.pivot_table(
            values='completion_time_minutes', 
            index='dyad_id', 
            columns='Variant', 
            aggfunc='first'
        )
        
        # Remove rows with missing data
        pivot_complete = pivot_data.dropna()
        
        if len(pivot_complete) > 2:  # Need at least 3 complete cases
            friedman_result = pg.friedman(data=pivot_complete.reset_index(), 
                                        dv='value', 
                                        within='variable', 
                                        subject='dyad_id')
            print(f"\nFriedman Test (non-parametric repeated measures):")
            print(f"Q = {friedman_result['Q'].iloc[0]:.3f}, p = {friedman_result['p-unc'].iloc[0]:.3f}")
            
            # Post-hoc analysis with Wilcoxon signed-rank tests
            variants = pivot_complete.columns
            print(f"\nPost-hoc Wilcoxon signed-rank tests:")
            for i, var1 in enumerate(variants):
                for var2 in variants[i+1:]:
                    if not pivot_complete[var1].empty and not pivot_complete[var2].empty:
                        statistic, p_val = wilcoxon(pivot_complete[var1], 
                                                   pivot_complete[var2], 
                                                   alternative='two-sided')
                        print(f"{var1} vs {var2}: W = {statistic:.0f}, p = {p_val:.3f}")
        else:
            print("Insufficient complete cases for Friedman test")
            
    except Exception as e:
        print(f"Friedman test failed: {e}")

# Kruskal-Wallis test (non-parametric ANOVA alternative)
# This still has independence issues but is more robust to normality violations
variant_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Variant')]
h_stat, p_kw = stats.kruskal(*variant_groups)
print(f"\nKruskal-Wallis Test (robust to non-normality): H = {h_stat:.3f}, p = {p_kw:.3f}")

# ===============================
# 3. ROBUST REGRESSION
# ===============================
print("\n=== 3. ROBUST REGRESSION ===")
print("For data with outliers")

if HAS_STATSMODELS:
    try:
        # Create dummy variables for categorical predictors
        df_robust = pd.get_dummies(df_valid, columns=['Variant'], prefix='Variant')
        
        # Fit robust regression (M-estimation)
        X = df_robust[['Variant_Open Ended', 'Variant_Roleplay', 'Variant_Silent', 'Variant_Timed']]
        y = df_robust['completion_time_minutes']
        
        # Remove one dummy to avoid multicollinearity
        X = X.drop('Variant_Open Ended', axis=1)  # Reference category
        X = sm.add_constant(X)
        
        robust_model = sm.RLM(y, X, M=sm.robust.norms.HuberT())
        robust_result = robust_model.fit()
        
        print("\nRobust Regression Results (M-estimation with Huber T):")
        print("=" * 60)
        print(robust_result.summary().tables[1])
        
    except Exception as e:
        print(f"Robust regression failed: {e}")

# ===============================
# 4. EFFECT SIZE CALCULATIONS
# ===============================
print("\n=== 4. EFFECT SIZE CALCULATIONS ===")

# Cohen's d for pairwise comparisons
def cohens_d(x, y):
    pooled_std = np.sqrt(((len(x) - 1) * np.var(x, ddof=1) + (len(y) - 1) * np.var(y, ddof=1)) / (len(x) + len(y) - 2))
    return (np.mean(x) - np.mean(y)) / pooled_std

variants = df_valid['Variant'].unique()
print("Cohen's d effect sizes (pairwise comparisons):")
for i, var1 in enumerate(variants):
    for var2 in variants[i+1:]:
        group1 = df_valid[df_valid['Variant'] == var1]['completion_time_minutes']
        group2 = df_valid[df_valid['Variant'] == var2]['completion_time_minutes']
        d = cohens_d(group1, group2)
        print(f"{var1} vs {var2}: d = {d:.3f}")

# Eta-squared for overall effect
variant_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Variant')]
f_stat, p_val = stats.f_oneway(*variant_groups)

# Calculate eta-squared
ss_between = sum([len(group) * (np.mean(group) - np.mean(df_valid['completion_time_minutes']))**2 for group in variant_groups])
ss_total = np.sum((df_valid['completion_time_minutes'] - np.mean(df_valid['completion_time_minutes']))**2)
eta_squared = ss_between / ss_total

print(f"\nOverall effect size (η²): {eta_squared:.3f}")

# ===============================
# 5. ASSUMPTION CHECKING
# ===============================
print("\n=== 5. ASSUMPTION CHECKING ===")

# Test for normality by group
print("Shapiro-Wilk normality tests by variant:")
for variant in variants:
    group_data = df_valid[df_valid['Variant'] == variant]['completion_time_minutes']
    if len(group_data) >= 3:  # Minimum for Shapiro-Wilk
        stat, p = stats.shapiro(group_data)
        print(f"{variant}: W = {stat:.3f}, p = {p:.3f}")

# Levene's test for homogeneity of variance
levene_stat, levene_p = stats.levene(*variant_groups)
print(f"\nLevene's test for equal variances: W = {levene_stat:.3f}, p = {levene_p:.3f}")

# ===============================
# 6. DESCRIPTIVE STATISTICS WITH CONFIDENCE INTERVALS
# ===============================
print("\n=== 6. ENHANCED DESCRIPTIVE STATISTICS ===")

def bootstrap_ci(data, statistic=np.mean, n_bootstrap=1000, confidence_level=0.95):
    """Calculate bootstrap confidence interval"""
    bootstrap_stats = []
    for _ in range(n_bootstrap):
        sample = np.random.choice(data, size=len(data), replace=True)
        bootstrap_stats.append(statistic(sample))
    
    alpha = 1 - confidence_level
    lower = np.percentile(bootstrap_stats, 100 * alpha/2)
    upper = np.percentile(bootstrap_stats, 100 * (1 - alpha/2))
    return lower, upper

print("Descriptive statistics with 95% bootstrap confidence intervals:")
for variant in variants:
    group_data = df_valid[df_valid['Variant'] == variant]['completion_time_minutes']
    mean_val = np.mean(group_data)
    lower_ci, upper_ci = bootstrap_ci(group_data)
    print(f"{variant}: Mean = {mean_val:.2f}, 95% CI = [{lower_ci:.2f}, {upper_ci:.2f}]")

# ===============================
# 7. SAVE RESULTS
# ===============================
print("\n=== 7. SAVING RESULTS ===")

# Save detailed results to file
with open('improved_statistical_analysis_results.txt', 'w') as f:
    f.write("IMPROVED STATISTICAL ANALYSIS RESULTS\n")
    f.write("====================================\n\n")
    f.write("PROBLEMS WITH ORIGINAL ANALYSIS:\n")
    f.write("- Used one-way ANOVA on repeated measures data (violates independence)\n")
    f.write("- Ignored dyadic dependencies\n")
    f.write("- Did not check assumptions\n")
    f.write("- Did not calculate appropriate effect sizes\n\n")
    
    f.write("IMPROVED METHODS USED:\n")
    f.write("- Mixed-effects models for repeated measures\n")
    f.write("- Non-parametric tests for non-normal data\n")
    f.write("- Robust regression for outlier resistance\n")
    f.write("- Proper effect size calculations\n")
    f.write("- Bootstrap confidence intervals\n\n")
    
    f.write(f"SUMMARY FINDINGS:\n")
    f.write(f"- Kruskal-Wallis test: H = {h_stat:.3f}, p = {p_kw:.3f}\n")
    f.write(f"- Overall effect size (η²): {eta_squared:.3f}\n")
    f.write(f"- Levene's test for equal variances: p = {levene_p:.3f}\n")

print("Analysis complete. Results saved to 'improved_statistical_analysis_results.txt'")
print("\nKEY RECOMMENDATIONS:")
print("1. Replace one-way ANOVAs with mixed-effects models")
print("2. Use non-parametric tests when normality is violated")
print("3. Report effect sizes alongside p-values")
print("4. Check and report assumption violations")
print("5. Use bootstrap confidence intervals for robust estimation") 