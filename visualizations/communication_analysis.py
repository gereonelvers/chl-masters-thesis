#!/usr/bin/env python3
"""
Communication Patterns Analysis for AR Collaboration Study
Analyzes transcript data to extract communication metrics and patterns
"""

import json
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
import re
from pathlib import Path
from scipy import stats
from collections import defaultdict
import warnings
warnings.filterwarnings('ignore')

# Set up paths
BASE_PATH = Path("../user-study-analysis")
TRANSCRIPTS_PATH = BASE_PATH / "transcripts"
ASSETS_PATH = Path("../assets/06")
RESULTS_CSV = BASE_PATH / "study-run-results.csv"

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

def load_study_metadata():
    """Load study metadata from CSV file"""
    df = pd.read_csv(RESULTS_CSV)
    
    # Clean up variant names
    df['Variant_clean'] = df['Variant'].str.strip()
    
    # Create session mapping
    session_map = {}
    for idx, row in df.iterrows():
        session_map[row['Run #']] = {
            'variant': row['Variant_clean'],
            'participants': [row['Participant 1 ID'], row['Participant 2 ID']],
            'completion_time': row['Completion time (exact, minutes)'],
            'environment': row['Environment'],
            'position': row['Position in study']
        }
    
    return session_map

def load_transcript(session_id):
    """Load and parse transcript for a given session"""
    transcript_file = TRANSCRIPTS_PATH / f"{session_id}.json"
    
    if not transcript_file.exists():
        print(f"Warning: Transcript file {session_id}.json not found")
        return None
    
    try:
        with open(transcript_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        if not data:  # Empty transcript
            return None
            
        # Convert to list of utterances
        utterances = []
        for key, utterance in data.items():
            if isinstance(utterance, dict) and 'speaker' in utterance:
                utterances.append({
                    'speaker': utterance['speaker'],
                    'words': utterance['words'],
                    'start': float(utterance['start']) if 'start' in utterance else 0,
                    'end': float(utterance['end']) if 'end' in utterance else 0,
                    'duration': float(utterance['end']) - float(utterance['start']) if 'end' in utterance and 'start' in utterance else 0
                })
        
        return utterances
    except Exception as e:
        print(f"Error loading transcript {session_id}: {e}")
        return None

def calculate_basic_metrics(utterances, session_info):
    """Calculate basic communication metrics for a session"""
    if not utterances:
        return None
    
    # Filter out HOST utterances for participant analysis
    participant_utterances = [u for u in utterances if u['speaker'] != 'HOST']
    
    if not participant_utterances:
        return None
    
    total_duration = max([u['end'] for u in utterances]) if utterances else 0
    total_words = sum([len(u['words'].split()) for u in participant_utterances])
    total_turns = len(participant_utterances)
    
    # Per-participant metrics
    participants = session_info['participants']
    participant_metrics = {}
    
    for participant in participants:
        p_utterances = [u for u in participant_utterances if u['speaker'] == participant]
        participant_metrics[participant] = {
            'word_count': sum([len(u['words'].split()) for u in p_utterances]),
            'turn_count': len(p_utterances),
            'speaking_time': sum([u['duration'] for u in p_utterances]),
            'average_turn_length': np.mean([len(u['words'].split()) for u in p_utterances]) if p_utterances else 0
        }
    
    # Calculate turn-taking metrics
    turn_transitions = 0
    for i in range(1, len(participant_utterances)):
        if participant_utterances[i]['speaker'] != participant_utterances[i-1]['speaker']:
            turn_transitions += 1
    
    # Communication density
    words_per_minute = total_words / (total_duration / 60) if total_duration > 0 else 0
    
    return {
        'session_id': session_info.get('session_id'),
        'variant': session_info['variant'],
        'total_duration': total_duration,
        'total_words': total_words,
        'total_turns': total_turns,
        'turn_transitions': turn_transitions,
        'words_per_minute': words_per_minute,
        'turns_per_minute': total_turns / (total_duration / 60) if total_duration > 0 else 0,
        'participant_metrics': participant_metrics,
        'communication_balance': abs(participant_metrics[participants[0]]['word_count'] - 
                                   participant_metrics[participants[1]]['word_count']) / total_words if total_words > 0 else 0
    }

def analyze_deictic_references(utterances):
    """Analyze spatial and deictic references in communication"""
    if not utterances:
        return {}
    
    # Patterns for spatial/deictic references
    spatial_patterns = [
        r'\b(here|there|this|that)\b',
        r'\b(left|right|up|down|over|under)\b',
        r'\b(next to|beside|behind|in front)\b',
        r'\b(put|place|move|go)\s+(it|this|that|there|here)\b',
        r'\b(on|in|at|to|from)\s+(the|this|that)\b'
    ]
    
    participant_utterances = [u for u in utterances if u['speaker'] != 'HOST']
    total_words = sum([len(u['words'].split()) for u in participant_utterances])
    
    deictic_count = 0
    spatial_count = 0
    
    for utterance in participant_utterances:
        text = utterance['words'].lower()
        
        # Count deictic references
        for pattern in spatial_patterns[:2]:  # this/that, here/there, directional
            deictic_count += len(re.findall(pattern, text))
        
        # Count spatial references
        for pattern in spatial_patterns[2:]:  # placement and positioning
            spatial_count += len(re.findall(pattern, text))
    
    return {
        'deictic_references': deictic_count,
        'spatial_references': spatial_count,
        'deictic_density': deictic_count / total_words if total_words > 0 else 0,
        'spatial_density': spatial_count / total_words if total_words > 0 else 0
    }

def analyze_planning_vs_execution(utterances):
    """Analyze communication phases: planning vs execution"""
    if not utterances:
        return {}
    
    # Patterns for planning-related communication
    planning_patterns = [
        r'\b(plan|strategy|should|let\'s|how about|what if|we could)\b',
        r'\b(first|then|next|after|before)\b',
        r'\b(idea|think|suggest|propose)\b'
    ]
    
    # Patterns for execution-related communication
    execution_patterns = [
        r'\b(put|place|move|grab|take|drop)\b',
        r'\b(there|here|now|wait|good|done)\b',
        r'\b(yes|no|ok|okay|right|wrong)\b'
    ]
    
    participant_utterances = [u for u in utterances if u['speaker'] != 'HOST']
    
    planning_count = 0
    execution_count = 0
    
    for utterance in participant_utterances:
        text = utterance['words'].lower()
        
        # Count planning language
        for pattern in planning_patterns:
            planning_count += len(re.findall(pattern, text))
        
        # Count execution language
        for pattern in execution_patterns:
            execution_count += len(re.findall(pattern, text))
    
    total_coded = planning_count + execution_count
    
    return {
        'planning_utterances': planning_count,
        'execution_utterances': execution_count,
        'planning_ratio': planning_count / total_coded if total_coded > 0 else 0,
        'execution_ratio': execution_count / total_coded if total_coded > 0 else 0
    }

def analyze_all_sessions():
    """Analyze all sessions and compile results"""
    session_map = load_study_metadata()
    results = []
    
    print("Analyzing transcripts...")
    
    for session_id, session_info in session_map.items():
        print(f"Processing session {session_id} ({session_info['variant']})")
        
        utterances = load_transcript(session_id)
        if utterances is None:
            print(f"  No transcript data for session {session_id}")
            continue
        
        # Add session_id to session_info for metrics
        session_info_with_id = session_info.copy()
        session_info_with_id['session_id'] = session_id
        
        # Calculate metrics
        basic_metrics = calculate_basic_metrics(utterances, session_info_with_id)
        if basic_metrics is None:
            continue
            
        deictic_metrics = analyze_deictic_references(utterances)
        planning_metrics = analyze_planning_vs_execution(utterances)
        
        # Combine all metrics
        combined_metrics = {**basic_metrics, **deictic_metrics, **planning_metrics}
        results.append(combined_metrics)
    
    return pd.DataFrame(results)

def create_communication_visualizations(df):
    """Create comprehensive communication pattern visualizations"""
    
    # Set up the figure with subplots
    fig = plt.figure(figsize=(20, 24))
    
    # 1. Word count by variant
    plt.subplot(4, 3, 1)
    variant_order = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    df_filtered = df[df['variant'].isin(variant_order)]
    
    sns.boxplot(data=df_filtered, x='variant', y='total_words', order=variant_order)
    plt.title('Total Word Count by Collaboration Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Total Words')
    plt.xticks(rotation=45)
    
    # 2. Communication density (words per minute)
    plt.subplot(4, 3, 2)
    sns.boxplot(data=df_filtered, x='variant', y='words_per_minute', order=variant_order)
    plt.title('Communication Density by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Words per Minute')
    plt.xticks(rotation=45)
    
    # 3. Turn-taking frequency
    plt.subplot(4, 3, 3)
    sns.boxplot(data=df_filtered, x='variant', y='turns_per_minute', order=variant_order)
    plt.title('Turn-Taking Frequency by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Turns per Minute')
    plt.xticks(rotation=45)
    
    # 4. Communication balance
    plt.subplot(4, 3, 4)
    sns.boxplot(data=df_filtered, x='variant', y='communication_balance', order=variant_order)
    plt.title('Communication Balance by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Communication Imbalance (0=perfect balance)')
    plt.xticks(rotation=45)
    
    # 5. Deictic reference density
    plt.subplot(4, 3, 5)
    sns.boxplot(data=df_filtered, x='variant', y='deictic_density', order=variant_order)
    plt.title('Deictic Reference Density by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Deictic References per Word')
    plt.xticks(rotation=45)
    
    # 6. Spatial reference density
    plt.subplot(4, 3, 6)
    sns.boxplot(data=df_filtered, x='variant', y='spatial_density', order=variant_order)
    plt.title('Spatial Reference Density by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Spatial References per Word')
    plt.xticks(rotation=45)
    
    # 7. Planning vs execution ratio
    plt.subplot(4, 3, 7)
    sns.boxplot(data=df_filtered, x='variant', y='planning_ratio', order=variant_order)
    plt.title('Planning Communication Ratio by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Planning/(Planning+Execution) Ratio')
    plt.xticks(rotation=45)
    
    # 8. Total communication duration vs completion time
    plt.subplot(4, 3, 8)
    # Load completion times from study results
    study_df = pd.read_csv(RESULTS_CSV)
    merged_df = df.merge(study_df[['Run #', 'Completion time (exact, minutes)']], 
                        left_on='session_id', right_on='Run #', how='left')
    
    # Parse completion time (format like "9:43")
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
    
    merged_df['completion_minutes'] = merged_df['Completion time (exact, minutes)'].apply(parse_time)
    merged_df = merged_df.dropna(subset=['completion_minutes'])
    
    plt.scatter(merged_df['completion_minutes'], merged_df['total_words'], alpha=0.6)
    plt.xlabel('Task Completion Time (minutes)')
    plt.ylabel('Total Words')
    plt.title('Communication Volume vs Task Duration')
    
    # Add correlation
    if len(merged_df) > 2:
        r, p = stats.pearsonr(merged_df['completion_minutes'], merged_df['total_words'])
        plt.text(0.05, 0.95, f'r = {r:.3f}, p = {p:.3f}', transform=plt.gca().transAxes)
    
    # 9. Communication patterns by variant (stacked bar)
    plt.subplot(4, 3, 9)
    
    # Calculate means for stacked bar chart
    comm_patterns = df_filtered.groupby('variant').agg({
        'planning_ratio': 'mean',
        'execution_ratio': 'mean'
    }).reset_index()
    
    x = np.arange(len(variant_order))
    width = 0.6
    
    planning_means = [comm_patterns[comm_patterns['variant'] == v]['planning_ratio'].iloc[0] 
                     if v in comm_patterns['variant'].values else 0 for v in variant_order]
    execution_means = [comm_patterns[comm_patterns['variant'] == v]['execution_ratio'].iloc[0] 
                      if v in comm_patterns['variant'].values else 0 for v in variant_order]
    
    plt.bar(x, planning_means, width, label='Planning', alpha=0.8)
    plt.bar(x, execution_means, width, bottom=planning_means, label='Execution', alpha=0.8)
    
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Proportion of Communication')
    plt.title('Planning vs Execution Communication')
    plt.xticks(x, variant_order, rotation=45)
    plt.legend()
    
    # 10. Turn transitions by variant
    plt.subplot(4, 3, 10)
    sns.boxplot(data=df_filtered, x='variant', y='turn_transitions', order=variant_order)
    plt.title('Turn Transitions by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Number of Turn Transitions')
    plt.xticks(rotation=45)
    
    # 11. Words per turn by variant
    plt.subplot(4, 3, 11)
    df_filtered['words_per_turn'] = df_filtered['total_words'] / df_filtered['total_turns']
    sns.boxplot(data=df_filtered, x='variant', y='words_per_turn', order=variant_order)
    plt.title('Average Words per Turn by Variant')
    plt.xlabel('Collaboration Variant')
    plt.ylabel('Words per Turn')
    plt.xticks(rotation=45)
    
    # 12. Communication efficiency (words per minute of completion time)
    plt.subplot(4, 3, 12)
    if 'completion_minutes' in merged_df.columns:
        merged_df['communication_efficiency'] = merged_df['total_words'] / merged_df['completion_minutes']
        merged_filtered = merged_df[merged_df['variant'].isin(variant_order)]
        sns.boxplot(data=merged_filtered, x='variant', y='communication_efficiency', order=variant_order)
        plt.title('Communication Efficiency by Variant')
        plt.xlabel('Collaboration Variant')
        plt.ylabel('Words per Completion Minute')
        plt.xticks(rotation=45)
    
    plt.tight_layout()
    plt.savefig(ASSETS_PATH / 'communication_patterns_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.close()
    
    print(f"Communication patterns visualization saved to {ASSETS_PATH / 'communication_patterns_analysis.pdf'}")

def create_detailed_communication_tables(df):
    """Create detailed statistical tables for communication analysis"""
    
    # Descriptive statistics by variant
    variant_order = ['Open Ended', 'Roleplay', 'Silent', 'Timed']
    df_filtered = df[df['variant'].isin(variant_order)]
    
    # Communication metrics summary
    comm_metrics = ['total_words', 'words_per_minute', 'turns_per_minute', 
                   'communication_balance', 'deictic_density', 'spatial_density', 
                   'planning_ratio', 'turn_transitions']
    
    summary_stats = df_filtered.groupby('variant')[comm_metrics].agg(['count', 'mean', 'std']).round(3)
    
    # Statistical tests (Friedman test for repeated measures)
    from scipy.stats import friedmanchisquare
    
    statistical_results = {}
    
    # Group by session pairs to test for repeated measures
    # We need to identify which sessions belong to the same dyads
    session_groups = {}
    for idx, row in df_filtered.iterrows():
        session_id = row['session_id']
        # Group sessions by sets of 4 (assuming each dyad does 4 variants)
        group_id = session_id // 4
        if group_id not in session_groups:
            session_groups[group_id] = {}
        session_groups[group_id][row['variant']] = row
    
    # Perform Friedman tests for metrics that have data across all variants
    for metric in comm_metrics:
        variant_data = {variant: [] for variant in variant_order}
        
        for group_id, group_sessions in session_groups.items():
            if all(variant in group_sessions for variant in variant_order):
                for variant in variant_order:
                    variant_data[variant].append(group_sessions[variant][metric])
        
        # Only test if we have complete data for all variants
        if all(len(variant_data[variant]) > 0 for variant in variant_order):
            try:
                stat, p_value = friedmanchisquare(*[variant_data[variant] for variant in variant_order])
                statistical_results[metric] = {'statistic': stat, 'p_value': p_value}
            except:
                statistical_results[metric] = {'statistic': None, 'p_value': None}
    
    return summary_stats, statistical_results

def main():
    """Main analysis function"""
    print("Starting communication pattern analysis...")
    
    # Create output directory if it doesn't exist
    ASSETS_PATH.mkdir(exist_ok=True)
    
    # Analyze all sessions
    df = analyze_all_sessions()
    
    if df.empty:
        print("No data to analyze!")
        return
    
    print(f"Analyzed {len(df)} sessions with communication data")
    print(f"Variants analyzed: {df['variant'].unique()}")
    
    # Create visualizations
    create_communication_visualizations(df)
    
    # Create statistical tables
    summary_stats, statistical_results = create_detailed_communication_tables(df)
    
    # Save raw data
    df.to_csv(BASE_PATH / 'communication_analysis_results.csv', index=False)
    
    # Print summary statistics
    print("\n=== COMMUNICATION PATTERNS SUMMARY ===")
    print("\nDescriptive Statistics by Variant:")
    print(summary_stats)
    
    print("\nStatistical Test Results (Friedman Tests):")
    for metric, result in statistical_results.items():
        if result['p_value'] is not None:
            significance = "***" if result['p_value'] < 0.001 else "**" if result['p_value'] < 0.01 else "*" if result['p_value'] < 0.05 else "ns"
            print(f"{metric}: χ² = {result['statistic']:.3f}, p = {result['p_value']:.3f} {significance}")
        else:
            print(f"{metric}: Unable to compute")
    
    print(f"\nResults saved to: {ASSETS_PATH / 'communication_patterns_analysis.pdf'}")
    print(f"Raw data saved to: {BASE_PATH / 'communication_analysis_results.csv'}")

if __name__ == "__main__":
    main() 