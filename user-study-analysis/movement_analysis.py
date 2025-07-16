#!/usr/bin/env python3

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
from matplotlib.patches import Rectangle
import warnings
warnings.filterwarnings('ignore')

# Set consistent style
plt.style.use('default')
plt.rcParams['font.size'] = 10
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['axes.linewidth'] = 0.8
plt.rcParams['axes.edgecolor'] = '#333333'
plt.rcParams['axes.labelcolor'] = '#333333'
plt.rcParams['xtick.color'] = '#333333'
plt.rcParams['ytick.color'] = '#333333'
plt.rcParams['figure.facecolor'] = 'white'
plt.rcParams['axes.facecolor'] = 'white'

# Load data
df = pd.read_csv('study-run-results.csv')

# Convert completion time to minutes
def parse_time(time_str):
    if pd.isna(time_str):
        return np.nan
    if ':' in str(time_str):
        parts = str(time_str).split(':')
        return float(parts[0]) + float(parts[1])/60
    return float(time_str)

df['completion_time_min'] = df['Completion time (exact, minutes)'].apply(parse_time)

# Clean data
df = df[df['Participant 1 distance (meter)'] != 'INVALID'].copy()
df['participant_1_distance'] = pd.to_numeric(df['Participant 1 distance (meter)'])
df['participant_2_distance'] = pd.to_numeric(df['Participant 2 distance (meter)'])
df['total_distance'] = df['participant_1_distance'] + df['participant_2_distance']
df['distance_difference'] = abs(df['participant_1_distance'] - df['participant_2_distance'])
df['movement_coordination'] = 1 - (df['distance_difference'] / df['total_distance'])

# Key insight: Movement efficiency (distance per minute) is more meaningful than total distance
df['movement_efficiency'] = df['total_distance'] / df['completion_time_min']

print("Movement Analysis Results (Corrected for Session Duration)")
print("=" * 60)

# Overall movement statistics - focus on efficiency
print(f"Total sessions analyzed: {len(df)}")
print(f"Mean movement efficiency: {df['movement_efficiency'].mean():.2f} m/min")
print(f"Range: {df['movement_efficiency'].min():.2f} - {df['movement_efficiency'].max():.2f} m/min")

# Correlation between total movement and completion time
total_time_corr = df['total_distance'].corr(df['completion_time_min'])
print(f"Total movement vs completion time correlation: r = {total_time_corr:.3f}")
print("This confirms movement scales with time - need to use efficiency!")

# Movement efficiency by variant (this is the key analysis)
print("\nMovement Efficiency by Collaboration Variant:")
efficiency_stats = df.groupby('Variant').agg({
    'movement_efficiency': ['mean', 'std', 'count'],
    'total_distance': ['mean', 'std'],
    'completion_time_min': ['mean', 'std'],
    'movement_coordination': ['mean', 'std']
}).round(2)

print(efficiency_stats)

# Statistical analysis - focus on efficiency, not total movement
variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
efficiency_by_variant = [df[df['Variant'] == v]['movement_efficiency'].values for v in variants]
f_stat_eff, p_value_eff = stats.f_oneway(*efficiency_by_variant)
print(f"\nANOVA for movement EFFICIENCY by variant: F={f_stat_eff:.3f}, p={p_value_eff:.3f}")

# Compare with total movement ANOVA (should be significant due to time confound)
movement_by_variant = [df[df['Variant'] == v]['total_distance'].values for v in variants]
f_stat_total, p_value_total = stats.f_oneway(*movement_by_variant)
print(f"ANOVA for total movement by variant: F={f_stat_total:.3f}, p={p_value_total:.3f}")
print("^ This significance is likely just due to longer sessions having more movement")

# Create corrected visualization focusing on efficiency
fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(12, 10))
fig.suptitle('Movement Behaviour Analysis (Time-Corrected)', fontsize=14, fontweight='bold')

