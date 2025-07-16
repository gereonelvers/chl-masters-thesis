#!/usr/bin/env python3
"""
Spatial Position Correlation Analysis

Analyzes actual spatial position correlations from movement logs to validate
whether movement distance correlations reflect genuine spatial coordination
or are artifacts of other factors.

Addresses methodological concerns:
1. "Leadership" vs "Movement Asymmetry" terminology  
2. Spatial coordination vs distance correlation
3. Evidence for coordination claims
"""

import pandas as pd
import numpy as np
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import seaborn as sns
from scipy.stats import pearsonr
import os
import re
from datetime import datetime
import warnings
warnings.filterwarnings('ignore')

def parse_position_log(filepath):
    """Parse a processed log file to extract position data"""
    positions = {'user_0': [], 'user_2': [], 'timestamps': []}
    
    try:
        with open(filepath, 'r') as f:
            for line in f:
                if '[PositionLogger]' in line:
                    # Extract user ID
                    if 'Id [0]' in line:
                        user_id = 'user_0'
                    elif 'Id [2]' in line:
                        user_id = 'user_2'
                    else:
                        continue
                    
                    # Extract position data
                    pos_match = re.search(r'P: \(([-\d.]+), ([-\d.]+), ([-\d.]+)\)', line)
                    if pos_match:
                        x, y, z = map(float, pos_match.groups())
                        
                        # Extract timestamp
                        timestamp_str = line.split(' - ')[0]
                        try:
                            timestamp = datetime.strptime(timestamp_str, '%Y-%m-%d %H:%M:%S')
                        except:
                            continue
                        
                        positions[user_id].append({'timestamp': timestamp, 'x': x, 'y': y, 'z': z})
    
    except Exception as e:
        print(f"Error processing {filepath}: {e}")
        return None
    
    return positions

def calculate_spatial_correlations(positions):
    """Calculate spatial position correlations between users"""
    if not positions or len(positions['user_0']) == 0 or len(positions['user_2']) == 0:
        return None
    
    # Convert to DataFrames
    df_0 = pd.DataFrame(positions['user_0'])
    df_2 = pd.DataFrame(positions['user_2'])
    
    # Align timestamps (match closest timestamps)
    correlations = {}
    
    try:
        # For simplicity, sample every 10th data point to reduce noise and computation
        df_0_sampled = df_0.iloc[::10].reset_index(drop=True)
        df_2_sampled = df_2.iloc[::10].reset_index(drop=True)
        
        # Take minimum length to ensure equal sample sizes
        min_len = min(len(df_0_sampled), len(df_2_sampled))
        if min_len < 10:  # Need minimum data points
            return None
        
        df_0_sampled = df_0_sampled.iloc[:min_len]
        df_2_sampled = df_2_sampled.iloc[:min_len]
        
        # Calculate correlations for each dimension
        for dim in ['x', 'y', 'z']:
            if np.var(df_0_sampled[dim]) > 0 and np.var(df_2_sampled[dim]) > 0:
                corr, p_val = pearsonr(df_0_sampled[dim], df_2_sampled[dim])
                correlations[f'{dim}_correlation'] = corr
                correlations[f'{dim}_p_value'] = p_val
            else:
                correlations[f'{dim}_correlation'] = 0
                correlations[f'{dim}_p_value'] = 1
        
        # Calculate overall 3D position correlation (magnitude)
        pos_0 = np.sqrt(df_0_sampled['x']**2 + df_0_sampled['y']**2 + df_0_sampled['z']**2)
        pos_2 = np.sqrt(df_2_sampled['x']**2 + df_2_sampled['y']**2 + df_2_sampled['z']**2)
        
        if np.var(pos_0) > 0 and np.var(pos_2) > 0:
            corr, p_val = pearsonr(pos_0, pos_2)
            correlations['magnitude_correlation'] = corr
            correlations['magnitude_p_value'] = p_val
        else:
            correlations['magnitude_correlation'] = 0
            correlations['magnitude_p_value'] = 1
        
        # Calculate movement patterns (change in position over time)
        if len(df_0_sampled) > 1:
            movement_0 = np.sqrt(np.diff(df_0_sampled['x'])**2 + 
                               np.diff(df_0_sampled['y'])**2 + 
                               np.diff(df_0_sampled['z'])**2)
            movement_2 = np.sqrt(np.diff(df_2_sampled['x'])**2 + 
                               np.diff(df_2_sampled['y'])**2 + 
                               np.diff(df_2_sampled['z'])**2)
            
            if np.var(movement_0) > 0 and np.var(movement_2) > 0:
                corr, p_val = pearsonr(movement_0, movement_2)
                correlations['movement_correlation'] = corr
                correlations['movement_p_value'] = p_val
            else:
                correlations['movement_correlation'] = 0
                correlations['movement_p_value'] = 1
        
        correlations['data_points'] = min_len
        return correlations
        
    except Exception as e:
        print(f"Error calculating correlations: {e}")
        return None

