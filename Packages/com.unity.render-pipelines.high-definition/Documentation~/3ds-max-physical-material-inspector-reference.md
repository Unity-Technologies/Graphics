# 3DS Physical Material Inspector reference

The 3DS Physical Material shader replicates the 3DS Physical Material shader available in Autodesk® 3DsMax for the High Definition Render Pipeline (HDRP). When Unity imports an FBX that includes a material using Autodesk's 3DS Physical Material shader, it applies HDRP's 3DS Physical Material shader to the material. The material properties and texture inputs are identical between the Unity and Autodesk versions of this shader. The materials themselves also look and respond to light similarly.

**Note**: There are slight differences between what you see in Autodesk® 3DsMax and what you see in HDRP. HDRP doesn't support some material features.

**Note**: The HDRP implementation of the 3DS Physical Material shader uses a Shader Graph.

## Create a 3DS Physical Material

When Unity imports an FBX with a compatible 3DS Physical Material shader, it automatically creates a 3DS Physical Material. If you want to manually create a 3DS Physical Material:

1. Create a new material (menu: **Assets** > **Create** > **Material**).
2. In the Inspector for the Material, click the Shader drop-down then click **HDRP** > **3DSMaxPhysicalMaterial** > **PhysicalMaterial3DSMax**.

## Properties

### Surface Options

**Surface Options** control the overall look of your Material's surface and how Unity renders the Material on screen.

<table>
<thead>
  <tr>
    <th>Property</th>
    <th></th>
    <th></th>
    <th>Description</th>
  </tr>
</thead>
<tbody>

[!include[](snippets/shader-properties/surface-options/surface-type.md)]
[!include[](snippets/shader-properties/surface-options/rendering-pass.md)]
[!include[](snippets/shader-properties/surface-options/blending-mode.md)]
[!include[](snippets/shader-properties/surface-options/preserve-specular-lighting.md)]
[!include[](snippets/shader-properties/surface-options/sorting-priority.md)]
[!include[](snippets/shader-properties/surface-options/receive-fog.md)]
[!include[](snippets/shader-properties/surface-options/transparent-depth-prepass.md)]
[!include[](snippets/shader-properties/surface-options/transparent-writes-motion-vectors.md)]
[!include[](snippets/shader-properties/surface-options/depth-write.md)]
[!include[](snippets/shader-properties/surface-options/depth-test.md)]
[!include[](snippets/shader-properties/surface-options/cull-mode.md)]
[!include[](snippets/shader-properties/surface-options/double-sided.md)]
[!include[](snippets/shader-properties/surface-options/normal-mode.md)]
[!include[](snippets/shader-properties/surface-options/receive-decals.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr.md)]
[!include[](snippets/shader-properties/surface-options/receive-ssr-transparent.md)]

</tbody>
</table>

### Exposed Properties
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>

[!include[](snippets/shader-properties/arnold/base-color-weight.md)]
[!include[](snippets/shader-properties/arnold/base-color.md)]
[!include[](snippets/shader-properties/arnold/base-color-map.md)]
[!include[](snippets/shader-properties/arnold/reflections-weight.md)]
[!include[](snippets/shader-properties/arnold/reflections-color.md)]
[!include[](snippets/shader-properties/arnold/reflections-color-map.md)]
[!include[](snippets/shader-properties/arnold/reflections-roughness.md)]
[!include[](snippets/shader-properties/arnold/reflections-roughness-map.md)]
[!include[](snippets/shader-properties/arnold/metalness.md)]
[!include[](snippets/shader-properties/arnold/metalness-map.md)]
[!include[](snippets/shader-properties/arnold/reflections-ior.md)]
[!include[](snippets/shader-properties/arnold/reflections-ior-map.md)]
[!include[](snippets/shader-properties/arnold/transparency-weight.md)]
[!include[](snippets/shader-properties/arnold/transparency.md)]
[!include[](snippets/shader-properties/arnold/transparency-map.md)]
[!include[](snippets/shader-properties/arnold/emission-weight.md)]
[!include[](snippets/shader-properties/arnold/emission.md)]
[!include[](snippets/shader-properties/arnold/emission-map.md)]
[!include[](snippets/shader-properties/arnold/bump-map-strength.md)]
[!include[](snippets/shader-properties/arnold/bump-map.md)]
[!include[](snippets/shader-properties/arnold/anisotropy.md)]
[!include[](snippets/shader-properties/arnold/anisotropy-map.md)]
[!include[](snippets/shader-properties/arnold/coat-normal.md)]
[!include[](snippets/shader-properties/arnold/coat-roughness.md)]
[!include[](snippets/shader-properties/arnold/coat-thickness.md)]
[!include[](snippets/shader-properties/arnold/coat-weight.md)]
[!include[](snippets/shader-properties/arnold/coat-color.md)]
[!include[](snippets/shader-properties/arnold/coat-ior.md)]

</table>

### Advanced Options

<table>
<thead>
  <tr>
    <th>Property</th>
    <th></th>
    <th>Description</th>
  </tr>
</thead>
<tbody>

[!include[](snippets/shader-properties/general/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/general/emission.md)]
[!include[](snippets/shader-properties/general/emission-global-illumination.md)]
[!include[](snippets/shader-properties/general/motion-vector-for-vertex-animation.md)]

</tbody>
</table>