# 1. Movement efficiency by variant (key analysis)
efficiency_data = []
variant_labels = []
for variant in variants:
    data = df[df['Variant'] == variant]['movement_efficiency']
    if len(data) > 0:
        efficiency_data.append(data)
        variant_labels.append(variant)

box_plot = ax1.boxplot(efficiency_data, labels=variant_labels, patch_artist=True)
for patch in box_plot['boxes']:
    patch.set_facecolor('#e6e6e6')
    patch.set_edgecolor('#333333')
for element in ['whiskers', 'fliers', 'medians', 'caps']:
    plt.setp(box_plot[element], color='#333333')
ax1.set_title('Movement Efficiency by Collaboration Variant', fontweight='bold')
ax1.set_ylabel('Movement Efficiency (metres/minute)')
ax1.tick_params(axis='x', rotation=45)
ax1.grid(axis='y', alpha=0.3)

# 2. Total movement vs time (showing the confound)
colors = {'Open Ended': '#666666', 'Roleplay': '#999999', 'Silent': '#cccccc', 'Timed': '#a44b9c'}
for variant in variants:
    variant_data = df[df['Variant'] == variant]
    if len(variant_data) > 0:
        ax2.scatter(variant_data['completion_time_min'], variant_data['total_distance'], 
                   color=colors.get(variant, '#666666'), label=variant, alpha=0.7, s=40)

ax2.set_xlabel('Completion Time (minutes)')
ax2.set_ylabel('Total Distance (metres)')
ax2.set_title('Total Movement vs Time (Showing Time Confound)', fontweight='bold')
ax2.legend(fontsize=8)
ax2.grid(alpha=0.3)