def load_run_mapping():
    """Load the mapping between run numbers and participant/variant data"""
    try:
        df = pd.read_csv('study-run-results.csv')
        
        # Rename columns for easier access
        df = df.rename(columns={
            'Run #': 'run_id',
            'Participant 1 ID': 'participant_1_id',
            'Participant 2 ID': 'participant_2_id', 
            'Variant': 'collaboration_variant',
            'Environment': 'environment',
            'Position in study': 'position',
            'Completion time (exact, minutes)': 'completion_time_minutes',
            'Participant 1 distance (meter)': 'p1_movement_distance',
            'Participant 2 distance (meter)': 'p2_movement_distance'
        })
        
        # Create a combined participant ID for dyad identification
        df['participant_id'] = df['participant_1_id'] + '_' + df['participant_2_id']
        
        # Convert numerical columns to numeric, handling any non-numeric values
        numeric_columns = ['completion_time_minutes', 'p1_movement_distance', 'p2_movement_distance', 'position']
        for col in numeric_columns:
            df[col] = pd.to_numeric(df[col], errors='coerce')
        
        return df
    except Exception as e:
        print(f"Error loading run mapping: {e}")
        return None

def analyze_all_spatial_correlations():
    """Analyze spatial correlations for all available processed logs"""
    
    # Load run mapping data
    run_mapping = load_run_mapping()
    if run_mapping is None:
        return None
    
    # Process all available log files
    log_dir = 'session_logs/processed_logs'
    spatial_results = []
    
    for filename in os.listdir(log_dir):
        if filename.startswith('run_') and filename.endswith('_processed.txt'):
            # Extract run number
            run_match = re.search(r'run_(\d+)_processed\.txt', filename)
            if not run_match:
                continue
                
            run_num = int(run_match.group(1))
            filepath = os.path.join(log_dir, filename)
            
            print(f"Processing {filename}...")
            
            # Parse position data
            positions = parse_position_log(filepath)
            if positions is None:
                continue
            
            # Calculate spatial correlations
            correlations = calculate_spatial_correlations(positions)
            if correlations is None:
                continue
            
            # Get run metadata
            run_data = run_mapping[run_mapping['run_id'] == run_num]
            if len(run_data) == 0:
                continue
            
            run_info = run_data.iloc[0]
            
            # Combine results
            result = {
                'run_id': run_num,
                'participant_id': run_info['participant_id'],
                'collaboration_variant': run_info['collaboration_variant'],
                'p1_movement_distance': run_info['p1_movement_distance'],
                'p2_movement_distance': run_info['p2_movement_distance'],
                **correlations
            }
            
            spatial_results.append(result)
    
    return pd.DataFrame(spatial_results)

