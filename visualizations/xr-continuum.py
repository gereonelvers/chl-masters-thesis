import matplotlib.pyplot as plt
from matplotlib.patches import FancyArrowPatch
import numpy as np

# Enforce a clean, modern sans-serif
plt.rcParams['font.family'] = 'sans-serif'
plt.rcParams['font.sans-serif'] = ['DejaVu Sans', 'Arial']

# Colors
accent = '#24d2b6'
text_color = '#333333'

# Figure and axis
fig, ax = plt.subplots(figsize=(12, 3))
ax.set_xlim(0, 100)
ax.set_ylim(0, 10)

# Virtuality Continuum: double-headed arrow
arrow = FancyArrowPatch(
    (10, 5), (90, 5),
    arrowstyle='<->,head_length=12,head_width=6',
    linewidth=4, color=accent
)
ax.add_patch(arrow)

# Mixed Reality bracket: horizontal then verticals down to arrow
ax.plot([30, 70], [6.5, 6.5], color=accent, linewidth=3)  # top horizontal
ax.plot([30, 30], [6.5, 5], color=accent, linewidth=3)    # left vertical down
ax.plot([70, 70], [6.5, 5], color=accent, linewidth=3)    # right vertical down

# Titles
ax.text(50, 8.2, 'Mixed Reality (MR)',
        ha='center', va='bottom',
        fontsize=16, fontweight='bold', color=text_color,
        fontfamily='sans-serif')
ax.text(50, 4.2, 'Virtuality Continuum (VC)',
        ha='center', va='top',
        fontsize=16, fontweight='bold', color=text_color,
        fontfamily='sans-serif')

# Section labels
sections = [
    ('Real\nEnvironment', 10),
    ('Augmented\nReality (AR)', 35),
    ('Augmented\nVirtuality (AV)', 65),
    ('Virtual\nEnvironment', 90),
]
for label, x in sections:
    ax.text(x, 2.2, label,
            ha='center', va='center',
            fontsize=12, color=text_color,
            fontfamily='sans-serif')

def main():
    # Clean up
    ax.axis('off')
    plt.tight_layout()
    plt.savefig('../assets/02/xr-continuum.pdf', bbox_inches='tight', dpi=300)
    plt.show()
    plt.close()

if __name__ == "__main__":
    main()
