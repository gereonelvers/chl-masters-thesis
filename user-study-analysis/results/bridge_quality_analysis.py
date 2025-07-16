import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from scipy import stats
import warnings
import os
warnings.filterwarnings('ignore')

# Create output directory if it doesn't exist
output_dir = '../../assets/06'
os.makedirs(output_dir, exist_ok=True)

# Load data
df = pd.read_csv('../study-run-results.csv')

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

# Remove invalid entries (keep only cases with valid structural analysis)
df_valid = df.dropna(subset=['safety_factor', 'von_mises_stress', 'displacement']).copy()

print(f"Total runs: {len(df)}")
print(f"Valid bridge quality data: {len(df_valid)}")
print(f"Failed/Invalid structural analysis: {len(df) - len(df_valid)}")

print("\n=== BRIDGE QUALITY DESCRIPTIVE STATISTICS ===")

# Overall statistics for each metric
metrics = ['price', 'height', 'safety_factor', 'von_mises_stress', 'displacement', 'total_objects', 'different_objects']
print("\nOverall Bridge Quality Metrics:")
for metric in metrics:
    if metric in df_valid.columns:
        data = df_valid[metric].dropna()
        print(f"{metric}:")
        print(f"  Mean: {data.mean():.3f}")
        print(f"  Median: {data.median():.3f}")
        print(f"  Std: {data.std():.3f}")
        print(f"  Range: {data.min():.3f} - {data.max():.3f}")
        print()

print("\n=== BRIDGE QUALITY BY VARIANT ===")
variant_stats = {}
for metric in ['safety_factor', 'von_mises_stress', 'displacement', 'price', 'total_objects']:
    if metric in df_valid.columns:
        stats_by_variant = df_valid.groupby('Variant')[metric].agg(['count', 'mean', 'std', 'median']).round(3)
        print(f"\n{metric.upper()}:")
        print(stats_by_variant)
        variant_stats[metric] = stats_by_variant

print("\n=== BRIDGE QUALITY BY ENVIRONMENT ===")
env_stats = {}
for metric in ['safety_factor', 'von_mises_stress', 'displacement', 'price']:
    if metric in df_valid.columns:
        stats_by_env = df_valid.groupby('Environment')[metric].agg(['count', 'mean', 'std', 'median']).round(3)
        print(f"\n{metric.upper()}:")
        print(stats_by_env)
        env_stats[metric] = stats_by_env

print("\n=== STATISTICAL TESTS ===")

# ANOVA for variants
for metric in ['safety_factor', 'von_mises_stress', 'displacement', 'price']:
    if metric in df_valid.columns and not df_valid[metric].isna().all():
        variant_groups = [group[metric].dropna().values for name, group in df_valid.groupby('Variant')]
        variant_groups = [group for group in variant_groups if len(group) > 0]
        if len(variant_groups) >= 2:
            f_stat, p_val = stats.f_oneway(*variant_groups)
            print(f"ANOVA {metric} (Variants): F={f_stat:.3f}, p={p_val:.3f}")

# Correlations between metrics
print("\n=== METRIC CORRELATIONS ===")
correlation_metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price', 'total_objects', 'completion_time_minutes']
corr_data = df_valid[correlation_metrics].dropna()
if len(corr_data) > 0:
    corr_matrix = corr_data.corr()
    print("Correlation Matrix:")
    print(corr_matrix.round(3))

# Quality vs Efficiency Analysis
print("\n=== QUALITY vs EFFICIENCY ANALYSIS ===")
if 'safety_factor' in df_valid.columns and 'completion_time_minutes' in df_valid.columns:
    quality_efficiency = df_valid[['safety_factor', 'completion_time_minutes', 'total_objects', 'price']].dropna()
    if len(quality_efficiency) > 0:
        # Correlation between safety and time
        safety_time_corr = quality_efficiency['safety_factor'].corr(quality_efficiency['completion_time_minutes'])
        safety_objects_corr = quality_efficiency['safety_factor'].corr(quality_efficiency['total_objects'])
        print(f"Safety Factor vs Completion Time correlation: r={safety_time_corr:.3f}")
        print(f"Safety Factor vs Total Objects correlation: r={safety_objects_corr:.3f}")

# Create visualizations
fig, axes = plt.subplots(2, 2, figsize=(12, 10))
fig.suptitle('Bridge Quality Analysis', fontsize=16, fontweight='bold')

