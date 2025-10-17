# Built-In Blocks

Define standard inputs and outputs for the Master Stack.

## Vertex Blocks

Set per-vertex attributes and pass data to fragments.

| Name                | Type     | Binding               | Description                                                                                                                                                                                                                                                                                                                                                                                                                                                |
|:--------------------|:---------|:----------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Position            | Vector 3 | Object Space Position | Defines the absolute object space vertex position per vertex.                                                                                                                                                                                                                                                                                                                                                                                              |
| Normal              | Vector 3 | Object Space Normal   | Defines the absolute object space vertex normal per vertex.                                                                                                                                                                                                                                                                                                                                                                                                |
| Tangent             | Vector 3 | Object Space Tangent  | Defines the absolute object space vertex tangent per vertex.                                                                                                                                                                                                                                                                                                                                                                                               |
| Color               | Vector 4 | Vertex Color          | Defines vertex color. Expected range 0 - 1.                                                                                                                                                                                                                                                                                                                                                                                                                |
| Custom Interpolator | Vector 4, vector 3, vector 2, or float | Custom Interpolator   | Passes custom data from the vertex stage to the fragment stage. |

## Fragment Blocks

Specify per-pixel material properties used for shading.

| Name                   | Type     | Binding              | Description                                                                                    |
|:-----------------------|:---------|:---------------------|:-----------------------------------------------------------------------------------------------|
| Base Color             | Vector 3 | None                 | Defines material's base color value. Expected range 0 - 1.                                     |
| Normal (Tangent Space) | Vector 3 | Tangent Space Normal | Defines material's normal value in tangent space.                                              |
| Normal (Object Space)  | Vector 3 | Object Space Normal  | Defines material's normal value in object space.                                               |
| Normal (World Space)   | Vector 3 | World Space Normal   | Defines material's normal value in world space.                                                |
| Emission               | Vector 3 | None                 | Defines material's emission color value. Expects positive values.                              |
| Metallic               | Float    | None                 | Defines material's metallic value, where 0 is non-metallic and 1 is metallic.                  |
| Specular               | Vector 3 | None                 | Defines material's specular color value. Expected range 0 - 1.                                 |
| Smoothness             | Float    | None                 | Defines material's smoothness value. Expected range 0 - 1.                                     |
| Ambient Occlusion      | Float    | None                 | Defines material's ambient occlusion value. Expected range 0 - 1.                              |
| Alpha                  | Float    | None                 | Defines material's alpha value. Used for transparency and/or alpha clip. Expected range 0 - 1. |
| Alpha Clip Threshold   | Float    | None                 | Fragments with an alpha below this value are discarded. Expected range 0 - 1.                  |

## Additional resources

[Add a custom interpolator](Custom-Interpolators.md)
