#!/usr/bin/env python3
"""
Interview Cross-Validation Analysis
===================================

This script performs cross-validation between individual and paired interview responses
to assess consistency and potential social desirability bias or partner influence.

The analysis compares:
1. Overall experience assessments
2. Task difficulty ratings
3. Frustration/enjoyment reports
4. Performance self-assessments
5. Technical challenge descriptions
6. Collaboration effectiveness ratings
"""

import re
import pandas as pd
from pathlib import Path
from typing import Dict, List, Tuple, Optional
import matplotlib.pyplot as plt
import seaborn as sns
from collections import defaultdict
import numpy as np

class InterviewCrossValidator:
    def __init__(self, transcript_dir: str = "transcripts/"):
        self.transcript_dir = Path(transcript_dir)
        self.individual_interviews = {}
        self.paired_discussions = {}
        self.validation_results = {}
        
    def load_interview_data(self):
        """Load all individual and paired interview transcripts"""
        print("Loading interview transcripts...")
        
        # Load individual interviews
        individual_files = list(self.transcript_dir.glob("*.md"))
        individual_files = [f for f in individual_files if '-' not in f.stem]  # No hyphen = individual
        
        for file_path in individual_files:
            participant_id = file_path.stem
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    self.individual_interviews[participant_id] = content
                    print(f"Loaded individual interview: {participant_id}")
            except Exception as e:
                print(f"Error loading {file_path}: {e}")
        
        # Load paired discussions
        paired_files = list(self.transcript_dir.glob("*-*.md"))  # Hyphen = paired
        
        for file_path in paired_files:
            dyad_id = file_path.stem
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                    self.paired_discussions[dyad_id] = content
                    print(f"Loaded paired discussion: {dyad_id}")
            except Exception as e:
                print(f"Error loading {file_path}: {e}")
        
        print(f"Loaded {len(self.individual_interviews)} individual interviews")
        print(f"Loaded {len(self.paired_discussions)} paired discussions")
        
    def extract_themes(self, text: str) -> Dict[str, List[str]]:
        """Extract key themes and sentiment from interview text"""
        # Convert to lowercase for analysis
        text_lower = text.lower()
        
        themes = {
            'overall_experience': [],
            'frustrations': [],
            'enjoyments': [],
            'technical_issues': [],
            'collaboration_quality': [],
            'performance_assessment': [],
            'variant_preferences': []
        }
        
        # Extract quotes related to overall experience
        experience_patterns = [
            r'overall experience.*?(?:\.|\n)',
            r'it was.*?fun.*?(?:\.|\n)',
            r'it was.*?good.*?(?:\.|\n)',
            r'it was.*?interesting.*?(?:\.|\n)',
            r'i.*?enjoyed.*?(?:\.|\n)',
            r'i.*?liked.*?(?:\.|\n)'
        ]
        
        for pattern in experience_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['overall_experience'].extend(matches)
        
        # Extract frustrations
        frustration_patterns = [
            r'frustrated.*?(?:\.|\n)',
            r'annoying.*?(?:\.|\n)',
            r'difficult.*?(?:\.|\n)',
            r'problem.*?(?:\.|\n)',
            r'didn\'t work.*?(?:\.|\n)',
            r'challenging.*?(?:\.|\n)'
        ]
        
        for pattern in frustration_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['frustrations'].extend(matches)
        
        # Extract enjoyments
        enjoyment_patterns = [
            r'enjoyed.*?(?:\.|\n)',
            r'fun.*?(?:\.|\n)',
            r'liked.*?(?:\.|\n)',
            r'satisfying.*?(?:\.|\n)',
            r'smooth.*?(?:\.|\n)'
        ]
        
        for pattern in enjoyment_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['enjoyments'].extend(matches)
        
        # Extract technical issues
        technical_patterns = [
            r'blocks.*?(?:\.|\n)',
            r'system.*?(?:\.|\n)',
            r'technical.*?(?:\.|\n)',
            r'align.*?(?:\.|\n)',
            r'calibrat.*?(?:\.|\n)',
            r'hololens.*?(?:\.|\n)'
        ]
        
        for pattern in technical_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['technical_issues'].extend(matches)
        
        # Extract collaboration assessment
        collaboration_patterns = [
            r'communication.*?(?:\.|\n)',
            r'partner.*?(?:\.|\n)',
            r'collaboration.*?(?:\.|\n)',
            r'work.*?together.*?(?:\.|\n)',
            r'team.*?(?:\.|\n)'
        ]
        
        for pattern in collaboration_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['collaboration_quality'].extend(matches)
        
        # Extract performance assessment
        performance_patterns = [
            r'performance.*?(?:\.|\n)',
            r'did.*?well.*?(?:\.|\n)',
            r'good.*?job.*?(?:\.|\n)',
            r'satisfied.*?(?:\.|\n)'
        ]
        
        for pattern in performance_patterns:
            matches = re.findall(pattern, text_lower, re.DOTALL)
            themes['performance_assessment'].extend(matches)
        
        return themes
    
    def calculate_sentiment_score(self, themes: Dict[str, List[str]]) -> Dict[str, float]:
        """Calculate basic sentiment scores for extracted themes"""
        positive_words = ['good', 'great', 'fun', 'easy', 'smooth', 'well', 'nice', 'satisfied', 'enjoyed', 'liked']
        negative_words = ['frustrated', 'difficult', 'problem', 'annoying', 'challenging', 'bad', 'wrong', 'hard']
        
        sentiment_scores = {}
        
        for theme, quotes in themes.items():
            if not quotes:
                sentiment_scores[theme] = 0.0
                continue
                
            text = ' '.join(quotes).lower()
            positive_count = sum(1 for word in positive_words if word in text)
            negative_count = sum(1 for word in negative_words if word in text)
            
            # Normalized sentiment score (-1 to 1)
            total_words = len(text.split())
            if total_words > 0:
                sentiment_scores[theme] = (positive_count - negative_count) / max(total_words, 1)
            else:
                sentiment_scores[theme] = 0.0
                
        return sentiment_scores
    
    def compare_individual_vs_paired(self) -> Dict[str, Dict]:
        """Compare themes between individual interviews and paired discussions"""
        comparison_results = {}
        
        # Map paired discussions to individual participants
        dyad_mappings = {}
        for dyad_id in self.paired_discussions.keys():
            participants = dyad_id.split('-')
            if len(participants) == 2:
                dyad_mappings[dyad_id] = participants
        
        for dyad_id, participants in dyad_mappings.items():
            p1_id, p2_id = participants
            
            # Skip if we don't have individual interviews for both participants
            if p1_id not in self.individual_interviews or p2_id not in self.individual_interviews:
                continue
            
            # Extract themes from individual interviews
            p1_individual_themes = self.extract_themes(self.individual_interviews[p1_id])
            p2_individual_themes = self.extract_themes(self.individual_interviews[p2_id])
            
            # Extract themes from paired discussion
            paired_themes = self.extract_themes(self.paired_discussions[dyad_id])
            
            # Calculate sentiment scores
            p1_individual_sentiment = self.calculate_sentiment_score(p1_individual_themes)
            p2_individual_sentiment = self.calculate_sentiment_score(p2_individual_themes)
            paired_sentiment = self.calculate_sentiment_score(paired_themes)
            
            # Store comparison results
            comparison_results[dyad_id] = {
                'participants': [p1_id, p2_id],
                'p1_individual_sentiment': p1_individual_sentiment,
                'p2_individual_sentiment': p2_individual_sentiment,
                'paired_sentiment': paired_sentiment,
                'p1_individual_themes': p1_individual_themes,
                'p2_individual_themes': p2_individual_themes,
                'paired_themes': paired_themes
            }
        
        return comparison_results
    
    def calculate_consistency_metrics(self, comparison_results: Dict) -> Dict[str, float]:
        """Calculate quantitative consistency metrics"""
        consistency_metrics = {
            'sentiment_correlation_p1': [],
            'sentiment_correlation_p2': [],
            'theme_overlap_p1': [],
            'theme_overlap_p2': [],
            'overall_consistency_score': []
        }
        
        theme_categories = ['overall_experience', 'frustrations', 'enjoyments', 'collaboration_quality']
        
        for dyad_id, results in comparison_results.items():
            p1_individual = results['p1_individual_sentiment']
            p2_individual = results['p2_individual_sentiment']
            paired = results['paired_sentiment']
            
            # Calculate sentiment correlations for each participant
            p1_individual_scores = [p1_individual.get(theme, 0) for theme in theme_categories]
            p2_individual_scores = [p2_individual.get(theme, 0) for theme in theme_categories]
            paired_scores = [paired.get(theme, 0) for theme in theme_categories]
            
            # Correlation between individual and paired sentiment
            if np.var(p1_individual_scores) > 0 and np.var(paired_scores) > 0:
                p1_corr = np.corrcoef(p1_individual_scores, paired_scores)[0, 1]
                if not np.isnan(p1_corr):
                    consistency_metrics['sentiment_correlation_p1'].append(p1_corr)
            
            if np.var(p2_individual_scores) > 0 and np.var(paired_scores) > 0:
                p2_corr = np.corrcoef(p2_individual_scores, paired_scores)[0, 1]
                if not np.isnan(p2_corr):
                    consistency_metrics['sentiment_correlation_p2'].append(p2_corr)
            
            # Calculate theme overlap (Jaccard similarity)
            for participant, individual_themes in [('p1', results['p1_individual_themes']), 
                                                  ('p2', results['p2_individual_themes'])]:
                individual_words = set()
                paired_words = set()
                
                for theme in theme_categories:
                    individual_text = ' '.join(individual_themes.get(theme, [])).lower()
                    paired_text = ' '.join(results['paired_themes'].get(theme, [])).lower()
                    
                    individual_words.update(individual_text.split())
                    paired_words.update(paired_text.split())
                
                if individual_words or paired_words:
                    jaccard = len(individual_words & paired_words) / len(individual_words | paired_words)
                    consistency_metrics[f'theme_overlap_{participant}'].append(jaccard)
        
        # Calculate overall consistency score
        all_correlations = (consistency_metrics['sentiment_correlation_p1'] + 
                          consistency_metrics['sentiment_correlation_p2'])
        all_overlaps = (consistency_metrics['theme_overlap_p1'] + 
                       consistency_metrics['theme_overlap_p2'])
        
        if all_correlations and all_overlaps:
            overall_score = (np.mean(all_correlations) + np.mean(all_overlaps)) / 2
            consistency_metrics['overall_consistency_score'] = overall_score
        else:
            consistency_metrics['overall_consistency_score'] = 0.0
        
        # Convert lists to summary statistics
        summary_metrics = {}
        for metric, values in consistency_metrics.items():
            if isinstance(values, list) and values:
                summary_metrics[metric] = {
                    'mean': np.mean(values),
                    'std': np.std(values),
                    'min': np.min(values),
                    'max': np.max(values),
                    'n': len(values)
                }
            else:
                summary_metrics[metric] = values
                
        return summary_metrics
    
    def generate_consistency_report(self, comparison_results: Dict, consistency_metrics: Dict):
        """Generate a comprehensive consistency analysis report"""
        print("\n" + "="*60)
        print("INTERVIEW CROSS-VALIDATION ANALYSIS REPORT")
        print("="*60)
        
        print(f"\nAnalyzed {len(comparison_results)} dyads with complete interview data")
        
        print("\n1. QUANTITATIVE CONSISTENCY METRICS")
        print("-" * 40)
        
        # Sentiment correlation analysis
        if 'sentiment_correlation_p1' in consistency_metrics:
            p1_corr = consistency_metrics['sentiment_correlation_p1']
            p2_corr = consistency_metrics['sentiment_correlation_p2']
            
            print(f"Individual vs Paired Sentiment Correlations:")
            if p1_corr['n'] > 0:
                print(f"  Participant 1: r = {p1_corr['mean']:.3f} ± {p1_corr['std']:.3f} (n={p1_corr['n']})")
            if p2_corr['n'] > 0:
                print(f"  Participant 2: r = {p2_corr['mean']:.3f} ± {p2_corr['std']:.3f} (n={p2_corr['n']})")
        
        # Theme overlap analysis
        if 'theme_overlap_p1' in consistency_metrics:
            p1_overlap = consistency_metrics['theme_overlap_p1']
            p2_overlap = consistency_metrics['theme_overlap_p2']
            
            print(f"\nTheme Overlap (Jaccard Similarity):")
            if p1_overlap['n'] > 0:
                print(f"  Participant 1: {p1_overlap['mean']:.3f} ± {p1_overlap['std']:.3f} (n={p1_overlap['n']})")
            if p2_overlap['n'] > 0:
                print(f"  Participant 2: {p2_overlap['mean']:.3f} ± {p2_overlap['std']:.3f} (n={p2_overlap['n']})")
        
        # Overall consistency
        overall_score = consistency_metrics.get('overall_consistency_score', 0)
        print(f"\nOverall Consistency Score: {overall_score:.3f}")
        
        print("\n2. QUALITATIVE CONSISTENCY ASSESSMENT")
        print("-" * 40)
        
        # Analyze specific examples of consistency/inconsistency
        high_consistency_dyads = []
        low_consistency_dyads = []
        
        for dyad_id, results in comparison_results.items():
            # Calculate dyad-specific consistency
            p1_sentiment = results['p1_individual_sentiment']
            p2_sentiment = results['p2_individual_sentiment']
            paired_sentiment = results['paired_sentiment']
            
            # Simple consistency check based on overall experience sentiment
            p1_exp = p1_sentiment.get('overall_experience', 0)
            p2_exp = p2_sentiment.get('overall_experience', 0)
            paired_exp = paired_sentiment.get('overall_experience', 0)
            
            individual_avg = (p1_exp + p2_exp) / 2
            consistency = 1 - abs(individual_avg - paired_exp)
            
            if consistency > 0.8:
                high_consistency_dyads.append((dyad_id, consistency))
            elif consistency < 0.5:
                low_consistency_dyads.append((dyad_id, consistency))
        
        print(f"High consistency dyads (>0.8): {len(high_consistency_dyads)}")
        print(f"Low consistency dyads (<0.5): {len(low_consistency_dyads)}")
        
        print("\n3. INTERPRETATION")
        print("-" * 40)
        
        if overall_score > 0.7:
            interpretation = "HIGH consistency between individual and paired responses."
        elif overall_score > 0.5:
            interpretation = "MODERATE consistency between individual and paired responses."
        else:
            interpretation = "LOW consistency between individual and paired responses."
        
        print(f"Result: {interpretation}")
        
        # Statistical significance assessment
        all_sentiment_corrs = []
        if 'sentiment_correlation_p1' in consistency_metrics and consistency_metrics['sentiment_correlation_p1']['n'] > 0:
            all_sentiment_corrs.extend([consistency_metrics['sentiment_correlation_p1']['mean']] * consistency_metrics['sentiment_correlation_p1']['n'])
        if 'sentiment_correlation_p2' in consistency_metrics and consistency_metrics['sentiment_correlation_p2']['n'] > 0:
            all_sentiment_corrs.extend([consistency_metrics['sentiment_correlation_p2']['mean']] * consistency_metrics['sentiment_correlation_p2']['n'])
        
        if all_sentiment_corrs:
            avg_correlation = np.mean(all_sentiment_corrs)
            if avg_correlation > 0.5:
                print(f"Strong positive correlation (r = {avg_correlation:.3f}) suggests participants provided")
                print("consistent accounts regardless of interview context.")
            elif avg_correlation > 0.3:
                print(f"Moderate positive correlation (r = {avg_correlation:.3f}) suggests generally")
                print("consistent responses with some variation.")
            else:
                print(f"Weak correlation (r = {avg_correlation:.3f}) may indicate social desirability")
                print("bias or partner influence in paired discussions.")
        
        return {
            'overall_score': overall_score,
            'interpretation': interpretation,
            'high_consistency_dyads': high_consistency_dyads,
            'low_consistency_dyads': low_consistency_dyads
        }
    
    def run_cross_validation(self):
        """Run the complete cross-validation analysis"""
        print("Starting interview cross-validation analysis...")
        
        # Load data
        self.load_interview_data()
        
        if not self.individual_interviews or not self.paired_discussions:
            print("Insufficient interview data for cross-validation analysis.")
            return None
        
        # Compare individual vs paired responses
        comparison_results = self.compare_individual_vs_paired()
        
        if not comparison_results:
            print("No matching individual and paired interviews found.")
            return None
        
        # Calculate consistency metrics
        consistency_metrics = self.calculate_consistency_metrics(comparison_results)
        
        # Generate report
        report = self.generate_consistency_report(comparison_results, consistency_metrics)
        
        # Save results
        results = {
            'comparison_results': comparison_results,
            'consistency_metrics': consistency_metrics,
            'report': report
        }
        
        return results

if __name__ == "__main__":
    validator = InterviewCrossValidator()
    results = validator.run_cross_validation()
    
    if results:
        print("\nCross-validation analysis completed successfully.")
        print(f"Results indicate: {results['report']['interpretation']}")
    else:
        print("\nCross-validation analysis failed - insufficient data.") 