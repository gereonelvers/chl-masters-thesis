#!/usr/bin/env python3

import pandas as pd
import numpy as np
from scipy import stats

def parse_nasa_tlx_score(value_str):
    """Parse NASA-TLX scores with custom format"""
    value_str = str(value_str).replace('"', '').strip()
    
    # Handle different formats
    if ',' in value_str and len(value_str.split(',')) > 2:
        # Format like '3,916,666,667' - treat commas as decimal separators
        # This appears to be a very long decimal number
        parts = value_str.split(',')
        if len(parts) >= 3:
            # Convert to decimal format: first part + decimal point + remaining parts joined
            decimal_value = parts[0] + '.' + ''.join(parts[1:])
            try:
                return float(decimal_value)
            except:
                pass
    
    # Handle simple cases (regular numbers, decimals)
    try:
        return float(value_str)
    except:
        return None

def load_and_process_data():
    """Load and process questionnaire data"""
    df = pd.read_csv('participant-results.csv')
    
    # Clean NASA-TLX scores
    nasa_tlx_cols = ['RTL X Score 1', 'RTL X Score 2', 'RTL X Score 3', 'RTL X Score 4']
    for col in nasa_tlx_cols:
        df[col] = df[col].apply(parse_nasa_tlx_score)
    
    # SUS scores are already clean
    sus_cols = ['SUS Score 1', 'SUS Score 2', 'SUS Score 3', 'SUS Score 4']
    
    return df, nasa_tlx_cols, sus_cols

def create_nasa_tlx_table(df, nasa_tlx_cols):
    """Create NASA-TLX summary table"""
    
    # Reshape data
    nasa_data = []
    for idx, row in df.iterrows():
        participant_id = row['Participant ID']
        for session_num, col in enumerate(nasa_tlx_cols, 1):
            if pd.notna(row[col]):
                nasa_data.append({
                    'Participant': participant_id,
                    'Session': session_num,
                    'NASA_TLX': row[col]
                })
    
    nasa_df = pd.DataFrame(nasa_data)
    
    # Calculate summary statistics
    summary = nasa_df.groupby('Session')['NASA_TLX'].agg(['count', 'mean', 'std', 'min', 'max'])
    summary['se'] = summary['std'] / np.sqrt(summary['count'])
    summary['ci_lower'] = summary['mean'] - 1.96 * summary['se']
    summary['ci_upper'] = summary['mean'] + 1.96 * summary['se']
    
    # Create LaTeX table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{NASA-TLX Workload Scores by Session}
\\label{tab:nasa_tlx_by_session}
\\begin{tabular}{lrrrrrr}
\\toprule
\\textbf{Session} & \\textbf{N} & \\textbf{Mean} & \\textbf{SD} & \\textbf{Min} & \\textbf{Max} & \\textbf{95\\% CI} \\\\
\\midrule
"""
    
    for session in summary.index:
        row = summary.loc[session]
        ci_str = f"[{row['ci_lower']:.1f}, {row['ci_upper']:.1f}]"
        latex_table += f"{session} & {int(row['count'])} & {row['mean']:.1f} & {row['std']:.1f} & {row['min']:.1f} & {row['max']:.1f} & {ci_str} \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}
"""
    
    return latex_table, summary

def create_sus_table(df, sus_cols):
    """Create SUS summary table"""
    
    # Reshape data
    sus_data = []
    for idx, row in df.iterrows():
        participant_id = row['Participant ID']
        for session_num, col in enumerate(sus_cols, 1):
            if pd.notna(row[col]):
                sus_data.append({
                    'Participant': participant_id,
                    'Session': session_num,
                    'SUS': row[col]
                })
    
    sus_df = pd.DataFrame(sus_data)
    
    # Calculate summary statistics
    summary = sus_df.groupby('Session')['SUS'].agg(['count', 'mean', 'std', 'min', 'max'])
    summary['se'] = summary['std'] / np.sqrt(summary['count'])
    summary['ci_lower'] = summary['mean'] - 1.96 * summary['se']
    summary['ci_upper'] = summary['mean'] + 1.96 * summary['se']
    
    # Create LaTeX table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{System Usability Scale (SUS) Scores by Session}
\\label{tab:sus_by_session}
\\begin{tabular}{lrrrrrr}
\\toprule
\\textbf{Session} & \\textbf{N} & \\textbf{Mean} & \\textbf{SD} & \\textbf{Min} & \\textbf{Max} & \\textbf{95\\% CI} \\\\
\\midrule
"""
    
    for session in summary.index:
        row = summary.loc[session]
        ci_str = f"[{row['ci_lower']:.1f}, {row['ci_upper']:.1f}]"
        latex_table += f"{session} & {int(row['count'])} & {row['mean']:.1f} & {row['std']:.1f} & {row['min']:.1f} & {row['max']:.1f} & {ci_str} \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}
"""
    
    return latex_table, summary

