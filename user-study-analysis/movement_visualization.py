#!/usr/bin/env python3
"""
Movement Pattern Visualization

Creates visualizations of actual movement patterns to clarify what movement
correlations represent and distinguish between spatial positioning and 
collaborative effectiveness.
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

def parse_position_log_for_visualization(filepath, max_points=1000):
    """Parse a processed log file to extract position data for visualization"""
    positions = {'user_0': [], 'user_2': []}
    
    try:
        with open(filepath, 'r') as f:
            lines = f.readlines()
            
        # Sample lines to avoid overwhelming visualizations
        step = max(1, len(lines) // max_points)
        
        for i, line in enumerate(lines[::step]):
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
                    
                    positions[user_id].append({
                        'timestamp': timestamp, 
                        'x': x, 'y': y, 'z': z,
                        'time_idx': i
                    })
    
    except Exception as e:
        print(f"Error processing {filepath}: {e}")
        return None
    
    return positions

def create_movement_pattern_visualizations():
    """Create visualizations showing what movement correlations actually represent"""
    
    # Load run mapping data
    try:
        df = pd.read_csv('study-run-results.csv')
        df = df.rename(columns={
            'Run #': 'run_id',
            'Participant 1 ID': 'participant_1_id',
            'Participant 2 ID': 'participant_2_id',
            'Variant': 'collaboration_variant'
        })
        df['participant_id'] = df['participant_1_id'] + '_' + df['participant_2_id']
    except Exception as e:
        print(f"Error loading run data: {e}")
        return
    
    # Select example runs: high correlation, low correlation, and medium correlation
    example_runs = [
        {'run': 0, 'title': 'High Distance Correlation Example', 'dyad': 'SAKl2Kyg_jFYQhuSp'},
        {'run': 28, 'title': 'Low Distance Correlation Example', 'dyad': 'j7hHkgiC_iN9X5S4Q'},
        {'run': 8, 'title': 'Medium Distance Correlation Example', 'dyad': 'YeUp7E4D_WzyEiaaj'}
    ]
    
    fig, axes = plt.subplots(3, 3, figsize=(18, 15))
    fig.suptitle('Movement Pattern Analysis: What Do Movement Correlations Actually Show?', 
                 fontsize=16, fontweight='bold')
    
    for i, example in enumerate(example_runs):
        run_id = example['run']
        log_file = f'session_logs/processed_logs/run_{run_id}_processed.txt'
        
        if not os.path.exists(log_file):
            continue
            
        # Parse position data
        positions = parse_position_log_for_visualization(log_file)
        if not positions or len(positions['user_0']) == 0 or len(positions['user_2']) == 0:
            continue
        
        # Convert to DataFrames
        df_0 = pd.DataFrame(positions['user_0'])
        df_2 = pd.DataFrame(positions['user_2'])
        
        # Align by taking minimum length
        min_len = min(len(df_0), len(df_2))
        df_0 = df_0.iloc[:min_len].reset_index(drop=True)
        df_2 = df_2.iloc[:min_len].reset_index(drop=True)
        
        if len(df_0) < 10:
            continue
        
        # Get run info
        run_info = df[df['run_id'] == run_id]
        if len(run_info) == 0:
            variant = "Unknown"
            p1_dist = 0
            p2_dist = 0
        else:
            variant = run_info.iloc[0]['collaboration_variant']
            p1_dist = pd.to_numeric(run_info.iloc[0]['Participant 1 distance (meter)'], errors='coerce')
            p2_dist = pd.to_numeric(run_info.iloc[0]['Participant 2 distance (meter)'], errors='coerce')
        
        # Calculate distance correlation
        if np.var([p1_dist, p2_dist]) > 0:
            dist_corr = 1.0  # Perfect correlation for single point
        else:
            dist_corr = np.nan
        
        # 1. X-Y position plot (bird's eye view)
        axes[i, 0].scatter(df_0['x'], df_0['z'], alpha=0.6, s=20, label='User 0', c=range(len(df_0)), cmap='Blues')
        axes[i, 0].scatter(df_2['x'], df_2['z'], alpha=0.6, s=20, label='User 2', c=range(len(df_2)), cmap='Reds')
        axes[i, 0].set_xlabel('X Position (m)')
        axes[i, 0].set_ylabel('Z Position (m)')
        axes[i, 0].set_title(f'{example["title"]}\nBird\'s Eye View - {variant}')
        axes[i, 0].legend()
        axes[i, 0].grid(True, alpha=0.3)
        axes[i, 0].axis('equal')
        
        # 2. Position over time
        time_indices = range(len(df_0))
        axes[i, 1].plot(time_indices, np.sqrt(df_0['x']**2 + df_0['z']**2), 
                       label=f'User 0 (total: {p1_dist:.1f}m)', alpha=0.7)
        axes[i, 1].plot(time_indices, np.sqrt(df_2['x']**2 + df_2['z']**2), 
                       label=f'User 2 (total: {p2_dist:.1f}m)', alpha=0.7)
        axes[i, 1].set_xlabel('Time Index')
        axes[i, 1].set_ylabel('Distance from Origin (m)')
        axes[i, 1].set_title(f'Position Magnitude Over Time\nMovement Distance Correlation')
        axes[i, 1].legend()
        axes[i, 1].grid(True, alpha=0.3)
        
        # 3. Movement velocity over time
        if len(df_0) > 1:
            vel_0 = np.sqrt(np.diff(df_0['x'])**2 + np.diff(df_0['z'])**2)
            vel_2 = np.sqrt(np.diff(df_2['x'])**2 + np.diff(df_2['z'])**2)
            
            axes[i, 2].plot(range(len(vel_0)), vel_0, label='User 0 velocity', alpha=0.7)
            axes[i, 2].plot(range(len(vel_2)), vel_2, label='User 2 velocity', alpha=0.7)
            axes[i, 2].set_xlabel('Time Index')
            axes[i, 2].set_ylabel('Movement Velocity')
            axes[i, 2].set_title('Movement Patterns Over Time')
            axes[i, 2].legend()
            axes[i, 2].grid(True, alpha=0.3)
        
        # Add correlation info as text
        axes[i, 1].text(0.02, 0.98, f'Total distances:\nU0: {p1_dist:.1f}m, U2: {p2_dist:.1f}m', 
                       transform=axes[i, 1].transAxes, verticalalignment='top',
                       bbox=dict(boxstyle="round", facecolor='wheat', alpha=0.8))
    
    plt.tight_layout()
    plt.savefig('../assets/06/movement_pattern_examples.pdf', dpi=300, bbox_inches='tight')
    plt.savefig('../assets/06/movement_pattern_examples.png', dpi=300, bbox_inches='tight')
    print("Movement pattern examples saved to ../assets/06/movement_pattern_examples.pdf and .png")

def create_collaboration_vs_spatial_analysis():
    """Create visualization distinguishing collaboration from spatial co-location"""
    
    fig, axes = plt.subplots(2, 2, figsize=(14, 10))
    fig.suptitle('Spatial Positioning vs Collaborative Effectiveness: Different Concepts', 
                 fontsize=16, fontweight='bold')
    
    # Create hypothetical examples to illustrate the concepts
    
    # Example 1: High spatial correlation, effective collaboration
    t = np.linspace(0, 10, 100)
    pos1_x = np.sin(t) + np.random.normal(0, 0.1, 100)
    pos1_z = np.cos(t) + np.random.normal(0, 0.1, 100)
    pos2_x = np.sin(t) + 0.5 + np.random.normal(0, 0.1, 100)  # Similar pattern, offset
    pos2_z = np.cos(t) + 0.5 + np.random.normal(0, 0.1, 100)
    
    axes[0, 0].scatter(pos1_x, pos1_z, alpha=0.6, label='User 1', s=20)
    axes[0, 0].scatter(pos2_x, pos2_z, alpha=0.6, label='User 2', s=20)
    axes[0, 0].set_title('High Spatial Correlation\n(Moving together, similar areas)')
    axes[0, 0].legend()
    axes[0, 0].grid(True, alpha=0.3)
    axes[0, 0].set_xlabel('X Position')
    axes[0, 0].set_ylabel('Z Position')
    
    # Example 2: Low spatial correlation, effective collaboration
    pos3_x = np.linspace(-2, -1, 100) + np.random.normal(0, 0.1, 100)  # User 1 on left
    pos3_z = np.random.normal(0, 0.3, 100)
    pos4_x = np.linspace(1, 2, 100) + np.random.normal(0, 0.1, 100)    # User 2 on right
    pos4_z = np.random.normal(0, 0.3, 100)
    
    axes[0, 1].scatter(pos3_x, pos3_z, alpha=0.6, label='User 1 (left side)', s=20)
    axes[0, 1].scatter(pos4_x, pos4_z, alpha=0.6, label='User 2 (right side)', s=20)
    axes[0, 1].set_title('Low Spatial Correlation\n(Working on different areas)')
    axes[0, 1].legend()
    axes[0, 1].grid(True, alpha=0.3)
    axes[0, 1].set_xlabel('X Position')
    axes[0, 1].set_ylabel('Z Position')
    
    # Example 3: Collaboration patterns over time
    axes[1, 0].text(0.1, 0.8, 'Effective Collaboration Patterns:', fontsize=14, fontweight='bold')
    axes[1, 0].text(0.1, 0.7, '• Complementary work areas', fontsize=12)
    axes[1, 0].text(0.1, 0.6, '• Turn-taking in shared spaces', fontsize=12)
    axes[1, 0].text(0.1, 0.5, '• Coordinated material gathering', fontsize=12)
    axes[1, 0].text(0.1, 0.4, '• Following/leading behaviors', fontsize=12)
    axes[1, 0].text(0.1, 0.3, '• Synchronized task phases', fontsize=12)
    
    axes[1, 0].text(0.1, 0.15, 'Spatial positioning correlation alone\ncannot capture these patterns', 
                   fontsize=11, style='italic', color='red')
    axes[1, 0].set_xlim(0, 1)
    axes[1, 0].set_ylim(0, 1)
    axes[1, 0].axis('off')
    
    # Example 4: What we actually measured
    axes[1, 1].text(0.1, 0.8, 'What Our Analysis Measured:', fontsize=14, fontweight='bold')
    axes[1, 1].text(0.1, 0.7, '✓ Total movement distance similarity', fontsize=12, color='green')
    axes[1, 1].text(0.1, 0.6, '✓ Position coordinate correlations', fontsize=12, color='green')
    axes[1, 1].text(0.1, 0.5, '✓ Movement timing patterns', fontsize=12, color='green')
    
    axes[1, 1].text(0.1, 0.35, 'What We Did Not Measure:', fontsize=14, fontweight='bold')
    axes[1, 1].text(0.1, 0.25, '✗ Task-relevant coordination', fontsize=12, color='red')
    axes[1, 1].text(0.1, 0.15, '✗ Complementary work patterns', fontsize=12, color='red')
    axes[1, 1].text(0.1, 0.05, '✗ Collaborative effectiveness', fontsize=12, color='red')
    
    axes[1, 1].set_xlim(0, 1)
    axes[1, 1].set_ylim(0, 1)
    axes[1, 1].axis('off')
    
    plt.tight_layout()
    plt.savefig('../assets/06/collaboration_vs_spatial_concepts.pdf', dpi=300, bbox_inches='tight')
    plt.savefig('../assets/06/collaboration_vs_spatial_concepts.png', dpi=300, bbox_inches='tight')
    print("Collaboration vs spatial concepts visualization saved to ../assets/06/collaboration_vs_spatial_concepts.pdf and .png")

def main():
    """Main visualization function"""
    print("=== MOVEMENT PATTERN VISUALIZATION ===\n")
    print("Creating visualizations to clarify what movement correlations represent...")
    
    # Create example movement patterns
    create_movement_pattern_visualizations()
    
    # Create conceptual distinction visualization
    create_collaboration_vs_spatial_analysis()
    
    print("\n=== KEY INSIGHTS ===")
    print("1. Movement distance correlation ≠ Collaborative effectiveness")
    print("2. High spatial correlation might indicate co-location, not coordination")
    print("3. Effective collaboration can involve working in different areas")
    print("4. Our metrics measured spatial positioning, not task coordination")
    print("\nVisualizations saved to ../assets/06/")

if __name__ == "__main__":
    main() 