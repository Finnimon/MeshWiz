# Intro
This entire repo is meant to provide various utilities around [Meshes](./MeshWiz.Math/IMesh3.cs).

# Features
## Interface
- [ ] Avalonia + OpenGL based 3d
  - [ ] 3d geometry
  - [ ] Slicing lines
  - [ ] Realtime slicing visualization
  - [ ] AudioViz mode (3d rendering audio visualizer)
- [ ] CLI 
  - [ ] querying meshes
  - [ ] mesh gen (see practical utilities)
  - [ ] starting other interfaces
## File types
- [X] Stl support
- [ ] 3dxml
- [ ] custom compressed filetype from indexed mesh
## Practical utilities
- [ ] Planar Slicing
- [ ] Non Planar Slicing
- [ ] Flat segmentation slicing
- [ ] Mesh generation (ie "meshwiz gen sphere --radius ~~~~1 --centroid 0 0 0")
- [ ] geometry streaming from other services~~~~

# Roadmap
- [X] STL IO
- [X] Experimental 3d viewer 
- [ ] Mesh computation
  - [X] Indexing
  - [ ] **BVH** (CURRENT WIP)
- [ ] CLI interface
- [ ] Basic CAM
  - [ ] Planar Slicing
  - [ ] G-Code postprocessor
  - [ ] Triangle flat packing (Assembling parts from flat cut triangles)
- [ ] Other filetypes
- [ ] GUI

# Credits