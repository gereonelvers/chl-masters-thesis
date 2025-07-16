#!/usr/bin/env python3
"""
Exploratory Analysis for Chapter 6 Results
==========================================

This script performs exploratory analysis for three key subsections:
1. Task Variant Effects
2. Individual Differences and System Interaction  
3. Technical Architecture Impact on User Experience
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
from scipy.stats import spearmanr, pearsonr
import warnings
warnings.filterwarnings('ignore')

# Set style for academic plots
plt.style.use('default')
sns.set_palette("husl")
plt.rcParams.update({
    'figure.figsize': (12, 8),
    'font.size': 11,
    'axes.titlesize': 12,
    'axes.labelsize': 11,
    'xtick.labelsize': 10,
    'ytick.labelsize': 10,
    'legend.fontsize': 10
})

def load_data():
    """Load and prepare all datasets"""
    # Load main study results
    study_data = pd.read_csv('study-run-results.csv')
    
    # Load participant data
    participant_data = pd.read_csv('participant-results.csv')
    
    # Clean study data - remove invalid entries
    study_data_clean = study_data[study_data['Completion time (exact, minutes)'].notna()].copy()
    study_data_clean = study_data_clean[~study_data_clean['Notes'].str.contains('failed to construct', na=False)]
    
    # Parse completion time to numeric
    study_data_clean['completion_time_min'] = study_data_clean['Completion time (exact, minutes)'].str.extract(r'(\d+):(\d+)').apply(
        lambda x: float(x[0]) + float(x[1])/60, axis=1)
    
    return study_data_clean, participant_data

def analyze_task_variant_effects(study_data):
    """
    1. Task Variant Effects
    Guided by literature on communication importance in collaboration
    """
    print("="*60)
    print("1. TASK VARIANT EFFECTS ANALYSIS")
    print("="*60)
    
    # Prepare data for analysis
    variants = ['Open Ended', 'Silent', 'Timed', 'Roleplay']
    
    # 1.1 Communication vs Non-Communication Variants
    print("\n1.1 Communication vs Non-Communication Effects")
    print("-" * 50)
    
    silent_data = study_data[study_data['Variant'] == 'Silent']
    talking_data = study_data[study_data['Variant'].isin(['Open Ended', 'Roleplay'])]
    
    print(f"Silent variant completion time: {silent_data['completion_time_min'].mean():.2f} ± {silent_data['completion_time_min'].std():.2f} min")
    print(f"Talking variants completion time: {talking_data['completion_time_min'].mean():.2f} ± {talking_data['completion_time_min'].std():.2f} min")
    
    # Statistical test
    stat, p_value = stats.mannwhitneyu(silent_data['completion_time_min'], 
                                       talking_data['completion_time_min'], 
                                       alternative='two-sided')
    print(f"Mann-Whitney U test: U={stat:.2f}, p={p_value:.3f}")
    
    # 1.2 Time Pressure Effects  
    print("\n1.2 Time Pressure Effects")
    print("-" * 50)
    
    timed_data = study_data[study_data['Variant'] == 'Timed']
    untimed_data = study_data[study_data['Variant'].isin(['Open Ended', 'Silent', 'Roleplay'])]
    
    print(f"Timed variant completion time: {timed_data['completion_time_min'].mean():.2f} ± {timed_data['completion_time_min'].std():.2f} min")
    print(f"Untimed variants completion time: {untimed_data['completion_time_min'].mean():.2f} ± {untimed_data['completion_time_min'].std():.2f} min")
    
    # Bridge quality comparison
    if 'Bridge evaluation 1: Safety Factor (min, higher better)' in study_data.columns:
        timed_safety = pd.to_numeric(timed_data['Bridge evaluation 1: Safety Factor (min, higher better)'], errors='coerce').dropna()
        untimed_safety = pd.to_numeric(untimed_data['Bridge evaluation 1: Safety Factor (min, higher better)'], errors='coerce').dropna()
        
        if len(timed_safety) > 0 and len(untimed_safety) > 0:
            stat, p_value = stats.mannwhitneyu(timed_safety, untimed_safety, alternative='two-sided')
            print(f"Safety Factor - Timed: {timed_safety.mean():.2f}, Untimed: {untimed_safety.mean():.2f}, p={p_value:.3f}")
    
    # 1.3 Split Objectives (Roleplay) Effects
    print("\n1.3 Split Objectives (Roleplay) Effects") 
    print("-" * 50)
    
    roleplay_data = study_data[study_data['Variant'] == 'Roleplay']
    other_data = study_data[study_data['Variant'].isin(['Open Ended', 'Silent', 'Timed'])]
    
    print(f"Roleplay variant completion time: {roleplay_data['completion_time_min'].mean():.2f} ± {roleplay_data['completion_time_min'].std():.2f} min")
    print(f"Other variants completion time: {other_data['completion_time_min'].mean():.2f} ± {other_data['completion_time_min'].std():.2f} min")
    
    # Movement distance analysis (as proxy for coordination effort)
    roleplay_movement = roleplay_data[['Participant 1 distance (meter)', 'Participant 2 distance (meter)']].sum(axis=1)
    other_movement = other_data[['Participant 1 distance (meter)', 'Participant 2 distance (meter)']].sum(axis=1)
    
    stat, p_value = stats.mannwhitneyu(roleplay_movement, other_movement, alternative='two-sided')
    print(f"Total movement - Roleplay: {roleplay_movement.mean():.2f}m, Others: {other_movement.mean():.2f}m, p={p_value:.3f}")
    
    # Create visualization
    fig, axes = plt.subplots(2, 2, figsize=(15, 12))
    fig.suptitle('Task Variant Effects Analysis', fontsize=16, fontweight='bold')
    
    # Completion time by variant
    axes[0,0].boxplot([study_data[study_data['Variant']==v]['completion_time_min'].values for v in variants],
                      labels=variants)
    axes[0,0].set_title('Completion Time by Variant')
    axes[0,0].set_ylabel('Completion Time (minutes)')
    axes[0,0].tick_params(axis='x', rotation=45)
    
    # Communication effect
    communication_data = [silent_data['completion_time_min'].values, talking_data['completion_time_min'].values]
    axes[0,1].boxplot(communication_data, labels=['Silent', 'Talking\n(Open/Roleplay)'])
    axes[0,1].set_title('Communication vs Silent Conditions')
    axes[0,1].set_ylabel('Completion Time (minutes)')
    
    # Time pressure effect  
    pressure_data = [timed_data['completion_time_min'].values, untimed_data['completion_time_min'].values]
    axes[1,0].boxplot(pressure_data, labels=['Timed', 'Untimed'])
    axes[1,0].set_title('Time Pressure Effects')
    axes[1,0].set_ylabel('Completion Time (minutes)')
    
    # Movement coordination by variant
    movement_by_variant = []
    for variant in variants:
        variant_data = study_data[study_data['Variant'] == variant]
        total_movement = variant_data[['Participant 1 distance (meter)', 'Participant 2 distance (meter)']].sum(axis=1)
        movement_by_variant.append(total_movement.values)
    
    axes[1,1].boxplot(movement_by_variant, labels=variants)
    axes[1,1].set_title('Total Movement by Variant')
    axes[1,1].set_ylabel('Total Movement (meters)')
    axes[1,1].tick_params(axis='x', rotation=45)
    
    plt.tight_layout()
    plt.savefig('../assets/06/task_variant_effects_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.show()
    
    return {
        'communication_effect': (silent_data['completion_time_min'].mean(), talking_data['completion_time_min'].mean()),
        'time_pressure_effect': (timed_data['completion_time_min'].mean(), untimed_data['completion_time_min'].mean()),
        'roleplay_effect': (roleplay_data['completion_time_min'].mean(), other_data['completion_time_min'].mean())
    }

def analyze_individual_differences(study_data, participant_data):
    """
    2. Individual Differences and System Interaction
    Building on literature's emphasis on user characteristics
    """
    print("\n" + "="*60)
    print("2. INDIVIDUAL DIFFERENCES AND SYSTEM INTERACTION")
    print("="*60)
    
    # Prepare personality data 
    personality_traits = ['Agreeableness', 'Conscientiousness', 'Extraversion', 'Neuroticism', 'Openness']
    
    # Create participant performance summary
    performance_summary = []
    for idx, row in participant_data.iterrows():
        participant_id = row['Participant ID']
        
        # Get study performance for this participant
        p1_sessions = study_data[study_data['Participant 1 ID'] == participant_id]
        p2_sessions = study_data[study_data['Participant 2 ID'] == participant_id]
        
        # Calculate average completion time
        completion_times = []
        completion_times.extend(p1_sessions['completion_time_min'].tolist())
        completion_times.extend(p2_sessions['completion_time_min'].tolist())
        
        if completion_times:
            avg_completion_time = np.mean(completion_times)
            
            # Get SUS progression
            sus_scores = [row[f'SUS Score {i}'] for i in range(1,5) if pd.notna(row[f'SUS Score {i}'])]
            sus_progression = sus_scores[-1] - sus_scores[0] if len(sus_scores) >= 2 else 0
            
            # Experience level
            experience_map = {
                'No experience': 0,
                'Limited experience': 1, 
                'Moderate experience': 2,
                'Extensive experience': 3
            }
            ar_experience = experience_map.get(row['General Information - 5. How would you rate your experience with Augmented Reality (AR) and Virtual Reality (VR) applications?'], 0)
            
            performance_summary.append({
                'participant_id': participant_id,
                'avg_completion_time': avg_completion_time,
                'sus_progression': sus_progression,
                'ar_experience': ar_experience,
                'agreeableness': row['Agreeableness'],
                'conscientiousness': row['Conscientiousness'], 
                'extraversion': row['Extraversion'],
                'neuroticism': row['Neuroticism'],
                'openness': row['Openness'],
                'age': row['General Information - 2. Please state your age:'],
                'gender': row['General Information - 1. Please select your gender:']
            })
    
    performance_df = pd.DataFrame(performance_summary)
    
    # 2.1 Personality-Performance Relationships
    print("\n2.1 Personality-Performance Relationships")
    print("-" * 50)
    
    for trait in personality_traits:
        if trait.lower() in performance_df.columns:
            corr, p_value = spearmanr(performance_df[trait.lower()], performance_df['avg_completion_time'])
            print(f"{trait} vs Completion Time: r={corr:.3f}, p={p_value:.3f}")
    
    # 2.2 Experience and Learning Effects
    print("\n2.2 Experience and Learning Effects") 
    print("-" * 50)
    
    experience_levels = performance_df['ar_experience'].unique()
    for level in sorted(experience_levels):
        level_data = performance_df[performance_df['ar_experience'] == level]
        exp_labels = ['No', 'Limited', 'Moderate', 'Extensive']
        print(f"{exp_labels[level]} experience (n={len(level_data)}): "
              f"Avg completion time = {level_data['avg_completion_time'].mean():.2f} min, "
              f"SUS progression = {level_data['sus_progression'].mean():.2f}")
    
    # Experience vs performance correlation
    corr, p_value = spearmanr(performance_df['ar_experience'], performance_df['avg_completion_time'])
    print(f"AR Experience vs Completion Time: r={corr:.3f}, p={p_value:.3f}")
    
    corr, p_value = spearmanr(performance_df['ar_experience'], performance_df['sus_progression']) 
    print(f"AR Experience vs SUS Progression: r={corr:.3f}, p={p_value:.3f}")
    
    # 2.3 Age and Gender Effects
    print("\n2.3 Age and Gender Effects")
    print("-" * 50)
    
    corr, p_value = spearmanr(performance_df['age'], performance_df['avg_completion_time'])
    print(f"Age vs Completion Time: r={corr:.3f}, p={p_value:.3f}")
    
    gender_groups = performance_df.groupby('gender')['avg_completion_time'].agg(['mean', 'std', 'count'])
    print("Completion time by gender:")
    print(gender_groups)
    
    # Create visualization
    fig, axes = plt.subplots(2, 3, figsize=(18, 12))
    fig.suptitle('Individual Differences and System Interaction', fontsize=16, fontweight='bold')
    
    # Personality traits vs performance
    axes[0,0].scatter(performance_df['extraversion'], performance_df['avg_completion_time'], alpha=0.6)
    axes[0,0].set_xlabel('Extraversion Score')
    axes[0,0].set_ylabel('Avg Completion Time (min)')
    axes[0,0].set_title('Extraversion vs Performance')
    
    axes[0,1].scatter(performance_df['conscientiousness'], performance_df['avg_completion_time'], alpha=0.6)
    axes[0,1].set_xlabel('Conscientiousness Score') 
    axes[0,1].set_ylabel('Avg Completion Time (min)')
    axes[0,1].set_title('Conscientiousness vs Performance')
    
    # Experience effects
    exp_labels = ['No', 'Limited', 'Moderate', 'Extensive']
    experience_data = [performance_df[performance_df['ar_experience']==i]['avg_completion_time'].values 
                      for i in range(4) if len(performance_df[performance_df['ar_experience']==i]) > 0]
    exp_used_labels = [exp_labels[i] for i in range(4) if len(performance_df[performance_df['ar_experience']==i]) > 0]
    
    axes[0,2].boxplot(experience_data, labels=exp_used_labels)
    axes[0,2].set_title('AR Experience vs Performance')
    axes[0,2].set_ylabel('Avg Completion Time (min)')
    axes[0,2].tick_params(axis='x', rotation=45)
    
    # Learning progression
    axes[1,0].scatter(performance_df['ar_experience'], performance_df['sus_progression'], alpha=0.6)
    axes[1,0].set_xlabel('AR Experience Level')
    axes[1,0].set_ylabel('SUS Score Progression')
    axes[1,0].set_title('Experience vs Learning')
    
    # Age effects
    axes[1,1].scatter(performance_df['age'], performance_df['avg_completion_time'], alpha=0.6)
    axes[1,1].set_xlabel('Age')
    axes[1,1].set_ylabel('Avg Completion Time (min)')
    axes[1,1].set_title('Age vs Performance')
    
    # Gender comparison
    gender_data = [performance_df[performance_df['gender']==g]['avg_completion_time'].values 
                  for g in performance_df['gender'].unique()]
    axes[1,2].boxplot(gender_data, labels=performance_df['gender'].unique())
    axes[1,2].set_title('Gender vs Performance')
    axes[1,2].set_ylabel('Avg Completion Time (min)')
    
    plt.tight_layout()
    plt.savefig('../assets/06/individual_differences_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.show()
    
    return performance_df

def analyze_technical_architecture_impact(study_data, participant_data):
    """
    3. Technical Architecture Impact on User Experience
    Connecting to implementation decisions
    """
    print("\n" + "="*60)
    print("3. TECHNICAL ARCHITECTURE IMPACT ON USER EXPERIENCE")
    print("="*60)
    
    # 3.1 Spatial Alignment Quality and Collaboration Effectiveness
    print("\n3.1 Spatial Alignment and Collaboration Effectiveness")
    print("-" * 50)
    
    # Use movement correlation as proxy for spatial coordination success
    coordination_metrics = []
    for dyad_id in study_data['Participant 1 ID'].unique():
        dyad_sessions = study_data[study_data['Participant 1 ID'] == dyad_id]
        
        for _, session in dyad_sessions.iterrows():
            p1_dist = session['Participant 1 distance (meter)']
            p2_dist = session['Participant 2 distance (meter)']
            completion_time = session['completion_time_min']
            variant = session['Variant']
            
            # Calculate movement asymmetry as coordination metric
            total_movement = p1_dist + p2_dist
            movement_asymmetry = abs(p1_dist - p2_dist) / total_movement if total_movement > 0 else 0
            
            coordination_metrics.append({
                'dyad_id': dyad_id,
                'variant': variant,
                'completion_time': completion_time,
                'total_movement': total_movement,
                'movement_asymmetry': movement_asymmetry,
                'p1_distance': p1_dist,
                'p2_distance': p2_dist
            })
    
    coord_df = pd.DataFrame(coordination_metrics)
    
    # Analyze coordination effectiveness
    corr, p_value = spearmanr(coord_df['movement_asymmetry'], coord_df['completion_time'])
    print(f"Movement Asymmetry vs Completion Time: r={corr:.3f}, p={p_value:.3f}")
    
    # 3.2 UI Design Choices and Usability Ratings
    print("\n3.2 UI Design Choices and Usability Ratings")
    print("-" * 50)
    
    # Analyze SUS progression across sessions
    sus_data = []
    for idx, row in participant_data.iterrows():
        participant_id = row['Participant ID']
        for session in range(1, 5):
            sus_col = f'SUS Score {session}'
            if pd.notna(row[sus_col]):
                sus_data.append({
                    'participant_id': participant_id,
                    'session': session,
                    'sus_score': row[sus_col],
                    'ar_experience': row['General Information - 5. How would you rate your experience with Augmented Reality (AR) and Virtual Reality (VR) applications?']
                })
    
    sus_df = pd.DataFrame(sus_data)
    
    # Calculate learning effect in usability
    session_means = sus_df.groupby('session')['sus_score'].mean()
    print("SUS scores by session:")
    for session, mean_score in session_means.items():
        print(f"  Session {session}: {mean_score:.1f}")
    
    # Experience level effect on SUS progression
    experience_groups = sus_df.groupby('ar_experience')
    for exp_level, group in experience_groups:
        if len(group) > 0:
            progression = group.groupby('session')['sus_score'].mean()
            if len(progression) >= 2:
                improvement = progression.iloc[-1] - progression.iloc[0]
                print(f"{exp_level}: SUS improvement = {improvement:.1f}")
    
    # 3.3 Network Performance and Perceived Responsiveness  
    print("\n3.3 Network Performance and Perceived Responsiveness")
    print("-" * 50)
    
    # Use completion time variance within dyads as proxy for technical issues
    dyad_stability = []
    for dyad_id in study_data['Participant 1 ID'].unique():
        dyad_sessions = study_data[study_data['Participant 1 ID'] == dyad_id]
        completion_times = dyad_sessions['completion_time_min'].values
        
        if len(completion_times) >= 3:  # Need multiple sessions for variance calculation
            time_variance = np.var(completion_times)
            time_mean = np.mean(completion_times)
            cv = time_variance / time_mean if time_mean > 0 else 0  # Coefficient of variation
            
            dyad_stability.append({
                'dyad_id': dyad_id,
                'time_variance': time_variance,
                'time_cv': cv,
                'mean_completion_time': time_mean
            })
    
    stability_df = pd.DataFrame(dyad_stability)
    
    if len(stability_df) > 0:
        print(f"Mean coefficient of variation in completion times: {stability_df['time_cv'].mean():.3f}")
        print(f"Dyads with high variability (CV > 0.5): {len(stability_df[stability_df['time_cv'] > 0.5])}")
    
    # Create visualization
    fig, axes = plt.subplots(2, 2, figsize=(15, 12))
    fig.suptitle('Technical Architecture Impact on User Experience', fontsize=16, fontweight='bold')
    
    # Movement coordination analysis
    axes[0,0].scatter(coord_df['movement_asymmetry'], coord_df['completion_time'], alpha=0.6)
    axes[0,0].set_xlabel('Movement Asymmetry')
    axes[0,0].set_ylabel('Completion Time (min)')
    axes[0,0].set_title('Spatial Coordination vs Performance')
    
    # SUS progression over sessions
    sus_progression = sus_df.groupby('session')['sus_score'].agg(['mean', 'std']).reset_index()
    axes[0,1].errorbar(sus_progression['session'], sus_progression['mean'], 
                       yerr=sus_progression['std'], marker='o', capsize=5)
    axes[0,1].set_xlabel('Session Number')
    axes[0,1].set_ylabel('SUS Score')
    axes[0,1].set_title('Usability Progression Across Sessions')
    axes[0,1].set_ylim(0, 100)
    
    # Movement patterns by variant
    variant_movement = coord_df.groupby('variant')['total_movement'].apply(list)
    axes[1,0].boxplot([variant_movement[v] for v in ['Open Ended', 'Silent', 'Timed', 'Roleplay']], 
                      labels=['Open Ended', 'Silent', 'Timed', 'Roleplay'])
    axes[1,0].set_title('Movement Patterns by Variant')
    axes[1,0].set_ylabel('Total Movement (meters)')
    axes[1,0].tick_params(axis='x', rotation=45)
    
    # Technical stability analysis
    if len(stability_df) > 0:
        axes[1,1].scatter(stability_df['mean_completion_time'], stability_df['time_cv'], alpha=0.6)
        axes[1,1].set_xlabel('Mean Completion Time (min)')
        axes[1,1].set_ylabel('Coefficient of Variation')
        axes[1,1].set_title('Performance Consistency vs Mean Performance')
    else:
        axes[1,1].text(0.5, 0.5, 'Insufficient data\nfor stability analysis', 
                       ha='center', va='center', transform=axes[1,1].transAxes)
        axes[1,1].set_title('Technical Stability Analysis')
    
    plt.tight_layout()
    plt.savefig('../assets/06/technical_architecture_impact_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.show()
    
    return {
        'coordination_metrics': coord_df,
        'sus_progression': sus_df,
        'stability_metrics': stability_df
    }

def generate_summary_statistics(study_data, participant_data, results):
    """Generate summary statistics for the exploratory analysis"""
    print("\n" + "="*60)
    print("EXPLORATORY ANALYSIS SUMMARY")
    print("="*60)
    
    print("\nKey Findings:")
    print("-" * 30)
    
    # Task variant effects
    comm_effect = results[0]
    print(f"1. Communication Effect: Silent ({comm_effect['communication_effect'][0]:.2f} min) vs "
          f"Talking ({comm_effect['communication_effect'][1]:.2f} min)")
    
    print(f"2. Time Pressure Effect: Timed ({comm_effect['time_pressure_effect'][0]:.2f} min) vs "
          f"Untimed ({comm_effect['time_pressure_effect'][1]:.2f} min)")
    
    print(f"3. Roleplay Effect: Roleplay ({comm_effect['roleplay_effect'][0]:.2f} min) vs "
          f"Others ({comm_effect['roleplay_effect'][1]:.2f} min)")
    
    # Individual differences  
    performance_df = results[1]
    print(f"\n4. Sample characteristics: N={len(performance_df)} participants")
    print(f"   Age range: {performance_df['age'].min()}-{performance_df['age'].max()} years")
    print(f"   Gender distribution: {performance_df['gender'].value_counts().to_dict()}")
    
    # Technical impact
    tech_results = results[2]
    coord_df = tech_results['coordination_metrics']
    print(f"\n5. Movement coordination: Mean asymmetry = {coord_df['movement_asymmetry'].mean():.3f}")
    
    sus_df = tech_results['sus_progression']
    if len(sus_df) > 0:
        first_session = sus_df[sus_df['session'] == 1]['sus_score'].mean()
        last_session = sus_df[sus_df['session'] == 4]['sus_score'].mean()
        print(f"6. Usability progression: Session 1 = {first_session:.1f}, Session 4 = {last_session:.1f}")

def main():
    """Main analysis function"""
    print("Loading data...")
    study_data, participant_data = load_data()
    
    print(f"Loaded {len(study_data)} study sessions and {len(participant_data)} participants")
    
    # Run analyses
    task_results = analyze_task_variant_effects(study_data)
    individual_results = analyze_individual_differences(study_data, participant_data)  
    technical_results = analyze_technical_architecture_impact(study_data, participant_data)
    
    # Generate summary
    generate_summary_statistics(study_data, participant_data, 
                               [task_results, individual_results, technical_results])
    
    print("\nAnalysis complete! Check /assets/06/ for generated visualizations.")

if __name__ == "__main__":
    main() 