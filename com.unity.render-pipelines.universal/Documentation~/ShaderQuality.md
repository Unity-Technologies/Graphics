# Shader Quality
The shader quality can be set to be one of three settings __Low__, __Medium__ or __High__.
## Default values:
If the shader quality is not defined at compile time then the shader quality will be defined to be:
__Low__ if GLES is targeted. __Medium__ if moblie platfrom is targeted. __High__ is chosen if the other 2 does not apply to the target.

## Feature support:
The supported features for each setting can be seen in the tabel below.
| __Feature__         | __Description__           | __Low__    | __Medium__ | __High__   |
| ------------------- | ------------------------- | ---------- | ---------- | ---------- |
| __Reflection Probe__| This enables refection probes. | x | x | x |
| __Bump Scale__ | This enabels scaling the bump map. |||x|
| __Shadow Fading__   | This enables blending between the the visible shadows and the invisible shadows when the shadows is outside of which shadows to render. |  | x | x |

## Nomalization of normals:
The nomalization of the nomals also changes with the shader quality:
__Low__: Normalize either per-vertex or per-pixel depending if normalmap is sampled.
__Medium__: Always normalize per-vertex. Normalize per-pixel only if using normal map
__High__: Normalize in both vertex and pixel shaders.