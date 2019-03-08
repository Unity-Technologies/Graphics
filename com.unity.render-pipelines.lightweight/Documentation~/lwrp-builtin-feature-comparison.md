# Feature comparison table

This table provides an overview of the current features supported in the Lightweight Render Pipeline (LWRP), compared to the Unity Built-in render pipeline. To see which features are in development, check this [comparison road map](https://docs.google.com/spreadsheets/d/1nlS8m1OXStUK4A6D7LTOyHr6aAxIaA2r3uaNf9FZRTI/edit#gid=0).  

**Note:** If a feature is marked as __Not supported__, it's because Unity is not planning to support it in any release. If a feature is left _blank_, the LWRP team hasn't yet made a decision about whether to support it, and might still be researching the feature.


| Feature                                                      | Unity Built-in render pipeline                               | Lightweight Render Pipeline                                  |
| ------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| ***Camera***                                                 |                                                              |                                                              |
| HDR                                                          | Yes                                                          | Yes                                                          |
| MSAA                                                         | Yes                                                          | Yes                                                          |
| Physical Camera                                              | Yes                                                          |                                                              |
| Dynamic Resolution                                           | Yes                                                          | Yes                                                          |
| Multi Display                                                | Yes                                                          | Yes                                                          |
| Stacking                                                     | Yes                                                          |                                                              |
| Flare Layer                                                  | Yes                                                          | Not supported                                                |
| Depth Texture                                                | Yes                                                          | Yes                                                          |
| Depth + Normals Texture                                      | Yes                                                          | Not supported                                                |
| Color Texture                                                | Not supported                                                | Yes                                                          |
| Motion vectors                                               | Yes                                                          |                                                              |
| ***Realtime Lights***                                        |                                                              |                                                              |
| *Light Types*<br/>Directional<br/>Spot<br/>Point<br/>Area    | <br/>Yes<br/>Yes<br/>Yes<br/>Not supported                   | <br/>Yes<br/>Yes<br/>Yes<br/>Not supported                   |
| Inner Spot Angle                                             | Not supported                                                |                                                              |
| Shading                                                      | Multiple Passes                                              | Single Pass                                                  |
| *Culling*<br/>Per-Object<br/>Per-Layer                       | <br/>Yes<br/>Yes                                             | <br/>Yes<br/>Yes                                             |
| *Light Limits*<br/>Directional Lights<br/>Per-Object<br/>Per-Camera | <br/>Unlimited<br/>Unlimited<br/>Unlimited                   | <br/>1<br/>4<br/>16                                          |
| Attenuation                                                  | Legacy                                                       | InverseSquared                                               |
| Vertex LIghts                                                | Yes                                                          | Yes                                                          |
| SH Lights                                                    | Yes                                                          |                                                              |
| ***Realtime Shadows***                                       |                                                              |                                                              |
| *Light Types*<br/>Directional<br/>Spot<br/>Point<br/>Area    | <br/>Yes<br/>Yes<br/>Yes<br/>Not supported                   | <br/>Yes<br/>Yes<br/><br/>Not supported                      |
| *Shadow Projection*<br/>Stable Fit<br/>Close Fit             | <br/>Yes<br/>Yes                                             | <br/>Yes<br>                                                 |
| *Shadow Cascades*<br/>Number of Cascades<br/>Control by Percentage<br/>Control by Distance | <br/>1, 2 or 4<br/>Yes<br/>Not supported                     | <br/>1, 2 or 4<br/>Yes<br/>                                  |
| *Shadow Resolve Type*<br/>Lighting Pass<br/>Screen Space Pass | <br/>Yes<br/>Yes                                             | <br/>Yes<br/>Yes                                             |
| Shadow Bias                                                  | Constant clip space offset + normal bias                     | Offsets shadowmap texels in the light direction + normal bias. This gives better shadow bias control. |
| ***Batching***                                               |                                                              |                                                              |
| Static Batching (By Shader)                                  | Not supported                                                | Yes                                                          |
| Static Batching (By Material)                                | Yes                                                          | Yes                                                          |
| Dynamic Batching                                             | Yes                                                          | Yes                                                          |
| Dynamic Batching (Shadows)                                   | Yes                                                          |                                                              |
| GPU Instancing                                               | Yes                                                          | Yes                                                          |
| ***Color Space***                                            |                                                              |                                                              |
| Linear                                                       | Yes                                                          | Yes                                                          |
| Gamma                                                        | Yes                                                          | Yes                                                          |
| ***Global Illumination (Backing Back End)***                 |                                                              |                                                              |
| Enlighten                                                    | Yes                                                          | Yes                                                          |
| Enlighten Realtime                                           | Yes                                                          |                                                              |
| Progressive CPU                                              | Yes                                                          | Yes                                                          |
| Progressive GPU                                              | Yes                                                          | Yes                                                          |
| ***Mixed Lighting***                                         |                                                              |                                                              |
| Subtractive                                                  | Yes                                                          | Yes                                                          |
| Baked Indirect                                               | Yes                                                          |                                                              |
| Shadow Mask                                                  | Yes                                                          |                                                              |
| Distance Shadow Mask                                         | Yes                                                          |                                                              |
| ***Global Illumination (Light Probes)***                     |                                                              |                                                              |
| Blending                                                     | Yes                                                          | Yes                                                          |
| Proxy Volume (LPPV)                                          | Yes                                                          |                                                              |
| Custom Provided                                              | Yes                                                          | Yes                                                          |
| Occlusion Probes                                             | Yes                                                          |                                                              |
| ***Global Illumination (Reflection Probes)***                |                                                              |                                                              |
| Realtime                                                     | Yes                                                          | Yes                                                          |
| Baked                                                        | Yes                                                          | Yes                                                          |
| *Sampling*<br/>Simple<br/>Blend Probes<br/>Blend Probes and Skybox | <br/>Yes<br/>Yes<br/>Yes                                     | <br/>Yes<br/><br/>                                           |
| Blox Projection                                              | Yes                                                          |                                                              |
| ***Global Illumination (Lightmap Modes)***                   |                                                              |                                                              |
| Non-Directional                                              | Yes                                                          | Yes                                                          |
| Directional                                                  | Yes                                                          | Yes                                                          |
| ***Global Illumination (Environmental)***                    |                                                              |                                                              |
| *Source*<br/>Skybox<br/>Gradient<br/>Color                   | <br/>Yes<br/>Yes<br/>Yes                                     | <br/>Yes<br/>Yes<br/>Yes                                     |
| *Ambient Mode*<br/>Realtime<br/>Baked                        | <br/>Yes<br/>Yes                                             | <br/><br/>Yes                                                |
| ***Skybox***                                                 |                                                              |                                                              |
| Procedural                                                   | Yes                                                          | Yes                                                          |
| 6 Sided                                                      | Yes                                                          | Yes                                                          |
| Cubemap                                                      | Yes                                                          | Yes                                                          |
| Panoramic                                                    | Yes                                                          | Yes                                                          |
| ***Fog***                                                    |                                                              |                                                              |
| Linear                                                       | Yes                                                          | Yes                                                          |
| Exponential                                                  | Yes                                                          | Yes                                                          |
| Exponential Squared                                          | Yes                                                          | Yes                                                          |
| ***Visual Effects Components***                              |                                                              |                                                              |
| Halo                                                         | Yes                                                          | Not supported                                                |
| Lens Flare                                                   | Yes                                                          | Not supported                                                |
| Trail Renderer                                               | Yes                                                          | Yes                                                          |
| Billboard Renderer                                           | Yes                                                          | Yes                                                          |
| Projector                                                    | Yes                                                          | Not supported                                                |
| ***Shaders (General)***                                      |                                                              |                                                              |
| Shader Graph                                                 | Not supported                                                | Yes                                                          |
| Surface Shaders                                              | Yes                                                          | Not supported                                                |
| Camera-relative Rendering                                    | Not supported                                                |                                                              |
| *Built-in Lit Uber Shader*<br/>Metallic Workflow<br/>Specular Workflow | <br/>Yes<br/>Yes                                             | Called [Lit Shader](lit-shader.md)<br/>Yes<br/>Yes           |
| *Surface Type and Blend Mode*<br/>Opaque<br/>Faded (Alpha Blend)<br/>Transparent<br/>Cutout<br/>Additive<br/>Multiply | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Not supported<br/>Not supported | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>        |
| *Surface Inputs*<br/>Albedo (Base Map)<br/>Specular<br/>Metallic<br/>Smoothness<br/>Ambient Occlusion<br/>Normal Map<br/>Detail Map<br/>Detail Normal Map<br/>Heightmap | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/> | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Not supported<br/>Not supported<br/>Not supported<br/> |
| Light Cookies                                                | Yes                                                          |                                                              |
| Parallax Mapping                                             | Yes                                                          | Not supported                                                |
| Light Distance Fade                                          | Not supported                                                |                                                              |
| Shadow Distance Fade                                         | Yes                                                          |                                                              |
| Shadow Cascade Blending                                      | Not supported                                                |                                                              |
| GPU Instancing                                               | Yes                                                          | Yes                                                          |
| Double Sided GI                                              | Yes                                                          | Yes                                                          |
| Two Sided                                                    | Not supported                                                | Yes                                                          |
| Order In Layer                                               | Not supported                                                |                                                              |
| ***Render Pipeline Hooks***                                  |                                                              |                                                              |
| Camera.RenderWithShader                                      | Yes                                                          | Not supported                                                |
| Camera.AddCommandBuffer*<br/>(Camera.Remove[All]CommandBuffer*) | Yes                                                          | Not supported                                                |
| Camera.Render                                                | Yes                                                          | Not supported                                                |
| Light.AddCommandBuffer*<br/>(LightRemove[All]CommandBuffer*) | Yes                                                          | Not supported                                                |
| OnPreCull                                                    | Yes                                                          | Not supported                                                |
| OnPreRender                                                  | Yes                                                          | Not supported                                                |
| OnPostRender                                                 | Yes                                                          | Not supported                                                |
| OnRenderImage                                                | Yes                                                          | Not supported                                                |
| OnRenderObject                                               | Yes                                                          | Not supported                                                |
| OnWillRenderObject                                           | Yes                                                          | Yes                                                          |
| OnBecameVisible                                              | Yes                                                          | Yes                                                          |
| OnBecameInvisible                                            | Yes                                                          | Yes                                                          |
| Camera Replacement Material                                  | Not supported                                                |                                                              |
| RenderPipeline.BeginFrameRendering                           | Not supported                                                | Yes                                                          |
| RenderPipeline.EndFrameRendering                             | Not supported                                                |                                                              |
| RenderPipeline.BeginCameraRendering                          | Not supported                                                | Yes                                                          |
| RenderPIpeline.EndCameraRendering                            | Not supported                                                |                                                              |
| LightweightRenderPipeline.RenderSingleCamera                 | Not supported                                                | Yes                                                          |
| ScriptableRenderPass                                         | Not supported                                                | Yes                                                          |
| Custom Renderers                                             | Not supported                                                |                                                              |
| ***Post-processing***                                        |                                                              |                                                              |
| Ambient Occlusion (MSVO)                                     | Yes                                                          |                                                              |
| Auto Exposure                                                | Yes                                                          | Yes                                                          |
| Bloom                                                        | Yes                                                          | Yes                                                          |
| Chromatic Aberration                                         | Yes                                                          | Yes                                                          |
| Color Grading                                                | Yes                                                          | Yes                                                          |
| Depth of Field                                               | Yes                                                          | Yes                                                          |
| Grain                                                        | Yes                                                          | Yes                                                          |
| Lens Distortion                                              | Yes                                                          | Yes                                                          |
| Motion Blur                                                  | Yes                                                          |                                                              |
| Screen Space Reflections                                     | Yes                                                          | Not supported                                                |
| Vignette                                                     | Yes                                                          | Yes                                                          |
| ***Particles***                                              |                                                              |                                                              |
| VFX Graph (GPU)                                              | Not supported                                                |                                                              |
| Particles System (CPU)                                       | Yes                                                          | Yes                                                          |
| *Shaders*<br/>Physically Based<br/>Simple LIghting (Blinn Phong)<br/>Unlit | <br/>Yes<br/>Yes<br/>Yes                                     | <br/>Yes ([Particles Lit](particles-lit-shader.md))<br/>Yes ([Particles Simple Lit](particles-simple-lit-shader.md))<br/>Yes ([Particles Unlit](particles-unlit-shader.md)) |
| Soft Particles                                               | Yes                                                          | Yes                                                          |
| Distortion                                                   | Yes                                                          | Yes                                                          |
| Flipbook Blending                                            | Yes                                                          | Yes                                                          |
| ***Terrain***                                                |                                                              |                                                              |
| *Shaders*<br/>Physically Based<br/>Simple Lighting (Blinn-Phong)<br/>Unlit<br/>Speed Tree<br/>Vegetation<br/>Detail | <br/>Yes<br/>Yes<br/>Not supported<br/>Yes<br/>Yes<br/>Yes   | <br/>Yes<br/><br/><br/><br/>Yes<br/>Yes                      |
| Wind Zone                                                    | Yes                                                          | Yes                                                          |
| Number of Layers                                             | Unlimited                                                    | 4                                                            |
| GPU Patch Generation                                         | Yes                                                          | Yes                                                          |
| Surface Mask                                                 | Not supported                                                |                                                              |
| ***2D***                                                     |                                                              |                                                              |
| Sprite                                                       | Yes                                                          | Yes                                                          |
| Tilemap                                                      | Yes                                                          | Yes                                                          |
| Sprite Shape                                                 | Yes                                                          | Yes                                                          |
| Pixel-Perfect                                                | Yes                                                          |                                                              |
| 2D Shape LIghts                                              | Not supported                                                |                                                              |
| 2D Point Lights with Normal Map                              | Not supported                                                |                                                              |
| 2D Point Lights with Shades                                  | Not supported                                                |                                                              |
| ***UI (Canvas Renderer)***                                   |                                                              |                                                              |
| Screen Space - Overlay                                       | Yes                                                          | Yes                                                          |
| Screen Space - Camera                                        | Yes                                                          |                                                              |
| World Space                                                  | Yes                                                          | Yes                                                          |
| Text Mesh Pro                                                | Yes                                                          | Yes                                                          |
| ***VR***                                                     |                                                              |                                                              |
| Mutipass                                                     | Yes                                                          |                                                              |
| Single Pass                                                  | Yes                                                          | Yes                                                          |
| Single Pass Instanced                                        | Yes                                                          | Yes                                                          |
| *Post-processing*<br>Oculus Rift<br/>OpenVR<br/>Steam VR<br/>PSVR<br/>WMR<br/>GearVR<br/>Cardboard<br/>Oculus Go<br/>Daydream | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes | <br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/>Yes<br/><br/><br/><br/><br/> |
| ***AR***                                                     |                                                              |                                                              |
| AR Toolkit                                                   | Yes                                                          | Yes                                                          |
| ***Debug***                                                  |                                                              |                                                              |
| Scene view modes                                             | Yes                                                          |                                                              |
