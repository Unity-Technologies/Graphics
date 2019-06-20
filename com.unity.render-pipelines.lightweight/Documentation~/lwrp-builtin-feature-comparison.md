

**Note:** This page is subject to change during the 2019.1 beta cycle.

# Feature comparison

This is an overview of how features work in the Unity built-in Render Pipeline and in the Lightweight Render Pipeline.

|                          | Unity Built-in render pipeline                               | Lightweight Render Pipeline                                  |
| ------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| Platform Coverage        | All                                                          | All                                                          |
| Rendering Paths          | Multi-pass Forward<br />Multi-pass Deferred                  | Single-pass Forward                                          |
| Lighting Attenuation     | Separate precomputed attenuation textures for Point and Spot<br />Vertex light attenuation does not reach 0 intensity at range boundary. | Physically Based Light Attenuation. Light intensity decreases by the inverse-square law. |
| Color Space              | Linear with sRGB light intensity<br />sRGB                   | Linear with linear light intensity<br /> sRGB.               |
| Realtime Lights          | Directional, Spot and Point<br />Amount of pixel lights controlled by Quality Settings<br />Forward path limited to 8 pixel lights.<br />Supports up to 4 vertex point lights. | Directional, Spot and Point<br />Amount of pixel lights controlled by LWRP Asset<br />1 Main Directional light, always shaded per pixel<br />Up to 8 additional lights that can be shaded per pixel or per vertex |
| Light Modes              | Baked<br />Mixed<br />- Baked Indirect<br />- Shadow Mask<br />- Distance Shadowmask<br />- Subtractive<br />Realtime | Baked<br />Mixed (WIP / ETA Unity 19.1)<br />- Baked Indirect<br />- Shadow Mask<br />- Subtractive<br />Realtime |
| Global Illumination      | Directional, Spot, Point and Rectangular Area Lights<br />Baked<br />Lightmap (Non-Directional and Directional)<br />Light Probes<br />Realtime<br />Dynamic Lightmap<br />Realtime Lightprobes | Directional, Spot, Point and Rectangular Area Lights<br />Baked<br />Lightmap (Non-Directional and Directional)<br />Light Probes<br />Realtime GI Not Supported. |
| Light Culling            | Per-Object. No Compute.                                      | Per-Object. No Compute.                                      |
| Shader Library           | Dozens of non physically based shaders specializations<br />Unified Standard PBS Shaders<br />Metallic workflow<br />Specular workflow<br />Roughness workflow | Unified non-physically based shader (Simple Lit)<br/>Unified physically based shader (Lit) that covers Metallic and Specular workflows |
| Physically Based Shading | Disney Diffuse + Cook Torrance (GGX, Smith, Schlick) Specular<br />Lambertian Diffuse + Simplified Cook Torrance (GGX, Simplified KSK and Schlick) Specular<br />Lambertian Diffuse + Non-Microfaceted LUT Specular | Lambertian Diffuse + Simplified Cook Torrance (GGX, Simplified KSK and Schlick) Specular |
| Light Cookies            | Monochrome                                                   | Single light cookie support for the main light.              |
| Light Probes Modes       | One interpolated probe<br />LPPV                             | One interpolated probe                                       |
| Reflection Probes        | Sorted per-object<br />Blend between at most 2 probes        | Sorted per-object<br />No blending                           |
| Shadows Features         | PSSM Stable and Close Fit·         Filtering: PCF<br />No depth clip. <br />Pancaking done in vertex. | PSSM Stable Fit<br />Filtering: PCF<br />No depth clip. <br />Pancaking done in vertex. |
| Shadow Modes             | Light Space<br />Screen Space                                | Light Space<br />Screen Space                                |
| Shadow Casting Lights    | Directional, Spot, Point<br />Multiple shadow light casters, one per pass. | Directional and Spot<br />Single shadow light caster supported as main light. |
| General                  |                                                              |                                                              |
| Camera                   | Sorts camera by depth value<br />Stack Management<br />Groups by common camera state<br />Handles depth state between cameras | Sorts camera by depth value<br />No stack management<br />RenderTarget scale supported<br />Game renders at scaled resolution<br />UI renders at native resolution |
| Anti-Aliasing            | MSAA<br />TAA                                                | MSAA                                                         |
| Pipeline Additional Data | Motion Vectors                                               | None                                                         |
| Post-Processing          | Legacy Post-Processing stack<br />New Post-Processing Stack  | Subset of the new Post-Processing Stack FX<br />No support for: <br />- TAA<br />- Motion Blur<br />- SSR |
| Debug option             | Display GBuffer<br />Display various bake lighting view mode |                                                              |
| Sky lighting             | Procedural Sky<br />Cubemap/LatLong Sky<br />Ambient Lighting | Procedural Sky<br />Cubemap<br />Ambient Lighting            |

 







 



