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

# Parse completion time from MM:SS format to minutes
def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    try:
        parts = time_str.split(':')
        return int(parts[0]) + int(parts[1]) / 60
    except:
        return np.nan

df['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(parse_time)

# Remove invalid entries
df_valid = df.dropna(subset=['completion_time_minutes']).copy()

print(f"Total runs: {len(df)}")
print(f"Valid completion times: {len(df_valid)}")
print(f"Failed/Invalid runs: {len(df) - len(df_valid)}")

# Basic descriptive statistics
print("\n=== COMPLETION TIME DESCRIPTIVE STATISTICS ===")
print(f"Mean: {df_valid['completion_time_minutes'].mean():.2f} minutes")
print(f"Median: {df_valid['completion_time_minutes'].median():.2f} minutes")
print(f"Std: {df_valid['completion_time_minutes'].std():.2f} minutes")
print(f"Min: {df_valid['completion_time_minutes'].min():.2f} minutes")
print(f"Max: {df_valid['completion_time_minutes'].max():.2f} minutes")

# Analysis by variant
print("\n=== COMPLETION TIME BY VARIANT ===")
variant_stats = df_valid.groupby('Variant')['completion_time_minutes'].agg(['count', 'mean', 'std', 'median']).round(2)
print(variant_stats)

# Analysis by environment
print("\n=== COMPLETION TIME BY ENVIRONMENT ===")
env_stats = df_valid.groupby('Environment')['completion_time_minutes'].agg(['count', 'mean', 'std', 'median']).round(2)
print(env_stats)

# Analysis by position in study
print("\n=== COMPLETION TIME BY POSITION IN STUDY ===")
pos_stats = df_valid.groupby('Position in study')['completion_time_minutes'].agg(['count', 'mean', 'std', 'median']).round(2)
print(pos_stats)

# Statistical tests
print("\n=== STATISTICAL TESTS ===")

# ANOVA for variants
variant_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Variant')]
f_stat, p_val = stats.f_oneway(*variant_groups)
print(f"ANOVA (Variants): F={f_stat:.3f}, p={p_val:.3f}")

# ANOVA for environments
env_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Environment')]
f_stat_env, p_val_env = stats.f_oneway(*env_groups)
print(f"ANOVA (Environments): F={f_stat_env:.3f}, p={p_val_env:.3f}")

# ANOVA for position in study
pos_groups = [group['completion_time_minutes'].values for name, group in df_valid.groupby('Position in study')]
f_stat_pos, p_val_pos = stats.f_oneway(*pos_groups)
print(f"ANOVA (Position): F={f_stat_pos:.3f}, p={p_val_pos:.3f}")

# Create visualizations
plt.style.use('default')
fig, axes = plt.subplots(2, 2, figsize=(12, 10))
fig.suptitle('Completion Time Analysis', fontsize=16, fontweight='bold')

# Overall distribution
axes[0,0].hist(df_valid['completion_time_minutes'], bins=12, color='grey', alpha=0.7, edgecolor='black')
axes[0,0].axvline(df_valid['completion_time_minutes'].mean(), color='#a44b9c', linestyle='--', linewidth=2, label=f'Mean: {df_valid["completion_time_minutes"].mean():.1f} min')
axes[0,0].axvline(df_valid['completion_time_minutes'].median(), color='black', linestyle='--', linewidth=2, label=f'Median: {df_valid["completion_time_minutes"].median():.1f} min')
axes[0,0].set_xlabel('Completion Time (minutes)')
axes[0,0].set_ylabel('Frequency')
axes[0,0].set_title('Distribution of Completion Times')
axes[0,0].legend()

# Box plot by variant
variant_order = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
variant_data = [df_valid[df_valid['Variant'] == variant]['completion_time_minutes'].values for variant in variant_order]
bp1 = axes[0,1].boxplot(variant_data, labels=variant_order, patch_artist=True)
for patch in bp1['boxes']:
    patch.set_facecolor('lightgrey')
axes[0,1].set_title('Completion Time by Variant')
axes[0,1].set_xlabel('Variant')
axes[0,1].set_ylabel('Completion Time (minutes)')
axes[0,1].tick_params(axis='x', rotation=45)

# Box plot by environment
env_data = [df_valid[df_valid['Environment'] == env]['completion_time_minutes'].values for env in sorted(df_valid['Environment'].unique())]
bp2 = axes[1,0].boxplot(env_data, labels=sorted(df_valid['Environment'].unique()), patch_artist=True)
for patch in bp2['boxes']:
    patch.set_facecolor('lightgrey')
axes[1,0].set_title('Completion Time by Environment')
axes[1,0].set_xlabel('Environment')
axes[1,0].set_ylabel('Completion Time (minutes)')

# Line plot by position in study
pos_means = df_valid.groupby('Position in study')['completion_time_minutes'].mean()
pos_std = df_valid.groupby('Position in study')['completion_time_minutes'].std()
axes[1,1].plot(pos_means.index, pos_means.values, 'o-', color='#a44b9c', linewidth=2, markersize=8)
axes[1,1].fill_between(pos_means.index, pos_means.values - pos_std.values, pos_means.values + pos_std.values, alpha=0.3, color='#a44b9c')
axes[1,1].set_xlabel('Position in Study')
axes[1,1].set_ylabel('Completion Time (minutes)')
axes[1,1].set_title('Learning Effect: Time by Position')
axes[1,1].set_xticks([0, 1, 2, 3])

plt.tight_layout()
plt.savefig(f'{output_dir}/completion_time_analysis.pdf', dpi=300, bbox_inches='tight')
plt.savefig(f'{output_dir}/completion_time_analysis.png', dpi=300, bbox_inches='tight')
plt.close()

# Detailed variant comparison
fig, ax = plt.subplots(1, 1, figsize=(10, 6))
variant_order = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
variant_data = [df_valid[df_valid['Variant'] == variant]['completion_time_minutes'].values for variant in variant_order]
bp = ax.boxplot(variant_data, labels=variant_order, patch_artist=True)
for patch in bp['boxes']:
    patch.set_facecolor('lightgrey')

# Add individual points
for i, variant in enumerate(variant_order):
    variant_times = df_valid[df_valid['Variant'] == variant]['completion_time_minutes'].values
    x = np.random.normal(i+1, 0.04, size=len(variant_times))
    ax.scatter(x, variant_times, alpha=0.7, color='#a44b9c', s=40)

ax.set_xlabel('Collaboration Variant')
ax.set_ylabel('Completion Time (minutes)')
ax.set_title('Completion Time Distribution by Collaboration Variant')
plt.xticks(rotation=45)
plt.tight_layout()
plt.savefig(f'{output_dir}/completion_time_by_variant.pdf', dpi=300, bbox_inches='tight')
plt.savefig(f'{output_dir}/completion_time_by_variant.png', dpi=300, bbox_inches='tight')
plt.close()

# Save summary statistics to file
with open('completion_time_summary.txt', 'w') as f:
    f.write("COMPLETION TIME ANALYSIS SUMMARY\n")
    f.write("================================\n\n")
    f.write(f"Total runs: {len(df)}\n")
    f.write(f"Valid completion times: {len(df_valid)}\n")
    f.write(f"Failed/Invalid runs: {len(df) - len(df_valid)}\n\n")
    f.write("Overall Statistics:\n")
    f.write(f"Mean: {df_valid['completion_time_minutes'].mean():.2f} minutes\n")
    f.write(f"Median: {df_valid['completion_time_minutes'].median():.2f} minutes\n")
    f.write(f"Standard deviation: {df_valid['completion_time_minutes'].std():.2f} minutes\n")
    f.write(f"Range: {df_valid['completion_time_minutes'].min():.2f} - {df_valid['completion_time_minutes'].max():.2f} minutes\n\n")
    f.write("By Variant:\n")
    f.write(variant_stats.to_string())
    f.write("\n\nBy Environment:\n")
    f.write(env_stats.to_string())
    f.write("\n\nBy Position in Study:\n")
    f.write(pos_stats.to_string())
    f.write(f"\n\nStatistical Tests:\n")
    f.write(f"ANOVA (Variants): F={f_stat:.3f}, p={p_val:.3f}\n")
    f.write(f"ANOVA (Environments): F={f_stat_env:.3f}, p={p_val_env:.3f}\n")
    f.write(f"ANOVA (Position): F={f_stat_pos:.3f}, p={p_val_pos:.3f}\n")

print(f"\nAnalysis complete. Files saved:")
print(f"- {output_dir}/completion_time_analysis.pdf/png")
print(f"- {output_dir}/completion_time_by_variant.pdf/png") 
print("- completion_time_summary.txt") 