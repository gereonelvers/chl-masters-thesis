import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from collections import defaultdict, Counter
import re

def parse_spawned_objects(spawned_str):
    """Parse spawned objects string into dictionary of object types and counts"""
    if pd.isna(spawned_str) or spawned_str == 'INVALID':
        return {}
    
    pattern = r'(\w+(?:\w+)*)\s*×(\d+)'
    matches = re.findall(pattern, spawned_str)
    return {obj_type: int(count) for obj_type, count in matches}

def load_and_process_data():
    """Load and process the study results data"""
    df = pd.read_csv('../study-run-results.csv')
    
    # Parse spawned objects
    df['spawned_objects_dict'] = df['Spawned objects'].apply(parse_spawned_objects)
    
    # Calculate total spawned objects
    df['total_spawned'] = df['spawned_objects_dict'].apply(lambda x: sum(x.values()) if x else 0)
    
    # Convert completion time from "MM:SS" to decimal minutes
    def convert_time_to_minutes(time_str):
        if pd.isna(time_str) or time_str == 'INVALID':
            return np.nan
        try:
            minutes, seconds = time_str.split(':')
            return int(minutes) + int(seconds) / 60
        except:
            return np.nan
    
    df['Completion time (exact, minutes)'] = df['Completion time (exact, minutes)'].apply(convert_time_to_minutes)
    
    # Filter out invalid runs
    valid_df = df[
        (df['# objects (final bridge, start block incl.)'] != 'INVALID') & 
        (df['total_spawned'] > 0)
    ].copy()
    
    # Convert numeric columns
    numeric_cols = ['# objects (final bridge, start block incl.)', '# different objects (final bridge)', 
                   'Bridge Price', 'Bridge height (max., cm)']
    for col in numeric_cols:
        valid_df[col] = pd.to_numeric(valid_df[col], errors='coerce')
    
    return valid_df

def calculate_efficiency_metrics(df):
    """Calculate construction efficiency metrics"""
    df['efficiency_ratio'] = df['# objects (final bridge, start block incl.)'] / df['total_spawned']
    df['waste_objects'] = df['total_spawned'] - df['# objects (final bridge, start block incl.)']
    df['waste_percentage'] = (df['waste_objects'] / df['total_spawned']) * 100
    return df

def analyze_object_preferences(df):
    """Analyze object type preferences across variants"""
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
    
    return object_preferences