def compare_distance_vs_spatial_correlations(spatial_df):
    """Compare movement distance correlations with spatial position correlations"""
    
    print("=== SPATIAL CORRELATION ANALYSIS ===\n")
    
    # Calculate movement distance correlations by dyad (like before)
    movement_correlations = {}
    
    for dyad_id in spatial_df['participant_id'].unique():
        dyad_data = spatial_df[spatial_df['participant_id'] == dyad_id]
        
        if len(dyad_data) < 2:
            continue
            
        p1_movement = dyad_data['p1_movement_distance'].values
        p2_movement = dyad_data['p2_movement_distance'].values
        
        if len(p1_movement) > 1 and np.var(p1_movement) > 0 and np.var(p2_movement) > 0:
            corr, p_val = pearsonr(p1_movement, p2_movement)
            movement_correlations[dyad_id] = {
                'movement_distance_correlation': corr,
                'movement_distance_p_value': p_val,
                'n_sessions': len(dyad_data)
            }
    
    # Aggregate spatial correlations by dyad
    spatial_correlations = {}
    
    for dyad_id in spatial_df['participant_id'].unique():
        dyad_data = spatial_df[spatial_df['participant_id'] == dyad_id]
        
        if len(dyad_data) < 2:
            continue
        
        # Average spatial correlations across sessions for this dyad
        spatial_correlations[dyad_id] = {
            'mean_x_correlation': dyad_data['x_correlation'].mean(),
            'mean_y_correlation': dyad_data['y_correlation'].mean(), 
            'mean_z_correlation': dyad_data['z_correlation'].mean(),
            'mean_magnitude_correlation': dyad_data['magnitude_correlation'].mean(),
            'mean_movement_correlation': dyad_data['movement_correlation'].mean(),
            'n_sessions': len(dyad_data)
        }
    
    # Combine and compare
    comparison_results = []
    
    for dyad_id in movement_correlations.keys():
        if dyad_id in spatial_correlations:
            result = {
                'dyad_id': dyad_id,
                **movement_correlations[dyad_id],
                **spatial_correlations[dyad_id]
            }
            comparison_results.append(result)
    
    comparison_df = pd.DataFrame(comparison_results)
    
    # Print results
    print("COMPARISON: Movement Distance vs Spatial Position Correlations")
    print("=" * 70)
    
    for _, row in comparison_df.iterrows():
        print(f"\nDyad {row['dyad_id']}:")
        print(f"  Movement Distance Correlation: r = {row['movement_distance_correlation']:.3f} (p = {row['movement_distance_p_value']:.3f})")
        print(f"  Spatial Position Correlations:")
        print(f"    X-coordinate: r = {row['mean_x_correlation']:.3f}")
        print(f"    Y-coordinate: r = {row['mean_y_correlation']:.3f}")
        print(f"    Z-coordinate: r = {row['mean_z_correlation']:.3f}")
        print(f"    3D Magnitude: r = {row['mean_magnitude_correlation']:.3f}")
        print(f"    Movement Pattern: r = {row['mean_movement_correlation']:.3f}")
    
    return comparison_df

