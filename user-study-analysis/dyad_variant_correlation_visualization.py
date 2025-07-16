#!/usr/bin/env python3
"""
Visualization showing movement distance correlations per dyad per task variant
to demonstrate the "relatively stable" patterns mentioned in the thesis results.
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy.stats import pearsonr
import warnings
warnings.filterwarnings('ignore')

def load_and_prepare_data():
    """Load and prepare the study data"""
    df = pd.read_csv('study-run-results.csv')
    
    # Rename columns for easier access
    df = df.rename(columns={
        'Run #': 'run_id',
        'Participant 1 ID': 'participant_1_id',
        'Participant 2 ID': 'participant_2_id', 
        'Variant': 'collaboration_variant',
        'Participant 1 distance (meter)': 'p1_movement_distance',
        'Participant 2 distance (meter)': 'p2_movement_distance'
    })
    
    # Create dyad identifier
    df['dyad_id'] = df['participant_1_id'] + '_' + df['participant_2_id']
    
    # Convert movement distances to numeric, handle INVALID values
    df['p1_movement_distance'] = pd.to_numeric(df['p1_movement_distance'], errors='coerce')
    df['p2_movement_distance'] = pd.to_numeric(df['p2_movement_distance'], errors='coerce')
    
    # Remove rows with invalid movement data
    df = df.dropna(subset=['p1_movement_distance', 'p2_movement_distance'])
    
    return df

def calculate_dyad_variant_correlations(df):
    """Calculate movement distance correlations for each dyad within each variant"""
    
    correlation_data = []
    dyad_summary = {}
    
    # Get unique dyads
    dyads = df['dyad_id'].unique()
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    
    for dyad_id in dyads:
        dyad_data = df[df['dyad_id'] == dyad_id]
        dyad_correlations = {}
        
        # Calculate overall dyad correlation (across all sessions)
        p1_all = dyad_data['p1_movement_distance'].values
        p2_all = dyad_data['p2_movement_distance'].values
        
        if len(p1_all) > 1 and np.var(p1_all) > 0 and np.var(p2_all) > 0:
            overall_corr, overall_p = pearsonr(p1_all, p2_all)
        else:
            overall_corr = np.nan
            overall_p = np.nan
        
        # Calculate correlation within each variant (when possible)
        for variant in variants:
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            
            if len(variant_data) == 0:
                # Dyad didn't participate in this variant
                correlation_data.append({
                    'dyad_id': dyad_id,
                    'variant': variant,
                    'correlation': np.nan,
                    'p_value': np.nan,
                    'n_sessions': 0,
                    'interpretable': False
                })
            elif len(variant_data) == 1:
                # Single session - use movement ratio as proxy
                p1_dist = variant_data['p1_movement_distance'].iloc[0]
                p2_dist = variant_data['p2_movement_distance'].iloc[0]
                
                if p1_dist > 0 and p2_dist > 0:
                    # Calculate similarity as ratio (closer to 1 = more similar)
                    ratio = min(p1_dist, p2_dist) / max(p1_dist, p2_dist)
                    # Convert to correlation-like scale (-1 to 1)
                    similarity = 2 * ratio - 1  # Maps 0.5->0, 1->1
                else:
                    similarity = 0
                
                correlation_data.append({
                    'dyad_id': dyad_id,
                    'variant': variant,
                    'correlation': similarity,
                    'p_value': np.nan,
                    'n_sessions': 1,
                    'interpretable': True,
                    'p1_distance': p1_dist,
                    'p2_distance': p2_dist
                })
            else:
                # Multiple sessions - calculate actual correlation
                p1_distances = variant_data['p1_movement_distance'].values
                p2_distances = variant_data['p2_movement_distance'].values
                
                if np.var(p1_distances) > 0 and np.var(p2_distances) > 0:
                    corr, p_val = pearsonr(p1_distances, p2_distances)
                else:
                    corr = 0  # No variance = perfect similarity
                    p_val = 1
                
                correlation_data.append({
                    'dyad_id': dyad_id,
                    'variant': variant,
                    'correlation': corr,
                    'p_value': p_val,
                    'n_sessions': len(variant_data),
                    'interpretable': True
                })
        
        dyad_summary[dyad_id] = {
            'overall_correlation': overall_corr,
            'overall_p_value': overall_p,
            'total_sessions': len(dyad_data)
        }
    
    return pd.DataFrame(correlation_data), dyad_summary

def create_stability_visualization(correlation_df, dyad_summary):
    """Create visualization showing dyad stability across variants"""
    
    # Set up the plot
    plt.style.use('default')
    fig, axes = plt.subplots(2, 2, figsize=(16, 12))
    fig.suptitle('Movement Distance Correlations: Dyad Stability Across Task Variants', 
                 fontsize=16, fontweight='bold')
    
    # Prepare data for plotting
    interpretable_data = correlation_df[correlation_df['interpretable'] == True]
    
    # Get dyads sorted by overall correlation (high to low)
    dyad_order = sorted(dyad_summary.keys(), 
                       key=lambda x: dyad_summary[x]['overall_correlation'] if not np.isnan(dyad_summary[x]['overall_correlation']) else -2, 
                       reverse=True)
    
    # Shorten dyad names for display
    dyad_display_names = [dyad[:12] + '...' if len(dyad) > 15 else dyad for dyad in dyad_order]
    
    # 1. Heatmap showing correlations per dyad per variant
    ax1 = axes[0, 0]
    
    # Create pivot table for heatmap
    heatmap_data = interpretable_data.pivot(index='dyad_id', columns='variant', values='correlation')
    heatmap_data = heatmap_data.reindex(dyad_order)
    
    # Create custom colormap (red for low correlation, green for high)
    im = ax1.imshow(heatmap_data.values, aspect='auto', cmap='RdYlGn', vmin=-1, vmax=1)
    
    # Set ticks and labels
    ax1.set_xticks(range(len(heatmap_data.columns)))
    ax1.set_xticklabels(heatmap_data.columns, rotation=45)
    ax1.set_yticks(range(len(dyad_order)))
    ax1.set_yticklabels(dyad_display_names, fontsize=8)
    ax1.set_title('Movement Distance Correlations\nby Dyad and Variant', fontweight='bold')
    
    # Add colorbar
    cbar = plt.colorbar(im, ax=ax1, shrink=0.8)
    cbar.set_label('Correlation Coefficient')
    
    # Add text annotations for correlation values
    for i in range(len(dyad_order)):
        for j in range(len(heatmap_data.columns)):
            if not np.isnan(heatmap_data.iloc[i, j]):
                text_color = 'white' if abs(heatmap_data.iloc[i, j]) > 0.5 else 'black'
                ax1.text(j, i, f'{heatmap_data.iloc[i, j]:.2f}', 
                        ha="center", va="center", color=text_color, fontsize=8)
    
    # 2. Line plot showing stability patterns for each dyad
    ax2 = axes[0, 1]
    
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    variant_positions = np.arange(len(variants))
    
    # Plot line for each dyad
    colors = plt.cm.tab10(np.linspace(0, 1, len(dyad_order)))
    
    for i, dyad_id in enumerate(dyad_order):
        dyad_data = interpretable_data[interpretable_data['dyad_id'] == dyad_id]
        
        correlations = []
        for variant in variants:
            variant_corr = dyad_data[dyad_data['variant'] == variant]['correlation']
            if len(variant_corr) > 0:
                correlations.append(variant_corr.iloc[0])
            else:
                correlations.append(np.nan)
        
        # Only plot if we have at least 2 valid points
        valid_points = sum(~np.isnan(correlations))
        if valid_points >= 2:
            ax2.plot(variant_positions, correlations, marker='o', alpha=0.7, 
                    color=colors[i], label=dyad_display_names[i], linewidth=1.5)
    
    ax2.set_xticks(variant_positions)
    ax2.set_xticklabels(variants, rotation=45)
    ax2.set_ylabel('Movement Distance Correlation')
    ax2.set_title('Correlation Stability Across Variants\n(Individual Dyad Trajectories)', fontweight='bold')
    ax2.grid(True, alpha=0.3)
    ax2.set_ylim(-1, 1)
    ax2.axhline(y=0, color='black', linestyle='--', alpha=0.5)
    
    # Add legend (only for dyads with enough data)
    handles, labels = ax2.get_legend_handles_labels()
    if len(handles) > 0:
        ax2.legend(bbox_to_anchor=(1.05, 1), loc='upper left', fontsize=8)
    
    # 3. Variance in correlations across variants (stability measure)
    ax3 = axes[1, 0]
    
    stability_scores = []
    dyad_names_with_data = []
    
    for dyad_id in dyad_order:
        dyad_data = interpretable_data[interpretable_data['dyad_id'] == dyad_id]
        correlations = []
        
        for variant in variants:
            variant_corr = dyad_data[dyad_data['variant'] == variant]['correlation']
            if len(variant_corr) > 0:
                correlations.append(variant_corr.iloc[0])
        
        if len(correlations) >= 2:
            # Calculate standard deviation (lower = more stable)
            stability = np.std(correlations)
            stability_scores.append(stability)
            dyad_names_with_data.append(dyad_id[:12] + '...' if len(dyad_id) > 15 else dyad_id)
    
    if stability_scores:
        bars = ax3.bar(range(len(stability_scores)), stability_scores, 
                      color='lightblue', edgecolor='navy', alpha=0.7)
        ax3.set_xticks(range(len(dyad_names_with_data)))
        ax3.set_xticklabels(dyad_names_with_data, rotation=45, ha='right', fontsize=8)
        ax3.set_ylabel('Standard Deviation of Correlations')
        ax3.set_title('Correlation Stability Across Variants\n(Lower = More Stable)', fontweight='bold')
        ax3.grid(axis='y', alpha=0.3)
        
        # Add value labels on bars
        for bar, score in zip(bars, stability_scores):
            ax3.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 0.01, 
                    f'{score:.3f}', ha='center', va='bottom', fontsize=8)
    
    # 4. Overall correlation vs stability scatter plot
    ax4 = axes[1, 1]
    
    overall_corrs = []
    stabilities = []
    scatter_labels = []
    
    for i, dyad_id in enumerate(dyad_order):
        if dyad_id in [d[:12] + '...' if len(d) > 15 else d for d in dyad_names_with_data]:
            overall_corr = dyad_summary[dyad_id]['overall_correlation']
            if not np.isnan(overall_corr):
                # Find corresponding stability
                dyad_display = dyad_id[:12] + '...' if len(dyad_id) > 15 else dyad_id
                if dyad_display in dyad_names_with_data:
                    idx = dyad_names_with_data.index(dyad_display)
                    overall_corrs.append(overall_corr)
                    stabilities.append(stability_scores[idx])
                    scatter_labels.append(dyad_display)
    
    if len(overall_corrs) > 0:
        scatter = ax4.scatter(overall_corrs, stabilities, c=range(len(overall_corrs)), 
                            cmap='viridis', s=100, alpha=0.7, edgecolors='black')
        
        # Add labels for each point
        for i, label in enumerate(scatter_labels):
            ax4.annotate(label, (overall_corrs[i], stabilities[i]), 
                        xytext=(5, 5), textcoords='offset points', fontsize=8)
        
        ax4.set_xlabel('Overall Movement Distance Correlation')
        ax4.set_ylabel('Stability (Std Dev Across Variants)')
        ax4.set_title('Overall Correlation vs Stability\n(Bottom-right = High corr + Stable)', fontweight='bold')
        ax4.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../assets/06/dyad_variant_correlation_stability.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    return interpretable_data

def print_summary_statistics(correlation_df, dyad_summary):
    """Print summary statistics about the correlation patterns"""
    
    print("=== DYAD MOVEMENT CORRELATION STABILITY ANALYSIS ===\n")
    
    interpretable_data = correlation_df[correlation_df['interpretable'] == True]
    
    # Overall statistics
    print("OVERALL STATISTICS:")
    print("-" * 40)
    print(f"Total dyads analyzed: {len(dyad_summary)}")
    print(f"Sessions with interpretable correlation data: {len(interpretable_data)}")
    
    # Dyad-level analysis
    print("\nINDIVIDUAL DYAD CORRELATIONS:")
    print("-" * 40)
    
    # Sort dyads by overall correlation
    sorted_dyads = sorted(dyad_summary.items(), 
                         key=lambda x: x[1]['overall_correlation'] if not np.isnan(x[1]['overall_correlation']) else -2, 
                         reverse=True)
    
    for dyad_id, summary in sorted_dyads:
        if not np.isnan(summary['overall_correlation']):
            print(f"{dyad_id}: r = {summary['overall_correlation']:.3f} (p = {summary['overall_p_value']:.3f}, n = {summary['total_sessions']})")
    
    # Stability analysis
    print("\nSTABILITY ANALYSIS:")
    print("-" * 40)
    
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    
    for dyad_id, _ in sorted_dyads:
        dyad_data = interpretable_data[interpretable_data['dyad_id'] == dyad_id]
        correlations = []
        
        for variant in variants:
            variant_corr = dyad_data[dyad_data['variant'] == variant]['correlation']
            if len(variant_corr) > 0:
                correlations.append(variant_corr.iloc[0])
        
        if len(correlations) >= 2:
            stability = np.std(correlations)
            mean_corr = np.mean(correlations)
            print(f"{dyad_id}: Mean r = {mean_corr:.3f}, Stability (SD) = {stability:.3f}")
    
    # Variant-level statistics
    print("\nCORRELATIONS BY VARIANT:")
    print("-" * 40)
    
    for variant in variants:
        variant_data = interpretable_data[interpretable_data['variant'] == variant]
        if len(variant_data) > 0:
            correlations = variant_data['correlation'].dropna()
            if len(correlations) > 0:
                print(f"{variant}: Mean r = {correlations.mean():.3f} ± {correlations.std():.3f} (n = {len(correlations)})")

def main():
    """Main function"""
    
    # Load data
    print("Loading study data...")
    df = load_and_prepare_data()
    
    # Calculate correlations
    print("Calculating dyad-variant correlations...")
    correlation_df, dyad_summary = calculate_dyad_variant_correlations(df)
    
    # Create visualization
    print("Creating stability visualization...")
    interpretable_data = create_stability_visualization(correlation_df, dyad_summary)
    
    # Print summary
    print_summary_statistics(correlation_df, dyad_summary)
    
    print(f"\nVisualization saved to: ../assets/06/dyad_variant_correlation_stability.pdf")

if __name__ == "__main__":
    main() 