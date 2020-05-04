# Shader Quality
The shader quality can be set to be one of three settings __Low__, __Medium__ or __High__.
## Default values:
If the shader quality is not defined at compile time then the shader quality will be defined to be:
__Low__ if OpenGL ES 2.0/WebGL 1.0 is targeted. __Medium__ if the mobile platform is targeted. __High__ is chosen if the other 2 does not apply to the target.

## Feature support:
The supported features for each setting can be seen in the table below.
| __Feature__         | __Description__           | __Low__    | __Medium__ | __High__   |
| ------------------- | ------------------------- | ---------- | ---------- | ---------- |
| __Reflection Probe__| This enables reflection probes. | x | x | x |
| __Bump Scale__ | This enables scaling the bump map. |||x|
| __Shadow Fading__   | This enables blending between the visible shadows and the invisible shadows when the shadows are too far away to be rendered. |  | x | x |

## Normalization of normals:
The normalization of the normals also changes with the shader quality:
__Low__: Normalize either per-vertex or per-pixel depending if normal map is sampled.
__Medium__: Always normalize per-vertex. Normalize per-pixel only if using normal map
__High__: Normalize in both vertex and pixel shaders.