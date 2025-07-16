#!/usr/bin/env python3

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
import warnings
warnings.filterwarnings('ignore')

# Set up plotting style
plt.style.use('default')
sns.set_palette("husl")
plt.rcParams.update({
    'font.size': 10,
    'axes.titlesize': 12,
    'axes.labelsize': 10,
    'xtick.labelsize': 9,
    'ytick.labelsize': 9,
    'legend.fontsize': 9,
    'figure.titlesize': 14
})

def load_and_process_data():
    """Load and process questionnaire data"""
    df = pd.read_csv('participant-results.csv')
    
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
    
    # Clean NASA-TLX scores
    nasa_tlx_cols = ['RTL X Score 1', 'RTL X Score 2', 'RTL X Score 3', 'RTL X Score 4']
    for col in nasa_tlx_cols:
        df[col] = df[col].apply(parse_nasa_tlx_score)
    
    # SUS scores are already clean
    sus_cols = ['SUS Score 1', 'SUS Score 2', 'SUS Score 3', 'SUS Score 4']
    
    return df, nasa_tlx_cols, sus_cols

def create_workload_analysis(df, nasa_tlx_cols):
    """Analyze NASA-TLX workload scores"""
    
    # Reshape data for analysis
    nasa_data = []
    for idx, row in df.iterrows():
        participant_id = row['Participant ID']
        for session_num, col in enumerate(nasa_tlx_cols, 1):
            nasa_data.append({
                'Participant': participant_id,
                'Session': session_num,
                'NASA_TLX': row[col]
            })
    
    nasa_df = pd.DataFrame(nasa_data)
    
    # Create visualization
    fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(12, 10))
    
    # Overall distribution
    nasa_df.boxplot(column='NASA_TLX', by='Session', ax=ax1)
    ax1.set_title('NASA-TLX Scores by Session')
    ax1.set_xlabel('Session')
    ax1.set_ylabel('NASA-TLX Score')
    ax1.grid(True, alpha=0.3)
    
    # Individual trajectories
    for participant in nasa_df['Participant'].unique():
        participant_data = nasa_df[nasa_df['Participant'] == participant]
        ax2.plot(participant_data['Session'], participant_data['NASA_TLX'], 
                alpha=0.6, marker='o', markersize=3)
    
    # Add mean trajectory
    mean_by_session = nasa_df.groupby('Session')['NASA_TLX'].mean()
    ax2.plot(mean_by_session.index, mean_by_session.values, 
            color='red', linewidth=3, marker='o', label='Mean')
    ax2.set_title('Individual NASA-TLX Trajectories')
    ax2.set_xlabel('Session')
    ax2.set_ylabel('NASA-TLX Score')
    ax2.legend()
    ax2.grid(True, alpha=0.3)
    
    # Learning effect
    first_last = nasa_df[nasa_df['Session'].isin([1, 4])]
    learning_data = []
    for participant in first_last['Participant'].unique():
        p_data = first_last[first_last['Participant'] == participant]
        if len(p_data) == 2:
            first_score = p_data[p_data['Session'] == 1]['NASA_TLX'].iloc[0]
            last_score = p_data[p_data['Session'] == 4]['NASA_TLX'].iloc[0]
            learning_data.append(last_score - first_score)
    
    ax3.hist(learning_data, bins=8, alpha=0.7, color='skyblue', edgecolor='black')
    ax3.axvline(x=0, color='red', linestyle='--', alpha=0.7)
    ax3.set_title('NASA-TLX Learning Effect\n(Session 4 - Session 1)')
    ax3.set_xlabel('Change in NASA-TLX Score')
    ax3.set_ylabel('Number of Participants')
    ax3.grid(True, alpha=0.3)
    
    # Mean and CI by session
    session_stats = nasa_df.groupby('Session')['NASA_TLX'].agg(['mean', 'std', 'count'])
    session_stats['se'] = session_stats['std'] / np.sqrt(session_stats['count'])
    session_stats['ci'] = 1.96 * session_stats['se']
    
    ax4.errorbar(session_stats.index, session_stats['mean'], 
                yerr=session_stats['ci'], marker='o', capsize=5, capthick=2)
    ax4.set_title('NASA-TLX Mean ± 95% CI by Session')
    ax4.set_xlabel('Session')
    ax4.set_ylabel('NASA-TLX Score')
    ax4.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../assets/06/nasa_tlx_analysis.pdf', bbox_inches='tight', dpi=300)
    plt.close()
    
    return nasa_df, session_stats

