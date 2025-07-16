import pandas as pd
import numpy as np
from scipy import stats
import warnings
warnings.filterwarnings('ignore')

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

print("=== DESCRIPTIVE STATISTICS TABLES ===")

# Table 1: Descriptive Statistics by Variant
print("\nTable 1: Descriptive Statistics for Completion Time by Collaboration Variant")
variant_desc = df_valid.groupby('Variant')['completion_time_minutes'].agg([
    ('N', 'count'),
    ('Mean', lambda x: f"{x.mean():.2f}"),
    ('SD', lambda x: f"{x.std():.2f}"),
    ('Min', lambda x: f"{x.min():.2f}"),
    ('Max', lambda x: f"{x.max():.2f}"),
    ('95% CI Lower', lambda x: f"{x.mean() - 1.96*x.std()/np.sqrt(len(x)):.2f}"),
    ('95% CI Upper', lambda x: f"{x.mean() + 1.96*x.std()/np.sqrt(len(x)):.2f}")
])
print(variant_desc.to_string())

# Table 2: Descriptive Statistics by Position
print("\nTable 2: Descriptive Statistics for Completion Time by Study Position")
position_desc = df_valid.groupby('Position in study')['completion_time_minutes'].agg([
    ('N', 'count'),
    ('Mean', lambda x: f"{x.mean():.2f}"),
    ('SD', lambda x: f"{x.std():.2f}"),
    ('Min', lambda x: f"{x.min():.2f}"),
    ('Max', lambda x: f"{x.max():.2f}"),
    ('95% CI Lower', lambda x: f"{x.mean() - 1.96*x.std()/np.sqrt(len(x)):.2f}"),
    ('95% CI Upper', lambda x: f"{x.mean() + 1.96*x.std()/np.sqrt(len(x)):.2f}")
])
print(position_desc.to_string())

# Table 3: Descriptive Statistics by Position (Excluding Roleplay)
print("\nTable 3: Descriptive Statistics for Completion Time by Study Position (Excluding Roleplay)")
df_no_roleplay = df_valid[df_valid['Variant'] != 'Roleplay'].copy()
position_desc_no_rp = df_no_roleplay.groupby('Position in study')['completion_time_minutes'].agg([
    ('N', 'count'),
    ('Mean', lambda x: f"{x.mean():.2f}"),
    ('SD', lambda x: f"{x.std():.2f}"),
    ('Min', lambda x: f"{x.min():.2f}"),
    ('Max', lambda x: f"{x.max():.2f}"),
    ('95% CI Lower', lambda x: f"{x.mean() - 1.96*x.std()/np.sqrt(len(x)):.2f}"),
    ('95% CI Upper', lambda x: f"{x.mean() + 1.96*x.std()/np.sqrt(len(x)):.2f}")
])
print(position_desc_no_rp.to_string())

print("\n=== ANOVA TABLES ===")

# ANOVA calculations for variants
def calculate_anova_table(groups, group_names, effect_name="Factor"):
    # Calculate basic statistics
    k = len(groups)  # number of groups
    n_total = sum(len(group) for group in groups)
    
    # Grand mean
    all_values = np.concatenate(groups)
    grand_mean = np.mean(all_values)
    
    # Sum of squares
    ss_between = sum(len(group) * (np.mean(group) - grand_mean)**2 for group in groups)
    ss_within = sum(sum((x - np.mean(group))**2 for x in group) for group in groups)
    ss_total = ss_between + ss_within
    
    # Degrees of freedom
    df_between = k - 1
    df_within = n_total - k
    df_total = n_total - 1
    
    # Mean squares
    ms_between = ss_between / df_between
    ms_within = ss_within / df_within
    
    # F-statistic
    f_stat = ms_between / ms_within
    
    # p-value
    p_value = 1 - stats.f.cdf(f_stat, df_between, df_within)
    
    # Create table
    anova_table = pd.DataFrame({
        'Source': [effect_name, 'Error', 'Total'],
        'SS': [f"{ss_between:.3f}", f"{ss_within:.3f}", f"{ss_total:.3f}"],
        'df': [df_between, df_within, df_total],
        'MS': [f"{ms_between:.3f}", f"{ms_within:.3f}", ""],
        'F': [f"{f_stat:.3f}", "", ""],
        'p': [f"{p_value:.3f}" if p_value >= 0.001 else "< 0.001", "", ""]
    })
    
    return anova_table

