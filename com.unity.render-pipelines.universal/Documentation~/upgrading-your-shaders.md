# Converting your shaders

Shaders written for the Built-in Render Pipeline are not compatible with the URP shaders.

For an overview of the mapping between built-in shaders and URP shaders, see [Shader mappings](#shader-mappings).

Use the [Render Pipeline Converter](features/rp-converter.md) to apply the shader mappings automatically.

> **NOTE:** The Render Pipeline Converter makes irreversible changes to the project. Back up your project before the conversion.

> **TIP:** If the preview thumbnails in the Project view are not shown correctly after the conversion, try right-clicking anywhere in the Project view and selecting __Reimport All__.

For [SpeedTree](https://docs.unity3d.com/Manual/SpeedTree.html) Shaders, Unity does not re-generate Materials when you re-import them, unless you click the **Generate Materials** or **Apply & Generate Materials** button.

<a name="built-in-to-urp-shader-mappings"></a>

## Shader mappings

The following table shows which URP shaders the Built-in Render Pipeline shaders convert to when you use the Render Pipeline Converter.

| Unity built-in shader                             | Universal Render Pipeline shader          |
| ------------------------------------------------- | ------------------------------------------- |
| Standard                                          | Universal Render Pipeline/Lit             |
| Standard (Specular Setup)                         | Universal Render Pipeline/Lit             |
| Standard Terrain                                  | Universal Render Pipeline/Terrain/Lit     |
| Particles/Standard Surface                        | Universal Render Pipeline/Particles/Lit   |
| Particles/Standard Unlit                          | Universal Render Pipeline/Particles/Unlit |
| Mobile/Diffuse                                    | Universal Render Pipeline/Simple Lit      |
| Mobile/Bumped Specular                            | Universal Render Pipeline/Simple Lit      |
| Mobile/Bumped Specular(1 Directional Light)       | Universal Render Pipeline/Simple Lit      |
| Mobile/Unlit (Supports Lightmap)                  | Universal Render Pipeline/Simple Lit      |
| Mobile/VertexLit                                  | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Diffuse                            | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Specular                           | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Bumped Diffuse                     | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Bumped Specular                    | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Diffuse               | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Bumped Diffuse        | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Specular              | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Bumped Specular       | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Diffuse                | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Specular               | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Bumped Diffuse         | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Bumped Specular        | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Diffuse         | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Specular        | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Bumped Diffuse  | Universal Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Bumped Specular | Universal Render Pipeline/Simple Lit      |
