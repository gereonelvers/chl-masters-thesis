#!/usr/bin/env python3
"""
Improved Communication Patterns Analysis
Fixed statistical approach and clearer calculation methods
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
from pathlib import Path
import warnings
import re
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

def perform_friedman_tests(df):
    """Perform Friedman tests for repeated measures design"""
    
    # Filter to variants with communication data
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    # Group sessions by dyads (every 4 sessions = 1 dyad)
    df_filtered = df_filtered.copy()
    df_filtered['dyad_id'] = df_filtered['session_id'] // 4
    
    # Prepare data for Friedman tests
    # We need to organize data by dyads with complete sets of variants
    dyad_data = {}
    for dyad_id in df_filtered['dyad_id'].unique():
        dyad_sessions = df_filtered[df_filtered['dyad_id'] == dyad_id]
        dyad_variants = set(dyad_sessions['variant'].values)
        
        # Only include dyads that have all three communication variants
        if all(variant in dyad_variants for variant in variants):
            dyad_data[dyad_id] = dyad_sessions.set_index('variant')
    
    print(f"Found {len(dyad_data)} dyads with complete communication data across all variants")
    
    # Statistical tests
    results = {}
    
    metrics = ['total_words', 'words_per_minute', 'turns_per_minute', 
              'communication_balance', 'deictic_density', 'spatial_density',
              'planning_ratio', 'turn_transitions']
    
    for metric in metrics:
        # Organize data for Friedman test
        variant_data = {variant: [] for variant in variants}
        
        for dyad_id, dyad_sessions in dyad_data.items():
            for variant in variants:
                if variant in dyad_sessions.index:
                    variant_data[variant].append(dyad_sessions.loc[variant, metric])
        
        # Check if we have complete data
        if all(len(variant_data[variant]) == len(dyad_data) for variant in variants):
            try:
                # Friedman test
                stat, p_value = stats.friedmanchisquare(*[variant_data[variant] for variant in variants])
                
                # Post-hoc Wilcoxon signed-rank tests if significant
                posthoc_results = {}
                if p_value < 0.05:
                    from itertools import combinations
                    for v1, v2 in combinations(variants, 2):
                        w_stat, w_p = stats.wilcoxon(variant_data[v1], variant_data[v2])
                        posthoc_results[f"{v1} vs {v2}"] = {'W': w_stat, 'p': w_p}
                
                results[metric] = {
                    'test': 'Friedman',
                    'statistic': stat,
                    'p_value': p_value,
                    'significant': p_value < 0.05,
                    'posthoc': posthoc_results
                }
            except Exception as e:
                print(f"Error in Friedman test for {metric}: {e}")
                results[metric] = {
                    'test': 'Friedman',
                    'statistic': None,
                    'p_value': None,
                    'significant': False,
                    'posthoc': {}
                }
        else:
            print(f"Incomplete data for {metric}")
            results[metric] = {
                'test': 'Friedman',
                'statistic': None,
                'p_value': None,
                'significant': False,
                'posthoc': {}
            }
    
    return results

def explain_calculations():
    """Explain how the communication metrics are calculated"""
    
    explanations = {
        'turn_taking': """
Turn-Taking Frequency Calculation:
1. Extract all participant utterances (excluding HOST statements)
2. Count total number of utterances = total_turns
3. Calculate session duration in minutes = total_duration / 60
4. Turn frequency = total_turns / duration_minutes

Turn Transitions:
1. Compare consecutive utterances
2. Count when speaker changes from one utterance to the next
3. This measures how often participants alternate speaking
        """,
        
        'deictic_references': """
Deictic Reference Calculation:
1. Define spatial/deictic patterns using regex:
   - Basic deixis: "here", "there", "this", "that"
   - Directional: "left", "right", "up", "down", "over", "under"
   
2. For each utterance:
   - Convert text to lowercase
   - Count matches for each pattern
   - Sum all deictic references
   
3. Calculate density:
   - Total deictic references / total words in session
   - This gives proportion of words that are spatial references
        """,
        
        'communication_balance': """
Communication Balance Calculation:
1. Count words spoken by each participant
2. Calculate absolute difference: |words_p1 - words_p2|
3. Normalize by total words: difference / total_words
4. Result ranges from 0 (perfect balance) to 1 (one person speaks all words)
        """,
        
        'planning_execution': """
Planning vs Execution Communication:
1. Define planning patterns:
   - Strategy words: "plan", "strategy", "should", "let's", "how about"
   - Sequence words: "first", "then", "next", "after", "before"
   - Ideation words: "idea", "think", "suggest", "propose"

2. Define execution patterns:
   - Action words: "put", "place", "move", "grab", "take", "drop"
   - Immediate words: "there", "here", "now", "wait", "good", "done"
   - Confirmation words: "yes", "no", "ok", "okay", "right", "wrong"

