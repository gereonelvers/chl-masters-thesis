import pandas as pd
import matplotlib.pyplot as plt
import numpy as np
from pathlib import Path

# ---------- config ----------
file_path = Path('./fishnet-metrics.csv')
output_prefix = '../assets/04/'
# Rationale: Starting and ending Play Mode in Unity can cause spikes in metrics, that are not representative of the actual network performance.
cut_start_seconds = 5                        # seconds to cut from start
cut_end_seconds = 0                          # seconds to cut from end

# Color scheme
accent_purple = "#a44b9c"
light_gray = "#f5f5f5"
dark_gray = "#2c2c2c"
medium_gray = "#666666"
border_gray = "#cccccc"
# ----------------------------

# 1. Load data
df = pd.read_csv(file_path)
if df.empty:
    raise ValueError("No data in file!")

# 2. Apply time cuts
if cut_start_seconds > 0 or cut_end_seconds > 0:
    min_time = df["t_s"].min()
    max_time = df["t_s"].max()
    
    start_cutoff = min_time + cut_start_seconds
    end_cutoff = max_time - cut_end_seconds
    
    df = df[(df["t_s"] >= start_cutoff) & (df["t_s"] <= end_cutoff)]
    
    if df.empty:
        raise ValueError("No data remaining after time cuts!")

# 3. Derived metrics
df["dt"] = df["t_s"].diff().fillna(df["t_s"].iloc[0]).replace(0, np.nan)

df["kbps_in"]  = (df["bytesIn"]  * 8) / (df["dt"] * 1000)
df["kbps_out"] = (df["bytesOut"] * 8) / (df["dt"] * 1000)

df.replace([np.inf, -np.inf], np.nan, inplace=True)
df.fillna(0, inplace=True)

# 4. Summary metrics
metrics_df = pd.DataFrame({
    "RTT_ms":   df["rtt_ms"].agg(["count", "mean", "std", "min", "max"]),
    "kbps_in":  df["kbps_in"].agg(["count", "mean", "std", "min", "max"]),
    "kbps_out": df["kbps_out"].agg(["count", "mean", "std", "min", "max"]),
}).T.round(2)

totals_series = pd.Series({
    "Total_KB_in":  round(df["bytesIn"].sum()  / 1024, 2),
    "Total_KB_out": round(df["bytesOut"].sum() / 1024, 2)
})

# 5. Aggregate to 1‑second buckets to smooth zero gaps
sec = df.groupby(df["t_s"].astype(int)).agg(
    bytesIn=("bytesIn", "sum"),
    bytesOut=("bytesOut", "sum"),
    rtt_ms=("rtt_ms", "mean")
).reset_index().rename(columns={"t_s": "second"})

sec["kbps_in"]  = sec["bytesIn"]  * 8 / 1000
sec["kbps_out"] = sec["bytesOut"] * 8 / 1000

# 6. Smoothing function for outlier detection
def smooth_with_outliers(x, y, window=5, outlier_threshold=2.0):
    """Apply rolling mean smoothing while identifying outliers"""
    smooth_y = y.rolling(window=window, center=True, min_periods=1).mean()
    residuals = np.abs(y - smooth_y)
    outlier_mask = residuals > (outlier_threshold * residuals.std())
    return smooth_y, outlier_mask

# 7. Plots -----------------------------------------------------
# RTT plot PDF with smoothing and outlier detection
plt.figure(figsize=(10,4))
plt.rcParams.update({'text.color': dark_gray, 'axes.labelcolor': dark_gray})
rtt_smooth, rtt_outliers = smooth_with_outliers(sec["second"], sec["rtt_ms"])

# Plot smoothed line
plt.plot(sec["second"], rtt_smooth, linestyle='-', color=accent_purple, alpha=0.8, linewidth=1.5, label='Smoothed RTT')
# Plot outliers as small dots
outlier_indices = sec[rtt_outliers]
if not outlier_indices.empty:
    plt.scatter(outlier_indices["second"], outlier_indices["rtt_ms"], 
               color=dark_gray, s=8, alpha=0.7, label='Outliers', zorder=5)

plt.xlabel("Time (s)")
plt.ylabel("RTT (ms)")
plt.title("RTT")
plt.legend()
plt.gca().spines['top'].set_visible(False)
plt.gca().spines['right'].set_visible(False)
plt.gca().spines['left'].set_color(border_gray)
plt.gca().spines['bottom'].set_color(border_gray)
plt.gca().set_facecolor(light_gray)
plt.tight_layout()
plt.savefig(output_prefix + 'rtt.pdf')
plt.close()

# Bandwidth plot PDF with outlier detection
plt.figure(figsize=(10,4))
plt.rcParams.update({'text.color': dark_gray, 'axes.labelcolor': dark_gray})
in_smooth, in_outliers = smooth_with_outliers(sec["second"], sec["kbps_in"])
out_smooth, out_outliers = smooth_with_outliers(sec["second"], sec["kbps_out"])

# Plot lines
plt.plot(sec["second"], in_smooth, label="Inbound", color=accent_purple, linewidth=1.5)
plt.plot(sec["second"], out_smooth, label="Outbound", color=medium_gray, linewidth=1.5)

# Plot outliers
in_outlier_indices = sec[in_outliers]
out_outlier_indices = sec[out_outliers]
if not in_outlier_indices.empty:
    plt.scatter(in_outlier_indices["second"], in_outlier_indices["kbps_in"], 
               color=accent_purple, s=8, alpha=0.7, label='Inbound outliers', zorder=5)
if not out_outlier_indices.empty:
    plt.scatter(out_outlier_indices["second"], out_outlier_indices["kbps_out"], 
               color=dark_gray, s=8, alpha=0.7, label='Outbound outliers', zorder=5)

plt.xlabel("Time (s)")
plt.ylabel("kbps")
plt.title("Bandwidth")
plt.legend()
plt.gca().spines['top'].set_visible(False)
plt.gca().spines['right'].set_visible(False)
plt.gca().spines['left'].set_color(border_gray)
plt.gca().spines['bottom'].set_color(border_gray)
plt.gca().set_facecolor(light_gray)
plt.tight_layout()
plt.savefig(output_prefix + 'bandwidth.pdf')
plt.close()

# 8. Print summary statistics
print("\n=== Network Performance Summary ===")
print(f"RTT (ms):")
print(f"  Average: {df['rtt_ms'].mean():.2f} ± {df['rtt_ms'].std():.2f}")
print(f"  99th percentile: {df['rtt_ms'].quantile(0.99):.2f}")

print(f"\nInbound Bandwidth (kbps):")
print(f"  Average: {df['kbps_in'].mean():.2f} ± {df['kbps_in'].std():.2f}")
print(f"  99th percentile: {df['kbps_in'].quantile(0.99):.2f}")

print(f"\nOutbound Bandwidth (kbps):")
print(f"  Average: {df['kbps_out'].mean():.2f} ± {df['kbps_out'].std():.2f}")
print(f"  99th percentile: {df['kbps_out'].quantile(0.99):.2f}")

print(f"\nTotal Data Transfer:")
print(f"  Inbound: {totals_series['Total_KB_in']} KB")
print(f"  Outbound: {totals_series['Total_KB_out']} KB")

# notify user of files
print("\nPDFs saved:",
      output_prefix + 'rtt.pdf',
      output_prefix + 'bandwidth.pdf')
