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
df_valid = df.dropna(subset=['completion_time_minutes']).copy()

print("=== LEARNING EFFECT DETAILED ANALYSIS ===")
print("\n1. Position-by-Variant Cross-tabulation:")
crosstab = pd.crosstab(df_valid['Position in study'], df_valid['Variant'])
print(crosstab)

print("\n2. Mean completion times by position and variant:")
pivot_table = df_valid.pivot_table(values='completion_time_minutes', 
                                  index='Position in study', 
                                  columns='Variant', 
                                  aggfunc='mean').round(2)
print(pivot_table)

print("\n3. Standard deviations by position and variant:")
pivot_table_std = df_valid.pivot_table(values='completion_time_minutes', 
                                       index='Position in study', 
                                       columns='Variant', 
                                       aggfunc='std').round(2)
print(pivot_table_std)

# Two-way ANOVA to control for variant effects
from scipy.stats import f_oneway
import itertools

print("\n4. Two-way ANOVA (Position × Variant):")
# Create dummy variables for ANOVA
position_groups = []
variant_groups = []
time_values = []

for pos in sorted(df_valid['Position in study'].unique()):
    for var in sorted(df_valid['Variant'].unique()):
        subset = df_valid[(df_valid['Position in study'] == pos) & (df_valid['Variant'] == var)]
        if len(subset) > 0:
            position_groups.extend([pos] * len(subset))
            variant_groups.extend([var] * len(subset))
            time_values.extend(subset['completion_time_minutes'].tolist())

# Learning effect excluding Roleplay (the high-variance condition)
print("\n5. Learning effect EXCLUDING Roleplay variant:")
df_no_roleplay = df_valid[df_valid['Variant'] != 'Roleplay'].copy()
pos_stats_no_roleplay = df_no_roleplay.groupby('Position in study')['completion_time_minutes'].agg(['count', 'mean', 'std']).round(2)
print(pos_stats_no_roleplay)

pos_groups_no_roleplay = [group['completion_time_minutes'].values for name, group in df_no_roleplay.groupby('Position in study')]
f_stat_no_roleplay, p_val_no_roleplay = stats.f_oneway(*pos_groups_no_roleplay)
print(f"ANOVA (Position, no Roleplay): F={f_stat_no_roleplay:.3f}, p={p_val_no_roleplay:.3f}")

# Learning effect for each variant separately
print("\n6. Learning effect within each variant:")
for variant in sorted(df_valid['Variant'].unique()):
    variant_data = df_valid[df_valid['Variant'] == variant]
    if len(variant_data) >= 4:  # Need at least 4 positions
        pos_means = variant_data.groupby('Position in study')['completion_time_minutes'].mean()
        print(f"\n{variant}:")
        for pos in sorted(pos_means.index):
            print(f"  Position {pos}: {pos_means[pos]:.2f} min")
        
        # Simple correlation with position
        correlation = variant_data['Position in study'].corr(variant_data['completion_time_minutes'])
        print(f"  Correlation with position: r={correlation:.3f}")

# Participant pair analysis
print("\n7. Learning effect by participant pair:")
participant_pairs = []
for _, row in df_valid.iterrows():
    pair = tuple(sorted([row['Participant 1 ID'], row['Participant 2 ID']]))
    participant_pairs.append(pair)

df_valid['pair'] = participant_pairs
pair_learning = []

for pair in df_valid['pair'].unique():
    pair_data = df_valid[df_valid['pair'] == pair].sort_values('Position in study')
    if len(pair_data) == 4:  # Complete set
        times = pair_data['completion_time_minutes'].values
        positions = pair_data['Position in study'].values
        correlation = np.corrcoef(positions, times)[0,1]
        pair_learning.append(correlation)
        print(f"Pair {pair}: times={times.round(1)}, correlation r={correlation:.3f}")

print(f"\nOverall learning correlations across pairs: mean={np.mean(pair_learning):.3f}, std={np.std(pair_learning):.3f}")

# Visualizations
fig, axes = plt.subplots(2, 2, figsize=(12, 10))
fig.suptitle('Learning Effect Analysis', fontsize=16, fontweight='bold')

# Position means with and without Roleplay
axes[0,0].plot(range(4), df_valid.groupby('Position in study')['completion_time_minutes'].mean().values, 
               'o-', label='All variants', linewidth=2, markersize=8, color='black')
axes[0,0].plot(range(4), df_no_roleplay.groupby('Position in study')['completion_time_minutes'].mean().values, 
               'o-', label='Excluding Roleplay', linewidth=2, markersize=8, color='#a44b9c')
axes[0,0].set_xlabel('Position in Study')
axes[0,0].set_ylabel('Mean Completion Time (minutes)')
axes[0,0].set_title('Learning Effect: With vs Without Roleplay')
axes[0,0].set_xticks(range(4))
axes[0,0].legend()

# Individual variant trends
for i, variant in enumerate(['Open Ended', 'Silent', 'Timed', 'Roleplay']):
    variant_data = df_valid[df_valid['Variant'] == variant]
    pos_means = variant_data.groupby('Position in study')['completion_time_minutes'].mean()
    color = ['grey', 'blue', 'green', 'red'][i]
    axes[0,1].plot(pos_means.index, pos_means.values, 'o-', label=variant, linewidth=1.5, alpha=0.7)

axes[0,1].set_xlabel('Position in Study')
axes[0,1].set_ylabel('Mean Completion Time (minutes)')
axes[0,1].set_title('Learning Curves by Variant')
axes[0,1].legend()
axes[0,1].set_xticks(range(4))

# Distribution of learning correlations
axes[1,0].hist(pair_learning, bins=6, color='lightgrey', alpha=0.7, edgecolor='black')
axes[1,0].axvline(np.mean(pair_learning), color='#a44b9c', linestyle='--', linewidth=2, 
                  label=f'Mean: {np.mean(pair_learning):.3f}')
axes[1,0].set_xlabel('Learning Correlation (r)')
axes[1,0].set_ylabel('Frequency')
axes[1,0].set_title('Distribution of Learning Effects Across Pairs')
axes[1,0].legend()

# Box plot by position (no Roleplay)
pos_data_no_roleplay = [df_no_roleplay[df_no_roleplay['Position in study'] == pos]['completion_time_minutes'].values 
                        for pos in range(4)]
bp = axes[1,1].boxplot(pos_data_no_roleplay, labels=range(4), patch_artist=True)
for patch in bp['boxes']:
    patch.set_facecolor('lightgrey')
axes[1,1].set_xlabel('Position in Study')
axes[1,1].set_ylabel('Completion Time (minutes)')
axes[1,1].set_title('Time by Position (Excluding Roleplay)')

plt.tight_layout()
plt.savefig(f'{output_dir}/learning_effect_analysis.pdf', dpi=300, bbox_inches='tight')
plt.savefig(f'{output_dir}/learning_effect_analysis.png', dpi=300, bbox_inches='tight')
plt.close()

print(f"\nVisualizations saved to {output_dir}/learning_effect_analysis.pdf/png") 