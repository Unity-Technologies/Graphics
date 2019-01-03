**Note:** This page is subject to change during the 2019.1 beta cycle.

# Upgrading your shaders

If your Project uses shaders from the built-in render pipeline, and you want to switch your Project to use the Lightweight Render Pipeline instead, you must convert those shader to the LWRP shaders. This is because built-in Lit shaders are not compatible with LWRP shaders. For an overview of the mapping between built-in shaders and LWRP shaders, see [Shader mappings](#shader-mappings).

To upgrade built-in shaders:

1. Open your Project in Unity, and go to __Edit__ > __Render Pipeline__. 
2. According to your needs, select either __Upgrade Project Materials to Lightweight RP Materials__ or __Upgrade Scene Materials to Lightweight RP Materials__.

**Note:** These changes cannot be undone. Backup your Project before you upgrade it.

## Shader mappings

The table below shows which LWRP shaders the Unity built-in shaders convert to when you run the shader upgrader.

| Unity built-in shader                             | Lightweight Render Pipeline shader          |
| ------------------------------------------------- | ------------------------------------------- |
| Standard                                          | Lightweight Render Pipeline/Lit             |
| Standard (Specular Setup)                         | Lightweight Render Pipeline/Lit             |
| Standard Terrain                                  | Lightweight Render Pipeline/Terrain/Lit     |
| Particles/Standard Surface                        | Lightweight Render Pipeline/Particles/Lit   |
| Particles/Standard Unlit                          | Lightweight Render Pipeline/Particles/Unlit |
| Mobile/Diffuse                                    | Lightweight Render Pipeline/Simple Lit      |
| Mobile/Bumped Specular                            | Lightweight Render Pipeline/Simple Lit      |
| Mobile/Bumped Specular(1 Directional Light)       | Lightweight Render Pipeline/Simple Lit      |
| Mobile/Unlit (Supports Lightmap)                  | Lightweight Render Pipeline/Simple Lit      |
| Mobile/VertexLit                                  | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Diffuse                            | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Specular                           | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Bumped Diffuse                     | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Bumped Specular                    | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Diffuse               | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Bumped Diffuse        | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Specular              | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Self-Illumin/Bumped Specular       | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Diffuse                | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Specular               | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Bumped Diffuse         | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Bumped Specular        | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Diffuse         | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Specular        | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Bumped Diffuse  | Lightweight Render Pipeline/Simple Lit      |
| Legacy Shaders/Transparent/Cutout/Bumped Specular | Lightweight Render Pipeline/Simple Lit      |