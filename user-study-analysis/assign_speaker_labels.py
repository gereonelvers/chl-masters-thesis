#!/usr/bin/env python3
"""
Script to assign individual speaker labels to transcript files.
Each transcript file can have different speaker labels.

Usage:
1. Interactive mode: python assign_speaker_labels.py
2. Config file mode: python assign_speaker_labels.py --config speaker_config.json
3. Single file mode: python assign_speaker_labels.py --file 0.json --labels "Alice,Bob,HOST"
"""

import json
import os
import sys
import argparse
from pathlib import Path
from typing import List, Dict, Any, Optional


def load_transcript(file_path: str) -> Dict[str, Any]:
    """Load a transcript JSON file."""
    with open(file_path, 'r', encoding='utf-8') as f:
        return json.load(f)


def save_transcript(file_path: str, transcript: Dict[str, Any]) -> None:
    """Save a transcript JSON file with proper formatting."""
    with open(file_path, 'w', encoding='utf-8') as f:
        json.dump(transcript, f, indent=2, ensure_ascii=False)


def get_unique_speakers(transcript: Dict[str, Any]) -> List[int]:
    """Get list of unique speaker numbers in a transcript."""
    speakers = set()
    for entry in transcript.values():
        if 'speaker' in entry:
            speakers.add(entry['speaker'])
    return sorted(list(speakers))


def assign_speaker_labels(transcript: Dict[str, Any], speaker_labels: List[str]) -> Dict[str, Any]:
    """
    Assign speaker labels to a transcript.
    
    Args:
        transcript: The transcript dictionary
        speaker_labels: List of labels where index corresponds to speaker number
    
    Returns:
        Updated transcript with speaker labels
    """
    updated_transcript = {}
    
    for key, entry in transcript.items():
        updated_entry = entry.copy()
        
        if 'speaker' in entry:
            speaker_num = entry['speaker']
            if speaker_num < len(speaker_labels):
                updated_entry['speaker'] = speaker_labels[speaker_num]
            else:
                # Keep original number if no label provided for this speaker
                print(f"Warning: No label provided for speaker {speaker_num}, keeping original number")
                
        updated_transcript[key] = updated_entry
    
    return updated_transcript


def analyze_transcript_file(file_path: str) -> List[int]:
    """Analyze a single transcript file to get speaker numbers."""
    transcript = load_transcript(file_path)
    return get_unique_speakers(transcript)


def create_speaker_config_template(transcripts_dir: str) -> Dict[str, List[str]]:
    """Create a template configuration for speaker labels."""
    config = {}
    transcript_files = sorted([f for f in os.listdir(transcripts_dir) if f.endswith('.json')])
    
    for filename in transcript_files:
        file_path = os.path.join(transcripts_dir, filename)
        speakers = analyze_transcript_file(file_path)
        # Create placeholder labels
        config[filename] = [f"Speaker_{i}" for i in speakers]
    
    return config


def save_speaker_config(config: Dict[str, List[str]], config_path: str) -> None:
    """Save speaker configuration to JSON file."""
    with open(config_path, 'w', encoding='utf-8') as f:
        json.dump(config, f, indent=2, ensure_ascii=False)


def load_speaker_config(config_path: str) -> Dict[str, List[str]]:
    """Load speaker configuration from JSON file."""
    with open(config_path, 'r', encoding='utf-8') as f:
        return json.load(f)


def process_single_transcript(transcripts_dir: str, filename: str, speaker_labels: List[str], backup: bool = True) -> None:
    """Process a single transcript file."""
    file_path = os.path.join(transcripts_dir, filename)
    
    if not os.path.exists(file_path):
        print(f"Error: File {filename} not found")
        return
    
    if backup:
        backup_dir = os.path.join(transcripts_dir, 'backup_original')
        os.makedirs(backup_dir, exist_ok=True)
        backup_path = os.path.join(backup_dir, filename)
        transcript = load_transcript(file_path)
        save_transcript(backup_path, transcript)
        print(f"Backup created: {backup_path}")
    
    # Load and process transcript
    transcript = load_transcript(file_path)
    original_speakers = get_unique_speakers(transcript)
    updated_transcript = assign_speaker_labels(transcript, speaker_labels)
    
    # Save updated transcript
    save_transcript(file_path, updated_transcript)
    
    print(f"✓ {filename}: speakers {original_speakers} → {speaker_labels[:len(original_speakers)]}")