def create_construction_patterns_visualizations(df, object_preferences):
    """Create visualizations for construction patterns"""
    plt.style.use('default')
    fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(14, 10))
    
    # 1. Efficiency by variant
    efficiency_by_variant = df.groupby('Variant')['efficiency_ratio'].agg(['mean', 'std', 'count'])
    variants = efficiency_by_variant.index
    means = efficiency_by_variant['mean']
    stds = efficiency_by_variant['std']
    
    bars1 = ax1.bar(variants, means, yerr=stds, capsize=5, color='lightgrey', 
                   edgecolor='black', linewidth=0.8)
    ax1.set_ylabel('Efficiency Ratio (Final/Spawned)')
    ax1.set_title('Construction Efficiency by Collaboration Variant', fontweight='bold')
    ax1.tick_params(axis='x', rotation=45)
    ax1.grid(axis='y', alpha=0.3)
    
    # Add value labels
    for bar, mean in zip(bars1, means):
        ax1.text(bar.get_x() + bar.get_width()/2, bar.get_height() + 0.01,
                f'{mean:.2f}', ha='center', va='bottom', fontsize=9)
    
    # 2. Object type usage patterns
    all_objects = set()
    for variant_objects in object_preferences.values():
        all_objects.update(variant_objects.keys())
    
    # Create matrix for heatmap
    heatmap_data = []
    object_types = sorted(all_objects)
    variant_order = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    
    for variant in variant_order:
        row = [object_preferences[variant].get(obj, 0) for obj in object_types]
        heatmap_data.append(row)
    
    im = ax2.imshow(heatmap_data, cmap='Blues', aspect='auto')
    ax2.set_xticks(range(len(object_types)))
    ax2.set_xticklabels(object_types, rotation=45, ha='right')
    ax2.set_yticks(range(len(variant_order)))
    ax2.set_yticklabels(variant_order)
    ax2.set_title('Object Type Usage by Variant (%)', fontweight='bold')
    
    # Add text annotations
    for i in range(len(variant_order)):
        for j in range(len(object_types)):
            value = heatmap_data[i][j]
            if value > 0:
                ax2.text(j, i, f'{value:.1f}', ha='center', va='center',
                        color='white' if value > 15 else 'black', fontsize=8)
    
    # 3. Waste vs Performance
    scatter = ax3.scatter(df['waste_percentage'], df['Completion time (exact, minutes)'], 
                         c=df['Variant'].astype('category').cat.codes, alpha=0.7, s=60,
                         cmap='Set2', edgecolors='black', linewidth=0.5)
    ax3.set_xlabel('Waste Percentage (%)')
    ax3.set_ylabel('Completion Time (minutes)')
    ax3.set_title('Construction Waste vs Task Performance', fontweight='bold')
    ax3.grid(True, alpha=0.3)
    
    # Add legend for variants
    variants_unique = df['Variant'].unique()
    handles = [plt.Line2D([0], [0], marker='o', color='w', 
                         markerfacecolor=plt.cm.Set2(i/len(variants_unique)), 
                         markersize=8, markeredgecolor='black') 
              for i in range(len(variants_unique))]
    ax3.legend(handles, variants_unique, loc='upper right', fontsize=9)
    
    # 4. Construction complexity
    complexity_metrics = df.groupby('Variant').agg({
        'total_spawned': 'mean',
        '# different objects (spawned)': 'mean',
        '# objects (final bridge, start block incl.)': 'mean'
    })
    
    x_pos = np.arange(len(complexity_metrics.index))
    width = 0.25
    
    bars_spawned = ax4.bar(x_pos - width, complexity_metrics['total_spawned'], 
                          width, label='Total Spawned', color='lightcoral', 
                          edgecolor='black', linewidth=0.8)
    bars_final = ax4.bar(x_pos, complexity_metrics['# objects (final bridge, start block incl.)'], 
                        width, label='Final Bridge', color='lightblue',
                        edgecolor='black', linewidth=0.8)
    bars_types = ax4.bar(x_pos + width, complexity_metrics['# different objects (spawned)'] * 3, 
                        width, label='Different Types (×3)', color='lightgreen',
                        edgecolor='black', linewidth=0.8)
    
    ax4.set_xlabel('Collaboration Variant')
    ax4.set_ylabel('Number of Objects')
    ax4.set_title('Construction Complexity by Variant', fontweight='bold')
    ax4.set_xticks(x_pos)
    ax4.set_xticklabels(complexity_metrics.index, rotation=45)
    ax4.legend(loc='upper right', fontsize=9)
    ax4.grid(axis='y', alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../../assets/06/construction_patterns_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.savefig('../../assets/06/construction_patterns_analysis.png', dpi=300, bbox_inches='tight')
    plt.close()

def create_efficiency_comparison_chart(df):
    """Create detailed efficiency comparison chart"""
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
    
    # Efficiency distribution by variant
    variants = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    efficiency_data = [df[df['Variant'] == v]['efficiency_ratio'].values for v in variants]
    
    bp = ax1.boxplot(efficiency_data, tick_labels=variants, patch_artist=True)
    for patch in bp['boxes']:
        patch.set_facecolor('lightgrey')
        patch.set_edgecolor('black')
    
    ax1.set_ylabel('Efficiency Ratio (Final Objects / Spawned Objects)')
    ax1.set_title('Construction Efficiency Distribution', fontweight='bold')
    ax1.tick_params(axis='x', rotation=45)
    ax1.grid(axis='y', alpha=0.3)
    
    # Efficiency vs bridge quality
    valid_quality = df.dropna(subset=['Bridge evaluation 1: Safety Factor (min, higher better)'])
    if not valid_quality.empty:
        scatter = ax2.scatter(valid_quality['efficiency_ratio'], 
                            valid_quality['Bridge evaluation 1: Safety Factor (min, higher better)'],
                            c=valid_quality['Variant'].astype('category').cat.codes,
                            alpha=0.7, s=60, cmap='Set2', edgecolors='black', linewidth=0.5)
        ax2.set_xlabel('Efficiency Ratio')
        ax2.set_ylabel('Bridge Safety Factor')
        ax2.set_title('Construction Efficiency vs Bridge Quality', fontweight='bold')
        ax2.grid(True, alpha=0.3)
        
        # Add correlation coefficient
        corr = valid_quality['efficiency_ratio'].corr(valid_quality['Bridge evaluation 1: Safety Factor (min, higher better)'])
        ax2.text(0.05, 0.95, f'r = {corr:.3f}', transform=ax2.transAxes, 
                bbox=dict(boxstyle='round', facecolor='white', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig('../../assets/06/construction_efficiency_comparison.pdf', dpi=300, bbox_inches='tight')
    plt.savefig('../../assets/06/construction_efficiency_comparison.png', dpi=300, bbox_inches='tight')
    plt.close()

def generate_construction_tables(df, object_preferences):
    """Generate tables for construction patterns analysis"""
    
    # Table 1: Construction efficiency by variant
    efficiency_stats = df.groupby('Variant').agg({
        'efficiency_ratio': ['mean', 'std', 'count'],
        'waste_percentage': ['mean', 'std'],
        'total_spawned': ['mean', 'std'],
        '# objects (final bridge, start block incl.)': ['mean', 'std']
    }).round(3)
    
    # Flatten column names
    efficiency_stats.columns = ['_'.join(col).strip() for col in efficiency_stats.columns]
    
    # Table 2: Object type preferences
    object_pref_df = pd.DataFrame(object_preferences).fillna(0).round(1)
    
    # Table 3: Correlations with performance
    correlations = df[['efficiency_ratio', 'waste_percentage', 'total_spawned', 
                      'Completion time (exact, minutes)']].corr().round(3)
    
    return efficiency_stats, object_pref_df, correlations

def main():
    """Main analysis function"""
    print("Loading and processing construction patterns data...")
    df = load_and_process_data()
    
    print("Calculating efficiency metrics...")
    df = calculate_efficiency_metrics(df)
    
    print("Analyzing object preferences...")
    object_preferences = analyze_object_preferences(df)
    
    print("Creating visualizations...")
    create_construction_patterns_visualizations(df, object_preferences)
    create_efficiency_comparison_chart(df)
    
    print("Generating tables...")
    efficiency_stats, object_pref_df, correlations = generate_construction_tables(df, object_preferences)
    
    # Save key results
    print("\n=== CONSTRUCTION PATTERNS ANALYSIS RESULTS ===")
    print("\nEfficiency by Variant:")
    print(efficiency_stats[['efficiency_ratio_mean', 'efficiency_ratio_std', 'waste_percentage_mean']])
    
    print("\nObject Type Preferences (%):")
    print(object_pref_df)
    
    print("\nKey Correlations:")
    print(f"Efficiency vs Completion Time: {correlations.loc['efficiency_ratio', 'Completion time (exact, minutes)']:.3f}")
    print(f"Waste vs Completion Time: {correlations.loc['waste_percentage', 'Completion time (exact, minutes)']:.3f}")
    
    # Save detailed results
    efficiency_stats.to_csv('construction_efficiency_stats.csv')
    object_pref_df.to_csv('object_preferences.csv')
    correlations.to_csv('construction_performance_correlations.csv')
    
    print("\nAnalysis complete! Results saved to assets/06/ and results/")

if __name__ == "__main__":
    main() 