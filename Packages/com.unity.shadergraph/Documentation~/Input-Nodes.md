# Input nodes

Supply shaders with essential data such as constants, mesh attributes, gradients, matrices, deformation, PBR parameters, scene details, and texture sampling options.

## Basic

| **Topic**                    | **Description**                                                      |
|------------------------------|----------------------------------------------------------------------|
| [Boolean](Boolean-Node.md)   | Defines a constant Boolean value in the shader.                      |
| [Color](Color-Node.md)       | Defines a constant Vector 4 value in the shader using a Color field. |
| [Constant](Constant-Node.md) | Defines a Float of a mathematical constant value in the shader.      |
| [Integer](Integer-Node.md)   | Defines a constant Float value in the shader using an Integer field. |
| [Slider](Slider-Node.md)     | Defines a constant Float value in the shader using a Slider field.   |
| [Time](Time-Node.md)         | Provides access to various Time parameters in the shader.            |
| [Float](Float-Node.md)       | Defines a Float value in the shader.                                 |
| [Vector 2](Vector-2-Node.md) | Defines a Vector 2 value in the shader.                              |
| [Vector 3](Vector-3-Node.md) | Defines a Vector 3 value in the shader.                              |
| [Vector 4](Vector-4-Node.md) | Defines a Vector 4 value in the shader.                              |

## Geometry

| **Topic**                                    | **Description**                                                         |
|----------------------------------------------|-------------------------------------------------------------------------|
| [Bitangent Vector](Bitangent-Vector-Node.md) | Provides access to the mesh vertex or fragment's Bitangent Vector.      |
| [Normal Vector](Normal-Vector-Node.md)       | Provides access to the mesh vertex or fragment's Normal Vector.         |
| [Position](Position-Node.md)                 | Provides access to the mesh vertex or fragment's Position.              |
| [Screen Position](Screen-Position-Node.md)   | Provides access to the mesh vertex or fragment's Screen Position.       |
| [Tangent Vector](Tangent-Vector-Node.md)     | Provides access to the mesh vertex or fragment's Tangent Vector.        |
| [UV](UV-Node.md)                             | Provides access to the mesh vertex or fragment's UV coordinates.        |
| [Vertex Color](Vertex-Color-Node.md)         | Provides access to the mesh vertex or fragment's Vertex Color value.    |
| [View Direction](View-Direction-Node.md)     | Provides access to the mesh vertex or fragment's View Direction vector. |
| [Vertex ID](Vertex-ID-Node.md)               | Provides access to the mesh vertex or fragment's Vertex ID value.       |

## Gradient

| **Topic**                                  | **Description**                                                        |
|--------------------------------------------|------------------------------------------------------------------------|
| [Blackbody](Blackbody-Node.md)             | Samples a radiation based gradient from temperature input (in Kelvin). |
| [Gradient](Gradient-Node.md)               | Defines a constant Gradient in the shader.                             |
| [Sample Gradient](Sample-Gradient-Node.md) | Samples a Gradient given the input of Time.                            |

## Matrix

| **Topic**                                              | **Description**                                                                              |
|--------------------------------------------------------|----------------------------------------------------------------------------------------------|
| [Matrix 2x2](Matrix-2x2-Node.md)                       | Defines a constant Matrix 2x2 value in the shader.                                           |
| [Matrix 3x3](Matrix-3x3-Node.md)                       | Defines a constant Matrix 3x3 value in the shader.                                           |
| [Matrix 4x4](Matrix-4x4-Node.md)                       | Defines a constant Matrix 4x4 value in the shader.                                           |
| [Transformation Matrix](Transformation-Matrix-Node.md) | Defines a constant Matrix 4x4 value for a default Unity Transformation Matrix in the shader. |

## Mesh Deformation

