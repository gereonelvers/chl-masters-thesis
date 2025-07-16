import pandas as pd
import numpy as np
from scipy import stats
import warnings
warnings.filterwarnings('ignore')

# Load and clean data
df = pd.read_csv('../study-run-results.csv')

def clean_numeric(value):
    if pd.isna(value) or value == 'INVALID':
        return np.nan
    try:
        return float(str(value).replace(',', ''))
    except:
        return np.nan

quality_columns = [
    'Bridge Price',
    'Bridge height (max., cm)',
    'Bridge evaluation 1: Safety Factor (min, higher better)',
    'Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)',
    'Bridge evaluation 3: Displacement (max, in mm, smaller better)',
    '# objects (final bridge, start block incl.)',
    '# different objects (final bridge)'
]

for col in quality_columns:
    df[col] = df[col].apply(clean_numeric)

df = df.rename(columns={
    'Bridge Price': 'price',
    'Bridge height (max., cm)': 'height',
    'Bridge evaluation 1: Safety Factor (min, higher better)': 'safety_factor',
    'Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)': 'von_mises_stress',
    'Bridge evaluation 3: Displacement (max, in mm, smaller better)': 'displacement',
    '# objects (final bridge, start block incl.)': 'total_objects',
    '# different objects (final bridge)': 'different_objects'
})

df_valid = df.dropna(subset=['safety_factor', 'von_mises_stress', 'displacement']).copy()

print("=== BRIDGE QUALITY STATISTICAL TABLES ===")

# Table 1: Overall Descriptive Statistics
print("\nTable 1: Overall Descriptive Statistics for Bridge Quality Metrics")
overall_stats = []
metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price', 'height', 'total_objects', 'different_objects']
metric_names = ['Safety Factor', 'von Mises Stress (MPa)', 'Displacement (mm)', 'Price', 'Height (cm)', 'Total Objects', 'Different Objects']

for i, metric in enumerate(metrics):
    if metric in df_valid.columns:
        data = df_valid[metric].dropna()
        overall_stats.append({
            'Metric': metric_names[i],
            'N': len(data),
            'Mean': f"{data.mean():.2f}",
            'SD': f"{data.std():.2f}",
            'Median': f"{data.median():.2f}",
            'Min': f"{data.min():.2f}",
            'Max': f"{data.max():.2f}",
            '95% CI Lower': f"{data.mean() - 1.96*data.std()/np.sqrt(len(data)):.2f}",
            '95% CI Upper': f"{data.mean() + 1.96*data.std()/np.sqrt(len(data)):.2f}"
        })

overall_df = pd.DataFrame(overall_stats)
print(overall_df.to_string(index=False))

# Table 2: Descriptive Statistics by Variant
print("\nTable 2: Bridge Quality Metrics by Collaboration Variant")
key_metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price']
for metric in key_metrics:
    if metric in df_valid.columns:
        variant_stats = df_valid.groupby('Variant')[metric].agg([
            ('N', 'count'),
            ('Mean', lambda x: f"{x.mean():.2f}"),
            ('SD', lambda x: f"{x.std():.2f}"),
            ('Median', lambda x: f"{x.median():.2f}"),
            ('95% CI Lower', lambda x: f"{x.mean() - 1.96*x.std()/np.sqrt(len(x)):.2f}"),
            ('95% CI Upper', lambda x: f"{x.mean() + 1.96*x.std()/np.sqrt(len(x)):.2f}")
        ])
        print(f"\n{metric.upper().replace('_', ' ')}:")
        print(variant_stats.to_string())

# Table 3: ANOVA Results
print("\nTable 3: One-Way ANOVA Results for Bridge Quality Metrics by Collaboration Variant")
anova_results = []
for metric in key_metrics:
    if metric in df_valid.columns and not df_valid[metric].isna().all():
        variant_groups = [group[metric].dropna().values for name, group in df_valid.groupby('Variant')]
        variant_groups = [group for group in variant_groups if len(group) > 0]
        if len(variant_groups) >= 2:
            f_stat, p_val = stats.f_oneway(*variant_groups)
            
            # Calculate effect size (eta-squared)
            grand_mean = df_valid[metric].mean()
            ss_total = sum((df_valid[metric] - grand_mean)**2)
            ss_between = sum([len(group) * (np.mean(group) - grand_mean)**2 for group in variant_groups])
            eta_squared = ss_between / ss_total if ss_total > 0 else 0
            
            anova_results.append({
                'Metric': metric.replace('_', ' ').title(),
                'F': f"{f_stat:.3f}",
                'df1': len(variant_groups) - 1,
                'df2': len(df_valid[metric].dropna()) - len(variant_groups),
                'p': f"{p_val:.3f}",
                'eta²': f"{eta_squared:.3f}",
                'Significance': '***' if p_val < 0.001 else '**' if p_val < 0.01 else '*' if p_val < 0.05 else 'ns'
            })

