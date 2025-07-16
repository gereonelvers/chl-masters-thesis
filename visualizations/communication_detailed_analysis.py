#!/usr/bin/env python3
"""
Detailed Communication Patterns Analysis
Focused analysis for the Communication Patterns subsection
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
from pathlib import Path
import warnings
warnings.filterwarnings('ignore')

# Configure plotting
plt.rcParams.update({
    'font.size': 10,
    'axes.titlesize': 12,
    'axes.labelsize': 10,
    'xtick.labelsize': 9,
    'ytick.labelsize': 9,
    'legend.fontsize': 9,
    'figure.titlesize': 14
})

def load_data():
    """Load the communication analysis results"""
    df = pd.read_csv('../user-study-analysis/communication_analysis_results.csv')
    study_df = pd.read_csv('../user-study-analysis/study-run-results.csv')
    
    # Parse completion times
    def parse_time(time_str):
        if pd.isna(time_str):
            return None
        try:
            if ':' in str(time_str):
                parts = str(time_str).split(':')
                return float(parts[0]) + float(parts[1]) / 60
            else:
                return float(time_str)
        except:
            return None
    
    study_df['completion_minutes'] = study_df['Completion time (exact, minutes)'].apply(parse_time)
    
    # Merge with communication data
    merged_df = df.merge(study_df[['Run #', 'completion_minutes']], 
                        left_on='session_id', right_on='Run #', how='left')
    
    return merged_df

def perform_statistical_tests(df):
    """Perform comprehensive statistical tests on communication metrics"""
    
    # Filter to variants with communication data
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    # Group sessions by dyads (every 4 sessions = 1 dyad)
    df_filtered = df_filtered.copy()
    df_filtered['dyad_id'] = df_filtered['session_id'] // 4
    
    # Statistical tests
    results = {}
    
    metrics = ['total_words', 'words_per_minute', 'turns_per_minute', 
              'communication_balance', 'deictic_density', 'spatial_density',
              'planning_ratio', 'turn_transitions']
    
    for metric in metrics:
        # ANOVA test between variants
        variant_groups = [df_filtered[df_filtered['variant'] == v][metric].values 
                         for v in variants]
        
        # Remove any empty groups
        variant_groups = [group for group in variant_groups if len(group) > 0]
        
        if len(variant_groups) >= 2:
            try:
                f_stat, p_value = stats.f_oneway(*variant_groups)
                results[metric] = {
                    'test': 'One-way ANOVA',
                    'statistic': f_stat,
                    'p_value': p_value,
                    'significant': p_value < 0.05
                }
            except:
                results[metric] = {
                    'test': 'One-way ANOVA',
                    'statistic': None,
                    'p_value': None,
                    'significant': False
                }
    
    return results

def create_communication_tables(df):
    """Create detailed tables for communication metrics"""
    
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    # Descriptive statistics table
    metrics = {
        'total_words': 'Total Words',
        'words_per_minute': 'Words per Minute',
        'turns_per_minute': 'Turns per Minute',
        'communication_balance': 'Communication Balance',
        'deictic_density': 'Deictic Density',
        'spatial_density': 'Spatial Density',
        'planning_ratio': 'Planning Ratio',
        'turn_transitions': 'Turn Transitions'
    }
    
    desc_stats = df_filtered.groupby('variant')[list(metrics.keys())].agg(['count', 'mean', 'std']).round(3)
    
    # Create summary table for LaTeX
    latex_table = []
    latex_table.append("\\begin{table}[htbp]")
    latex_table.append("\\centering")
    latex_table.append("\\caption{Communication Patterns Descriptive Statistics by Collaboration Variant}")
    latex_table.append("\\label{tab:communication_descriptive_stats}")
    latex_table.append("\\begin{tabular}{lrrrrrrr}")
    latex_table.append("\\toprule")
    latex_table.append("\\textbf{Metric} & \\multicolumn{2}{c}{\\textbf{Open Ended}} & \\multicolumn{2}{c}{\\textbf{Roleplay}} & \\multicolumn{2}{c}{\\textbf{Timed}} \\\\")
    latex_table.append("& \\textbf{Mean} & \\textbf{SD} & \\textbf{Mean} & \\textbf{SD} & \\textbf{Mean} & \\textbf{SD} \\\\")
    latex_table.append("\\midrule")
    
    for metric, label in metrics.items():
        try:
            oe_mean = desc_stats.loc['Open Ended', (metric, 'mean')]
            oe_std = desc_stats.loc['Open Ended', (metric, 'std')]
            rp_mean = desc_stats.loc['Roleplay', (metric, 'mean')]
            rp_std = desc_stats.loc['Roleplay', (metric, 'std')]
            tm_mean = desc_stats.loc['Timed', (metric, 'mean')]
            tm_std = desc_stats.loc['Timed', (metric, 'std')]
            
            latex_table.append(f"{label} & {oe_mean:.2f} & {oe_std:.2f} & {rp_mean:.2f} & {rp_std:.2f} & {tm_mean:.2f} & {tm_std:.2f} \\\\")
        except:
            latex_table.append(f"{label} & -- & -- & -- & -- & -- & -- \\\\")
    
    latex_table.append("\\bottomrule")
    latex_table.append("\\end{tabular}")
    latex_table.append("\\end{table}")
    
    # Save to file
    with open('../assets/06/communication_descriptive_table.tex', 'w') as f:
        f.write('\n'.join(latex_table))
    
    return desc_stats

def create_focused_visualizations(df):
    """Create focused visualizations for key communication metrics"""
    
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    # Create a comprehensive communication analysis figure
    fig, axes = plt.subplots(2, 3, figsize=(18, 12))
    
    # 1. Total word count by variant
    sns.boxplot(data=df_filtered, x='variant', y='total_words', order=variants, ax=axes[0,0])
    axes[0,0].set_title('Total Word Count by Variant')
    axes[0,0].set_xlabel('Collaboration Variant')
    axes[0,0].set_ylabel('Total Words')
    axes[0,0].tick_params(axis='x', rotation=45)
    
    # Add sample sizes
    for i, variant in enumerate(variants):
        n = len(df_filtered[df_filtered['variant'] == variant])
        axes[0,0].text(i, axes[0,0].get_ylim()[1] * 0.9, f'n={n}', ha='center')
    
    # 2. Communication density (words per minute)
    sns.boxplot(data=df_filtered, x='variant', y='words_per_minute', order=variants, ax=axes[0,1])
    axes[0,1].set_title('Communication Density')
    axes[0,1].set_xlabel('Collaboration Variant')
    axes[0,1].set_ylabel('Words per Minute')
    axes[0,1].tick_params(axis='x', rotation=45)
    
    # 3. Turn-taking frequency
    sns.boxplot(data=df_filtered, x='variant', y='turns_per_minute', order=variants, ax=axes[0,2])
    axes[0,2].set_title('Turn-Taking Frequency')
    axes[0,2].set_xlabel('Collaboration Variant')
    axes[0,2].set_ylabel('Turns per Minute')
    axes[0,2].tick_params(axis='x', rotation=45)
    
    # 4. Communication balance
    sns.boxplot(data=df_filtered, x='variant', y='communication_balance', order=variants, ax=axes[1,0])
    axes[1,0].set_title('Communication Balance')
    axes[1,0].set_xlabel('Collaboration Variant')
    axes[1,0].set_ylabel('Communication Imbalance\n(0 = perfect balance)')
    axes[1,0].tick_params(axis='x', rotation=45)
    
    # 5. Deictic reference density
    sns.boxplot(data=df_filtered, x='variant', y='deictic_density', order=variants, ax=axes[1,1])
    axes[1,1].set_title('Deictic Reference Density')
    axes[1,1].set_xlabel('Collaboration Variant')
    axes[1,1].set_ylabel('Deictic References per Word')
    axes[1,1].tick_params(axis='x', rotation=45)
    
    # 6. Planning vs execution communication
    df_filtered['execution_ratio'] = 1 - df_filtered['planning_ratio']
    planning_means = df_filtered.groupby('variant')['planning_ratio'].mean()
    execution_means = df_filtered.groupby('variant')['execution_ratio'].mean()
    
    x = np.arange(len(variants))
    width = 0.6
    
    p1 = axes[1,2].bar(x, [planning_means[v] for v in variants], width, label='Planning', alpha=0.8)
    p2 = axes[1,2].bar(x, [execution_means[v] for v in variants], width, 
                      bottom=[planning_means[v] for v in variants], label='Execution', alpha=0.8)
    
    axes[1,2].set_title('Communication Type Distribution')
    axes[1,2].set_xlabel('Collaboration Variant')
    axes[1,2].set_ylabel('Proportion of Communication')
    axes[1,2].set_xticks(x)
    axes[1,2].set_xticklabels(variants, rotation=45)
    axes[1,2].legend()
    
    plt.tight_layout()
    plt.savefig('../assets/06/communication_patterns_focused.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    # Create a separate figure for communication vs performance relationship
    fig, axes = plt.subplots(1, 2, figsize=(12, 5))
    
    # Communication volume vs completion time
    df_clean = df_filtered.dropna(subset=['completion_minutes'])
    
    axes[0].scatter(df_clean['completion_minutes'], df_clean['total_words'], 
                   c=[{'Open Ended': 'blue', 'Roleplay': 'red', 'Timed': 'green'}[v] 
                      for v in df_clean['variant']], alpha=0.7)
    axes[0].set_xlabel('Task Completion Time (minutes)')
    axes[0].set_ylabel('Total Words Spoken')
    axes[0].set_title('Communication Volume vs Task Duration')
    
    # Add correlation
    if len(df_clean) > 2:
        r, p = stats.pearsonr(df_clean['completion_minutes'], df_clean['total_words'])
        axes[0].text(0.05, 0.95, f'r = {r:.3f}, p = {p:.3f}', transform=axes[0].transAxes)
    
    # Add legend
    handles = [plt.Line2D([0], [0], marker='o', color='w', markerfacecolor=c, markersize=8) 
               for c in ['blue', 'red', 'green']]
    axes[0].legend(handles, variants, loc='upper right')
    
    # Communication efficiency (words per completion minute)
    df_clean['communication_efficiency'] = df_clean['total_words'] / df_clean['completion_minutes']
    
    sns.boxplot(data=df_clean, x='variant', y='communication_efficiency', order=variants, ax=axes[1])
    axes[1].set_title('Communication Efficiency by Variant')
    axes[1].set_xlabel('Collaboration Variant')
    axes[1].set_ylabel('Words per Completion Minute')
    axes[1].tick_params(axis='x', rotation=45)
    
    plt.tight_layout()
    plt.savefig('../assets/06/communication_performance_relationship.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    print("Focused visualizations saved:")
    print("- ../assets/06/communication_patterns_focused.pdf")
    print("- ../assets/06/communication_performance_relationship.pdf")

def analyze_communication_adaptations(df):
    """Analyze specific communication adaptations by variant"""
    
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    print("\n=== COMMUNICATION ADAPTATION ANALYSIS ===")
    
    # 1. Communication volume analysis
    print("\n1. Communication Volume by Variant:")
    volume_stats = df_filtered.groupby('variant')['total_words'].agg(['count', 'mean', 'std', 'min', 'max'])
    print(volume_stats.round(2))
    
    # 2. Communication efficiency analysis
    print("\n2. Communication Density (words per minute):")
    density_stats = df_filtered.groupby('variant')['words_per_minute'].agg(['count', 'mean', 'std'])
    print(density_stats.round(2))
    
    # 3. Turn-taking patterns
    print("\n3. Turn-Taking Patterns:")
    turn_stats = df_filtered.groupby('variant')[['turns_per_minute', 'turn_transitions']].agg(['mean', 'std'])
    print(turn_stats.round(2))
    
    # 4. Communication balance
    print("\n4. Communication Balance (0 = perfect balance):")
    balance_stats = df_filtered.groupby('variant')['communication_balance'].agg(['mean', 'std'])
    print(balance_stats.round(3))
    
    # 5. Spatial and deictic references
    print("\n5. Spatial Communication Patterns:")
    spatial_stats = df_filtered.groupby('variant')[['deictic_density', 'spatial_density']].agg(['mean', 'std'])
    print(spatial_stats.round(4))
    
    # 6. Planning vs execution communication
    print("\n6. Planning vs Execution Communication:")
    planning_stats = df_filtered.groupby('variant')['planning_ratio'].agg(['mean', 'std'])
    print(planning_stats.round(3))
    
    return {
        'volume': volume_stats,
        'density': density_stats,
        'turns': turn_stats,
        'balance': balance_stats,
        'spatial': spatial_stats,
        'planning': planning_stats
    }

def main():
    """Main analysis function"""
    print("Starting detailed communication analysis...")
    
    # Load data
    df = load_data()
    
    # Perform statistical tests
    test_results = perform_statistical_tests(df)
    
    # Create tables
    desc_stats = create_communication_tables(df)
    
    # Create visualizations
    create_focused_visualizations(df)
    
    # Analyze communication adaptations
    adaptation_analysis = analyze_communication_adaptations(df)
    
    # Print statistical test results
    print("\n=== STATISTICAL TEST RESULTS ===")
    for metric, result in test_results.items():
        if result['p_value'] is not None:
            sig = "***" if result['p_value'] < 0.001 else "**" if result['p_value'] < 0.01 else "*" if result['p_value'] < 0.05 else "ns"
            print(f"{metric}: F = {result['statistic']:.3f}, p = {result['p_value']:.3f} {sig}")
        else:
            print(f"{metric}: Unable to compute")
    
    print("\nFiles created:")
    print("- ../assets/06/communication_descriptive_table.tex")
    print("- ../assets/06/communication_patterns_focused.pdf")
    print("- ../assets/06/communication_performance_relationship.pdf")

if __name__ == "__main__":
    main() 