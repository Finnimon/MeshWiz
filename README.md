# Intro
This entire repo is meant to provide various utilities around [Meshes](./MeshWiz.Math/IMesh.cs).

# Requirements
- .NET10
- OpenGL Driver
- [OpenCL Driver]

# How To Build
cd $repo_dir  
dotnet restore  
dotnet build [-c Release]

# Remarks
- See the different Polyline partials for more complex usage examples.
  - [Evaluate](./MeshWiz.Math/Polyline.Evaluate.cs)
  - [Simplicity](./MeshWiz.Math/Polyline.Simplicity.cs)
  - [Reduction](./MeshWiz.Math/Polyline.Reduction.cs)
  - ...
- Generally everything is generic
  - TNum always refers to a number-type such as half, float, double
  - TVec or TVector almost always refers to a vector-type that extends [IFloatingVector](./MeshWiz.Math/IFloatingVector.cs)
  - Number generic examples:
    - [Vector2](./MeshWiz.Math/Vector2.cs)
    - [Vector3](./MeshWiz.Math/Vector3.cs)
    - [Vector4](./MeshWiz.Math/Vector4.cs)
    - [Matrix4x4](./MeshWiz.Math/Matrix4x4.cs)
  - Most number-type generics offer a [To<TOtherNum>()](./MeshWiz.Math/Vector3.cs) Method to convert between the possible types
  - Dimensionally generic examples:
    - [Polyline](./MeshWiz.Math/Polyline.cs)
    - [Line](./MeshWiz.Math/Line.cs)
  - Some Dimensionally generic types offer strictly dimensional counterparts; when possible prefer those 
    - [NonDimensional Triangle](./MeshWiz.Math/Triangle.cs)
    - [2D Triangle](./MeshWiz.Math/Triangle2.cs)
    - [3D Triangle](./MeshWiz.Math/Triangle3.cs)

# Credits
I was greatly inspired by the following: 
- [Sam Byass' OpenTKAvalonia](https://github.com/SamboyCoding/OpenTKAvalonia) for my [OpenGLParent](./MeshWiz.Abstraction.Avalonia/OpenGLParent.cs) and [GLWrapper](./MeshWiz.Abstraction.Avalonia/GLWrapper.cs).
  I however decided to create them as reusable sealed Components for wrapping other [OpenGL abstractions](./MeshWiz.Abstraction.OpenTK/IOpenGLControl.cs).
- [Sebastian Lague's BVH Mesh](https://github.com/SebLague/Ray-Tracing/blob/main/Assets/Scripts/BVH.cs) 
  for my own [BvhMesh3](./MeshWiz.Math/BvhMesh3.cs) and all related components. 
  I However decided to place all the logic in a [Static Computer](./MeshWiz.Math/MeshMath.cs) for all things mesh.
