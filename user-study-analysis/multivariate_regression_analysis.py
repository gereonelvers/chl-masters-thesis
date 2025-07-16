#!/usr/bin/env python3
"""
Multivariate Regression Analysis for Chapter 6 Results
=====================================================

This script builds linear regression models to predict:
1. Task Performance (Completion Time) from personality + demographics + experience  
2. Learning (SUS Progression) from experience + personality + initial SUS
3. Bridge Quality (Safety Factor) from personality + experience + completion time
"""

import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import StandardScaler
from sklearn.metrics import r2_score, mean_squared_error
import warnings
warnings.filterwarnings('ignore')

# Set style for academic plots
plt.style.use('default')
sns.set_palette("husl")
plt.rcParams.update({
    'figure.figsize': (15, 10),
    'font.size': 11,
    'axes.titlesize': 12,
    'axes.labelsize': 11,
    'xtick.labelsize': 10,
    'ytick.labelsize': 10,
    'legend.fontsize': 10
})

def load_and_prepare_data():
    """Load and merge all datasets for analysis"""
    # Load main study results
    study_data = pd.read_csv('study-run-results.csv')
    
    # Load participant data
    participant_data = pd.read_csv('participant-results.csv')
    
    # Clean study data - remove invalid entries
    study_data_clean = study_data[study_data['Completion time (exact, minutes)'].notna()].copy()
    study_data_clean = study_data_clean[~study_data_clean['Notes'].str.contains('failed to construct', na=False)]
    
    # Parse completion time to numeric
    def parse_time(time_str):
        if pd.isna(time_str):
            return np.nan
        try:
            parts = str(time_str).split(':')
            return float(parts[0]) + float(parts[1])/60
        except:
            return np.nan
            
    study_data_clean['completion_time_min'] = study_data_clean['Completion time (exact, minutes)'].apply(parse_time)
    
    # Clean bridge quality data
    def clean_numeric(col):
        return pd.to_numeric(col, errors='coerce')
    
    study_data_clean['safety_factor'] = clean_numeric(study_data_clean['Bridge evaluation 1: Safety Factor (min, higher better)'])
    study_data_clean['von_mises_stress'] = clean_numeric(study_data_clean['Bridge Evaluation 2: von Mises Stress (max, in MPa, smaller better)'])
    study_data_clean['displacement'] = clean_numeric(study_data_clean['Bridge evaluation 3: Displacement (max, in mm, smaller better)'])
    study_data_clean['bridge_price'] = clean_numeric(study_data_clean['Bridge Price'])
    
    # Create participant-level summary
    participant_summary = []
    
    for _, row in participant_data.iterrows():
        participant_id = row['Participant ID']
        
        # Get sessions for this participant
        p1_sessions = study_data_clean[study_data_clean['Participant 1 ID'] == participant_id]
        p2_sessions = study_data_clean[study_data_clean['Participant 2 ID'] == participant_id]
        
        # Combine all sessions
        all_sessions = pd.concat([p1_sessions, p2_sessions])
        
        if len(all_sessions) > 0:
            # Calculate averages
            avg_completion_time = all_sessions['completion_time_min'].mean()
            avg_safety_factor = all_sessions['safety_factor'].mean()
            avg_bridge_price = all_sessions['bridge_price'].mean()
            
            # Get SUS progression
            sus_scores = []
            for i in range(1, 5):
                score = row.get(f'SUS Score {i}')
                if pd.notna(score):
                    sus_scores.append(float(score))
            
            sus_progression = sus_scores[-1] - sus_scores[0] if len(sus_scores) >= 2 else 0
            initial_sus = sus_scores[0] if len(sus_scores) > 0 else np.nan
            
            # Experience level mapping
            experience_map = {
                'No experience': 0,
                'Limited experience': 1, 
                'Moderate experience': 2,
                'Extensive experience': 3
            }
            ar_experience = experience_map.get(row['General Information - 5. How would you rate your experience with Augmented Reality (AR) and Virtual Reality (VR) applications?'], 0)
            
            # Gender encoding
            gender = 1 if row['General Information - 1. Please select your gender:'] == 'Male' else 0
            
            participant_summary.append({
                'participant_id': participant_id,
                'avg_completion_time': avg_completion_time,
                'avg_safety_factor': avg_safety_factor,
                'avg_bridge_price': avg_bridge_price,
                'sus_progression': sus_progression,
                'initial_sus': initial_sus,
                'ar_experience': ar_experience,
                'agreeableness': row['Agreeableness'],
                'conscientiousness': row['Conscientiousness'], 
                'extraversion': row['Extraversion'],
                'neuroticism': row['Neuroticism'],
                'openness': row['Openness'],
                'age': row['General Information - 2. Please state your age:'],
                'gender': gender  # 1=Male, 0=Female
            })
    
    return pd.DataFrame(participant_summary)