def process_with_config(transcripts_dir: str, config: Dict[str, List[str]], backup: bool = True) -> None:
    """Process all transcripts using configuration."""
    if backup:
        backup_dir = os.path.join(transcripts_dir, 'backup_original')
        os.makedirs(backup_dir, exist_ok=True)
        print(f"Creating backup in: {backup_dir}")
    
    for filename, speaker_labels in config.items():
        if speaker_labels:  # Only process if labels are provided
            process_single_transcript(transcripts_dir, filename, speaker_labels, backup=False)  # Backup handled above


def interactive_mode(transcripts_dir: str) -> None:
    """Interactive mode for assigning speaker labels."""
    transcript_files = sorted([f for f in os.listdir(transcripts_dir) if f.endswith('.json')])
    
    print("Interactive Speaker Label Assignment")
    print("=" * 50)
    print("For each transcript file, you can:")
    print("- Enter speaker labels (comma-separated)")
    print("- Press Enter to skip")
    print("- Type 'quit' to exit")
    print()
    
    config = {}
    
    for filename in transcript_files:
        file_path = os.path.join(transcripts_dir, filename)
        speakers = analyze_transcript_file(file_path)
        
        print(f"\n{filename}")
        print(f"Current speakers: {speakers}")
        print(f"Need {len(speakers)} labels for speakers: {', '.join(map(str, speakers))}")
        
        labels_input = input("Enter labels (comma-separated) or press Enter to skip: ").strip()
        
        if labels_input.lower() == 'quit':
            break
        elif labels_input:
            speaker_labels = [label.strip() for label in labels_input.split(',')]
            if len(speaker_labels) >= len(speakers):
                config[filename] = speaker_labels
                print(f"✓ Will assign: {speakers} → {speaker_labels[:len(speakers)]}")
            else:
                print(f"Warning: Need at least {len(speakers)} labels, got {len(speaker_labels)}. Skipping.")
        else:
            print("Skipped.")
    
    if config:
        print(f"\nProcessing {len(config)} files...")
        confirm = input("Proceed with updates? (y/N): ").strip().lower()
        if confirm in ['y', 'yes']:
            process_with_config(transcripts_dir, config)
            print("All updates completed!")
        else:
            print("Operation cancelled.")
    else:
        print("No files to process.")


def main():
    parser = argparse.ArgumentParser(description="Assign speaker labels to transcript files")
    parser.add_argument('--config', help='Path to speaker configuration JSON file')
    parser.add_argument('--file', help='Process single file')
    parser.add_argument('--labels', help='Comma-separated speaker labels for single file')
    parser.add_argument('--create-template', action='store_true', help='Create speaker configuration template')
    parser.add_argument('--transcripts-dir', default='transcripts', help='Transcripts directory')
    
    args = parser.parse_args()
    
    transcripts_dir = args.transcripts_dir
    
    if args.create_template:
        # Create configuration template
        config = create_speaker_config_template(transcripts_dir)
        config_path = 'speaker_config_template.json'
        save_speaker_config(config, config_path)
        print(f"Configuration template created: {config_path}")
        print("Edit this file with your speaker labels and use --config to apply them.")
        return
    
    elif args.config:
        # Use configuration file
        try:
            config = load_speaker_config(args.config)
            print(f"Loaded configuration from: {args.config}")
            process_with_config(transcripts_dir, config)
            print("All updates completed!")
        except Exception as e:
            print(f"Error loading configuration: {e}")
            return
    
    elif args.file and args.labels:
        # Process single file
        speaker_labels = [label.strip() for label in args.labels.split(',')]
        process_single_transcript(transcripts_dir, args.file, speaker_labels)
    
    else:
        # Interactive mode
        interactive_mode(transcripts_dir)


if __name__ == "__main__":
    main() 