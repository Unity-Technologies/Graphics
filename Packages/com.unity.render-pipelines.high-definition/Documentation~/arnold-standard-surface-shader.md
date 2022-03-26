# Arnold Standard Surface shader

The Arnold Standard Surface shader replicates the Arnold Standard Surface shader available in Autodesk® 3DsMax and Autodesk® Maya for the High Definition Render Pipeline (HDRP). When Unity imports an FBX that includes a material using Autodesk's Arnold Standard Surface shader, it applies HDRP's Arnold Standard Surface shader to the material. The material properties and texture inputs are identical between the Unity and Autodesk versions of this shader. The materials themselves also look and respond to light similarly.

**Note**: There are slight differences between what you see in Autodesk® Maya or Autodesk® 3DsMax and what you see in HDRP and HDRP doesn't support some material features.

![](Images/arnold-standard-surface-example-maya.png)

Arnold Standard materials seen in **Autodesk® Maya** viewport.

![](Images/arnold-standard-surface-example-unity.png)
The same materials imported from FBX seen in Unity.

Note that the HDRP implementation of this shader uses a Shader Graph.

## Creating an Arnold Standard Surface material

When Unity imports an FBX with a compatible Arnold shader, it automatically creates an Arnold material. If you want to manually create an Arnold Standard Surface material:

1. Create a new material (menu: **Assets** > **Create** > **Material**).
2. In the Inspector for the Material, click the Shader drop-down then click **HDRP** > **ArnoldStandardSurface** > **ArnoldStandardSurface**.

## Properties

### Surface Options

**Surface Options** control the look of your Material's surface and how Unity renders the Material on screen.

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
</table>

### Exposed Properties

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/arnold/base-color.md)]
[!include[](snippets/shader-properties/arnold/base-color-map.md)]
[!include[](snippets/shader-properties/arnold/metalness.md)]
[!include[](snippets/shader-properties/arnold/metalness-map.md)]
[!include[](snippets/shader-properties/arnold/specular-weight.md)]
[!include[](snippets/shader-properties/arnold/specular-color.md)]
[!include[](snippets/shader-properties/arnold/specular-color-map.md)]
[!include[](snippets/shader-properties/arnold/specular-roughness.md)]
[!include[](snippets/shader-properties/arnold/specular-roughness-map.md)]
[!include[](snippets/shader-properties/arnold/specular-ior.md)]
[!include[](snippets/shader-properties/arnold/specular-ior-map.md)]
[!include[](snippets/shader-properties/arnold/specular-anisotropy.md)]
[!include[](snippets/shader-properties/arnold/specular-rotation.md)]
[!include[](snippets/shader-properties/arnold/specular-rotation-map.md)]
[!include[](snippets/shader-properties/arnold/emission-color.md)]
[!include[](snippets/shader-properties/arnold/emission-color-map.md)]
[!include[](snippets/shader-properties/arnold/coat-weight.md)]
[!include[](snippets/shader-properties/arnold/coat-color.md)]
[!include[](snippets/shader-properties/arnold/coat-roughness.md)]
[!include[](snippets/shader-properties/arnold/coat-thickness.md)]
[!include[](snippets/shader-properties/arnold/coat-ior.md)]
[!include[](snippets/shader-properties/arnold/coat-normal.md)]
[!include[](snippets/shader-properties/arnold/normal-map.md)]
[!include[](snippets/shader-properties/arnold/opacity-map.md)]
[!include[](snippets/shader-properties/arnold/opacity.md)]
</table>

### Advanced Options

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/general/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/general/emission.md)]
[!include[](snippets/shader-properties/general/emission-global-illumination.md)]
[!include[](snippets/shader-properties/general/motion-vector-for-vertex-animation.md)]
</table>