def compute_regression_statistics(X, y, y_pred):
    """Compute detailed regression statistics"""
    n = len(y)
    k = X.shape[1]  # number of predictors
    
    # R-squared and adjusted R-squared
    r2 = r2_score(y, y_pred)
    adj_r2 = 1 - (1 - r2) * (n - 1) / (n - k - 1)
    
    # F-statistic
    mse_model = np.sum((y_pred - y.mean())**2) / k
    mse_residual = np.sum((y - y_pred)**2) / (n - k - 1)
    f_stat = mse_model / mse_residual if mse_residual != 0 else np.nan
    f_pvalue = 1 - stats.f.cdf(f_stat, k, n - k - 1) if not np.isnan(f_stat) else np.nan
    
    # Residual standard error
    residual_se = np.sqrt(mse_residual)
    
    return {
        'r2': r2,
        'adj_r2': adj_r2,
        'f_stat': f_stat,
        'f_pvalue': f_pvalue,
        'residual_se': residual_se,
        'n': n,
        'k': k
    }

def build_regression_models(data):
    """Build and evaluate the three regression models"""
    
    results = {}
    
    # ===========================================
    # MODEL 1: Task Performance Prediction
    # ===========================================
    print("="*60)
    print("MODEL 1: PREDICTING TASK PERFORMANCE (Completion Time)")
    print("="*60)
    
    # Prepare data for Model 1
    model1_data = data.dropna(subset=['avg_completion_time', 'agreeableness', 'conscientiousness', 
                                      'extraversion', 'neuroticism', 'openness', 'age', 'gender', 'ar_experience'])
    
    if len(model1_data) < 5:
        print(f"Warning: Only {len(model1_data)} complete cases for Model 1")
    
    # Features and target
    X1_features = ['agreeableness', 'conscientiousness', 'extraversion', 'neuroticism', 'openness', 
                   'age', 'gender', 'ar_experience']
    X1 = model1_data[X1_features]
    y1 = model1_data['avg_completion_time']
    
    # Fit model
    model1 = LinearRegression()
    model1.fit(X1, y1)
    y1_pred = model1.predict(X1)
    
    # Get statistics
    stats1 = compute_regression_statistics(X1, y1, y1_pred)
    
    print(f"Model 1 Results (n={len(model1_data)}):")
    print(f"R² = {stats1['r2']:.3f}")
    print(f"Adjusted R² = {stats1['adj_r2']:.3f}")
    print(f"F-statistic = {stats1['f_stat']:.3f}, p = {stats1['f_pvalue']:.3f}")
    print(f"Residual Standard Error = {stats1['residual_se']:.3f}")
    print("\nCoefficients:")
    for feature, coef in zip(X1_features, model1.coef_):
        print(f"  {feature:15}: {coef:8.3f}")
    print(f"  {'intercept':15}: {model1.intercept_:8.3f}")
    
    results['model1'] = {
        'model': model1,
        'data': model1_data,
        'features': X1_features,
        'stats': stats1,
        'y_true': y1,
        'y_pred': y1_pred
    }
    
    # ===========================================
    # MODEL 2: Learning Prediction (SUS Progression)
    # ===========================================
    print("\n" + "="*60)
    print("MODEL 2: PREDICTING LEARNING (SUS Progression)")
    print("="*60)
    
    # Prepare data for Model 2
    model2_data = data.dropna(subset=['sus_progression', 'initial_sus', 'ar_experience', 
                                      'agreeableness', 'conscientiousness', 'extraversion', 
                                      'neuroticism', 'openness'])
    
    if len(model2_data) < 5:
        print(f"Warning: Only {len(model2_data)} complete cases for Model 2")
    
    # Features and target
    X2_features = ['initial_sus', 'ar_experience', 'agreeableness', 'conscientiousness', 
                   'extraversion', 'neuroticism', 'openness']
    X2 = model2_data[X2_features]
    y2 = model2_data['sus_progression']
    
    # Fit model
    model2 = LinearRegression()
    model2.fit(X2, y2)
    y2_pred = model2.predict(X2)
    
    # Get statistics
    stats2 = compute_regression_statistics(X2, y2, y2_pred)
    
    print(f"Model 2 Results (n={len(model2_data)}):")
    print(f"R² = {stats2['r2']:.3f}")
    print(f"Adjusted R² = {stats2['adj_r2']:.3f}")
    print(f"F-statistic = {stats2['f_stat']:.3f}, p = {stats2['f_pvalue']:.3f}")
    print(f"Residual Standard Error = {stats2['residual_se']:.3f}")
    print("\nCoefficients:")
    for feature, coef in zip(X2_features, model2.coef_):
        print(f"  {feature:15}: {coef:8.3f}")
    print(f"  {'intercept':15}: {model2.intercept_:8.3f}")
    
    results['model2'] = {
        'model': model2,
        'data': model2_data,
        'features': X2_features,
        'stats': stats2,
        'y_true': y2,
        'y_pred': y2_pred
    }
    
    # ===========================================
    # MODEL 3: Bridge Quality Prediction
    # ===========================================
    print("\n" + "="*60)
    print("MODEL 3: PREDICTING BRIDGE QUALITY (Safety Factor)")
    print("="*60)
    
    # Prepare data for Model 3
    model3_data = data.dropna(subset=['avg_safety_factor', 'avg_completion_time', 'ar_experience',
                                      'agreeableness', 'conscientiousness', 'extraversion', 
                                      'neuroticism', 'openness'])
    
    if len(model3_data) < 5:
        print(f"Warning: Only {len(model3_data)} complete cases for Model 3")
    
    # Features and target
    X3_features = ['avg_completion_time', 'ar_experience', 'agreeableness', 'conscientiousness', 
                   'extraversion', 'neuroticism', 'openness']
    X3 = model3_data[X3_features]
    y3 = model3_data['avg_safety_factor']
    
    # Fit model
    model3 = LinearRegression()
    model3.fit(X3, y3)
    y3_pred = model3.predict(X3)
    
    # Get statistics
    stats3 = compute_regression_statistics(X3, y3, y3_pred)
    
    print(f"Model 3 Results (n={len(model3_data)}):")
    print(f"R² = {stats3['r2']:.3f}")
    print(f"Adjusted R² = {stats3['adj_r2']:.3f}")
    print(f"F-statistic = {stats3['f_stat']:.3f}, p = {stats3['f_pvalue']:.3f}")
    print(f"Residual Standard Error = {stats3['residual_se']:.3f}")
    print("\nCoefficients:")
    for feature, coef in zip(X3_features, model3.coef_):
        print(f"  {feature:15}: {coef:8.3f}")
    print(f"  {'intercept':15}: {model3.intercept_:8.3f}")
    
    results['model3'] = {
        'model': model3,
        'data': model3_data,
        'features': X3_features,
        'stats': stats3,
        'y_true': y3,
        'y_pred': y3_pred
    }
    
    return results