# Table 4: ANOVA for Collaboration Variants
print("\nTable 4: One-Way ANOVA for Completion Time by Collaboration Variant")
variant_groups = [df_valid[df_valid['Variant'] == variant]['completion_time_minutes'].values 
                  for variant in ['Open Ended', 'Roleplay', 'Silent', 'Timed']]
variant_anova = calculate_anova_table(variant_groups, ['Open Ended', 'Roleplay', 'Silent', 'Timed'], 
                                     "Collaboration Variant")
print(variant_anova.to_string(index=False))

# Table 5: ANOVA for Study Position (All Variants)
print("\nTable 5: One-Way ANOVA for Completion Time by Study Position (All Variants)")
position_groups = [df_valid[df_valid['Position in study'] == pos]['completion_time_minutes'].values 
                   for pos in [0, 1, 2, 3]]
position_anova = calculate_anova_table(position_groups, ['Position 0', 'Position 1', 'Position 2', 'Position 3'], 
                                      "Study Position")
print(position_anova.to_string(index=False))

# Table 6: ANOVA for Study Position (Excluding Roleplay)
print("\nTable 6: One-Way ANOVA for Completion Time by Study Position (Excluding Roleplay)")
position_groups_no_rp = [df_no_roleplay[df_no_roleplay['Position in study'] == pos]['completion_time_minutes'].values 
                         for pos in [0, 1, 2, 3]]
position_anova_no_rp = calculate_anova_table(position_groups_no_rp, 
                                            ['Position 0', 'Position 1', 'Position 2', 'Position 3'], 
                                            "Study Position")
print(position_anova_no_rp.to_string(index=False))

# Table 7: Learning Effect Correlations by Variant
print("\nTable 7: Learning Effect Correlations by Collaboration Variant")
learning_correlations = []
for variant in ['Open Ended', 'Silent', 'Timed', 'Roleplay']:
    variant_data = df_valid[df_valid['Variant'] == variant]
    if len(variant_data) >= 4:
        correlation = variant_data['Position in study'].corr(variant_data['completion_time_minutes'])
        n = len(variant_data)
        learning_correlations.append({
            'Variant': variant,
            'N': n,
            'Correlation (r)': f"{correlation:.3f}",
            'Interpretation': 'Learning effect' if correlation < -0.3 else 
                            'No learning effect' if abs(correlation) < 0.3 else 'Reverse learning'
        })

learning_table = pd.DataFrame(learning_correlations)
print(learning_table.to_string(index=False))

# Save tables to LaTeX format
with open('completion_time_tables.tex', 'w') as f:
    f.write("% Completion Time Analysis Tables\n\n")
    
    # Table 1
    f.write("\\begin{table}[htbp]\n")
    f.write("\\centering\n")
    f.write("\\caption{Descriptive Statistics for Completion Time by Collaboration Variant}\n")
    f.write("\\label{tab:completion_time_by_variant}\n")
    f.write("\\begin{tabular}{lrrrrrrr}\n")
    f.write("\\toprule\n")
    f.write("Variant & N & Mean & SD & Min & Max & 95\\% CI Lower & 95\\% CI Upper \\\\\n")
    f.write("\\midrule\n")
    for variant in ['Open Ended', 'Roleplay', 'Silent', 'Timed']:
        row = variant_desc.loc[variant]
        f.write(f"{variant} & {row['N']} & {row['Mean']} & {row['SD']} & {row['Min']} & {row['Max']} & {row['95% CI Lower']} & {row['95% CI Upper']} \\\\\n")
    f.write("\\bottomrule\n")
    f.write("\\end{tabular}\n")
    f.write("\\end{table}\n\n")
    
    # ANOVA Table
    f.write("\\begin{table}[htbp]\n")
    f.write("\\centering\n")
    f.write("\\caption{One-Way ANOVA for Completion Time by Collaboration Variant}\n")
    f.write("\\label{tab:anova_completion_time_variant}\n")
    f.write("\\begin{tabular}{lrrrrr}\n")
    f.write("\\toprule\n")
    f.write("Source & SS & df & MS & F & p \\\\\n")
    f.write("\\midrule\n")
    for _, row in variant_anova.iterrows():
        f.write(f"{row['Source']} & {row['SS']} & {row['df']} & {row['MS']} & {row['F']} & {row['p']} \\\\\n")
    f.write("\\bottomrule\n")
    f.write("\\end{tabular}\n")
    f.write("\\end{table}\n\n")

print("\nTables saved to completion_time_tables.tex") 