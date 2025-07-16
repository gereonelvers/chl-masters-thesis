#!/usr/bin/env python3
"""participant_analytics.py  –  v1.0

Enhanced analytics for Unity user study logs with participant tracking across sessions.

Features:
- Tracks participants across sessions (handles IP switching)
- Aggregates metrics per participant across their 4 sessions
- Exports data in multiple formats for further analytics
- Supports both session-level and participant-level analysis

Usage:
```
python participant_analytics.py
```

Outputs:
- Console summary
- participant_metrics.csv (for analysis)
- session_data.json (for heatmaps, etc.)
- participant_summary.json (aggregated data)
"""

import json
import csv
import math
import re
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Dict, List, Optional, Tuple

# ----------------------------------------------------------------------------
# Regular expressions (same as distance-analysis.py)
TS_RE   = re.compile(r"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})")
POS_RE  = re.compile(
    r"Address \[(?P<ip>[\d\.]+)\].*?\[PositionLogger\].*?P: \("
    r"(?P<x>-?\d+\.\d+), (?P<y>-?\d+\.\d+), (?P<z>-?\d+\.\d+)\)"
)
WIPE_RE = re.compile(r"All instances of")

# ----------------------------------------------------------------------------
# Helper functions
def parse_timestamp(line: str) -> Optional[datetime]:
    """Extract a datetime from the beginning of line or return None."""
    m = TS_RE.match(line)
    if not m:
        return None
    return datetime.strptime(m.group(1), "%Y-%m-%d %H:%M:%S")

def find_last_wipe_line_number(file_path: Path) -> Optional[int]:
    """Find the line number of the last 'All instances of' line in the file."""
    last_wipe_line = None
    with open(file_path, "r", errors="ignore") as f:
        for line_num, line in enumerate(f, 1):
            if WIPE_RE.search(line):
                last_wipe_line = line_num
    return last_wipe_line

def euclidean(a: Tuple[float, float, float], b: Tuple[float, float, float]) -> float:
    return math.sqrt(sum((ai - bi) ** 2 for ai, bi in zip(a, b)))

def bucket_key(ts: datetime, hz: float = 1.0) -> float:
    """Quantise timestamp to hz bucket."""
    if hz >= 1.0:
        return ts.replace(microsecond=0).timestamp()
    step = 1 / hz
    return math.floor(ts.timestamp() / step) * step

# ----------------------------------------------------------------------------
# Core processing functions

def process_session_with_positions(file_path: Path, ignore_ips: List[str] = ["192.168.1.100"], hz: float = 1.0):
    """Process a session and return both metrics and raw position data."""
    last_wipe_line = find_last_wipe_line_number(file_path)
    
    samples = defaultdict(dict)  # ip → bucket → pos
    heights = defaultdict(list)  # ip → [y_coordinates] 
    positions = defaultdict(list)  # ip → [(timestamp, x, y, z)]
    start_time = None
    end_time = None
    
    with open(file_path, "r", errors="ignore") as f:
        lines = f.readlines()
    
    start_line = last_wipe_line if last_wipe_line is not None else 0
    
    for line in lines[start_line:]:
        ts = parse_timestamp(line)
        if ts is None:
            continue
            
        if start_time is None:
            start_time = ts
        end_time = ts
        
        m = POS_RE.search(line)
        if not m:
            continue
            
        ip = m.group("ip")
        if ip in ignore_ips:
            continue
            
        pos = (float(m.group("x")), float(m.group("y")), float(m.group("z")))
        
        # Store bucketed samples for distance calculation
        b = bucket_key(ts, hz)
        if b not in samples[ip]:
            samples[ip][b] = pos
            
        # Store all positions for heatmap/trajectory analysis
        positions[ip].append((ts.isoformat(), pos[0], pos[1], pos[2]))
        heights[ip].append(pos[1])
    
    # Calculate metrics
    results = {}
    duration = (end_time - start_time).total_seconds() if start_time and end_time else 0.0
    
    for ip in samples:
        pts = sorted(samples[ip].items())
        distance = sum(
            euclidean(p1, p2) for (_, p1), (_, p2) in zip(pts, pts[1:])
        ) if len(pts) > 1 else 0.0
        
        avg_height = sum(heights[ip]) / len(heights[ip]) if heights[ip] else 0.0
        
        results[ip] = {
            'distance': distance,
            'duration': duration,
            'avg_height': avg_height,
            'sample_count': len(pts),
            'positions': positions[ip]
        }
    
    return results, start_time, end_time

def load_participant_mapping(mapping_file: Path = Path("participant_mapping.json")) -> Dict:
    """Load the participant mapping configuration."""
    if not mapping_file.exists():
        raise FileNotFoundError(f"Participant mapping file {mapping_file} not found")
    return json.loads(mapping_file.read_text())