def create_regression_plots(results):
    """Create visualizations for the regression models"""
    
    fig, axes = plt.subplots(2, 3, figsize=(18, 12))
    fig.suptitle('Multivariate Regression Analysis Results', fontsize=16, fontweight='bold')
    
    models = ['model1', 'model2', 'model3']
    titles = ['Task Performance\n(Completion Time)', 'Learning\n(SUS Progression)', 'Bridge Quality\n(Safety Factor)']
    
    for i, (model_key, title) in enumerate(zip(models, titles)):
        if model_key not in results:
            continue
            
        result = results[model_key]
        y_true = result['y_true']
        y_pred = result['y_pred']
        
        # Predicted vs Actual (top row)
        ax1 = axes[0, i]
        ax1.scatter(y_true, y_pred, alpha=0.7)
        
        # Perfect prediction line
        min_val = min(y_true.min(), y_pred.min())
        max_val = max(y_true.max(), y_pred.max())
        ax1.plot([min_val, max_val], [min_val, max_val], 'r--', alpha=0.7)
        
        ax1.set_xlabel('Actual Values')
        ax1.set_ylabel('Predicted Values')
        ax1.set_title(f'{title}\nR² = {result["stats"]["r2"]:.3f}')
        
        # Residuals plot (bottom row)
        ax2 = axes[1, i]
        residuals = y_true - y_pred
        ax2.scatter(y_pred, residuals, alpha=0.7)
        ax2.axhline(y=0, color='r', linestyle='--', alpha=0.7)
        ax2.set_xlabel('Predicted Values')
        ax2.set_ylabel('Residuals')
        ax2.set_title('Residuals Plot')
    
    plt.tight_layout()
    plt.savefig('../assets/06/multivariate_regression_analysis.pdf', dpi=300, bbox_inches='tight')
    plt.show()

