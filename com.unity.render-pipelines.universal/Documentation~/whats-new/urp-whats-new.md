# What's new in URP 10

This section contains information about new features, improvements, and issues fixed in URP 10.

* [URP 10.5](#urp-10-5)
* [URP 10.4](#urp-10-4)
* [URP 10.3](#urp-10-3)
* [URP 10.2](#urp-10-2)
* [URP 10.0–10.1](#urp-10-1)

For a complete list of changes made in URP 10, refer to the [Changelog](../../changelog/CHANGELOG.html).

For information on the known issues in URP 10, see the section [Known issues](../known-issues.md).

# What's new in URP 10.5<a name="urp-10-5"></a>

URP 10.5 is a maintenance update, it contains fixes to issues found in previous releases of URP 10.

For a complete list of changes, refer to the [Changelog](../../changelog/CHANGELOG.html).

# What's new in URP 10.4<a name="urp-10-4"></a>

URP 10.4 is a maintenance update, it contains fixes to issues found in previous releases of URP 10.

For a complete list of changes, refer to the [Changelog](../../changelog/CHANGELOG.html).

# What's new in URP 10.3<a name="urp-10-3"></a>

URP 10.3 is a maintenance update, it contains fixes to issues found in previous releases of URP 10.

For a complete list of changes made in URP 10.3, refer to the [Changelog](../../changelog/CHANGELOG.html).

# What's new in URP 10.2<a name="urp-10-2"></a>

URP 10.2 is a maintenance update, it contains fixes to issues found in previous releases of URP 10.

For a complete list of changes made in URP 10.2, refer to the [Changelog](../../changelog/CHANGELOG.html).

# What's new in URP 10.0–10.1<a name="urp-10-1"></a>

This page contains an overview of new features, major improvements, and issues resolved in URP versions 10.0 and 10.1.

For a complete list of changes made in URP 10.1, refer to the [Changelog](../../changelog/CHANGELOG.html).

## Features

This section contains the overview of the new features in this release.

### Screen Space Ambient Occlusion (SSAO)

The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other. In the real world, such areas tend to block out or occlude ambient light, so they appear darker.

URP implements the real-time Screen Space Ambient Occlusion (SSAO) effect as a Renderer Feature.

![Scene showing the Ambient Occlusion effect turned On and Off.](../Images/whats-new/urp-10/ssao.png)<br/>*Scene showing the Ambient Occlusion effect turned On and Off.*

For more information on the Ambient Occlusion effect, see the page [Ambient Occlusion](../post-processing-ssao.md).

### Clear Coat

The Clear Coat feature adds an extra Material layer which simulates a transparent and thin coating on top of the base Material. The feature is available in the Complex Lit shader.

For more information on the feature, see section [Surface Inputs](../shader-complex-lit.md#surface-inputs) on the [Complex Lit](../shader-complex-lit.md) page.

![Clear Coat effect (Left: Off, Right: On)](../Images/whats-new/urp-10/clear-coat.png)<br/>*Clear Coat effect (Left: Off, Right: On)*

### Camera Normals Texture

URP 10.0 implements the `DepthNormals` Pass block that generates the normal texture `_CameraNormalsTexture` for the current frame.

URP creates a `_CameraNormalsTexture` if at least one render Pass requires it. To ensure that the URP Renderer creates the `_CameraNormalsTexture` texture, add a call to the <xref:UnityEngine.Rendering.Universal.ScriptableRenderPass.ConfigureInput*> method in `ScriptableRendererFeature.AddRenderPasses`.

### Detail Map, Detail Normal Map

A Detail map lets you overlay another texture on top of the Base Map. A Detail Normal Map is a special texture that lets you add surface detail such as bumps, grooves, and scratches which catch the light as if they exist in the mesh geometry.

![Detail Map](../Images/whats-new/urp-10/detail-map.png)<br/>*Left: rendered object only with the Base Map. Right: rendered object with the Detail Map.*

For more information, see section [Detail Inputs](../lit-shader.md#detail-inputs) on the [Lit shader](../lit-shader.md) page.

### Shadow Distance Fade

With Shadow Distance Fade, shadows fade smoothly when they reach the maximum shadow rendering distance (__Shadows > Max Distance__ in the render pipeline asset).

![Shadow Distance Fade](../Images/whats-new/urp-10/shadow-distance-fade.png)<br/>*Illustration showing the Shadow Distance Fade effect (right).*

### Shadow Cascade

With shadow cascades, you can avoid crude shadows close to the Camera and keep the Shadow Resolution reasonably low. For more information, see the page [Shadow Cascades](https://docs.unity3d.com/Manual/shadow-cascades.html).

URP supports 1–4 shadow cascades now, and also supports two working units, percent and meters.

![Shadow Cascades](../Images/lighting/urp-asset-shadows.png)<br/>*The Shadows section in the Universal Render Pipeline asset.*

### Shadowmask

URP 10.1 supports the Shadowmask Lighting Mode. Shadowmask Lighting Mode combines real-time direct lighting with baked indirect lighting. For more information, see the page [Lighting Mode: Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-Shadowmask.html).

<table style="text-align:center; border:none;">
  <tbody><tr>
    <td style="width:33%; border:none;"><img src="../Images/whats-new/urp-10/lightmode-subtractive.png" /></td>
    <td style="width:33%; border:none;"><img src="../Images/whats-new/urp-10/lightmode-all-lights-realtime.png" /></td>
    <td style="width:33%; border:none;"><img src="../Images/whats-new/urp-10/lightmode-shadowmask.png" /></td>
  </tr>
  <tr>
    <td style="padding:3px; border:none;"><em>Lighting Mode: Subtractive. Shadows within the Maximum Shadow Distance have lower quality.</em></td>
    <td style="padding:3px; border:none;"><em>Shadows only from real-time lights. Shadows further than the Maximum Distance are missing.</em></td>
    <td style="padding:3px; border:none;"><em>Lighting Mode: Shadowmask.</em></td>
  </tr>
</tbody></table>

### Parallax mapping and Height Map property

URP implements the parallax mapping technique which uses the height map to achieve surface-level occlusion effect. The **Height Map** property is available in the Lit shader. To read more about how parallax mapping works, refer to the [Heightmap](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterHeightMap.html) page.

The following illustration shows how a mesh looks with only the Base Map (left), Base Map and Normal Map (middle), and Base, Normal, and Height Map (right).

![Mesh with only the Base Map (left), Base Map and Normal Map (middle), and Base, Normal, and Height Map (right).](../Images/whats-new/urp-10/parallax-height.png)

## Improvements

This section contains the overview of the major improvements in this release.

### Shaded Wireframe

Shaded Wireframe scene view mode works in URP now. Shaded Wireframe helps you check and validate scene geometry, for example, when checking geometry density to select a proper LOD level.

![Shaded Wireframe view](../Images/whats-new/urp-10/shaded-wireframe.png)<br/>*Shaded Wireframe scene view.*

### Improved shader stripping

URP 10 fixes a few issues in the C# shader preprocessor. The fixes improve the speed of shader stripping. The pipeline now compiles different sets of vertex and fragment shaders for the platforms that support it. This improvement lets the pipeline strip vertex shader variants more efficiently. The improvement significantly reduces the number of post-processing shader variants, since most of the variants are in fragment shaders only.

### Reduced number of global shader keywords

Shaders included in the URP package now use local Material keywords instead of global keywords. This increases the amount of available global user-defined Material keywords.

### GPU instanced mesh particles

GPU instanced mesh particles provide a significant performance improvement compared with CPU rendering. This feature lets you configure your particle systems to render Mesh particles.

## Issues resolved

For information on issues resolved in URP 10, see the [Changelog](../../changelog/CHANGELOG.html).

For information on the known issues in URP 10, see the section [Known issues](../known-issues.md).
