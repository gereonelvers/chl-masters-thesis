import json
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from pathlib import Path
import re
from collections import defaultdict, Counter
import warnings
warnings.filterwarnings('ignore')

# Set up plotting style
plt.style.use('default')
sns.set_palette("husl")

class TranscriptAnalyzer:
    def __init__(self, transcript_dir, results_csv):
        self.transcript_dir = Path(transcript_dir)
        self.results_df = pd.read_csv(results_csv)
        self.transcript_data = {}
        self.analysis_results = {}
        
    def load_transcripts(self):
        """Load all transcript JSON files"""
        print("Loading transcripts...")
        for run_num in range(32):  # Based on the CSV having 32 runs
            transcript_file = self.transcript_dir / f"{run_num}.json"
            if transcript_file.exists():
                try:
                    with open(transcript_file, 'r', encoding='utf-8') as f:
                        data = json.load(f)
                        self.transcript_data[run_num] = data
                        print(f"Loaded run {run_num}: {len(data)} utterances")
                except (json.JSONDecodeError, UnicodeDecodeError) as e:
                    print(f"Error loading run {run_num}: {e}")
                    self.transcript_data[run_num] = {}
            else:
                print(f"No transcript file for run {run_num}")
                self.transcript_data[run_num] = {}
    
    def analyze_basic_communication_stats(self):
        """Analyze basic communication statistics for each run"""
        stats = []
        
        for run_num, transcript in self.transcript_data.items():
            if not transcript:  # Empty transcript (Silent condition)
                stats.append({
                    'run': run_num,
                    'total_utterances': 0,
                    'total_words': 0,
                    'total_duration': 0,
                    'unique_speakers': 0,
                    'words_per_minute': 0,
                    'avg_utterance_length': 0,
                    'participant_1_words': 0,
                    'participant_2_words': 0,
                    'participant_1_utterances': 0,
                    'participant_2_utterances': 0,
                    'host_words': 0,
                    'host_utterances': 0
                })
                continue
            
            # Get run info from CSV
            run_info = self.results_df.iloc[run_num] if run_num < len(self.results_df) else None
            
            # Basic counts
            total_utterances = len(transcript)
            total_words = sum(len(utterance.get('words', '').split()) for utterance in transcript.values())
            
            # Duration analysis
            if transcript:
                end_times = [utterance.get('end', 0) for utterance in transcript.values() if utterance.get('end')]
                total_duration = max(end_times) if end_times else 0
            else:
                total_duration = 0
            
            # Speaker analysis
            speakers = [utterance.get('speaker', 'unknown') for utterance in transcript.values()]
            unique_speakers = len(set(speakers))
            speaker_counts = Counter(speakers)
            
            # Get participant IDs from CSV
            if run_info is not None:
                p1_id = run_info['Participant 1 ID']
                p2_id = run_info['Participant 2 ID']
            else:
                p1_id = p2_id = None
            
            # Count words and utterances by speaker
            participant_1_words = participant_1_utterances = 0
            participant_2_words = participant_2_utterances = 0
            host_words = host_utterances = 0
            
            for utterance in transcript.values():
                speaker = utterance.get('speaker', 'unknown')
                words = len(utterance.get('words', '').split())
                
                if speaker == 'HOST':
                    host_words += words
                    host_utterances += 1
                elif speaker == p1_id or (isinstance(speaker, (int, str)) and str(speaker) in ['0', p1_id]):
                    participant_1_words += words
                    participant_1_utterances += 1
                elif speaker == p2_id or (isinstance(speaker, (int, str)) and str(speaker) in ['1', p2_id]):
                    participant_2_words += words
                    participant_2_utterances += 1
            
            stats.append({
                'run': run_num,
                'total_utterances': total_utterances,
                'total_words': total_words,
                'total_duration': total_duration,
                'unique_speakers': unique_speakers,
                'words_per_minute': (total_words / total_duration * 60) if total_duration > 0 else 0,
                'avg_utterance_length': total_words / total_utterances if total_utterances > 0 else 0,
                'participant_1_words': participant_1_words,
                'participant_2_words': participant_2_words,
                'participant_1_utterances': participant_1_utterances,
                'participant_2_utterances': participant_2_utterances,
                'host_words': host_words,
                'host_utterances': host_utterances
            })
        
        return pd.DataFrame(stats)
    
    def analyze_by_variant(self, stats_df):
        """Analyze communication patterns by task variant"""
        # Merge with results data
        merged_df = pd.merge(stats_df, self.results_df[['Variant', 'Completion time (exact, minutes)']], 
                           left_on='run', right_index=True, how='left')
        
        # Parse completion time
        merged_df['completion_minutes'] = merged_df['Completion time (exact, minutes)'].apply(
            lambda x: float(x.split(':')[0]) + float(x.split(':')[1])/60 if isinstance(x, str) and ':' in x else np.nan
        )
        
        variant_analysis = merged_df.groupby('Variant').agg({
            'total_utterances': ['mean', 'std', 'count'],
            'total_words': ['mean', 'std'],
            'words_per_minute': ['mean', 'std'],
            'completion_minutes': ['mean', 'std'],
            'participant_1_words': ['mean', 'std'],
            'participant_2_words': ['mean', 'std']
        }).round(2)
        
        return variant_analysis, merged_df
    
    def analyze_speaker_balance(self, stats_df):
        """Analyze balance of communication between participants"""
        stats_df['total_participant_words'] = stats_df['participant_1_words'] + stats_df['participant_2_words']
        
        # Calculate speaking balance (0.5 = perfectly balanced, closer to 0 or 1 = imbalanced)
        stats_df['speaking_balance'] = np.where(
            stats_df['total_participant_words'] > 0,
            stats_df['participant_1_words'] / stats_df['total_participant_words'],
            0.5
        )
        
        # Calculate absolute deviation from perfect balance
        stats_df['balance_deviation'] = abs(stats_df['speaking_balance'] - 0.5)
        
        return stats_df
    
    def analyze_temporal_patterns(self):
        """Analyze when people speak during sessions"""
        temporal_data = []
        
        for run_num, transcript in self.transcript_data.items():
            if not transcript:
                continue
                
            run_info = self.results_df.iloc[run_num] if run_num < len(self.results_df) else None
            if run_info is None:
                continue
                
            p1_id = run_info['Participant 1 ID']
            p2_id = run_info['Participant 2 ID']
            variant = run_info['Variant']
            
            # Get session duration
            end_times = [utterance.get('end', 0) for utterance in transcript.values() if utterance.get('end')]
            session_duration = max(end_times) if end_times else 0
            
            if session_duration == 0:
                continue
            
            # Divide session into quartiles and count words in each
            for quarter in range(4):
                quarter_start = (quarter * session_duration) / 4
                quarter_end = ((quarter + 1) * session_duration) / 4
                
                quarter_words = {'p1': 0, 'p2': 0, 'host': 0}
                
                for utterance in transcript.values():
                    start_time = utterance.get('start', 0)
                    speaker = utterance.get('speaker', 'unknown')
                    words = len(utterance.get('words', '').split())
                    
                    if quarter_start <= start_time < quarter_end:
                        if speaker == p1_id or str(speaker) == '0':
                            quarter_words['p1'] += words
                        elif speaker == p2_id or str(speaker) == '1':
                            quarter_words['p2'] += words
                        elif speaker == 'HOST':
                            quarter_words['host'] += words
                
                temporal_data.append({
                    'run': run_num,
                    'variant': variant,
                    'quarter': quarter + 1,
                    'p1_words': quarter_words['p1'],
                    'p2_words': quarter_words['p2'],
                    'host_words': quarter_words['host'],
                    'total_words': sum(quarter_words.values())
                })
        
        return pd.DataFrame(temporal_data)
    
    def analyze_content_themes(self):
        """Analyze what participants talk about"""
        themes = {
            'building': ['build', 'place', 'put', 'move', 'position', 'block', 'cube', 'plank'],
            'planning': ['plan', 'strategy', 'idea', 'think', 'should', 'could', 'would', 'maybe'],
            'coordination': ['here', 'there', 'this', 'that', 'left', 'right', 'side', 'middle'],
            'evaluation': ['good', 'bad', 'better', 'worse', 'price', 'cost', 'strong', 'stable'],
            'problems': ['problem', 'issue', 'wrong', 'error', 'stuck', 'difficult', 'help'],
            'agreement': ['yes', 'okay', 'right', 'sure', 'agree', 'no', 'wait']
        }
        
        content_analysis = []
        
        for run_num, transcript in self.transcript_data.items():
            if not transcript:
                continue
                
            run_info = self.results_df.iloc[run_num] if run_num < len(self.results_df) else None
            if run_info is None:
                continue
            
            variant = run_info['Variant']
            all_words = []
            
            for utterance in transcript.values():
                words = utterance.get('words', '').lower().split()
                all_words.extend(words)
            
            total_words = len(all_words)
            theme_counts = {}
            
            for theme, keywords in themes.items():
                count = sum(1 for word in all_words if any(keyword in word for keyword in keywords))
                theme_counts[theme] = count / total_words if total_words > 0 else 0
            
            theme_counts['run'] = run_num
            theme_counts['variant'] = variant
            theme_counts['total_words'] = total_words
            content_analysis.append(theme_counts)
        
        return pd.DataFrame(content_analysis)
    
    def generate_visualizations(self, stats_df, variant_analysis_df, temporal_df, content_df):
        """Generate comprehensive visualizations"""
        fig, axes = plt.subplots(3, 3, figsize=(20, 15))
        fig.suptitle('Transcript Communication Analysis', fontsize=16, fontweight='bold')
        
        # 1. Words per minute by variant
        variant_summary = variant_analysis_df.groupby('Variant').agg({
            'words_per_minute': 'mean',
            'total_words': 'mean'
        }).reset_index()
        
        sns.barplot(data=variant_summary, x='Variant', y='words_per_minute', ax=axes[0,0])
        axes[0,0].set_title('Communication Rate by Variant')
        axes[0,0].set_ylabel('Words per Minute')
        axes[0,0].tick_params(axis='x', rotation=45)
        
        # 2. Total words by variant
        sns.barplot(data=variant_summary, x='Variant', y='total_words', ax=axes[0,1])
        axes[0,1].set_title('Total Communication by Variant')
        axes[0,1].set_ylabel('Total Words')
        axes[0,1].tick_params(axis='x', rotation=45)
        
        # 3. Speaking balance distribution
        sns.histplot(data=stats_df[stats_df['total_participant_words'] > 0], 
                    x='speaking_balance', bins=20, ax=axes[0,2])
        axes[0,2].set_title('Speaking Balance Distribution')
        axes[0,2].set_xlabel('Participant 1 Speaking Ratio')
        axes[0,2].axvline(x=0.5, color='red', linestyle='--', label='Perfect Balance')
        axes[0,2].legend()
        
        # 4. Communication vs completion time
        valid_data = variant_analysis_df[variant_analysis_df['completion_minutes'].notna()]
        sns.scatterplot(data=valid_data, x='total_words', y='completion_minutes', 
                       hue='Variant', ax=axes[1,0])
        axes[1,0].set_title('Communication vs Task Duration')
        axes[1,0].set_xlabel('Total Words')
        axes[1,0].set_ylabel('Completion Time (minutes)')
        
        # 5. Temporal patterns
        if not temporal_df.empty:
            temporal_summary = temporal_df.groupby(['variant', 'quarter'])['total_words'].mean().reset_index()
            sns.lineplot(data=temporal_summary, x='quarter', y='total_words', hue='variant', ax=axes[1,1])
            axes[1,1].set_title('Communication Patterns Over Time')
            axes[1,1].set_xlabel('Session Quarter')
            axes[1,1].set_ylabel('Average Words')
        
        # 6. Content themes by variant
        if not content_df.empty:
            theme_cols = ['building', 'planning', 'coordination', 'evaluation', 'problems', 'agreement']
            theme_data = content_df.groupby('variant')[theme_cols].mean()
            theme_data.plot(kind='bar', ax=axes[1,2])
            axes[1,2].set_title('Communication Themes by Variant')
            axes[1,2].set_ylabel('Relative Frequency')
            axes[1,2].tick_params(axis='x', rotation=45)
            axes[1,2].legend(bbox_to_anchor=(1.05, 1), loc='upper left')
        
        # 7. Utterance length distribution
        sns.boxplot(data=variant_analysis_df, x='Variant', y='avg_utterance_length', ax=axes[2,0])
        axes[2,0].set_title('Utterance Length by Variant')
        axes[2,0].set_ylabel('Average Words per Utterance')
        axes[2,0].tick_params(axis='x', rotation=45)
        
        # 8. Speaker participation by variant
        participant_data = variant_analysis_df.melt(
            id_vars=['Variant'], 
            value_vars=['participant_1_words', 'participant_2_words'],
            var_name='participant', value_name='words'
        )
        sns.boxplot(data=participant_data, x='Variant', y='words', hue='participant', ax=axes[2,1])
        axes[2,1].set_title('Participant Communication by Variant')
        axes[2,1].set_ylabel('Words Spoken')
        axes[2,1].tick_params(axis='x', rotation=45)
        
        # 9. Communication efficiency
        valid_data = variant_analysis_df[variant_analysis_df['completion_minutes'].notna()]
        valid_data['efficiency'] = valid_data['total_words'] / valid_data['completion_minutes']
        sns.boxplot(data=valid_data, x='Variant', y='efficiency', ax=axes[2,2])
        axes[2,2].set_title('Communication Efficiency by Variant')
        axes[2,2].set_ylabel('Words per Minute of Task')
        axes[2,2].tick_params(axis='x', rotation=45)
        
        plt.tight_layout()
        plt.savefig('transcript_analysis_comprehensive.pdf', dpi=300, bbox_inches='tight')
        plt.show()
    
    def generate_summary_report(self, stats_df, variant_analysis, merged_df, content_df):
        """Generate a comprehensive summary report"""
        print("=== TRANSCRIPT ANALYSIS SUMMARY REPORT ===\n")
        
        print("1. OVERALL COMMUNICATION STATISTICS")
        print("-" * 40)
        print(f"Total runs analyzed: {len(stats_df)}")
        print(f"Runs with communication: {(stats_df['total_words'] > 0).sum()}")
        print(f"Silent runs (no transcripts): {(stats_df['total_words'] == 0).sum()}")
        print(f"Average words per session: {stats_df['total_words'].mean():.1f}")
        print(f"Average communication rate: {stats_df['words_per_minute'].mean():.1f} words/min")
        print()
        
        print("2. COMMUNICATION BY VARIANT")
        print("-" * 40)
        variant_summary = merged_df.groupby('Variant').agg({
            'total_words': ['mean', 'std', 'count'],
            'words_per_minute': ['mean', 'std'],
            'total_utterances': ['mean', 'std']
        }).round(2)
        
        for variant in merged_df['Variant'].unique():
            if pd.isna(variant):
                continue
            variant_data = merged_df[merged_df['Variant'] == variant]
            print(f"\n{variant} Variant:")
            print(f"  Sessions: {len(variant_data)}")
            print(f"  Avg total words: {variant_data['total_words'].mean():.1f} ± {variant_data['total_words'].std():.1f}")
            print(f"  Avg words/min: {variant_data['words_per_minute'].mean():.1f} ± {variant_data['words_per_minute'].std():.1f}")
            print(f"  Avg utterances: {variant_data['total_utterances'].mean():.1f} ± {variant_data['total_utterances'].std():.1f}")
        
        print("\n3. SPEAKING BALANCE ANALYSIS")
        print("-" * 40)
        balanced_sessions = stats_df[
            (stats_df['total_participant_words'] > 0) & 
            (stats_df['balance_deviation'] < 0.1)
        ]
        print(f"Well-balanced sessions (deviation < 0.1): {len(balanced_sessions)}/{(stats_df['total_participant_words'] > 0).sum()}")
        print(f"Average balance deviation: {stats_df['balance_deviation'].mean():.3f}")
        print(f"Most imbalanced session: Run {stats_df.loc[stats_df['balance_deviation'].idxmax(), 'run']} (deviation: {stats_df['balance_deviation'].max():.3f})")
        
        print("\n4. CONTENT ANALYSIS")
        print("-" * 40)
        if not content_df.empty:
            theme_cols = ['building', 'planning', 'coordination', 'evaluation', 'problems', 'agreement']
            overall_themes = content_df[theme_cols].mean()
            print("Average theme frequencies:")
            for theme in theme_cols:
                print(f"  {theme.capitalize()}: {overall_themes[theme]:.3f}")
        
        print("\n5. NOTABLE FINDINGS")
        print("-" * 40)
        
        # Silent condition analysis
        silent_runs = merged_df[merged_df['Variant'] == 'Silent']['total_words'].sum()
        print(f"Silent condition total words: {silent_runs} (confirming constraint compliance)")
        
        # Longest and shortest communication sessions
        if len(stats_df[stats_df['total_words'] > 0]) > 0:
            max_words_run = stats_df.loc[stats_df['total_words'].idxmax()]
            min_words_run = stats_df[stats_df['total_words'] > 0].loc[stats_df[stats_df['total_words'] > 0]['total_words'].idxmin()]
            
            print(f"Most talkative session: Run {max_words_run['run']} ({max_words_run['total_words']} words)")
            print(f"Least talkative session: Run {min_words_run['run']} ({min_words_run['total_words']} words)")
        
        # Communication efficiency
        efficient_sessions = merged_df[merged_df['words_per_minute'] > merged_df['words_per_minute'].quantile(0.75)]
        print(f"High-efficiency communication sessions: {len(efficient_sessions)} runs (>75th percentile)")
        
        return variant_summary
    
    def run_analysis(self):
        """Run the complete analysis pipeline"""
        print("Starting transcript analysis...")
        
        # Load data
        self.load_transcripts()
        
        # Basic statistics
        stats_df = self.analyze_basic_communication_stats()
        stats_df = self.analyze_speaker_balance(stats_df)
        
        # Variant analysis
        variant_analysis, merged_df = self.analyze_by_variant(stats_df)
        
        # Temporal patterns
        temporal_df = self.analyze_temporal_patterns()
        
        # Content analysis
        content_df = self.analyze_content_themes()
        
        # Generate visualizations
        self.generate_visualizations(stats_df, merged_df, temporal_df, content_df)
        
        # Generate report
        variant_summary = self.generate_summary_report(stats_df, variant_analysis, merged_df, content_df)
        
        # Save detailed results
        stats_df.to_csv('transcript_communication_stats.csv', index=False)
        merged_df.to_csv('transcript_variant_analysis.csv', index=False)
        temporal_df.to_csv('transcript_temporal_patterns.csv', index=False)
        content_df.to_csv('transcript_content_analysis.csv', index=False)
        
        print(f"\nAnalysis complete! Results saved to CSV files and transcript_analysis_comprehensive.pdf")
        
        return {
            'stats': stats_df,
            'variant_analysis': merged_df,
            'temporal': temporal_df,
            'content': content_df,
            'summary': variant_summary
        }

if __name__ == "__main__":
    analyzer = TranscriptAnalyzer(
        transcript_dir="transcripts/",
        results_csv="study-run-results.csv"
    )
    
    results = analyzer.run_analysis() 