import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
import seaborn as sns
from scipy import stats
import warnings
warnings.filterwarnings('ignore')

# Load the questionnaire data
print("Loading questionnaire data...")
df = pd.read_excel('user-study-participant-surveys.xlsx')

# Display basic information about the dataset
print("\n=== DATASET OVERVIEW ===")
print(f"Shape: {df.shape}")
print(f"Columns: {list(df.columns)}")
print("\nFirst few rows:")
print(df.head())

print("\n=== DATA TYPES ===")
print(df.dtypes)

print("\n=== MISSING VALUES ===")
print(df.isnull().sum())

# Display unique values for categorical columns
print("\n=== UNIQUE VALUES IN CATEGORICAL COLUMNS ===")
for col in df.columns:
    if df[col].dtype == 'object' or df[col].nunique() < 10:
        print(f"\n{col}:")
        print(df[col].value_counts())

# Basic descriptive statistics
print("\n=== DESCRIPTIVE STATISTICS ===")
print(df.describe(include='all')) 