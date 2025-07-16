#!/usr/bin/env python3
"""
Visualization showing how individual dyads maintain consistent movement patterns across variants.
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy.stats import pearsonr
import warnings
warnings.filterwarnings('ignore')

def load_and_prepare_data():
    """Load and prepare the data"""
    df = pd.read_csv('study-run-results.csv')
    df = df.rename(columns={
        'Participant 1 ID': 'participant_1_id',
        'Participant 2 ID': 'participant_2_id', 
        'Variant': 'collaboration_variant',
        'Participant 1 distance (meter)': 'p1_movement_distance',
        'Participant 2 distance (meter)': 'p2_movement_distance'
    })
    df['participant_id'] = df['participant_1_id'] + '_' + df['participant_2_id']
    
    # Convert to numeric
    df['p1_movement_distance'] = pd.to_numeric(df['p1_movement_distance'], errors='coerce')
    df['p2_movement_distance'] = pd.to_numeric(df['p2_movement_distance'], errors='coerce')
    
    return df

def calculate_dyad_patterns(df):
    """Calculate movement patterns for each dyad across variants"""
    dyad_patterns = {}
    
    for dyad_id in df['participant_id'].unique():
        dyad_data = df[df['participant_id'] == dyad_id]
        
        patterns = {}
        for variant in dyad_data['collaboration_variant'].unique():
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            
            p1_distances = variant_data['p1_movement_distance'].values
            p2_distances = variant_data['p2_movement_distance'].values
            
            patterns[variant] = {
                'p1_mean': np.mean(p1_distances),
                'p2_mean': np.mean(p2_distances),
                'asymmetry': np.mean(abs(p1_distances - p2_distances) / (p1_distances + p2_distances)),
                'sessions': len(variant_data)
            }
        
        # Calculate overall correlation for this dyad
        if len(dyad_data) > 1:
            p1_all = dyad_data['p1_movement_distance'].values
            p2_all = dyad_data['p2_movement_distance'].values
            if np.var(p1_all) > 0 and np.var(p2_all) > 0:
                overall_corr, _ = pearsonr(p1_all, p2_all)
            else:
                overall_corr = 0
        else:
            overall_corr = 0
            
        dyad_patterns[dyad_id] = {
            'patterns': patterns,
            'overall_correlation': overall_corr,
            'n_variants': len(patterns)
        }
    
    return dyad_patterns

def create_consistency_visualization(df, dyad_patterns):
    """Create visualization showing dyad consistency across variants"""
    
    fig, axes = plt.subplots(2, 2, figsize=(15, 12))
    fig.suptitle('Individual Dyad Movement Patterns Across Collaboration Variants', 
                 fontsize=16, fontweight='bold')
    
    # 1. Movement correlation consistency across variants
    ax1 = axes[0, 0]
    
    # Get dyads that participated in all 4 variants
    complete_dyads = [dyad_id for dyad_id, data in dyad_patterns.items() 
                     if data['n_variants'] == 4]
    
    # Create heatmap data
    correlation_matrix = []
    dyad_labels = []
    variants = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    
    for dyad_id in complete_dyads[:6]:  # Show top 6 dyads
        dyad_data = df[df['participant_id'] == dyad_id]
        correlations_by_variant = []
        
        for variant in variants:
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            p1 = variant_data['p1_movement_distance'].values
            p2 = variant_data['p2_movement_distance'].values
            
            # For single session, calculate as ratio
            if len(p1) == 1:
                ratio = min(p1[0], p2[0]) / max(p1[0], p2[0]) if max(p1[0], p2[0]) > 0 else 0
                correlations_by_variant.append(ratio)
            else:
                if np.var(p1) > 0 and np.var(p2) > 0:
                    corr, _ = pearsonr(p1, p2)
                    correlations_by_variant.append(corr)
                else:
                    correlations_by_variant.append(0)
        
        correlation_matrix.append(correlations_by_variant)
        dyad_labels.append(dyad_id[:8] + '...')
    
    if correlation_matrix:
        correlation_matrix = np.array(correlation_matrix)
        im = ax1.imshow(correlation_matrix, cmap='RdYlBu_r', vmin=-1, vmax=1, aspect='auto')
        
        ax1.set_xticks(range(len(variants)))
        ax1.set_xticklabels(variants, rotation=45)
        ax1.set_yticks(range(len(dyad_labels)))
        ax1.set_yticklabels(dyad_labels)
        ax1.set_title('Movement Similarity Across Variants')
        
        # Add text annotations
        for i in range(len(dyad_labels)):
            for j in range(len(variants)):
                text = f'{correlation_matrix[i, j]:.2f}'
                ax1.text(j, i, text, ha="center", va="center", color="black", fontsize=8)
        
        plt.colorbar(im, ax=ax1, label='Movement Similarity')
    
    # 2. Asymmetry patterns across variants
    ax2 = axes[0, 1]
    
    asymmetry_data = []
    asymmetry_labels = []
    
    for variant in variants:
        variant_asymmetries = []
        for dyad_id in complete_dyads:
            patterns = dyad_patterns[dyad_id]['patterns']
            if variant in patterns:
                variant_asymmetries.append(patterns[variant]['asymmetry'])
        
        if variant_asymmetries:
            asymmetry_data.append(variant_asymmetries)
            asymmetry_labels.append(variant)
    
    if asymmetry_data:
        box_plot = ax2.boxplot(asymmetry_data, labels=asymmetry_labels, patch_artist=True)
        colors = ['#1f77b4', '#ff7f0e', '#2ca02c', '#d62728']
        for patch, color in zip(box_plot['boxes'], colors):
            patch.set_facecolor(color)
            patch.set_alpha(0.7)
        
        ax2.set_title('Movement Asymmetry by Variant')
        ax2.set_ylabel('Movement Asymmetry')
        ax2.tick_params(axis='x', rotation=45)
        ax2.grid(axis='y', alpha=0.3)
    
    # 3. Individual dyad trajectories
    ax3 = axes[1, 0]
    
    # Show 3 example dyads with different patterns
    example_dyads = sorted(complete_dyads, 
                          key=lambda x: dyad_patterns[x]['overall_correlation'], 
                          reverse=True)[:3]
    
    colors = ['#1f77b4', '#ff7f0e', '#2ca02c']
    
    for i, dyad_id in enumerate(example_dyads):
        dyad_data = df[df['participant_id'] == dyad_id]
        
        variant_asymmetries = []
        for variant in variants:
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            if len(variant_data) > 0:
                p1 = variant_data['p1_movement_distance'].values[0]
                p2 = variant_data['p2_movement_distance'].values[0]
                asymmetry = abs(p1 - p2) / (p1 + p2) if (p1 + p2) > 0 else 0
                variant_asymmetries.append(asymmetry)
            else:
                variant_asymmetries.append(np.nan)
        
        ax3.plot(range(len(variants)), variant_asymmetries, 'o-', 
                color=colors[i], label=f'{dyad_id[:8]}... (r={dyad_patterns[dyad_id]["overall_correlation"]:.2f})',
                linewidth=2, markersize=6)
    
    ax3.set_xticks(range(len(variants)))
    ax3.set_xticklabels(variants, rotation=45)
    ax3.set_ylabel('Movement Asymmetry')
    ax3.set_title('Individual Dyad Patterns')
    ax3.legend(fontsize=8)
    ax3.grid(alpha=0.3)
    
    # 4. Correlation stability analysis
    ax4 = axes[1, 1]
    
    # Calculate correlation between dyad's overall correlation and variant-specific patterns
    stability_scores = []
    dyad_ids_for_stability = []
    
    for dyad_id in complete_dyads:
        dyad_data = df[df['participant_id'] == dyad_id]
        overall_corr = dyad_patterns[dyad_id]['overall_correlation']
        
        variant_similarities = []
        for variant in variants:
            variant_data = dyad_data[dyad_data['collaboration_variant'] == variant]
            p1 = variant_data['p1_movement_distance'].values[0]
            p2 = variant_data['p2_movement_distance'].values[0]
            similarity = min(p1, p2) / max(p1, p2) if max(p1, p2) > 0 else 0
            variant_similarities.append(similarity)
        
        # Calculate consistency (lower variance = more consistent)
        consistency = 1 - np.var(variant_similarities)
        stability_scores.append(consistency)
        dyad_ids_for_stability.append(dyad_id[:8] + '...')
    
    if stability_scores:
        bars = ax4.bar(range(len(stability_scores)), stability_scores, 
                      color='lightblue', edgecolor='navy')
        ax4.set_xticks(range(len(dyad_ids_for_stability)))
        ax4.set_xticklabels(dyad_ids_for_stability, rotation=45)
        ax4.set_ylabel('Pattern Consistency')
        ax4.set_title('Dyadic Pattern Stability')
        ax4.grid(axis='y', alpha=0.3)
        
        # Add values on bars
        for i, (bar, score) in enumerate(zip(bars, stability_scores)):
            ax4.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 0.01, 
                    f'{score:.2f}', ha='center', va='bottom', fontsize=8)
    
    plt.tight_layout()
    plt.savefig('../assets/06/dyad_consistency_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    return dyad_patterns

def main():
    """Main analysis"""
    df = load_and_prepare_data()
    dyad_patterns = calculate_dyad_patterns(df)
    create_consistency_visualization(df, dyad_patterns)
    
    print("DYAD CONSISTENCY ANALYSIS")
    print("=" * 50)
    
    # Print summary statistics
    complete_dyads = [dyad_id for dyad_id, data in dyad_patterns.items() 
                     if data['n_variants'] == 4]
    
    print(f"Number of dyads with complete data: {len(complete_dyads)}")
    
    print("\nDyad overall correlations:")
    for dyad_id in sorted(complete_dyads, 
                         key=lambda x: dyad_patterns[x]['overall_correlation'], 
                         reverse=True):
        corr = dyad_patterns[dyad_id]['overall_correlation']
        print(f"{dyad_id[:13]}...: r = {corr:.3f}")
    
    print("\nRoleplay variant asymmetry patterns:")
    for dyad_id in complete_dyads:
        patterns = dyad_patterns[dyad_id]['patterns']
        if 'Roleplay' in patterns:
            asymmetry = patterns['Roleplay']['asymmetry']
            print(f"{dyad_id[:13]}...: asymmetry = {asymmetry:.3f}")

if __name__ == "__main__":
    main() 