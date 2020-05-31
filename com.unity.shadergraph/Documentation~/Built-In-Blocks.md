# Built In Blocks

## Vertex Blocks
|            | Name            | Type     | Binding               | Description | 
|:-----------|:----------------|:---------|:----------------------|:------------|
| ![image]() | Vertex Position | Vector 3 | Object Space Position | Defines the absolute object space vertex position per vertex.|
| ![image]() | Vertex Normal   | Vector 3 | Object Space Normal | Defines the absolute object space vertex normal per vertex.|
| ![image]() | Vertex Tangent  | Vector 3 | Object Space Tangent | Defines the absolute object space vertex tangent per vertex.|

## Fragment Blocks
|            | Name     | Type     | Binding              | Description | 
|:-----------|:---------|:---------|:---------------------|:------------|
| ![image]() | Albedo | Vector 3 | None | Defines material's albedo value. Expected range 0 - 1. |
| ![image]() | Normal (Tangent Space) | Vector 3 | Tangent Space Normal | Defines material's normal value in tangent space. |
| ![image]() | Normal (Object Space) | Vector 3| Object Space Normal | Defines material's normal value in object space. |
| ![image]() | Normal (World Space) | Vector 3 | World Space Normal | Defines material's normal value in world space. |
| ![image]() | Emission | Vector 3 | None | Defines material's emission color value. Expects positive values. |
| ![image]() | Metallic | Vector 1 | None | Defines material's metallic value where 0 is non-metallic and 1 is metallic. |
| ![image]() | Specular | Vector 3 | None | Defines material's specular color value. Expected range 0 - 1.  |
| ![image]() | Smoothness | Vector 1 | None | Defines material's smoothness value. Expected range 0 - 1. |
| ![image]() | Occlusion | Vector 1 | None | Defines material's ambient occlusion value. Expected range 0 - 1. |
| ![image]() | Alpha | Vector 1 | None | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1. |
| ![image]() | Alpha Clip Threshold | Vector 1 | None | Fragments with an alpha below this value will be discarded. Expected range 0 - 1. |