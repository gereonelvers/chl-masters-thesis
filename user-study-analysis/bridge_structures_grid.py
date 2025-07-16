import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import os
import numpy as np

def create_bridge_structures_grid():
    """
    Create a grid visualization of all 32 bridge structures from the user study.
    Each dyad has 4 runs, creating a 8x4 grid organized by dyad and variant.
    Uses the correct Latin square randomization order for task variants.
    """
    
    # Define the dyad folders and their corresponding numbering
    dyad_folders = [
        "0_SAKl2Kyg-jFYQhuSp",
        "1_6m7xtdFy-kHxWHBLy", 
        "2_YeUp7E4D-WzyEiaaj",
        "3_OYYOwkG2-dckk6p6S",
        "4_kDp3Cy37-wocE408P",
        "5_Oa3Qww1v-zIDJJG4M",
        "6_uTSV9lZx-iB6kR2uo",
        "7_j7hHkgiC-iN9X5S4Q"
    ]
    
    # Latin square randomization from Table 5.2 in study design chapter
    # Each dyad follows a different sequence order
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
    
    # Create figure with subplots - much more compact to fit within page margins
    fig, axes = plt.subplots(8, 4, figsize=(12, 20))
    fig.suptitle('Bridge Structures by Dyad and Task Variant (N=32)', fontsize=16, fontweight='bold', y=0.98)
    
    # Track images loaded and missing
    images_loaded = 0
    missing_images = []
    
    for dyad_idx, dyad_folder in enumerate(dyad_folders):
        
        for period_idx in range(4):  # Period 1-4
            # Calculate the global image number (0-31)
            img_num = dyad_idx * 4 + period_idx
            
            # Get the task variant for this dyad and period from Latin square
            variant_name = latin_square_sequences[dyad_idx][period_idx]
            
            # Construct the path to the image
            run_folder = f"{img_num}-{dyad_folder.split('_')[1]}"
            img_path = os.path.join(dyad_folder, run_folder, f"{img_num}.png")
            
            ax = axes[dyad_idx, period_idx]
            
            try:
                # Load and display the image
                img = mpimg.imread(img_path)
                ax.imshow(img)
                ax.axis('off')
                
                # Add title with image number and variant - smaller font
                title = f"#{img_num}: {variant_name}"
                ax.set_title(title, fontsize=8, fontweight='bold', pad=3)
                
                images_loaded += 1
                
            except FileNotFoundError:
                # Handle missing images
                ax.text(0.5, 0.5, f'Image #{img_num}\nNot Found', 
                       horizontalalignment='center', verticalalignment='center',
                       transform=ax.transAxes, fontsize=10, 
                       bbox=dict(boxstyle="round,pad=0.3", facecolor="lightgray"))
                ax.set_xlim(0, 1)
                ax.set_ylim(0, 1)
                ax.axis('off')
                ax.set_title(f"#{img_num}: {variant_name} (Missing)", 
                           fontsize=8, color='red', pad=3)
                missing_images.append(img_num)
            
            except Exception as e:
                # Handle other errors
                ax.text(0.5, 0.5, f'Error loading\nimage #{img_num}', 
                       horizontalalignment='center', verticalalignment='center',
                       transform=ax.transAxes, fontsize=10,
                       bbox=dict(boxstyle="round,pad=0.3", facecolor="lightcoral"))
                ax.set_xlim(0, 1)
                ax.set_ylim(0, 1)
                ax.axis('off')
                ax.set_title(f"#{img_num}: {variant_name} (Error)", 
                           fontsize=8, color='red', pad=3)
                missing_images.append(img_num)
        
        # Add simplified dyad label on the left (without IDs) - smaller font and closer
        fig.text(0.03, 0.92 - (dyad_idx * 0.105), f'Dyad {dyad_idx}', 
                rotation=90, verticalalignment='center', fontsize=10, fontweight='bold')
    
    # Add period labels at the top - smaller font and lower position
    for period_idx in range(4):
        fig.text(0.16 + (period_idx * 0.2), 0.96, f'Period {period_idx + 1}', 
                horizontalalignment='center', fontsize=12, fontweight='bold')
    
    # Adjust layout to be much more compact
    plt.tight_layout()
    plt.subplots_adjust(left=0.1, top=0.94, bottom=0.02, right=0.98, hspace=0.12, wspace=0.03)
    
    # Save the figure
    output_path = "../assets/06/bridge_structures_grid.pdf"
    plt.savefig(output_path, dpi=300, bbox_inches='tight', 
                facecolor='white', edgecolor='none')
    
    print(f"Bridge structures grid saved to: {output_path}")
    print(f"Successfully loaded {images_loaded} out of 32 images")
    if missing_images:
        print(f"Missing images: {missing_images}")
    
    # Also save as PNG for easier viewing
    png_output_path = "../assets/06/bridge_structures_grid.png"
    plt.savefig(png_output_path, dpi=300, bbox_inches='tight', 
                facecolor='white', edgecolor='none')
    print(f"Bridge structures grid also saved as: {png_output_path}")
    
    # Print the Latin square mapping for verification
    print("\nLatin Square Mapping:")
    for dyad_idx, sequence in enumerate(latin_square_sequences):
        print(f"Dyad {dyad_idx}: {' -> '.join(sequence)}")
    
    plt.show()

if __name__ == "__main__":
    # Change to the user-study-analysis directory
    os.chdir('/Users/gereonelvers/Desktop/Uni/CAM/thesis/user-study-analysis')
    create_bridge_structures_grid() 