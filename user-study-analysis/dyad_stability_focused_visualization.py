#!/usr/bin/env python3
"""
Clean, focused visualization showing movement distance correlation stability
across task variants for each dyad - for thesis inclusion.
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

def calculate_dyad_variant_patterns(df):
    """Calculate movement patterns for each dyad across variants"""
    
    # Get unique dyads and variants
    dyads = df['dyad_id'].unique()
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    
    # Calculate overall correlations for each dyad
    dyad_overall_corr = {}
    for dyad_id in dyads:
        dyad_data = df[df['dyad_id'] == dyad_id]
        p1_all = dyad_data['p1_movement_distance'].values
        p2_all = dyad_data['p2_movement_distance'].values
        
        if len(p1_all) > 1 and np.var(p1_all) > 0 and np.var(p2_all) > 0:
            overall_corr, overall_p = pearsonr(p1_all, p2_all)
            dyad_overall_corr[dyad_id] = {'corr': overall_corr, 'p': overall_p}
    
    # Calculate movement similarity for each dyad-variant combination
    stability_data = []
    
    for dyad_id in dyads:
        dyad_data = df[df['dyad_id'] == dyad_id]
        variant_similarities = []
        
        for variant in variants:
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            
            if len(variant_data) == 1:
                # Single session - calculate movement similarity as ratio
                p1_dist = variant_data['p1_movement_distance'].iloc[0]
                p2_dist = variant_data['p2_movement_distance'].iloc[0]
                
                if p1_dist > 0 and p2_dist > 0:
                    # Movement similarity (0 = completely different, 1 = identical)
                    similarity = min(p1_dist, p2_dist) / max(p1_dist, p2_dist)
                    variant_similarities.append(similarity)
                    
                    stability_data.append({
                        'dyad_id': dyad_id,
                        'variant': variant,
                        'similarity': similarity,
                        'p1_distance': p1_dist,
                        'p2_distance': p2_dist
                    })
        
        # Calculate stability (consistency across variants)
        if len(variant_similarities) >= 3:  # Need at least 3 variants for meaningful stability
            stability = 1 - np.std(variant_similarities)  # Higher = more stable
            mean_similarity = np.mean(variant_similarities)
            
            # Add overall info to each record
            for record in stability_data:
                if record['dyad_id'] == dyad_id:
                    record['dyad_stability'] = stability
                    record['dyad_mean_similarity'] = mean_similarity
                    if dyad_id in dyad_overall_corr:
                        record['overall_correlation'] = dyad_overall_corr[dyad_id]['corr']
                        record['overall_p_value'] = dyad_overall_corr[dyad_id]['p']
    
    return pd.DataFrame(stability_data), dyad_overall_corr

def create_focused_stability_visualization(stability_df, dyad_overall_corr):
    """Create a focused visualization showing dyad stability patterns"""
    
    # Set style for publication
    plt.style.use('default')
    plt.rcParams.update({
        'font.size': 10,
        'axes.titlesize': 12,
        'axes.labelsize': 10,
        'xtick.labelsize': 9,
        'ytick.labelsize': 9,
        'legend.fontsize': 8,
        'figure.titlesize': 14
    })
    
    # Create figure with subplots
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    fig.suptitle('Movement Distance Correlation Stability Across Task Variants', 
                 fontsize=14, fontweight='bold')
    
    # Prepare data
    dyads = stability_df['dyad_id'].unique()
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    
    # Sort dyads by overall correlation (high to low)
    dyad_order = sorted(dyads, 
                       key=lambda x: dyad_overall_corr.get(x, {'corr': -2})['corr'], 
                       reverse=True)
    
    # Shorten dyad names for display (use first 8 chars of each participant)
    dyad_labels = []
    for dyad_id in dyad_order:
        parts = dyad_id.split('_')
        if len(parts) == 2:
            label = f"{parts[0][:4]}-{parts[1][:4]}"
        else:
            label = dyad_id[:8]
        dyad_labels.append(label)
    
    # 1. Line plot showing individual dyad trajectories across variants
    ax1 = axes[0]
    
    # Use colormap for dyads
    colors = plt.cm.tab10(np.linspace(0, 1, len(dyad_order)))
    
    variant_positions = np.arange(len(variants))
    
    for i, dyad_id in enumerate(dyad_order):
        dyad_data = stability_df[stability_df['dyad_id'] == dyad_id]
        
        similarities = []
        for variant in variants:
            variant_data = dyad_data[dyad_data['variant'] == variant]
            if len(variant_data) > 0:
                similarities.append(variant_data['similarity'].iloc[0])
            else:
                similarities.append(np.nan)
        
        # Only plot if we have at least 3 valid points
        valid_points = sum(~np.isnan(similarities))
        if valid_points >= 3:
            ax1.plot(variant_positions, similarities, marker='o', alpha=0.8, 
                    color=colors[i], label=dyad_labels[i], linewidth=2, markersize=6)
    
    ax1.set_xticks(variant_positions)
    ax1.set_xticklabels(variants, rotation=30, ha='right')
    ax1.set_ylabel('Movement Distance Similarity')
    ax1.set_title('Individual Dyad Trajectories Across Variants', fontweight='bold')
    ax1.grid(True, alpha=0.3)
    ax1.set_ylim(0, 1)
    ax1.legend(bbox_to_anchor=(1.05, 1), loc='upper left', fontsize=8)
    
    # Add text annotation about stability
    ax1.text(0.02, 0.98, 'Higher values = more similar\nmovement distances', 
             transform=ax1.transAxes, fontsize=8, verticalalignment='top',
             bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))
    
    # 2. Stability vs Overall Correlation scatter plot
    ax2 = axes[1]
    
    # Calculate stability and overall correlation for each dyad
    scatter_data = []
    for dyad_id in dyad_order:
        dyad_data = stability_df[stability_df['dyad_id'] == dyad_id]
        if len(dyad_data) >= 3 and dyad_id in dyad_overall_corr:
            similarities = dyad_data['similarity'].values
            stability = 1 - np.std(similarities)  # Higher = more stable
            overall_corr = dyad_overall_corr[dyad_id]['corr']
            overall_p = dyad_overall_corr[dyad_id]['p']
            
            scatter_data.append({
                'dyad_id': dyad_id,
                'stability': stability,
                'overall_correlation': overall_corr,
                'overall_p_value': overall_p,
                'significant': overall_p < 0.05
            })
    
    scatter_df = pd.DataFrame(scatter_data)
    
    if len(scatter_df) > 0:
        # Color by significance
        colors_sig = ['red' if sig else 'blue' for sig in scatter_df['significant']]
        sizes = [100 if sig else 60 for sig in scatter_df['significant']]
        
        scatter = ax2.scatter(scatter_df['overall_correlation'], scatter_df['stability'], 
                            c=colors_sig, s=sizes, alpha=0.7, edgecolors='black')
        
        # Add labels
        for i, row in scatter_df.iterrows():
            dyad_label = dyad_labels[dyad_order.index(row['dyad_id'])]
            ax2.annotate(dyad_label, 
                        (row['overall_correlation'], row['stability']), 
                        xytext=(5, 5), textcoords='offset points', fontsize=8)
        
        ax2.set_xlabel('Overall Movement Distance Correlation (r)')
        ax2.set_ylabel('Stability Across Variants\n(1 - std of similarities)')
        ax2.set_title('Overall Correlation vs Cross-Variant Stability', fontweight='bold')
        ax2.grid(True, alpha=0.3)
        
        # Add legend for significance
        ax2.scatter([], [], c='red', s=100, alpha=0.7, edgecolors='black', label='p < 0.05')
        ax2.scatter([], [], c='blue', s=60, alpha=0.7, edgecolors='black', label='p ≥ 0.05')
        ax2.legend(loc='upper left', fontsize=8)
        
        # Add quadrant labels
        ax2.axhline(y=np.median(scatter_df['stability']), color='gray', linestyle='--', alpha=0.5)
        ax2.axvline(x=np.median(scatter_df['overall_correlation']), color='gray', linestyle='--', alpha=0.5)
        
        # Add interpretive text
        ax2.text(0.02, 0.98, 'Top-right = High correlation\n& high stability', 
                transform=ax2.transAxes, fontsize=8, verticalalignment='top',
                bbox=dict(boxstyle='round', facecolor='lightyellow', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig('../assets/06/dyad_movement_correlation_stability_focused.pdf', 
                dpi=300, bbox_inches='tight')
    plt.close()
    
    return scatter_df

def print_focused_summary(stability_df, scatter_df, dyad_overall_corr):
    """Print focused summary for thesis"""
    
    print("\n=== MOVEMENT CORRELATION STABILITY SUMMARY ===\n")
    
    # Overall correlation statistics
    overall_corrs = [data['corr'] for data in dyad_overall_corr.values() if not np.isnan(data['corr'])]
    print(f"Overall movement distance correlations:")
    print(f"  Range: {min(overall_corrs):.3f} to {max(overall_corrs):.3f}")
    print(f"  Mean: {np.mean(overall_corrs):.3f} ± {np.std(overall_corrs):.3f}")
    
    # Stability statistics
    if len(scatter_df) > 0:
        print(f"\nStability across variants:")
        print(f"  Range: {scatter_df['stability'].min():.3f} to {scatter_df['stability'].max():.3f}")
        print(f"  Mean: {scatter_df['stability'].mean():.3f} ± {scatter_df['stability'].std():.3f}")
        
        # Correlation between overall correlation and stability
        stability_corr = scatter_df['overall_correlation'].corr(scatter_df['stability'])
        print(f"\nCorrelation between overall correlation and stability: r = {stability_corr:.3f}")
    
    # Dyad-specific results (matching the text)
    print("\nSpecific dyad results (matching thesis text):")
    dyad_results = [
        ("SAKl2Kyg_jFYQhuSp", "Dyad 0"),
        ("kDp3Cy37_wocE408P", "Dyad 4"), 
        ("j7hHkgiC_iN9X5S4Q", "Dyad 7")
    ]
    
    for dyad_id, display_name in dyad_results:
        if dyad_id in dyad_overall_corr:
            corr_data = dyad_overall_corr[dyad_id]
            print(f"  {display_name}: r = {corr_data['corr']:.3f}, p = {corr_data['p']:.3f}")

def main():
    """Main function"""
    
    # Load data
    print("Loading study data...")
    df = load_and_prepare_data()
    
    # Calculate patterns
    print("Calculating dyad stability patterns...")
    stability_df, dyad_overall_corr = calculate_dyad_variant_patterns(df)
    
    # Create focused visualization
    print("Creating focused stability visualization...")
    scatter_df = create_focused_stability_visualization(stability_df, dyad_overall_corr)
    
    # Print summary
    print_focused_summary(stability_df, scatter_df, dyad_overall_corr)
    
    print(f"\nFocused visualization saved to: ../assets/06/dyad_movement_correlation_stability_focused.pdf")

if __name__ == "__main__":
    main() 