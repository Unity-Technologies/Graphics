# 3DS Physical Material shader

The 3DS Physical Material shader replicates the 3DS Physical Material shader available in Autodesk® 3DsMax for the High Definition Render Pipeline (HDRP). When Unity imports an FBX that includes a material using Autodesk's 3DS Physical Material shader, it applies HDRP's 3DS Physical Material shader to the material. The material properties and texture inputs are identical between the Unity and Autodesk versions of this shader. The materials themselves also look and respond to light similarly.

**Note**: There are slight differences between what you see in Autodesk® 3DsMax and what you see in HDRP. HDRP doesn't support some material features.

**Note**: The HDRP implementation of the 3DS Physical Material shader uses a Shader Graph.

## Creating a 3DS Physical Material

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
  <tr>
    <td>Surface Type</td>
    <td></td>
    <td></td>
    <td>Specifies whether the material supports transparency or not. Materials with a Transparent Surface Type are more resource intensive to render than Materials with an Opaque Surface Type. Depending on the option you select, HDRP exposes more properties. The options are:<br>• Opaque:<br>• Transparent: Simulates a translucent Material that light can penetrate, such as clear plastic or glass.<br>For more information about the feature and for the list of properties each Surface Type exposes, see the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Surface-Type.html">Surface Type documentation</a>.</td>
  </tr>
  <tr>
    <td></td>
    <td>Rendering Pass</td>
    <td></td>
    <td>Specifies the rendering pass that HDRP processes this material in.<br>• Before Refraction: Draws the GameObject before the refraction pass. This means that HDRP includes this Material when it processes refraction. To expose this option, select Transparent from the Surface Type drop-down.<br>• Default: Draws the GameObject in the default opaque or transparent rendering pass pass, depending on the Surface Type.<br>• Low Resolution: Draws the GameObject in half resolution after the Default pass.<br>• After post-process: For <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Unlit-Shader.html">Unlit Materials</a> only. Draws the GameObject after all post-processing effects.</td>
  </tr>
  <tr>
    <td></td>
    <td>Blending Mode</td>
    <td></td>
    <td>Specifies the method HDRP uses to blend the color of each pixel of the material with the background pixels. The options are:<br>• Alpha: Uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds.<br>• Additive: Adds the Material’s RGB values to the background color. The alpha channel of the Material modulates the intensity. A value of 0 adds nothing and a value of 1 adds 100% of the Material color to the background color.<br>• Premultiply: Assumes that you have already multiplied the RGB values of the Material by the alpha channel. This gives better results than Alpha blending when filtering images or composing different layers.<br>This property only appears if you set Surface Type to Transparent.</td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td>Preserve Specular Lighting</td>
    <td>Indicates whether to make alpha blending not reduce the intensity of specular highlights. This preserves the specular elements on the transparent surface, such as sunbeams shining off glass or water.<br>This property only appears if you set Surface Type to Transparent.</td>
  </tr>
  <tr>
    <td></td>
    <td>Sorting Priority</td>
    <td></td>
    <td>Allows you to change the rendering order of overlaid transparent surfaces. For more information and an example of usage, see the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Renderer-And-Material-Priority.html#SortingByMaterial">Material sorting documentation</a>.<br>This property only appears if you set Surface Type to Transparent.</td>
  </tr>
  <tr>
    <td></td>
    <td>Receive Fog</td>
    <td></td>
    <td>Indicates whether fog affects the transparent surface. When disabled, HDRP doesn't take this material into account when it calculates the fog in the Scene.</td>
  </tr>
  <tr>
    <td></td>
    <td>Transparent Depth Prepass</td>
    <td></td>
    <td>Indicates whether HDRP adds polygons from the transparent surface to the depth buffer to improve their sorting. HDRP performs this operation before the lighting pass and this process improves GPU performance.</td>
  </tr>
  <tr>
    <td></td>
    <td>Transparent Writes Motion Vectors</td>
    <td></td>
    <td>Indicates whether HDRP writes motion vectors for transparent GameObjects that use this Material. This allows HDRP to process effects like motion blur for transparent objects. For more information on motion vectors, see the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Motion-Vectors.html">motion vectors documentation</a>.<br>This property only appears if you set Surface Type to Transparent.</td>
  </tr>
  <tr>
    <td></td>
    <td>Depth Write</td>
    <td></td>
    <td>Indicates whether HDRP writes depth values for GameObjects that use this material.</td>
  </tr>
  <tr>
    <td></td>
    <td>Depth Test</td>
    <td></td>
    <td>Specifies the comparison function HDRP uses for the depth test.</td>
  </tr>
  <tr>
    <td></td>
    <td>Cull Mode</td>
    <td></td>
    <td>Specifies the face to cull for GameObjects that use this material. The options are:<br>• Front: Culls the front face of the mesh.<br>• Back: Culls the back face of the mesh.<br>This property only appears if you disable Double Sided.</td>
  </tr>
  <tr>
    <td>Double-Sided GI</td>
    <td></td>
    <td></td>
    <td>Determines how HDRP handles a material with regards to Double Sided GI. When selecting Auto, Double-Sided GI is enabled if the material is Double-Sided; otherwise selecting On or Off respectively enables or disables double sided GI regardless of the material's Double-Sided option. When enabled, the lightmapper accounts for both sides of the geometry when calculating Global Illumination. Backfaces aren't rendered or added to lightmaps, but get treated as valid when seen from other objects. When using the Progressive Lightmapper backfaces bounce light using the same emission and albedo as frontfaces. (Currently this setting is only available when baking with the Progressive Lightmapper backend.).</td>
  </tr>
  <tr>
    <td></td>
    <td>Normal Mode</td>
    <td></td>
    <td>Specifies the mode HDRP uses to calculate the normals for back facing geometry.<br>• Flip: The normal of the back face is 180° of the front facing normal. This also applies to the Material which means that it looks the same on both sides of the geometry.<br>• Mirror: The normal of the back face mirrors the front facing normal. This also applies to the Material which means that it inverts on the back face. This is useful when you want to keep the same shapes on both sides of the geometry, for example, for leaves.<br>• None: The normal of the back face is the same as the front face.<br>This property only appears if you enable Double-Sided.</td>
  </tr>
  <tr>
    <td>Receive Decals</td>
    <td></td>
    <td></td>
    <td>Indicates whether HDRP can draw decals on this material’s surface.</td>
  </tr>
  <tr>
    <td>Receive SSR</td>
    <td></td>
    <td></td>
    <td>Indicates whether HDRP includes this material when it processes the screen space reflection pass.<br>This property only appears if you set Surface Type to Opaque.</td>
  </tr>
  <tr>
    <td>Receive SSR Transparent</td>
    <td></td>
    <td></td>
    <td>Indicates whether HDRP includes this material when it processes the screen space reflection pass.<br>This property only appears if you set Surface Type to Transparent.</td>
  </tr>
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
  <tr>
    <td>Enable GPU Instancing</td>
    <td></td>
    <td>Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP can't render Meshes in one batch if they have different Materials, or if the hardware doesn't support GPU instancing. For example, you can't <a href="https://docs.unity3d.com/Manual/DrawCallBatching.html">static-batch</a> GameObjects that have an animation based on the object pivot, but the GPU can instance them.</td>
  </tr>
  <tr>
    <td>Emission</td>
    <td></td>
    <td>Toggles whether emission affects global illumination.</td>
  </tr>
  <tr>
    <td></td>
    <td>Global Illumination</td>
    <td>The mode HDRP uses to determine how color emission interacts with global illumination.<br>• Realtime: Select this option to make emission affect the result of real-time global illumination.<br>• Baked: Select this option to make emission only affect global illumination during the baking process.<br>• None: Select this option to make emission not affect global illumination.</td>
  </tr>
  <tr>
    <td>Motion Vector For Vertex Animation</td>
    <td></td>
    <td>Enable the checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause.</td>
  </tr>
</tbody>
</table>