def aggregate_participant_data(session_data: Dict, mapping: Dict) -> Dict:
    """Aggregate session data by participant."""
    participant_totals = defaultdict(lambda: {
        'total_distance': 0.0,
        'total_duration': 0.0, 
        'avg_height': [],
        'session_count': 0,
        'sessions': [],
        'all_positions': []
    })
    
    # Create reverse mapping: session -> participant assignments
    session_to_participants = {}
    for pair in mapping['participant_pairs']:
        for session_info in pair['sessions']:
            session_id = session_info['session']
            session_to_participants[session_id] = {}
            for participant, ip in session_info.items():
                if participant != 'session':
                    session_to_participants[session_id][ip] = participant
    
    # Aggregate data by participant
    for session_id, session_metrics in session_data.items():
        if session_id in session_to_participants:
            for ip, metrics in session_metrics.items():
                if ip in session_to_participants[session_id]:
                    participant = session_to_participants[session_id][ip]
                    
                    participant_totals[participant]['total_distance'] += metrics['distance']
                    participant_totals[participant]['total_duration'] += metrics['duration']
                    participant_totals[participant]['avg_height'].extend([metrics['avg_height']] * metrics['sample_count'])
                    participant_totals[participant]['session_count'] += 1
                    participant_totals[participant]['sessions'].append({
                        'session': session_id,
                        'distance': metrics['distance'],
                        'duration': metrics['duration'],
                        'avg_height': metrics['avg_height']
                    })
                    participant_totals[participant]['all_positions'].extend(metrics['positions'])
    
    # Calculate final averages
    for participant, data in participant_totals.items():
        if data['avg_height']:
            data['avg_height'] = sum(data['avg_height']) / len(data['avg_height'])
        else:
            data['avg_height'] = 0.0
    
    return dict(participant_totals)

# ----------------------------------------------------------------------------
# Export functions

def export_participant_csv(participant_data: Dict, output_file: Path = Path("participant_metrics.csv")):
    """Export participant aggregated data to CSV."""
    with open(output_file, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(['Participant', 'Total_Distance_m', 'Total_Duration_s', 'Avg_Height_m', 'Sessions_Completed'])
        
        for participant, data in sorted(participant_data.items()):
            writer.writerow([
                participant,
                f"{data['total_distance']:.2f}",
                f"{data['total_duration']:.1f}",
                f"{data['avg_height']:.2f}",
                data['session_count']
            ])

def export_session_json(session_data: Dict, mapping: Dict, output_file: Path = Path("session_data.json")):
    """Export detailed session data with participant mapping for heatmaps/trajectories."""
    
    # Add participant labels to session data
    session_to_participants = {}
    for pair in mapping['participant_pairs']:
        for session_info in pair['sessions']:
            session_id = session_info['session']
            session_to_participants[session_id] = {}
            for participant, ip in session_info.items():
                if participant != 'session':
                    session_to_participants[session_id][ip] = participant
    
    enhanced_data = {}
    for session_id, session_metrics in session_data.items():
        enhanced_data[session_id] = {}
        for ip, metrics in session_metrics.items():
            participant = session_to_participants.get(session_id, {}).get(ip, f"Unknown_{ip}")
            enhanced_data[session_id][participant] = {
                'distance': metrics['distance'],
                'duration': metrics['duration'], 
                'avg_height': metrics['avg_height'],
                'positions': metrics['positions'],
                'ip': ip
            }
    
    with open(output_file, 'w') as f:
        json.dump(enhanced_data, f, indent=2)

def export_participant_summary(participant_data: Dict, output_file: Path = Path("participant_summary.json")):
    """Export participant summary without position data."""
    summary_data = {}
    for participant, data in participant_data.items():
        summary_data[participant] = {
            'total_distance': data['total_distance'],
            'total_duration': data['total_duration'],
            'avg_height': data['avg_height'],
            'session_count': data['session_count'],
            'sessions': data['sessions']
        }
    
    with open(output_file, 'w') as f:
        json.dump(summary_data, f, indent=2)

# ----------------------------------------------------------------------------
# Main function

def main():
    """Main analysis pipeline."""
    print("🔍 Processing user study data...")
    
    # Load configuration
    try:
        mapping = load_participant_mapping()
        print(f"✓ Loaded mapping for {len(mapping['participant_pairs'])} participant pairs")
    except FileNotFoundError as e:
        print(f"❌ {e}")
        print("Please create participant_mapping.json first")
        return
    
    # Process all session logs
    session_logs_dir = Path("./session_logs")
    if not session_logs_dir.exists():
        print(f"❌ {session_logs_dir} directory not found")
        return
    
    log_files = sorted(session_logs_dir.glob("run_*.txt"))
    if not log_files:
        print(f"❌ No run_*.txt files found in {session_logs_dir}")
        return
    
    print(f"📁 Found {len(log_files)} session log files")
    
    # Process each session
    session_data = {}
    for log_file in log_files:
        session_id = int(log_file.stem.split("_")[1])
        try:
            results, start_time, end_time = process_session_with_positions(log_file)
            if results:
                session_data[session_id] = results
                print(f"✓ Processed session {session_id}")
            else:
                print(f"⚠️  Session {session_id}: No valid data")
        except Exception as e:
            print(f"❌ Error processing session {session_id}: {e}")
    
    # Aggregate by participant
    print(f"\n📊 Aggregating data for {len(session_data)} sessions...")
    participant_data = aggregate_participant_data(session_data, mapping)
    
    # Display summary
    print(f"\n🎯 PARTICIPANT SUMMARY")
    print("=" * 80)
    print(f"{'Participant':<12} {'Total Dist (m)':<15} {'Total Time (s)':<15} {'Avg Height (m)':<15} {'Sessions':<10}")
    print("-" * 80)
    
    for participant, data in sorted(participant_data.items()):
        print(f"{participant:<12} {data['total_distance']:>12.2f} {data['total_duration']:>13.1f} "
              f"{data['avg_height']:>13.2f} {data['session_count']:>8d}")
    
    # Export data
    print(f"\n💾 Exporting data...")
    export_participant_csv(participant_data)
    export_session_json(session_data, mapping)
    export_participant_summary(participant_data)
    
    print(f"✓ participant_metrics.csv - Participant summaries for analysis")
    print(f"✓ session_data.json - Detailed session data for heatmaps/trajectories") 
    print(f"✓ participant_summary.json - Participant aggregated data")
    print(f"\n🚀 Ready for further analytics!")

if __name__ == "__main__":
    main() 