def print_summary_table(results):
    """Print a summary table of all models"""
    
    print("\n" + "="*80)
    print("SUMMARY TABLE: MULTIVARIATE REGRESSION MODELS")
    print("="*80)
    
    print(f"{'Model':<25} {'n':<4} {'R²':<8} {'Adj R²':<8} {'F-stat':<8} {'p-value':<10}")
    print("-" * 80)
    
    model_names = {
        'model1': 'Task Performance',
        'model2': 'Learning (SUS Progression)', 
        'model3': 'Bridge Quality'
    }
    
    for key, name in model_names.items():
        if key in results:
            stats = results[key]['stats']
            p_str = f"{stats['f_pvalue']:.3f}" if stats['f_pvalue'] >= 0.001 else "< 0.001"
            print(f"{name:<25} {stats['n']:<4} {stats['r2']:<8.3f} {stats['adj_r2']:<8.3f} {stats['f_stat']:<8.3f} {p_str:<10}")

def main():
    """Main analysis function"""
    print("Loading and preparing data...")
    data = load_and_prepare_data()
    
    print(f"Loaded data for {len(data)} participants")
    print(f"Available variables: {list(data.columns)}")
    
    # Build models
    results = build_regression_models(data)
    
    # Create plots
    create_regression_plots(results)
    
    # Print summary
    print_summary_table(results)
    
    # Save detailed results
    print("\nSaving detailed results...")
    with open('multivariate_regression_results.txt', 'w') as f:
        f.write("MULTIVARIATE REGRESSION ANALYSIS RESULTS\n")
        f.write("="*50 + "\n\n")
        
        model_names = {
            'model1': 'Task Performance Prediction',
            'model2': 'Learning Prediction', 
            'model3': 'Bridge Quality Prediction'
        }
        
        for key, result in results.items():
            f.write(f"{model_names[key]}\n")
            f.write("-" * 30 + "\n")
            f.write(f"Sample size: {result['stats']['n']}\n")
            f.write(f"R²: {result['stats']['r2']:.3f}\n")
            f.write(f"Adjusted R²: {result['stats']['adj_r2']:.3f}\n")
            f.write(f"F-statistic: {result['stats']['f_stat']:.3f} (p = {result['stats']['f_pvalue']:.3f})\n")
            f.write(f"Features: {', '.join(result['features'])}\n\n")
            
            f.write("Coefficients:\n")
            for feature, coef in zip(result['features'], result['model'].coef_):
                f.write(f"  {feature:15}: {coef:8.3f}\n")
            f.write(f"  {'intercept':15}: {result['model'].intercept_:8.3f}\n")
            f.write("\n" + "="*50 + "\n\n")
    
    print("Analysis complete! Results saved to multivariate_regression_results.txt")
    return results

if __name__ == "__main__":
    results = main() 