import pandas as pd
import numpy as np

def load_processed_data():
    """Load the processed data from the main analysis"""
    df = pd.read_csv('../study-run-results.csv')
    
    # Parse spawned objects
    import re
    def parse_spawned_objects(spawned_str):
        if pd.isna(spawned_str) or spawned_str == 'INVALID':
            return {}
        pattern = r'(\w+(?:\w+)*)\s*×(\d+)'
        matches = re.findall(pattern, spawned_str)
        return {obj_type: int(count) for obj_type, count in matches}
    
    df['spawned_objects_dict'] = df['Spawned objects'].apply(parse_spawned_objects)
    df['total_spawned'] = df['spawned_objects_dict'].apply(lambda x: sum(x.values()) if x else 0)
    
    # Convert time
    def convert_time_to_minutes(time_str):
        if pd.isna(time_str) or time_str == 'INVALID':
            return np.nan
        try:
            minutes, seconds = time_str.split(':')
            return int(minutes) + int(seconds) / 60
        except:
            return np.nan
    
    df['completion_time_minutes'] = df['Completion time (exact, minutes)'].apply(convert_time_to_minutes)
    
    # Filter valid runs
    valid_df = df[
        (df['# objects (final bridge, start block incl.)'] != 'INVALID') & 
        (df['total_spawned'] > 0)
    ].copy()
    
    # Convert numeric columns
    numeric_cols = ['# objects (final bridge, start block incl.)', '# different objects (final bridge)']
    for col in numeric_cols:
        valid_df[col] = pd.to_numeric(valid_df[col], errors='coerce')
    
    # Calculate efficiency metrics
    valid_df['efficiency_ratio'] = valid_df['# objects (final bridge, start block incl.)'] / valid_df['total_spawned']
    valid_df['waste_objects'] = valid_df['total_spawned'] - valid_df['# objects (final bridge, start block incl.)']
    valid_df['waste_percentage'] = (valid_df['waste_objects'] / valid_df['total_spawned']) * 100
    
    return valid_df

def generate_construction_efficiency_table():
    """Generate LaTeX table for construction efficiency metrics"""
    df = load_processed_data()
    
    # Calculate statistics by variant
    stats = df.groupby('Variant').agg({
        'efficiency_ratio': ['mean', 'std', 'count'],
        'waste_percentage': ['mean', 'std'],
        'total_spawned': ['mean', 'std'],
        '# objects (final bridge, start block incl.)': ['mean', 'std']
    }).round(3)
    
    # Flatten columns
    stats.columns = ['_'.join(col).strip() for col in stats.columns]
    
    # Create LaTeX table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{Construction Efficiency Metrics by Collaboration Variant}
\\label{tab:construction_efficiency}
\\begin{tabular}{lrrrrrr}
\\toprule
\\textbf{Variant} & \\textbf{N} & \\textbf{Efficiency} & \\textbf{Waste \\%} & \\textbf{Objects Spawned} & \\textbf{Objects Used} & \\textbf{95\\% CI} \\\\
\\midrule
"""
    
    variant_order = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    for variant in variant_order:
        if variant in stats.index:
            row = stats.loc[variant]
            n = int(row['efficiency_ratio_count'])
            eff_mean = row['efficiency_ratio_mean']
            eff_std = row['efficiency_ratio_std']
            waste_mean = row['waste_percentage_mean']
            spawned_mean = row['total_spawned_mean']
            used_mean = row['# objects (final bridge, start block incl.)_mean']
            
            # Calculate 95% CI
            ci_margin = 1.96 * eff_std / np.sqrt(n)
            ci_lower = eff_mean - ci_margin
            ci_upper = eff_mean + ci_margin
            
            latex_table += f"{variant} & {n} & {eff_mean:.3f} & {waste_mean:.1f} & {spawned_mean:.1f} & {used_mean:.1f} & [{ci_lower:.3f}, {ci_upper:.3f}] \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}"""
    
    return latex_table

