import math
import re
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import List, Tuple, Dict, Optional

# ----------------------------------------------------------------------------
# Regular expressions
TS_RE   = re.compile(r"^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})")
POS_RE  = re.compile(
    r"Address \[(?P<participant_id>[^\]]+)\].*?\[PositionLogger\].*?P: \("    # prefix
    r"(?P<x>-?\d+\.\d+), (?P<y>-?\d+\.\d+), (?P<z>-?\d+\.\d+)\)" # coords
)
WIPE_RE = re.compile(r"All instances of")

# ----------------------------------------------------------------------------
# Helpers --------------------------------------------------------------------

def parse_timestamp(line: str) -> Optional[datetime]:
    """Extract a `datetime` from the beginning of *line* or return None."""
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
    """Quantise timestamp to *hz* bucket (wall‑clock second for hz ≥ 1)."""
    if hz >= 1.0:
        return ts.replace(microsecond=0).timestamp()
    step = 1 / hz
    return math.floor(ts.timestamp() / step) * step

# ----------------------------------------------------------------------------
# Core algorithm -------------------------------------------------------------

def process_session_log(file_path: Path, ignore_participants: List[str] = ["Host"], hz: float = 1.0):
    """Process a single session log file and return metrics for each participant."""
    
    # Find the last bulk wipe line number
    last_wipe_line = find_last_wipe_line_number(file_path)
    
    samples = defaultdict(dict)  # participant_id → bucket → pos
    heights = defaultdict(list)  # participant_id → [y_coordinates]
    start_time = None
    end_time = None
    
    with open(file_path, "r", errors="ignore") as f:
        lines = f.readlines()
    
    # Start from after the last wipe, or from beginning if no wipe found
    start_line = last_wipe_line if last_wipe_line is not None else 0
    
    for line in lines[start_line:]:
        ts = parse_timestamp(line)
        if ts is None:
            continue
            
        # Track time range
        if start_time is None:
            start_time = ts
        end_time = ts
        
        # Position lines only
        m = POS_RE.search(line)
        if not m:
            continue
            
        participant_id = m.group("participant_id")
        if participant_id in ignore_participants:
            continue
            
        pos = (float(m.group("x")), float(m.group("y")), float(m.group("z")))
        
        # Store position sample (first sample in each bucket)
        b = bucket_key(ts, hz)
        if b not in samples[participant_id]:
            samples[participant_id][b] = pos
            
        # Track heights for averaging
        heights[participant_id].append(pos[1])  # Y coordinate
    
    # Calculate metrics for each participant
    results = {}
    duration = (end_time - start_time).total_seconds() if start_time and end_time else 0.0
    
    for participant_id in samples:
        # Calculate distance
        pts = sorted(samples[participant_id].items())
        distance = sum(
            euclidean(p1, p2) for (_, p1), (_, p2) in zip(pts, pts[1:])
        ) if len(pts) > 1 else 0.0
        
        # Calculate average height
        avg_height = sum(heights[participant_id]) / len(heights[participant_id]) if heights[participant_id] else 0.0
        
        results[participant_id] = {
            'distance': distance,
            'duration': duration,
            'avg_height': avg_height,
            'sample_count': len(pts)
        }
    
    return results, start_time, end_time


# ----------------------------------------------------------------------------
# Main -----------------------------------------------------------------------

def main() -> None:
    processed_logs_dir = Path("./session_logs/processed_logs")
    
    if not processed_logs_dir.exists():
        print(f"Error: {processed_logs_dir} directory not found", file=sys.stderr)
        return
    
    # Process each session log file
    log_files = sorted(processed_logs_dir.glob("run_*_processed.txt"))
    
    if not log_files:
        print(f"Error: No run_*_processed.txt files found in {processed_logs_dir}", file=sys.stderr)
        return
    
    # Print header
    print("Session,ParticipantID,Distance(m),Duration(s),AvgHeight(m),Samples,StartTime,EndTime")
    print("-" * 80)
    
    for log_file in log_files:
        # Extract session ID from filename (e.g., run_0_processed.txt -> 0)
        session_id = log_file.stem.split("_")[1]
        
        try:
            results, start_time, end_time = process_session_log(log_file)
            
            if results:
                print(f"\nSession {session_id}:")
                for participant_id, metrics in sorted(results.items()):
                    print(f"  {participant_id:15} | {metrics['distance']:8.2f} | {metrics['duration']:8.1f} | "
                          f"{metrics['avg_height']:8.2f} | {metrics['sample_count']:7d} | "
                          f"{start_time.strftime('%H:%M:%S') if start_time else 'N/A':8} | "
                          f"{end_time.strftime('%H:%M:%S') if end_time else 'N/A':8}")
            else:
                print(f"\nSession {session_id}: No valid data found")
                      
        except Exception as e:
            print(f"Error processing {log_file}: {e}", file=sys.stderr)


if __name__ == "__main__":
    main()