def create_spatial_correlation_visualization(comparison_df):
    """Create visualizations comparing different types of correlations"""
    
    fig, axes = plt.subplots(2, 2, figsize=(14, 10))
    fig.suptitle('Movement Distance vs Spatial Position Correlations', fontsize=16, fontweight='bold')
    
    # 1. Movement distance vs X-coordinate correlation
    axes[0, 0].scatter(comparison_df['movement_distance_correlation'], 
                      comparison_df['mean_x_correlation'], alpha=0.7)
    axes[0, 0].set_xlabel('Movement Distance Correlation')
    axes[0, 0].set_ylabel('X-Coordinate Correlation')
    axes[0, 0].set_title('Distance vs X-Position Correlation')
    axes[0, 0].grid(True, alpha=0.3)
    
    # Add correlation coefficient
    if len(comparison_df) > 2:
        corr, p_val = pearsonr(comparison_df['movement_distance_correlation'], 
                              comparison_df['mean_x_correlation'])
        axes[0, 0].text(0.05, 0.95, f'r = {corr:.3f}, p = {p_val:.3f}', 
                       transform=axes[0, 0].transAxes, bbox=dict(boxstyle="round", facecolor='wheat'))
    
    # 2. Movement distance vs 3D magnitude correlation  
    axes[0, 1].scatter(comparison_df['movement_distance_correlation'], 
                      comparison_df['mean_magnitude_correlation'], alpha=0.7)
    axes[0, 1].set_xlabel('Movement Distance Correlation')
    axes[0, 1].set_ylabel('3D Position Magnitude Correlation')
    axes[0, 1].set_title('Distance vs 3D Position Correlation')
    axes[0, 1].grid(True, alpha=0.3)
    
    if len(comparison_df) > 2:
        corr, p_val = pearsonr(comparison_df['movement_distance_correlation'], 
                              comparison_df['mean_magnitude_correlation'])
        axes[0, 1].text(0.05, 0.95, f'r = {corr:.3f}, p = {p_val:.3f}', 
                       transform=axes[0, 1].transAxes, bbox=dict(boxstyle="round", facecolor='wheat'))
    
    # 3. Movement distance vs movement pattern correlation
    axes[1, 0].scatter(comparison_df['movement_distance_correlation'], 
                      comparison_df['mean_movement_correlation'], alpha=0.7)
    axes[1, 0].set_xlabel('Movement Distance Correlation')
    axes[1, 0].set_ylabel('Movement Pattern Correlation')
    axes[1, 0].set_title('Distance vs Movement Pattern Correlation')
    axes[1, 0].grid(True, alpha=0.3)
    
    if len(comparison_df) > 2:
        corr, p_val = pearsonr(comparison_df['movement_distance_correlation'], 
                              comparison_df['mean_movement_correlation'])
        axes[1, 0].text(0.05, 0.95, f'r = {corr:.3f}, p = {p_val:.3f}', 
                       transform=axes[1, 0].transAxes, bbox=dict(boxstyle="round", facecolor='wheat'))
    
    # 4. Dyad comparison chart
    dyad_names = [f"D{i+1}" for i in range(len(comparison_df))]
    x_pos = np.arange(len(dyad_names))
    
    width = 0.35
    axes[1, 1].bar(x_pos - width/2, comparison_df['movement_distance_correlation'], 
                  width, label='Movement Distance', alpha=0.7)
    axes[1, 1].bar(x_pos + width/2, comparison_df['mean_magnitude_correlation'], 
                  width, label='3D Position', alpha=0.7)
    
    axes[1, 1].set_xlabel('Dyad')
    axes[1, 1].set_ylabel('Correlation Coefficient')
    axes[1, 1].set_title('Correlation Comparison by Dyad')
    axes[1, 1].set_xticks(x_pos)
    axes[1, 1].set_xticklabels(dyad_names, rotation=45)
    axes[1, 1].legend()
    axes[1, 1].grid(True, alpha=0.3)
    
    plt.tight_layout()
    plt.savefig('../assets/06/spatial_correlation_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.savefig('../assets/06/spatial_correlation_analysis.png', dpi=300, bbox_inches='tight')
    print("Spatial correlation visualization saved to ../assets/06/spatial_correlation_analysis.pdf and .png")

def main():
    """Main analysis function"""
    print("=== SPATIAL POSITION CORRELATION ANALYSIS ===\n")
    print("Analyzing actual spatial position correlations from movement logs...")
    print("This addresses the question: Do movement distance correlations reflect")
    print("genuine spatial coordination or are they artifacts?\n")
    
    # Analyze spatial correlations from logs
    spatial_df = analyze_all_spatial_correlations()
    
    if spatial_df is None or len(spatial_df) == 0:
        print("No spatial correlation data could be extracted from logs.")
        return
    
    print(f"Successfully analyzed {len(spatial_df)} sessions from {spatial_df['participant_id'].nunique()} dyads.\n")
    
    # Compare distance vs spatial correlations
    comparison_df = compare_distance_vs_spatial_correlations(spatial_df)
    
    if len(comparison_df) > 0:
        create_spatial_correlation_visualization(comparison_df)
        
        print(f"\n=== SUMMARY ===")
        print(f"Analyzed {len(comparison_df)} dyads with both movement distance and spatial position data.")
        print(f"Key findings:")
        
        # Overall correlation between movement distance and spatial correlations
        if len(comparison_df) > 2:
            overall_corr, overall_p = pearsonr(comparison_df['movement_distance_correlation'], 
                                             comparison_df['mean_magnitude_correlation'])
            print(f"- Movement distance correlation vs 3D position correlation: r = {overall_corr:.3f}, p = {overall_p:.3f}")
            
            movement_corr, movement_p = pearsonr(comparison_df['movement_distance_correlation'], 
                                               comparison_df['mean_movement_correlation'])
            print(f"- Movement distance correlation vs movement pattern correlation: r = {movement_corr:.3f}, p = {movement_p:.3f}")
            
            if overall_corr > 0.5 and overall_p < 0.05:
                print("✓ Movement distance correlations appear to reflect genuine spatial coordination")
            elif overall_corr < 0.3 or overall_p > 0.1:
                print("⚠ Movement distance correlations may not reflect spatial coordination")
            else:
                print("? Mixed evidence for spatial coordination interpretation")
    
    return spatial_df, comparison_df

if __name__ == "__main__":
    main() 