# Built In Blocks

## Vertex Blocks
|            | Name            | Type     | Binding               | Description |
|:-----------|:----------------|:---------|:----------------------|:------------|
| ![image](images/Blocks-Vertex-Position.png) | Position | Vector 3 | Object Space Position | Defines the absolute object space vertex position per vertex.|
| ![image](images/Blocks-Vertex-Normal.png) | Normal   | Vector 3 | Object Space Normal | Defines the absolute object space vertex normal per vertex.|
| ![image](images/Blocks-Vertex-Tangent.png) | Tangent  | Vector 3 | Object Space Tangent | Defines the absolute object space vertex tangent per vertex.|

## Fragment Blocks
|            | Name     | Type     | Binding              | Description |
|:-----------|:---------|:---------|:---------------------|:------------|
| ![image](images/Blocks-Fragment-Base-Color.png) | Base Color | Vector 3 | None | Defines material's base color value. Expected range 0 - 1. |
| ![image](images/Blocks-Fragment-NormalTS.png) | Normal (Tangent Space) | Vector 3 | Tangent Space Normal | Defines material's normal value in tangent space. |
| ![image](images/Blocks-Fragment-NormalOS.png) | Normal (Object Space) | Vector 3| Object Space Normal | Defines material's normal value in object space. |
| ![image](images/Blocks-Fragment-NormalWS.png) | Normal (World Space) | Vector 3 | World Space Normal | Defines material's normal value in world space. |
| ![image](images/Blocks-Fragment-Emission.png) | Emission | Vector 3 | None | Defines material's emission color value. Expects positive values. |
| ![image](images/Blocks-Fragment-Metallic.png) | Metallic | Vector 1 | None | Defines material's metallic value, where 0 is non-metallic and 1 is metallic. |
| ![image](images/Blocks-Fragment-Specular.png) | Specular | Vector 3 | None | Defines material's specular color value. Expected range 0 - 1.  |
| ![image](images/Blocks-Fragment-Smoothness.png) | Smoothness | Vector 1 | None | Defines material's smoothness value. Expected range 0 - 1. |
| ![image](images/Blocks-Fragment-Ambient-Occlusion.png) | Ambient Occlusion | Vector 1 | None | Defines material's ambient occlusion value. Expected range 0 - 1. |
| ![image](images/Blocks-Fragment-Alpha.png) | Alpha | Vector 1 | None | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1. |
| ![image](images/Blocks-Fragment-Alpha-Clip-Threshold.png) | Alpha Clip Threshold | Vector 1 | None | Fragments with an alpha below this value are discarded. Expected range 0 - 1. |