def create_usability_analysis(df, sus_cols):
    """Analyze SUS scores"""
    
    # Reshape data for analysis
    sus_data = []
    for idx, row in df.iterrows():
        participant_id = row['Participant ID']
        for session_num, col in enumerate(sus_cols, 1):
            sus_data.append({
                'Participant': participant_id,
                'Session': session_num,
                'SUS': row[col]
            })
    
    sus_df = pd.DataFrame(sus_data)
    
    # Create compact visualization with only bottom two panels
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
    
    # Learning effect (previously ax3)
    first_last = sus_df[sus_df['Session'].isin([1, 4])]
    learning_data = []
    for participant in first_last['Participant'].unique():
        p_data = first_last[first_last['Participant'] == participant]
        if len(p_data) == 2:
            first_score = p_data[p_data['Session'] == 1]['SUS'].iloc[0]
            last_score = p_data[p_data['Session'] == 4]['SUS'].iloc[0]
            learning_data.append(last_score - first_score)
    
    ax1.hist(learning_data, bins=8, alpha=0.7, color='lightgreen', edgecolor='black')
    ax1.axvline(x=0, color='red', linestyle='--', alpha=0.7)
    ax1.set_title('SUS Learning Effect\n(Session 4 - Session 1)')
    ax1.set_xlabel('Change in SUS Score')
    ax1.set_ylabel('Number of Participants')
    ax1.grid(True, alpha=0.3)
    
    # Experience level analysis (previously ax4)
    experience_mapping = {
        'No experience': 'None',
        'Limited experience': 'Limited',
        'Moderate experience': 'Moderate',
        'Extensive experience': 'Extensive'
    }
    
    df['Experience_Level'] = df['General Information - 5. How would you rate your experience with Augmented Reality (AR) and Virtual Reality (VR) applications?'].map(experience_mapping)
    
    # Merge experience with SUS data
    exp_sus_data = []
    for idx, row in df.iterrows():
        participant_id = row['Participant ID']
        experience = row['Experience_Level']
        for session_num, col in enumerate(sus_cols, 1):
            exp_sus_data.append({
                'Participant': participant_id,
                'Session': session_num,
                'SUS': row[col],
                'Experience': experience
            })
    
    exp_sus_df = pd.DataFrame(exp_sus_data)
    exp_mean = exp_sus_df.groupby(['Experience', 'Session'])['SUS'].mean().reset_index()
    
    for exp_level in exp_mean['Experience'].unique():
        if pd.notna(exp_level):
            exp_data = exp_mean[exp_mean['Experience'] == exp_level]
            ax2.plot(exp_data['Session'], exp_data['SUS'], marker='o', label=exp_level)
    
    ax2.set_title('SUS Scores by AR/VR Experience')
    ax2.set_xlabel('Session')
    ax2.set_ylabel('Mean SUS Score')
    ax2.set_ylim(40, 100)
    ax2.legend()
    ax2.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../assets/06/sus_analysis.pdf', bbox_inches='tight', dpi=300)
    plt.close()
    
    return sus_df, exp_sus_df

def analyze_relationship_measures(df):
    """Analyze relationship and trust measures"""
    
    # Extract pre and post IOS scores
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
    
    # Create relationship analysis visualization
    fig, ((ax1, ax2), (ax3, ax4)) = plt.subplots(2, 2, figsize=(12, 10))
    
    # IOS change
    ios_changes = [post - pre for pre, post in zip(pre_ios, post_ios)]
    ax1.hist(ios_changes, bins=range(-3, 4), alpha=0.7, color='lightcoral', edgecolor='black')
    ax1.axvline(x=0, color='red', linestyle='--', alpha=0.7)
    ax1.set_title('Change in Relationship Closeness\n(IOS Scale)')
    ax1.set_xlabel('Change in IOS Score')
    ax1.set_ylabel('Number of Participants')
    ax1.grid(True, alpha=0.3)
    
    # Pre vs Post scatter
    ax2.scatter(pre_ios, post_ios, alpha=0.7, s=50)
    ax2.plot([1, 7], [1, 7], 'r--', alpha=0.7)
    ax2.set_title('Pre vs Post IOS Scores')
    ax2.set_xlabel('Pre-Study IOS Score')
    ax2.set_ylabel('Post-Study IOS Score')
    ax2.grid(True, alpha=0.3)
    
    # Trust analysis (example with one trust measure)
    trust_cols = [col for col in df.columns if 'trust' in col.lower() and 'Post-Task' in col]
    if trust_cols:
        trust_data = df[trust_cols[0]].map({
            'Not at all true': 1,
            'Slightly true': 2,
            'Somewhat true': 3,
            'Moderately true': 4,
            'Fairly true': 5,
            'Very true': 6,
            'Extremely true': 7
        }).dropna()
        
        ax3.hist(trust_data, bins=range(1, 9), alpha=0.7, color='lightblue', edgecolor='black')
        ax3.set_title('Post-Study Trust Ratings')
        ax3.set_xlabel('Trust Rating (1-7)')
        ax3.set_ylabel('Number of Participants')
        ax3.grid(True, alpha=0.3)
    
    # Competence ratings
    comp_cols = [col for col in df.columns if 'confident' in col.lower() and 'partner' in col.lower() and 'Post-Task' in col]
    if comp_cols:
        comp_data = df[comp_cols[0]].map({
            'Not at all true': 1,
            'Slightly true': 2,
            'Somewhat true': 3,
            'Moderately true': 4,
            'Fairly true': 5,
            'Very true': 6,
            'Extremely true': 7
        }).dropna()
        
        ax4.hist(comp_data, bins=range(1, 9), alpha=0.7, color='lightyellow', edgecolor='black')
        ax4.set_title('Partner Competence Confidence')
        ax4.set_xlabel('Confidence Rating (1-7)')
        ax4.set_ylabel('Number of Participants')
        ax4.grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../assets/06/relationship_analysis.pdf', bbox_inches='tight', dpi=300)
    plt.close()
    
    return ios_changes, trust_data if trust_cols else None, comp_data if comp_cols else None

