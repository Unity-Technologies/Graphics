# Terrain Shaders

The Terrain Shaders sample shows you how to design your own terrain material solutions and customize them to fit your specific goals and performance budgets. Possibilities include high quality tile repetition break-up solutions, detail mapping, smooth blending with the distant material, parallax mapping, auto materials, triplanar projection, and more.

This sample includes the following:
- A few examples of effects that you can achieve with Shader Graph on Terrain.
- Several components to mix and match in your own shaders.
- Guidelines to tackle common problems and help build community consensus around best practices.


| Topic | Description   |
|:------|:--------------|
| **[Texture Packing Schemes](Shader-Graph-Sample-Terrain-Packing.md)** | Describes the ways that the various terrain texture maps (color, normal, smoothness, ambient occlusion, and height) are packed together to reduce the number of texture samples required in the shaders.|
| **[Shaders](Shader-Graph-Sample-Terrain-Shaders.md)** | A description of each of the example terrain shaders included in the sample. |
| **[Layer Types](Shader-Graph-Sample-Terrain-Layers.md)** | A description of each of the terrain layer type subgraphs included in the sample. Layer types generally determine the type of tile break-up, but can also include other features such as parallax mapping, detail mapping, or triplanar projection. |
| **[Components and Blends](Shader-Graph-Sample-Terrain-Components.md)** | A description of each of the component subgraphs included in the sample. |
| **[Problems and Solutions](Shader-Graph-Sample-Terrain-Solutions.md)** | A list of potential problems or questions you might have when working with terrain shaders and some answers and solutions. |
| **[Performance Comparison](Shader-Graph-Sample-Terrain-Performance.md)** | Performance scores for each of the included shaders along with some observations about the results and what we can learn from the comparisons. |