def generate_object_preferences_table():
    """Generate LaTeX table for object type preferences"""
    df = load_processed_data()
    
    # Calculate object preferences
    from collections import defaultdict
    object_counts = defaultdict(lambda: defaultdict(int))
    variant_totals = defaultdict(int)
    
    for _, row in df.iterrows():
        variant = row['Variant']
        for obj_type, count in row['spawned_objects_dict'].items():
            object_counts[variant][obj_type] += count
            variant_totals[variant] += count
    
    # Convert to percentages
    object_preferences = {}
    for variant in object_counts:
        object_preferences[variant] = {
            obj_type: (count / variant_totals[variant]) * 100 
            for obj_type, count in object_counts[variant].items()
        }
    
    # Get all object types
    all_objects = set()
    for variant_objects in object_preferences.values():
        all_objects.update(variant_objects.keys())
    object_types = sorted(all_objects)
    
    # Create LaTeX table
    latex_table = """\\begin{table}[htbp]
\\centering
\\caption{Object Type Usage Preferences by Collaboration Variant (\\%)}
\\label{tab:object_preferences}
\\begin{tabular}{l""" + "r" * len(['Open Ended', 'Silent', 'Timed', 'Roleplay']) + """}
\\toprule
\\textbf{Object Type} & \\textbf{Open Ended} & \\textbf{Silent} & \\textbf{Timed} & \\textbf{Roleplay} \\\\
\\midrule
"""
    
    variant_order = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    for obj_type in object_types:
        latex_table += f"{obj_type}"
        for variant in variant_order:
            value = object_preferences.get(variant, {}).get(obj_type, 0)
            latex_table += f" & {value:.1f}"
        latex_table += " \\\\\n"
    
    latex_table += """\\bottomrule
\\end{tabular}
\\end{table}"""
    
    return latex_table

def generate_construction_summary_table():
    """Generate summary table for construction patterns"""
    df = load_processed_data()
    
    # Overall statistics
    total_runs = len(df)
    avg_efficiency = df['efficiency_ratio'].mean()
    avg_waste = df['waste_percentage'].mean()
    
    # Correlations with performance
    corr_eff_time = df['efficiency_ratio'].corr(df['completion_time_minutes'])
    corr_waste_time = df['waste_percentage'].corr(df['completion_time_minutes'])
    
    # Best and worst efficiency variants
    variant_efficiency = df.groupby('Variant')['efficiency_ratio'].mean().sort_values(ascending=False)
    best_variant = variant_efficiency.index[0]
    worst_variant = variant_efficiency.index[-1]
    
    latex_table = f"""\\begin{{table}}[htbp]
\\centering
\\caption{{Construction Patterns Summary Statistics}}
\\label{{tab:construction_summary}}
\\begin{{tabular}}{{lr}}
\\toprule
\\textbf{{Metric}} & \\textbf{{Value}} \\\\
\\midrule
Total Valid Constructions & {total_runs} \\\\
Average Efficiency Ratio & {avg_efficiency:.3f} \\\\
Average Waste Percentage & {avg_waste:.1f}\\% \\\\
\\midrule
Efficiency vs Completion Time (r) & {corr_eff_time:.3f} \\\\
Waste vs Completion Time (r) & {corr_waste_time:.3f} \\\\
\\midrule
Most Efficient Variant & {best_variant} \\\\
Least Efficient Variant & {worst_variant} \\\\
\\bottomrule
\\end{{tabular}}
\\end{{table}}"""
    
    return latex_table

def main():
    """Generate all construction pattern tables"""
    print("Generating construction efficiency table...")
    efficiency_table = generate_construction_efficiency_table()
    
    print("Generating object preferences table...")
    preferences_table = generate_object_preferences_table()
    
    print("Generating summary table...")
    summary_table = generate_construction_summary_table()
    
    # Save tables
    with open('construction_patterns_tables.tex', 'w') as f:
        f.write(efficiency_table)
        f.write('\n\n')
        f.write(preferences_table)
        f.write('\n\n')
        f.write(summary_table)
    
    print("Tables saved to construction_patterns_tables.tex")
    
    # Print for immediate use
    print("\n=== CONSTRUCTION EFFICIENCY TABLE ===")
    print(efficiency_table)
    print("\n=== OBJECT PREFERENCES TABLE ===")
    print(preferences_table)
    print("\n=== SUMMARY TABLE ===")
    print(summary_table)

if __name__ == "__main__":
    main() 