| **Topic**                                                   | **Description**                                                                                                                                                                 |
|-------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Compute Deformation Node](Compute-Deformation-Node.md)     | Passes compute deformed vertex data to a vertex shader. Only works with the [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/). |
| [Linear Blend Skinning Node](Linear-Blend-Skinning-Node.md) | Applies Linear Blend Vertex Skinning. Only works with the [Entities Graphics package](https://docs.unity3d.com/Packages/com.unity.entities.graphics@latest/).                   |

## Sprite Deformation

| **Topic**                                       | **Description**                                                                                                                           |
|-------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------|
| [Sprite Skinning Node](Sprite-Skinning-Node.md) | Applies Vertex Skinning on Sprites. Only works with the [2D Animation](https://docs.unity3d.com/Packages/com.unity.2d.animation@latest/). |

## PBR

| **Topic**                                          | **Description**                                                         |
|----------------------------------------------------|-------------------------------------------------------------------------|
| [Dielectric Specular](Dielectric-Specular-Node.md) | Returns a Dielectric Specular F0 value for a physically based material. |
| [Metal Reflectance](Metal-Reflectance-Node.md)     | Returns a Metal Reflectance value for a physically based material.      |

## Scene

| **Topic**                                    | **Description**                                                              |
|----------------------------------------------|------------------------------------------------------------------------------|
| [Ambient](Ambient-Node.md)                   | Provides access to the Scene's Ambient color values.                         |
| [Camera](Camera-Node.md)                     | Provides access to various parameters of the current Camera.                 |
| [Fog](Fog-Node.md)                           | Provides access to the Scene's Fog parameters.                               |
| [Baked GI](Baked-GI-Node.md)                 | Provides access to the Baked GI values at the vertex or fragment's position. |
| [Object](Object-Node.md)                     | Provides access to various parameters of the Object.                         |
| [Reflection Probe](Reflection-Probe-Node.md) | Provides access to the nearest Reflection Probe to the object.               |
| [Scene Color](Scene-Color-Node.md)           | Provides access to the current Camera's color buffer.                        |
| [Scene Depth](Scene-Depth-Node.md)           | Provides access to the current Camera's depth buffer.                        |
| [Screen](Screen-Node.md)                     | Provides access to parameters of the screen.                                 |
| [Eye Index](Eye-Index-Node.md)               | Provides access to the Eye Index when stereo rendering.                      |

## Texture

| **Topic**                                                         | **Description**                                                                                   |
|-------------------------------------------------------------------|---------------------------------------------------------------------------------------------------|
| [Cubemap Asset](Cubemap-Asset-Node.md)                            | Defines a constant Cubemap Asset for use in the shader.                                           |
| [Sample Cubemap](Sample-Cubemap-Node.md)                          | Samples a Cubemap and returns a Vector 4 color value for use in the shader.                       |
| [Sample Reflected Cubemap Node](Sample-Reflected-Cubemap-Node.md) | Samples a Cubemap with reflected vector and returns a Vector 4 color value for use in the shader. |
| [Sample Texture 2D](Sample-Texture-2D-Node.md)                    | Samples a Texture 2D and returns a color value for use in the shader.                             |
| [Sample Texture 2D Array](Sample-Texture-2D-Array-Node.md)        | Samples a Texture 2D Array at an Index and returns a color value for use in the shader.           |
| [Sample Texture 2D LOD](Sample-Texture-2D-LOD-Node.md)            | Samples a Texture 2D at a specific LOD and returns a color value for use in the shader.           |
| [Sample Texture 3D](Sample-Texture-3D-Node.md)                    | Samples a Texture 3D and returns a color value for use in the shader.                             |
| [Sample Virtual Texture](Sample-Virtual-Texture-Node.md)          | Samples a Virtual Texture and returns color values for use in the shader.                         |
| [Sampler State](Sampler-State-Node.md)                            | Defines a Sampler State for sampling textures.                                                    |
| [Texture Size](Texture-Size-Node.md)                              | Returns the Width and Height of the texel size of Texture 2D input.                               |
| [Texture 2D Array Asset](Texture-2D-Array-Asset-Node.md)          | Defines a constant Texture 2D Array Asset for use in the shader.                                  |
| [Texture 2D Asset](Texture-2D-Asset-Node.md)                      | Defines a constant Texture 2D Asset for use in the shader.                                        |
| [Texture 3D Asset](Texture-3D-Asset-Node.md)                      | Defines a constant Texture 3D Asset for use in the shader.                                        |