def create_summary_table(nasa_df, sus_df):
    """Create summary statistics table"""
    
    # NASA-TLX summary
    nasa_summary = nasa_df.groupby('Session')['NASA_TLX'].agg(['mean', 'std', 'count'])
    nasa_summary['se'] = nasa_summary['std'] / np.sqrt(nasa_summary['count'])
    
    # SUS summary
    sus_summary = sus_df.groupby('Session')['SUS'].agg(['mean', 'std', 'count'])
    sus_summary['se'] = sus_summary['std'] / np.sqrt(sus_summary['count'])
    
    # Test for learning effects
    nasa_first_last = []
    sus_first_last = []
    
    for participant in nasa_df['Participant'].unique():
        p_nasa = nasa_df[nasa_df['Participant'] == participant]
        p_sus = sus_df[sus_df['Participant'] == participant]
        
        if len(p_nasa) >= 4 and len(p_sus) >= 4:
            nasa_first_last.extend([
                p_nasa[p_nasa['Session'] == 1]['NASA_TLX'].iloc[0],
                p_nasa[p_nasa['Session'] == 4]['NASA_TLX'].iloc[0]
            ])
            sus_first_last.extend([
                p_sus[p_sus['Session'] == 1]['SUS'].iloc[0],
                p_sus[p_sus['Session'] == 4]['SUS'].iloc[0]
            ])
    
    # Paired t-test for learning effects
    nasa_pairs = [(nasa_first_last[i], nasa_first_last[i+1]) for i in range(0, len(nasa_first_last), 2)]
    sus_pairs = [(sus_first_last[i], sus_first_last[i+1]) for i in range(0, len(sus_first_last), 2)]
    
    nasa_first = [pair[0] for pair in nasa_pairs]
    nasa_last = [pair[1] for pair in nasa_pairs]
    sus_first = [pair[0] for pair in sus_pairs]
    sus_last = [pair[1] for pair in sus_pairs]
    
    nasa_ttest = stats.ttest_rel(nasa_last, nasa_first)
    sus_ttest = stats.ttest_rel(sus_last, sus_first)
    
    print("=== SUBJECTIVE EXPERIENCE ANALYSIS ===")
    print("\nNASA-TLX Summary by Session:")
    print(nasa_summary.round(2))
    print(f"\nNASA-TLX Learning Effect (Session 4 vs 1):")
    print(f"t-statistic: {nasa_ttest.statistic:.3f}, p-value: {nasa_ttest.pvalue:.3f}")
    print(f"Mean change: {np.mean(nasa_last) - np.mean(nasa_first):.2f}")
    
    print("\nSUS Summary by Session:")
    print(sus_summary.round(2))
    print(f"\nSUS Learning Effect (Session 4 vs 1):")
    print(f"t-statistic: {sus_ttest.statistic:.3f}, p-value: {sus_ttest.pvalue:.3f}")
    print(f"Mean change: {np.mean(sus_last) - np.mean(sus_first):.2f}")
    
    return nasa_summary, sus_summary, nasa_ttest, sus_ttest

def main():
    print("Loading and processing data...")
    df, nasa_tlx_cols, sus_cols = load_and_process_data()
    
    print("Analyzing NASA-TLX workload scores...")
    nasa_df, nasa_stats = create_workload_analysis(df, nasa_tlx_cols)
    
    print("Analyzing SUS usability scores...")
    sus_df, exp_sus_df = create_usability_analysis(df, sus_cols)
    
    print("Analyzing relationship measures...")
    ios_changes, trust_data, comp_data = analyze_relationship_measures(df)
    
    print("Creating summary statistics...")
    nasa_summary, sus_summary, nasa_ttest, sus_ttest = create_summary_table(nasa_df, sus_df)
    
    print("Analysis complete. Visualizations saved to ../assets/06/")

if __name__ == "__main__":
    main() 