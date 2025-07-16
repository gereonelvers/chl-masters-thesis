#!/usr/bin/env python3
"""
Script to analyze spawned and removed objects from processed session logs.
"""

import os
import re
from collections import defaultdict
from pathlib import Path

def analyze_session_log(file_path):
    """Analyze a single session log file for spawned and removed objects."""
    
    spawned_objects = defaultdict(int)
    removed_objects = set()
    
    with open(file_path, 'r', encoding='utf-8') as f:
        for line in f:
            # Look for spawned objects
            spawn_match = re.search(r'Spawned (Grid\w+) at', line)
            if spawn_match:
                object_type = spawn_match.group(1)
                spawned_objects[object_type] += 1
            
            # Look for removed objects
            remove_match = re.search(r'All instances of (Grid\w+) have been removed', line)
            if remove_match:
                object_type = remove_match.group(1)
                removed_objects.add(object_type)
    
    return spawned_objects, removed_objects

def format_object_list(objects_dict):
    """Format object dictionary into a readable string."""
    if not objects_dict:
        return "None"
    
    items = []
    for obj_type, count in sorted(objects_dict.items()):
        # Remove 'Grid' prefix for cleaner display
        clean_name = obj_type.replace('Grid', '')
        items.append(f"{clean_name} × {count}")
    
    return ", ".join(items)

def format_removed_list(removed_set, spawned_dict):
    """Format removed objects list, showing how many were spawned of each type."""
    if not removed_set:
        return "None"
    
    items = []
    for obj_type in sorted(removed_set):
        clean_name = obj_type.replace('Grid', '')
        count = spawned_dict.get(obj_type, 0)
        items.append(f"{clean_name} × {count}")
    
    return ", ".join(items)

def main():
    processed_dir = Path("processed_logs")
    
    if not processed_dir.exists():
        print("Error: processed_logs directory not found!")
        return
    
    print("Session Object Analysis")
    print("=" * 50)
    
    for session_num in range(32):
        log_file = processed_dir / f"run_{session_num}_processed.txt"
        
        if not log_file.exists():
            print(f"\nSession {session_num}: File not found")
            continue
        
        spawned_objects, removed_objects = analyze_session_log(log_file)
        
        print(f"\nSession {session_num}:")
        print(f"  Spawned objects: {format_object_list(spawned_objects)}")
        print(f"  Destroyed objects: {format_removed_list(removed_objects, spawned_objects)}")
        print(f"  Number of unique objects spawned: {len(spawned_objects)}")
    
    print("\n" + "=" * 50)
    print("Analysis complete!")

if __name__ == "__main__":
    main() 