# Safety Factor by Variant
safety_data = [df_valid[df_valid['Variant'] == variant]['safety_factor'].dropna().values 
               for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']]
safety_data = [data for data in safety_data if len(data) > 0]
if safety_data:
    bp1 = axes[0,0].boxplot(safety_data, labels=['Open\nEnded', 'Silent', 'Timed', 'Roleplay'], patch_artist=True)
    for patch in bp1['boxes']:
        patch.set_facecolor('lightgrey')
    axes[0,0].set_title('Safety Factor by Collaboration Variant')
    axes[0,0].set_ylabel('Safety Factor (higher = better)')

# von Mises Stress by Variant
stress_data = [df_valid[df_valid['Variant'] == variant]['von_mises_stress'].dropna().values 
               for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']]
stress_data = [data for data in stress_data if len(data) > 0]
if stress_data:
    bp2 = axes[0,1].boxplot(stress_data, labels=['Open\nEnded', 'Silent', 'Timed', 'Roleplay'], patch_artist=True)
    for patch in bp2['boxes']:
        patch.set_facecolor('lightgrey')
    axes[0,1].set_title('von Mises Stress by Collaboration Variant')
    axes[0,1].set_ylabel('von Mises Stress (MPa, lower = better)')

# Quality vs Efficiency Scatter
quality_efficiency_clean = df_valid[['safety_factor', 'completion_time_minutes']].dropna()
if len(quality_efficiency_clean) > 0:
    axes[1,0].scatter(quality_efficiency_clean['completion_time_minutes'], 
                     quality_efficiency_clean['safety_factor'], 
                     alpha=0.7, color='#a44b9c', s=50)
    axes[1,0].set_xlabel('Completion Time (minutes)')
    axes[1,0].set_ylabel('Safety Factor')
    axes[1,0].set_title('Bridge Quality vs Task Efficiency')
    
    # Add trend line
    if len(quality_efficiency_clean) > 1:
        z = np.polyfit(quality_efficiency_clean['completion_time_minutes'], 
                      quality_efficiency_clean['safety_factor'], 1)
        p = np.poly1d(z)
        axes[1,0].plot(quality_efficiency_clean['completion_time_minutes'], 
                      p(quality_efficiency_clean['completion_time_minutes']), 
                      "--", alpha=0.7, color='black')

# Price vs Objects Used
price_objects_clean = df_valid[['price', 'total_objects']].dropna()
if len(price_objects_clean) > 0:
    axes[1,1].scatter(price_objects_clean['total_objects'], 
                     price_objects_clean['price'], 
                     alpha=0.7, color='#a44b9c', s=50)
    axes[1,1].set_xlabel('Total Objects Used')
    axes[1,1].set_ylabel('Bridge Price')
    axes[1,1].set_title('Construction Cost vs Complexity')
    
    # Add trend line
    if len(price_objects_clean) > 1:
        z = np.polyfit(price_objects_clean['total_objects'], 
                      price_objects_clean['price'], 1)
        p = np.poly1d(z)
        axes[1,1].plot(price_objects_clean['total_objects'], 
                      p(price_objects_clean['total_objects']), 
                      "--", alpha=0.7, color='black')

plt.tight_layout()
plt.savefig(f'{output_dir}/bridge_quality_analysis.pdf', dpi=300, bbox_inches='tight')
plt.savefig(f'{output_dir}/bridge_quality_analysis.png', dpi=300, bbox_inches='tight')
plt.close()

# Detailed quality metrics comparison
fig, ax = plt.subplots(1, 1, figsize=(10, 6))
variants = ['Open Ended', 'Silent', 'Timed', 'Roleplay']

# Normalize metrics to 0-1 scale for comparison
metrics_to_plot = ['safety_factor', 'von_mises_stress', 'displacement']
normalized_data = {}

for metric in metrics_to_plot:
    variant_means = []
    for variant in variants:
        variant_data = df_valid[df_valid['Variant'] == variant][metric].dropna()
        if len(variant_data) > 0:
            if metric == 'safety_factor':  # Higher is better
                variant_means.append(variant_data.mean())
            else:  # Lower is better for stress and displacement
                variant_means.append(1 / (1 + variant_data.mean()) if variant_data.mean() > 0 else 0)
        else:
            variant_means.append(0)
    normalized_data[metric] = variant_means

x = np.arange(len(variants))
width = 0.25

if 'safety_factor' in normalized_data:
    ax.bar(x - width, normalized_data['safety_factor'], width, label='Safety Factor', alpha=0.8, color='#a44b9c')
if 'von_mises_stress' in normalized_data:
    ax.bar(x, [1-v for v in normalized_data['von_mises_stress']], width, label='Stress (inverted)', alpha=0.8, color='grey')
if 'displacement' in normalized_data:
    ax.bar(x + width, [1-v for v in normalized_data['displacement']], width, label='Displacement (inverted)', alpha=0.8, color='lightblue')

ax.set_xlabel('Collaboration Variant')
ax.set_ylabel('Relative Quality Score')
ax.set_title('Bridge Quality Metrics by Collaboration Variant')
ax.set_xticks(x)
ax.set_xticklabels(variants)
ax.legend()

plt.tight_layout()
plt.savefig(f'{output_dir}/bridge_quality_comparison.pdf', dpi=300, bbox_inches='tight')
plt.savefig(f'{output_dir}/bridge_quality_comparison.png', dpi=300, bbox_inches='tight')
plt.close()

print(f"\nAnalysis complete. Files saved:")
print(f"- {output_dir}/bridge_quality_analysis.pdf/png")
print(f"- {output_dir}/bridge_quality_comparison.pdf/png") 