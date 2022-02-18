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
[!include[](snippets/shader-properties/surface-options/surface-type.md)]
[!include[](snippets/shader-properties/surface-options/rendering-pass.md)]
[!include[](snippets/shader-properties/surface-options/blending-mode.md)]
[!include[](snippets/shader-properties/surface-options/preserve-specular-lighting.md)]
[!include[](snippets/shader-properties/surface-options/sorting-priority.md)]
[!include[](snippets/shader-properties/surface-options/receive-fog.md)]
[!include[](snippets/shader-properties/surface-options/depth-write.md)]
[!include[](snippets/shader-properties/surface-options/depth-test.md)]
[!include[](snippets/shader-properties/surface-options/cull-mode.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping.md)]
[!include[](snippets/shader-properties/surface-options/alpha-clipping-threshold.md)]
[!include[](snippets/shader-properties/surface-options/alpha-to-mask.md)]
[!include[](snippets/shader-properties/surface-options/double-sided.md)]
[!include[](snippets/shader-properties/surface-options/normal-mode.md)]
[!include[](snippets/shader-properties/surface-options/material-type-layered.md)]
[!include[](snippets/shader-properties/surface-options/transmission.md)]
[!include[](snippets/shader-properties/surface-options/receive-decals.md)]
[!include[](snippets/shader-properties/surface-options/geometric-specular-aa.md)]
[!include[](snippets/shader-properties/surface-options/screen-space-variance.md)]
[!include[](snippets/shader-properties/surface-options/gsaa-threshold.md)]
[!include[](snippets/shader-properties/surface-options/displacement-mode.md)]
[!include[](snippets/shader-properties/surface-options/lock-with-object-scale.md)]
[!include[](snippets/shader-properties/surface-options/lock-with-height-map-tiling-rate.md)]
</table>


### Tessellation Options

For information on the properties in this section, see the [Tessellation documentation](Tessellation.md).

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
[!include[](snippets/shader-properties/emission-inputs/use-emission-intensity.md)]
[!include[](snippets/shader-properties/emission-inputs/emissive-color.md)]
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-layered-tessellation.md)]
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-tiling.md)]
[!include[](snippets/shader-properties/emission-inputs/emission-uv-mapping-offset.md)]
[!include[](snippets/shader-properties/emission-inputs/emission-intensity.md)]
[!include[](snippets/shader-properties/emission-inputs/exposure-weight.md)]
[!include[](snippets/shader-properties/emission-inputs/emission-multiply-with-base.md)]
[!include[](snippets/shader-properties/emission-inputs/global-illumination.md)]
</table>


### Advanced Options
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/advanced-options/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)]
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)]
</table>
