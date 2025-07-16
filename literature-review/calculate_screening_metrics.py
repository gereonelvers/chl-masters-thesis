import pandas as pd
import numpy as np
from sklearn.metrics import f1_score, cohen_kappa_score, confusion_matrix, precision_score, recall_score

def calculate_overall_decision(row, model_prefix):
    """
    Calculate overall inclusion decision based on all criteria.
    A paper is excluded if ANY criterion is 'no'.
    """
    criteria = ['AR', 'Collaboration', 'Industrial', 'Device', 'Setting']
    
    for criterion in criteria:
        if model_prefix == 'GPT4o':
            col_name = f'{criterion}_Decision'
        else:  # HUMAN
            col_name = f'HUMAN_{criterion}_Decision'
        
        decision = row.get(col_name, '')
        if decision == 'no':
            return 'exclude'
    
    # If no criterion is 'no', then include (yes or unsure on all criteria)
    return 'include'

def calculate_screening_metrics():
    """
    Calculate F1, Cohen's kappa, sensitivity, specificity, and precision 
    for the literature screening validation sample based on overall decisions.
    """
    
    # Load the Excel file with validation results
    print("Loading screening results...")
    df = pd.read_excel('./literature-baseline-export-updated.xlsx', sheet_name='All_Papers')
    
    # Filter to validation sample only
    validation_df = df[df['In_Validation_Sample'] == True].copy()
    print(f"Validation sample size: {len(validation_df)}")
    
    if len(validation_df) == 0:
        print("No validation sample found!")
        return
    
    # Debug: Check what columns exist
    print("\nColumns in validation_df:")
    print([col for col in validation_df.columns if 'Decision' in col])
    
    # Debug: Look at a few sample rows to understand the data structure
    print("\nSample of first few rows:")
    decision_cols = [col for col in validation_df.columns if 'Decision' in col]
    if decision_cols:
        print(validation_df[decision_cols].head())
    
    # Calculate overall decisions for both GPT-4o and human reviewer
    print("\nCalculating overall decisions based on all screening criteria...")
    
    gpt4o_overall = []
    human_overall = []
    
    for idx, row in validation_df.iterrows():
        gpt4o_decision = calculate_overall_decision(row, 'GPT4o')
        human_decision = calculate_overall_decision(row, 'HUMAN')
        
        gpt4o_overall.append(gpt4o_decision)
        human_overall.append(human_decision)
        
        # Debug: Print first few decisions
        if len(gpt4o_overall) <= 5:
            print(f"Row {idx}: GPT4o={gpt4o_decision}, Human={human_decision}")
            # Show the individual criteria for debugging
            criteria = ['AR', 'Collaboration', 'Industrial', 'Device', 'Setting']
            for criterion in criteria:
                gpt4o_col = f'{criterion}_Decision'
                human_col = f'HUMAN_{criterion}_Decision'
                gpt4o_val = row.get(gpt4o_col, 'N/A')
                human_val = row.get(human_col, 'N/A')
                print(f"  {criterion}: GPT4o={gpt4o_val}, Human={human_val}")
    
    gpt4o_decisions = np.array(gpt4o_overall)
    human_decisions = np.array(human_overall)
    
    print(f"\nValid decision pairs: {len(gpt4o_decisions)}")
    print(f"GPT-4o decisions: {np.unique(gpt4o_decisions, return_counts=True)}")
    print(f"Human decisions: {np.unique(human_decisions, return_counts=True)}")
    
    # Only proceed if we have both include and exclude decisions
    if len(np.unique(gpt4o_decisions)) == 1 and len(np.unique(human_decisions)) == 1:
        print("\nWARNING: All decisions are the same! This suggests an issue with the data or logic.")
        print("Cannot calculate meaningful metrics when there's no variation.")
        return None
    
    # Calculate metrics using different approaches
    
    # 1. Binary classification (include/exclude)
    print("\n=== BINARY CLASSIFICATION (include/exclude) ===")
    
    # Convert to numerical labels for sklearn  
    label_map = {'include': 1, 'exclude': 0}
    gpt4o_numeric = np.array([label_map[d] for d in gpt4o_decisions])
    human_numeric = np.array([label_map[d] for d in human_decisions])
    
    # Calculate binary metrics with human as ground truth
    f1_binary = f1_score(human_numeric, gpt4o_numeric)
    kappa = cohen_kappa_score(human_numeric, gpt4o_numeric)
    sensitivity = recall_score(human_numeric, gpt4o_numeric)  # True positive rate
    precision = precision_score(human_numeric, gpt4o_numeric)
    
    # Calculate specificity manually
    cm_binary = confusion_matrix(human_numeric, gpt4o_numeric, labels=[0, 1])
    tn, fp, fn, tp = cm_binary.ravel()
    specificity = tn / (tn + fp) if (tn + fp) > 0 else 0.0
    
    print(f"F1 score: {f1_binary:.3f}")
    print(f"Cohen's kappa: {kappa:.3f}")
    print(f"Sensitivity (recall): {sensitivity:.3f}")
    print(f"Specificity: {specificity:.3f}")
    print(f"Precision: {precision:.3f}")
    
    # Binary confusion matrix
    print(f"Confusion matrix (rows=human, cols=gpt4o):")
    print(f"Labels: 0=exclude, 1=include")
    print(cm_binary)
    print(f"TP: {tp}, TN: {tn}, FP: {fp}, FN: {fn}")
    
    # Also calculate macro F1 for consistency with three-class approach
    # For binary classification, macro F1 is the same as regular F1
    f1_macro = f1_binary
    
    # 3. Agreement analysis
    print("\n=== AGREEMENT ANALYSIS ===")
    agreement = (gpt4o_decisions == human_decisions).sum()
    total = len(gpt4o_decisions)
    agreement_rate = agreement / total
    
    print(f"Perfect agreement: {agreement}/{total} ({agreement_rate:.1%})")
    
    # Detailed breakdown
    decision_pairs = list(zip(gpt4o_decisions, human_decisions))
    unique_pairs, pair_counts = np.unique(decision_pairs, axis=0, return_counts=True)
    
    print("\nDecision pair breakdown (GPT-4o, Human):")
    for pair, count in zip(unique_pairs, pair_counts):
        print(f"  {pair}: {count}")
    
    # Show breakdown by individual criteria for context
    print("\n=== INDIVIDUAL CRITERIA BREAKDOWN ===")
    criteria = ['AR', 'Collaboration', 'Industrial', 'Device', 'Setting']
    for criterion in criteria:
        gpt4o_col = f'{criterion}_Decision'
        human_col = f'HUMAN_{criterion}_Decision'
        if gpt4o_col in validation_df.columns and human_col in validation_df.columns:
            matches = (validation_df[gpt4o_col] == validation_df[human_col]).sum()
            total_criterion = len(validation_df)
            agreement_rate_criterion = (matches / total_criterion) * 100
            print(f"  {criterion}: {matches}/{total_criterion} ({agreement_rate_criterion:.1f}% agreement)")
            
            # Show the distribution of decisions for this criterion
            gpt4o_dist = validation_df[gpt4o_col].value_counts()
            human_dist = validation_df[human_col].value_counts()
            print(f"    GPT4o {criterion}: {dict(gpt4o_dist)}")
            print(f"    Human {criterion}: {dict(human_dist)}")
    
    return {
        'f1_macro': f1_macro,
        'cohen_kappa': kappa,
        'sensitivity': sensitivity,
        'specificity': specificity,
        'precision': precision,
        'sample_size': len(gpt4o_decisions)
    }

if __name__ == "__main__":
    metrics = calculate_screening_metrics()
    
    if metrics is not None:
        print("\n=== SUMMARY FOR THESIS ===")
        if metrics['f1_macro'] is not None:
            print(f"Macro-averaged F₁: {metrics['f1_macro']:.3f}")
        if metrics['cohen_kappa'] is not None:
            print(f"Cohen's κ: {metrics['cohen_kappa']:.3f}")
        if metrics['sensitivity'] is not None:
            print(f"Sensitivity: {metrics['sensitivity']:.3f}")
        if metrics['specificity'] is not None:
            print(f"Specificity: {metrics['specificity']:.3f}")
        if metrics['precision'] is not None:
            print(f"Precision: {metrics['precision']:.3f}") 