# Add correlation line
from scipy.stats import linregress
slope, intercept, r_value, p_value, std_err = linregress(df['completion_time_min'], df['total_distance'])
line_x = np.array([df['completion_time_min'].min(), df['completion_time_min'].max()])
line_y = slope * line_x + intercept
ax2.plot(line_x, line_y, '--', color='#a44b9c', alpha=0.8, linewidth=2)
ax2.text(0.05, 0.95, f'r = {r_value:.3f}', transform=ax2.transAxes, 
         bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

# 3. Movement coordination by variant
coord_data = []
for variant in variants:
    data = df[df['Variant'] == variant]['movement_coordination']
    if len(data) > 0:
        coord_data.append(data)

box_plot2 = ax3.boxplot(coord_data, labels=variant_labels, patch_artist=True)
for patch in box_plot2['boxes']:
    patch.set_facecolor('#e6e6e6')
    patch.set_edgecolor('#333333')
for element in ['whiskers', 'fliers', 'medians', 'caps']:
    plt.setp(box_plot2[element], color='#333333')
ax3.set_title('Movement Coordination by Collaboration Variant', fontweight='bold')
ax3.set_ylabel('Movement Coordination Index')
ax3.tick_params(axis='x', rotation=45)
ax3.grid(axis='y', alpha=0.3)

# 4. Learning effect on movement efficiency (not total movement)
position_efficiency = df.groupby('Position in study')['movement_efficiency'].agg(['mean', 'std']).reset_index()
ax4.errorbar(position_efficiency['Position in study'], position_efficiency['mean'], 
            yerr=position_efficiency['std'], marker='o', capsize=5, 
            color='#333333', markerfacecolor='#a44b9c', markersize=6)
ax4.set_xlabel('Study Position')
ax4.set_ylabel('Mean Movement Efficiency (m/min)')
ax4.set_title('Learning Effect on Movement Efficiency', fontweight='bold')
ax4.set_xticks([0, 1, 2, 3])
ax4.grid(alpha=0.3)

plt.tight_layout()
plt.savefig('../assets/06/movement_spatial_analysis.pdf', dpi=300, bbox_inches='tight')
plt.close()

# Partner coordination analysis (still valid)
fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
fig.suptitle('Movement Coordination Analysis', fontsize=14, fontweight='bold')

# Partner movement correlation
ax1.scatter(df['participant_1_distance'], df['participant_2_distance'], 
           alpha=0.6, s=40, color='#666666')
max_dist = max(df['participant_1_distance'].max(), df['participant_2_distance'].max())
ax1.plot([0, max_dist], [0, max_dist], '--', color='#a44b9c', alpha=0.7, linewidth=2)
ax1.set_xlabel('Participant 1 Distance (metres)')
ax1.set_ylabel('Participant 2 Distance (metres)')
ax1.set_title('Partner Movement Correlation', fontweight='bold')
ax1.grid(alpha=0.3)

correlation = df['participant_1_distance'].corr(df['participant_2_distance'])
ax1.text(0.05, 0.95, f'r = {correlation:.3f}', transform=ax1.transAxes, 
         bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

# Movement efficiency vs coordination
ax2.scatter(df['movement_coordination'], df['movement_efficiency'], 
           alpha=0.6, s=40, color='#666666')
ax2.set_xlabel('Movement Coordination Index')
ax2.set_ylabel('Movement Efficiency (m/min)')
ax2.set_title('Coordination vs Efficiency', fontweight='bold')
ax2.grid(alpha=0.3)

coord_eff_corr = df['movement_coordination'].corr(df['movement_efficiency'])
ax2.text(0.05, 0.95, f'r = {coord_eff_corr:.3f}', transform=ax2.transAxes, 
         bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))

plt.tight_layout()
plt.savefig('../assets/06/individual_movement_patterns.pdf', dpi=300, bbox_inches='tight')
plt.close()

# Corrected statistical results
print("\n" + "="*60)
print("CORRECTED ANALYSIS: MOVEMENT EFFICIENCY (TIME-CONTROLLED)")
print("="*60)

print("\nMovement Efficiency Statistics by Variant:")
for variant in variants:
    data = df[df['Variant'] == variant]
    if len(data) > 0:
        print(f"{variant} & {len(data)} & {data['movement_efficiency'].mean():.2f} & {data['movement_efficiency'].std():.2f} & "
              f"{data['movement_coordination'].mean():.3f} \\\\")

print(f"\nKey Correlations (Corrected):")
print(f"Movement efficiency vs completion time: r = {df['movement_efficiency'].corr(df['completion_time_min']):.3f}")
print(f"Movement coordination vs completion time: r = {df['movement_coordination'].corr(df['completion_time_min']):.3f}")
print(f"Partner movement correlation: r = {correlation:.3f}")
print(f"Movement coordination vs efficiency: r = {coord_eff_corr:.3f}")

# Learning effect on efficiency (not total movement)
position_eff_corr = df['Position in study'].corr(df['movement_efficiency'])
print(f"Learning effect (position vs efficiency): r = {position_eff_corr:.3f}")

print("\nCorrected Statistical Tests:")
print(f"Movement EFFICIENCY by variant: F({len(variants)-1},{len(df)-len(variants)}) = {f_stat_eff:.3f}, p = {p_value_eff:.3f}")
print(f"Total movement by variant: F({len(variants)-1},{len(df)-len(variants)}) = {f_stat_total:.3f}, p = {p_value_total:.3f}")
print(f"^ Total movement significance is confounded by session duration")

# Effect sizes
from scipy.stats import f_oneway
eta_squared_eff = (f_stat_eff * (len(variants)-1)) / (f_stat_eff * (len(variants)-1) + len(df) - len(variants))
eta_squared_total = (f_stat_total * (len(variants)-1)) / (f_stat_total * (len(variants)-1) + len(df) - len(variants))
print(f"\nEffect sizes (eta-squared):")
print(f"Movement efficiency: {eta_squared_eff:.3f}")
print(f"Total movement: {eta_squared_total:.3f}")

print(f"\n\nCONCLUSION:")
print(f"The apparent variant effect on movement (p={p_value_total:.3f}) disappears when controlling")
print(f"for session duration. Movement efficiency shows no significant differences (p={p_value_eff:.3f}).")
print(f"Movement patterns are primarily driven by session length, not collaboration mode.") 