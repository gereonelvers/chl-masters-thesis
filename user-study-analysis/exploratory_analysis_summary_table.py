#!/usr/bin/env python3
"""
Generate Summary Table for Exploratory Analysis Results
======================================================
"""

import pandas as pd

def generate_summary_table():
    """Generate LaTeX table summarizing key exploratory analysis findings"""
    
    # Key findings from the analysis
    findings_data = [
        {
            'Domain': 'Task Variant Effects',
            'Finding': 'Communication Constraints',
            'Result': 'Silent condition significantly faster (5.00 vs 10.72 min, p=0.025)',
            'Implication': 'Communication overhead may reduce efficiency in co-located collaboration'
        },
        {
            'Domain': 'Task Variant Effects', 
            'Finding': 'Time Pressure',
            'Result': 'Timed variant 3x faster (3.08 vs 8.81 min) with no quality loss',
            'Implication': 'Time constraints enhance focus without compromising outcomes'
        },
        {
            'Domain': 'Task Variant Effects',
            'Finding': 'Split Objectives',
            'Result': 'Roleplay variant 2.6x slower with 2.2x more movement (p=0.031)',
            'Implication': 'Role-based constraints create coordination overhead'
        },
        {
            'Domain': 'Individual Differences',
            'Finding': 'Personality Traits',
            'Result': 'All Big Five traits show weak correlations (|r| < 0.41, all p > 0.1)',
            'Implication': 'Situational factors may dominate over personality traits'
        },
        {
            'Domain': 'Individual Differences',
            'Finding': 'Experience Levels',
            'Result': 'Non-linear relationship; moderate experience shows worst performance',
            'Implication': 'Intermediate experience may create problematic expectations'
        },
        {
            'Domain': 'Individual Differences',
            'Finding': 'Learning Patterns',
            'Result': 'Limited experience users show largest SUS improvement (+9.6 points)',
            'Implication': 'System design well-suited for AR/VR novices'
        },
        {
            'Domain': 'Technical Architecture',
            'Finding': 'Spatial Alignment',
            'Result': 'Movement asymmetry uncorrelated with performance (r=0.041, p=0.830)',
            'Implication': 'Marker-based anchoring successfully eliminates spatial constraints'
        },
        {
            'Domain': 'Technical Architecture',
            'Finding': 'UI Design Impact',
            'Result': 'Significant SUS improvement from 67.5 to 72.5 across sessions',
            'Implication': 'Spatially-anchored controls support learning and adaptation'
        },
        {
            'Domain': 'Technical Architecture',
            'Finding': 'Performance Consistency',
            'Result': 'High variability (CV=3.374) despite robust networking',
            'Implication': 'Network robustness necessary but insufficient for consistency'
        }
    ]
    
    # Create DataFrame
    findings_df = pd.DataFrame(findings_data)
    
    # Generate LaTeX table
    latex_table = """
\\begin{table}[htbp]
\\centering
\\caption{Summary of Key Exploratory Analysis Findings}
\\label{tab:exploratory_analysis_summary}
\\begin{tabular}{@{}p{2.5cm}p{2.8cm}p{4.2cm}p{4.5cm}@{}}
\\toprule
\\textbf{Domain} & \\textbf{Finding} & \\textbf{Result} & \\textbf{Implication} \\\\
\\midrule
"""
    
    current_domain = ""
    for _, row in findings_df.iterrows():
        # Only show domain name for first occurrence
        domain_text = row['Domain'] if row['Domain'] != current_domain else ""
        current_domain = row['Domain']
        
        latex_table += f"{domain_text} & {row['Finding']} & {row['Result']} & {row['Implication']} \\\\\n"
        
        # Add midrule between domains
        if domain_text and _ < len(findings_df) - 1 and findings_df.iloc[_ + 1]['Domain'] != current_domain:
            latex_table += "\\midrule\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}
"""
    
    # Write to file
    with open('exploratory_analysis_summary_table.tex', 'w') as f:
        f.write(latex_table)
    
    print("LaTeX table generated: exploratory_analysis_summary_table.tex")
    print("\nTable preview:")
    print(latex_table)
    
    return findings_df

if __name__ == "__main__":
    findings_df = generate_summary_table() 