3. Count occurrences in each utterance
4. Calculate ratios: planning_count / (planning_count + execution_count)
        """
    }
    
    print("=== CALCULATION METHODS EXPLAINED ===")
    for calc_type, explanation in explanations.items():
        print(f"\n{calc_type.upper().replace('_', ' ')}:")
        print(explanation)

def create_corrected_statistical_tables(df):
    """Create statistical tables with Friedman tests"""
    
    # Perform Friedman tests
    test_results = perform_friedman_tests(df)
    
    # Create descriptive statistics
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
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
    
    # Descriptive statistics
    desc_stats = df_filtered.groupby('variant')[list(metrics.keys())].agg(['count', 'mean', 'std']).round(3)
    
    # Create LaTeX table for Friedman test results
    latex_table = []
    latex_table.append("\\begin{table}[htbp]")
    latex_table.append("\\centering")
    latex_table.append("\\caption{Friedman Test Results for Communication Patterns by Collaboration Variant}")
    latex_table.append("\\label{tab:communication_friedman_tests}")
    latex_table.append("\\begin{tabular}{lrrrl}")
    latex_table.append("\\toprule")
    latex_table.append("\\textbf{Metric} & \\textbf{$\\chi^2$} & \\textbf{df} & \\textbf{p-value} & \\textbf{Significance} \\\\")
    latex_table.append("\\midrule")
    
    for metric, label in metrics.items():
        result = test_results.get(metric, {})
        if result.get('statistic') is not None:
            chi2 = result['statistic']
            p_val = result['p_value']
            sig = "***" if p_val < 0.001 else "**" if p_val < 0.01 else "*" if p_val < 0.05 else "ns"
            latex_table.append(f"{label} & {chi2:.3f} & 2 & {p_val:.3f} & {sig} \\\\")
        else:
            latex_table.append(f"{label} & -- & -- & -- & -- \\\\")
    
    latex_table.append("\\bottomrule")
    latex_table.append("\\end{tabular}")
    latex_table.append("\\end{table}")
    
    # Save tables
    with open('../assets/06/communication_friedman_table.tex', 'w') as f:
        f.write('\n'.join(latex_table))
    
    return desc_stats, test_results

def create_summary_visualization(df):
    """Create a focused summary visualization"""
    
    variants = ['Open Ended', 'Roleplay', 'Timed']
    df_filtered = df[df['variant'].isin(variants)]
    
    # Create summary figure
    fig, axes = plt.subplots(2, 2, figsize=(12, 10))
    
    # 1. Communication volume
    sns.boxplot(data=df_filtered, x='variant', y='total_words', order=variants, ax=axes[0,0])
    axes[0,0].set_title('Communication Volume by Variant')
    axes[0,0].set_xlabel('Collaboration Variant')
    axes[0,0].set_ylabel('Total Words')
    axes[0,0].tick_params(axis='x', rotation=45)
    
    # 2. Turn-taking frequency  
    sns.boxplot(data=df_filtered, x='variant', y='turns_per_minute', order=variants, ax=axes[0,1])
    axes[0,1].set_title('Turn-Taking Frequency')
    axes[0,1].set_xlabel('Collaboration Variant')
    axes[0,1].set_ylabel('Turns per Minute')
    axes[0,1].tick_params(axis='x', rotation=45)
    
    # 3. Deictic reference density
    sns.boxplot(data=df_filtered, x='variant', y='deictic_density', order=variants, ax=axes[1,0])
    axes[1,0].set_title('Spatial Reference Density')
    axes[1,0].set_xlabel('Collaboration Variant')
    axes[1,0].set_ylabel('Deictic References per Word')
    axes[1,0].tick_params(axis='x', rotation=45)
    
    # 4. Communication balance
    sns.boxplot(data=df_filtered, x='variant', y='communication_balance', order=variants, ax=axes[1,1])
    axes[1,1].set_title('Communication Balance')
    axes[1,1].set_xlabel('Collaboration Variant')
    axes[1,1].set_ylabel('Balance Score (0 = perfect balance)')
    axes[1,1].tick_params(axis='x', rotation=45)
    
    plt.tight_layout()
    plt.savefig('../assets/06/communication_patterns_corrected.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    print("Corrected visualization saved to: ../assets/06/communication_patterns_corrected.pdf")

def main():
    """Main analysis function with corrected statistics"""
    print("Starting corrected communication analysis...")
    
    # Explain calculation methods
    explain_calculations()
    
    # Load data
    df = load_data()
    
    # Create corrected statistical analysis
    desc_stats, test_results = create_corrected_statistical_tables(df)
    
    # Create summary visualization
    create_summary_visualization(df)
    
    # Print results
    print("\n=== CORRECTED STATISTICAL RESULTS (FRIEDMAN TESTS) ===")
    for metric, result in test_results.items():
        if result.get('p_value') is not None:
            significance = "***" if result['p_value'] < 0.001 else "**" if result['p_value'] < 0.01 else "*" if result['p_value'] < 0.05 else "ns"
            print(f"{metric}: χ² = {result['statistic']:.3f}, p = {result['p_value']:.3f} {significance}")
            
            # Print post-hoc results if significant
            if result['posthoc']:
                print(f"  Post-hoc tests:")
                for comparison, posthoc in result['posthoc'].items():
                    w_sig = "***" if posthoc['p'] < 0.001 else "**" if posthoc['p'] < 0.01 else "*" if posthoc['p'] < 0.05 else "ns"
                    print(f"    {comparison}: W = {posthoc['W']:.1f}, p = {posthoc['p']:.3f} {w_sig}")
        else:
            print(f"{metric}: Unable to compute (insufficient data)")
    
    print("\nFiles created:")
    print("- ../assets/06/communication_friedman_table.tex")
    print("- ../assets/06/communication_patterns_corrected.pdf")

if __name__ == "__main__":
    main() 