anova_df = pd.DataFrame(anova_results)
print(anova_df.to_string(index=False))

# Table 4: Correlation Matrix
print("\nTable 4: Correlation Matrix for Bridge Quality and Performance Metrics")
correlation_metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price', 'total_objects']
correlation_names = ['Safety Factor', 'von Mises Stress', 'Displacement', 'Price', 'Total Objects']

corr_data = df_valid[correlation_metrics].dropna()
if len(corr_data) > 0:
    corr_matrix = corr_data.corr()
    corr_matrix.index = correlation_names
    corr_matrix.columns = correlation_names
    print(corr_matrix.round(3).to_string())

# Table 5: Quality-Efficiency Relationships
print("\nTable 5: Bridge Quality vs Task Performance Correlations")
def parse_time(time_str):
    if pd.isna(time_str) or time_str == 'INVALID':
        return np.nan
    try:
        parts = time_str.split(':')
        return int(parts[0]) + int(parts[1]) / 60
    except:
        return np.nan

df_valid['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(parse_time)

performance_correlations = []
quality_metrics = ['safety_factor', 'von_mises_stress', 'displacement', 'price']
quality_names = ['Safety Factor', 'von Mises Stress', 'Displacement', 'Price']

for i, metric in enumerate(quality_metrics):
    if metric in df_valid.columns:
        time_corr = df_valid[[metric, 'completion_time_minutes']].dropna().corr().iloc[0,1]
        objects_corr = df_valid[[metric, 'total_objects']].dropna().corr().iloc[0,1]
        
        performance_correlations.append({
            'Quality Metric': quality_names[i],
            'vs Completion Time': f"{time_corr:.3f}",
            'vs Total Objects': f"{objects_corr:.3f}",
            'Interpretation': 'Positive' if max(abs(time_corr), abs(objects_corr)) > 0.3 else 'Weak/No relationship'
        })

perf_corr_df = pd.DataFrame(performance_correlations)
print(perf_corr_df.to_string(index=False))

# Save to LaTeX
with open('bridge_quality_tables.tex', 'w') as f:
    f.write("% Bridge Quality Analysis Tables\n\n")
    
    # Overall statistics table
    f.write("\\begin{table}[htbp]\n")
    f.write("\\centering\n")
    f.write("\\caption{Descriptive Statistics for Bridge Quality Metrics}\n")
    f.write("\\label{tab:bridge_quality_overall}\n")
    f.write("\\begin{tabular}{lrrrrrr}\n")
    f.write("\\toprule\n")
    f.write("Metric & N & Mean & SD & Median & Min & Max \\\\\n")
    f.write("\\midrule\n")
    for _, row in overall_df.iterrows():
        f.write(f"{row['Metric']} & {row['N']} & {row['Mean']} & {row['SD']} & {row['Median']} & {row['Min']} & {row['Max']} \\\\\n")
    f.write("\\bottomrule\n")
    f.write("\\end{tabular}\n")
    f.write("\\end{table}\n\n")
    
    # ANOVA results table
    f.write("\\begin{table}[htbp]\n")
    f.write("\\centering\n")
    f.write("\\caption{One-Way ANOVA Results for Bridge Quality Metrics by Collaboration Variant}\n")
    f.write("\\label{tab:anova_bridge_quality}\n")
    f.write("\\begin{tabular}{lrrrrrl}\n")
    f.write("\\toprule\n")
    f.write("Metric & F & df1 & df2 & p & $\\eta^2$ & Significance \\\\\n")
    f.write("\\midrule\n")
    for _, row in anova_df.iterrows():
        f.write(f"{row['Metric']} & {row['F']} & {row['df1']} & {row['df2']} & {row['p']} & {row['eta²']} & {row['Significance']} \\\\\n")
    f.write("\\bottomrule\n")
    f.write("\\end{tabular}\n")
    f.write("\\end{table}\n\n")

print("\nTables saved to bridge_quality_tables.tex") 