def create_learning_effects_table(df, nasa_tlx_cols, sus_cols):
    """Create learning effects statistical test table"""
    
    # NASA-TLX learning effect
    nasa_first = []
    nasa_last = []
    
    for idx, row in df.iterrows():
        if pd.notna(row[nasa_tlx_cols[0]]) and pd.notna(row[nasa_tlx_cols[3]]):
            nasa_first.append(row[nasa_tlx_cols[0]])
            nasa_last.append(row[nasa_tlx_cols[3]])
    
    nasa_ttest = stats.ttest_rel(nasa_last, nasa_first)
    nasa_change = np.mean(nasa_last) - np.mean(nasa_first)
    
    # SUS learning effect
    sus_first = []
    sus_last = []
    
    for idx, row in df.iterrows():
        if pd.notna(row[sus_cols[0]]) and pd.notna(row[sus_cols[3]]):
            sus_first.append(row[sus_cols[0]])
            sus_last.append(row[sus_cols[3]])
    
    sus_ttest = stats.ttest_rel(sus_last, sus_first)
    sus_change = np.mean(sus_last) - np.mean(sus_first)
    
    # Create LaTeX table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{Learning Effects Analysis: Session 1 vs Session 4}
\\label{tab:learning_effects}
\\begin{tabular}{lrrrl}
\\toprule
\\textbf{Measure} & \\textbf{Mean Change} & \\textbf{t-statistic} & \\textbf{p-value} & \\textbf{Significance} \\\\
\\midrule
"""
    
    # NASA-TLX row
    nasa_sig = "**" if nasa_ttest.pvalue < 0.01 else "*" if nasa_ttest.pvalue < 0.05 else "ns"
    latex_table += f"NASA-TLX & {nasa_change:.2f} & {nasa_ttest.statistic:.3f} & {nasa_ttest.pvalue:.3f} & {nasa_sig} \\\\\n"
    
    # SUS row
    sus_sig = "**" if sus_ttest.pvalue < 0.01 else "*" if sus_ttest.pvalue < 0.05 else "ns"
    latex_table += f"SUS & {sus_change:.2f} & {sus_ttest.statistic:.3f} & {sus_ttest.pvalue:.3f} & {sus_sig} \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}

\\textbf{Note:} * p < 0.05, ** p < 0.01, ns = not significant
"""
    
    return latex_table

def analyze_relationship_measures(df):
    """Analyze relationship and trust changes"""
    
    # Extract IOS scores
    pre_ios = []
    post_ios = []
    
    for idx, row in df.iterrows():
        # Extract IOS scores from option strings
        pre_option = row['Introductory Survey - Which picture best describes your relationship with the other participant?']
        post_option = row['Post-Task Survey - Which picture best describes your relationship with the other participant? (1-7, 1 least close, 7 closest)']
        
        # Convert option to numeric
        if 'Option' in str(pre_option):
            pre_score = int(pre_option.split(' ')[1])
        else:
            pre_score = None
            
        if 'Option' in str(post_option):
            post_score = int(post_option.split(' ')[1])
        else:
            try:
                post_score = int(post_option)
            except:
                post_score = None
        
        if pre_score is not None and post_score is not None:
            pre_ios.append(pre_score)
            post_ios.append(post_score)
    
    # IOS change analysis
    ios_changes = [post - pre for pre, post in zip(pre_ios, post_ios)]
    ios_ttest = stats.ttest_1samp(ios_changes, 0)
    
    # Create relationship measures table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{Relationship Measures Analysis}
\\label{tab:relationship_measures}
\\begin{tabular}{lrrrl}
\\toprule
\\textbf{Measure} & \\textbf{Pre-Study Mean} & \\textbf{Post-Study Mean} & \\textbf{Change} & \\textbf{Significance} \\\\
\\midrule
"""
    
    ios_sig = "**" if ios_ttest.pvalue < 0.01 else "*" if ios_ttest.pvalue < 0.05 else "ns"
    latex_table += f"IOS Closeness & {np.mean(pre_ios):.2f} & {np.mean(post_ios):.2f} & {np.mean(ios_changes):.2f} & {ios_sig} \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}
"""
    
    return latex_table

def main():
    print("Loading data...")
    df, nasa_tlx_cols, sus_cols = load_and_process_data()
    
    print("Creating NASA-TLX table...")
    nasa_table, nasa_summary = create_nasa_tlx_table(df, nasa_tlx_cols)
    
    print("Creating SUS table...")
    sus_table, sus_summary = create_sus_table(df, sus_cols)
    
    print("Creating learning effects table...")
    learning_table = create_learning_effects_table(df, nasa_tlx_cols, sus_cols)
    
    print("Creating relationship measures table...")
    relationship_table = analyze_relationship_measures(df)
    
    # Write tables to file
    with open('subjective_experience_tables.tex', 'w') as f:
        f.write("% NASA-TLX Table\n")
        f.write(nasa_table)
        f.write("\n% SUS Table\n")
        f.write(sus_table)
        f.write("\n% Learning Effects Table\n")
        f.write(learning_table)
        f.write("\n% Relationship Measures Table\n")
        f.write(relationship_table)
    
    print("Tables saved to subjective_experience_tables.tex")
    
    # Print summary statistics
    print("\n=== NASA-TLX SUMMARY ===")
    print(nasa_summary)
    print("\n=== SUS SUMMARY ===")
    print(sus_summary)

if __name__ == "__main__":
    main() 