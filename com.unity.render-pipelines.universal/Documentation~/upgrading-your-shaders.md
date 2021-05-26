# Upgrading your Shaders

Lit shaders from the Built-in Render Pipeline are not compatible with URP shaders. When migrating a project from the Built-in Render Pipeline to URP, it's necessary to convert the shaders.

Unity provides the option to convert the built-in (non-custom) Built-in Render Pipeline shaders to URP-compatible shaders. If your project uses custom shaders, you need to convert them manually yo URP-compatible shaders. For information on writing  URP-compatible shaders, see [Writing custom shaders](writing-custom-shaders-urp.md).

To convert the built-in (non-custom) Built-in Render Pipeline shaders:

1. Open your Project in Unity, and go to __Edit__ > __Render Pipeline__ > **Universal Render Pipeline**.
2. According to your needs, select either __Upgrade Project Materials to URP Materials__ or __Upgrade Selected Materials to URP Materials__.

For an overview of the mapping between Built-in Render Pipeline shaders and URP shaders, see [Shader mappings](#shader-mappings).

> **Note:** These changes cannot be undone. Backup your project before you upgrade it.

> **Tip:** If the Preview thumbnails in Project View are incorrect after you've upgraded, try right-clicking anywhere in the Project View window and selecting __Reimport All__.

For [SpeedTree](https://docs.unity3d.com/Manual/SpeedTree.html) Shaders, Unity does not re-generate Materials when you re-import them, unless you click the **Generate Materials** or **Apply & Generate Materials** button.

<a name="built-in-to-urp-shader-mappings"></a>

## Shader mappings

The table below shows which URP shaders the Unity built-in shaders convert to when you run the shader upgrader.

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
