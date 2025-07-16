#!/usr/bin/env python3
"""
Simple test to see exactly what's in the .obj files
"""

def analyze_obj_file(filepath):
    """Print exactly what's in the .obj file"""
    print(f"\n=== ANALYZING {filepath} ===")
    
    vertices = []
    faces = []
    objects = []
    current_object = "unknown"
    
    with open(filepath, 'r') as f:
        lines = f.readlines()
    
    print(f"Total lines: {len(lines)}")
    
    for i, line in enumerate(lines):
        line = line.strip()
        if line.startswith('o '):
            current_object = line[2:]
            objects.append(current_object)
            print(f"Line {i+1}: Object '{current_object}'")
        elif line.startswith('v '):
            parts = line.split()
            if len(parts) >= 4:
                x, y, z = float(parts[1]), float(parts[2]), float(parts[3])
                vertices.append([x, y, z])
                if len(vertices) <= 5:  # Print first few vertices
                    print(f"Line {i+1}: Vertex {len(vertices)}: ({x:.3f}, {y:.3f}, {z:.3f})")
        elif line.startswith('f '):
            parts = line.split()
            face_verts = []
            for part in parts[1:]:
                vertex_idx = int(part.split('/')[0])
                face_verts.append(vertex_idx)
            faces.append(face_verts)
            if len(faces) <= 5:  # Print first few faces
                print(f"Line {i+1}: Face {len(faces)}: {face_verts} (object: {current_object})")
    
    print(f"\nSUMMARY:")
    print(f"Objects found: {objects}")
    print(f"Total vertices: {len(vertices)}")
    print(f"Total faces: {len(faces)}")
    
    # Show vertex range
    if vertices:
        import numpy as np
        vertices = np.array(vertices)
        print(f"Vertex X range: {vertices[:,0].min():.3f} to {vertices[:,0].max():.3f}")
        print(f"Vertex Y range: {vertices[:,1].min():.3f} to {vertices[:,1].max():.3f}")
        print(f"Vertex Z range: {vertices[:,2].min():.3f} to {vertices[:,2].max():.3f}")
    
    return vertices, faces, objects

if __name__ == "__main__":
    # Test with first bridge
    analyze_obj_file("0_SAKl2Kyg-jFYQhuSp/0-SAKl2Kyg-jFYQhuSp/0.obj") 