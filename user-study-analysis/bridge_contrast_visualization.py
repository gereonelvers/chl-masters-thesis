import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import os
import numpy as np

def create_bridge_contrast_visualization():
    """
    Create a visualization contrasting traditional bridge builders (runs 0-3) 
    with number optimizers (runs 12-15). Shows 2 rows of 4 images each.
    """
    
    # Define the specific runs we want to show
    # Row 1: Traditional bridge builders (Dyad 0, runs 0-3)
    # Row 2: Number optimizers (Dyad 3, runs 12-15)
    
    dyad_info = [
        {
            "dyad_folder": "0_SAKl2Kyg-jFYQhuSp",
            "runs": [0, 1, 2, 3],
            "label": "Traditional Bridge Builders (Dyad 0)",
            "description": "Engineering-focused approach with vertical elements"
        },
        {
            "dyad_folder": "3_OYYOwkG2-dckk6p6S", 
            "runs": [12, 13, 14, 15],
            "label": "Resource Optimizers (Dyad 3)",
            "description": "Efficiency-focused ground-level solutions"
        }
    ]
    
    # Latin square sequences to get task variant labels
    latin_square_sequences = [
        ["Open Ended", "Timed", "Silent", "Roleplay"],      # Sequence 1 (Dyad 0)
        ["Timed", "Roleplay", "Open Ended", "Silent"],       # Sequence 2 (Dyad 1)  
        ["Silent", "Open Ended", "Roleplay", "Timed"],       # Sequence 3 (Dyad 2)
        ["Roleplay", "Silent", "Timed", "Open Ended"],       # Sequence 4 (Dyad 3)
        ["Open Ended", "Timed", "Silent", "Roleplay"],      # Sequence 5 (Dyad 4)
        ["Timed", "Roleplay", "Open Ended", "Silent"],       # Sequence 6 (Dyad 5)
        ["Silent", "Open Ended", "Roleplay", "Timed"],       # Sequence 7 (Dyad 6)  
        ["Roleplay", "Silent", "Timed", "Open Ended"]        # Sequence 8 (Dyad 7)
    ]
    
    # Create figure with 2x4 subplot grid
    fig, axes = plt.subplots(2, 4, figsize=(16, 8))
    fig.suptitle('Development of Bridge Templates over Time', 
                 fontsize=16, fontweight='bold', y=0.95)
    
    # Track images loaded and missing
    images_loaded = 0
    missing_images = []
    
    for row_idx, dyad_data in enumerate(dyad_info):
        dyad_folder = dyad_data["dyad_folder"]
        runs = dyad_data["runs"]
        dyad_id = 0 if row_idx == 0 else 3  # Dyad 0 or Dyad 3
        
        for col_idx, run_num in enumerate(runs):
            # Get the task variant for this run
            period_idx = col_idx  # Period 0-3 corresponds to column 0-3
            variant_name = latin_square_sequences[dyad_id][period_idx]
            
            # Construct the path to the image
            run_folder = f"{run_num}-{dyad_folder.split('_')[1]}"
            img_path = os.path.join(dyad_folder, run_folder, f"{run_num}.png")
            
            ax = axes[row_idx, col_idx]
            
            try:
                # Load and display the image
                img = mpimg.imread(img_path)
                ax.imshow(img)
                ax.axis('off')
                
                # Add title with run number and variant
                title = f"Run #{run_num}: {variant_name}"
                ax.set_title(title, fontsize=12, fontweight='bold', pad=8)
                
                images_loaded += 1
                
            except FileNotFoundError:
                # Handle missing images
                ax.text(0.5, 0.5, f'Run #{run_num}\nNot Found', 
                       horizontalalignment='center', verticalalignment='center',
                       transform=ax.transAxes, fontsize=12, 
                       bbox=dict(boxstyle="round,pad=0.3", facecolor="lightgray"))
                ax.set_xlim(0, 1)
                ax.set_ylim(0, 1)
                ax.axis('off')
                ax.set_title(f"Run #{run_num}: {variant_name} (Missing)", 
                           fontsize=12, color='red', pad=8)
                missing_images.append(run_num)
            
            except Exception as e:
                # Handle other errors
                ax.text(0.5, 0.5, f'Error loading\nRun #{run_num}', 
                       horizontalalignment='center', verticalalignment='center',
                       transform=ax.transAxes, fontsize=12,
                       bbox=dict(boxstyle="round,pad=0.3", facecolor="lightcoral"))
                ax.set_xlim(0, 1)
                ax.set_ylim(0, 1)
                ax.axis('off')
                ax.set_title(f"Run #{run_num}: {variant_name} (Error)", 
                           fontsize=12, color='red', pad=8)
                missing_images.append(run_num)
        
    # Adjust layout
    plt.tight_layout()
    plt.subplots_adjust(left=0.12, top=0.88, bottom=0.05, right=0.98, hspace=0.15, wspace=0.05)
    
    # Save the figure
    output_path = "../assets/06/bridge_contrast_traditional_vs_optimized.pdf"
    plt.savefig(output_path, dpi=300, bbox_inches='tight', 
                facecolor='white', edgecolor='none')
    
    print(f"Bridge contrast visualization saved to: {output_path}")
    print(f"Successfully loaded {images_loaded} out of 8 images")
    if missing_images:
        print(f"Missing images: {missing_images}")
    
    # Also save as PNG for easier viewing
    png_output_path = "../assets/06/bridge_contrast_traditional_vs_optimized.png"
    plt.savefig(png_output_path, dpi=300, bbox_inches='tight', 
                facecolor='white', edgecolor='none')
    print(f"Bridge contrast visualization also saved as: {png_output_path}")
    
    # Print the mapping for verification
    print("\nBridge Contrast Mapping:")
    for dyad_data in dyad_info:
        print(f"{dyad_data['label']}: Runs {dyad_data['runs']}")
    
    plt.show()

if __name__ == "__main__":
    # Change to the user-study-analysis directory
    os.chdir('/Users/gereonelvers/Desktop/Uni/CAM/thesis/user-study-analysis')
    create_bridge_contrast_visualization() 