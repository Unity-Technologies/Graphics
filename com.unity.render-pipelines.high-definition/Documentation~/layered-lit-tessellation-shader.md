# Layered Lit Tessellation Shader

The Layered Lit Tessellation Shader allows you to stack up to four tessellated Materials on the same GameObject in the High Definition Render Pipeline (HDRP).

The Materials that it uses for each layer are HDRP [Lit Tessellation Materials](Lit-Tessellation-Shader.md). This makes it easy to create layered Materials that provide adaptive vertex density for meshes. The **Main Layer** is the undermost layer and can influence upper layers with albedo, normals, and height. HDRP renders **Layer 1**, **Layer 2**, and **Layer 3** in that order on top of the **Main Layer**. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

## Creating a Layered Lit Tessellation Material

To create a new Lit Tessellation Material:

1. Right-click in your Project's Asset window.
2. Select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Select the Material and, in the Inspector, select the **Shader** drop-down.
4. Select **HDRP > LayeredLitTessellation**.

## Properties
Surface Options
These properties control the overall look of your Material's surface and how Unity renders the Material on screen.

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](surface-type.md)]
[!include[](rendering-pass.md)]
[!include[](blending-mode.md)]
[!include[](preserve-specular-lighting.md)]
[!include[](sorting-priority.md)]
[!include[](receive-fog.md)]
[!include[](depth-write.md)]
[!include[](depth-test.md)]
[!include[](cull-mode.md)]
[!include[](alpha-clipping.md)]
[!include[](alpha-clipping-threshold.md)]
[!include[](alpha-to-mask.md)]
[!include[](double-sided.md)]
[!include[](normal-mode.md)]
[!include[](material-type-layered.md)]
[!include[](transmission.md)]
[!include[](receive-decals.md)]
[!include[](geometric-specular-aa.md)]
[!include[](screen-space-variance.md)]
[!include[](gsaa-threshold.md)]
[!include[](displacement-mode.md)]
[!include[](lock-with-object-scale.md)]
[!include[](lock-with-height-map-tiling-rate.md)]
</table>


### Tessellation Options
[!include[](snippets/shader-properties/tessellation-options.md)]

### Surface Inputs
[!include[](snippets/shader-properties/surface-inputs/layered-surface-inputs.md)]
### Layer List
[!include[](snippets/shader-properties/layer-list.md)]
### Layers
[!include[](snippets/shader-properties/layers.md)]
### Emission inputs
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/emission-inputs/use-emission-intensity.md)
[!include[](snippets/shader-properties/emission-inputs/emissive-color.md)
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-layered-tessellation.md)
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-tiling.md)
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-offset.md)
[!include[](snippets/shader-properties/emission-inputs/emission-intensity.md)
[!include[](snippets/shader-properties/emission-inputs/exposure-weight.md)
[!include[](snippets/shader-properties/emission-inputs/emission-multiply-with-base.md)
</table>


### Advanced Options
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/advanced-options/enable-gpu-instancing.md)
[!include[](snippets/shader-properties/advanced-options/baked-emission.md)
[!include[](snippets/shader-properties/advanced-options/motion-vector-for-vertex-animation.md)
[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)
</table>
