# Fragment Context reference

This Context represents the fragment stage of a shader. You can add [compatible Blocks](#compatible-blocks) to this Context to set properties for the final shader. Any Node you connect to a Block becomes part of the final shader's fragment function.

## Compatible Blocks

This section lists the Blocks that are compatible with Fragment Contexts in the High Definition Render Pipeline (HDRP). Each entry includes:

- The Block's name.
- A description of what the Block does.
- Settings in the [Graph Settings menu](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html?subfolder=/manual/Graph-Settings-Menu.html) that the Block is relevant to. If you enable these settings, Shader Graph adds the Block to the Context; if you disable the setting, Shader Graph removes the Block from the Context. If you add the Block and do not enable the setting, Shader Graph ignores the Block and its connected Nodes when it builds the final shader.
- The default value that Shader Graph uses if you enable the Block's **Setting Dependency** then remove the Block from the Context.

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-postpass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-depth-prepass.md)]
[!include[](snippets/shader-graph-blocks/alpha-clip-threshold-shadow.md)]
[!include[](snippets/shader-graph-blocks/anisotropy.md)]
[!include[](snippets/shader-graph-blocks/anisotropy-b.md)]
[!include[](snippets/shader-graph-blocks/baked-back-gi.md)]
[!include[](snippets/shader-graph-blocks/baked-gi.md)]
[!include[](snippets/shader-graph-blocks/bent-normal.md)]
[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/coat-extinction.md)]
[!include[](snippets/shader-graph-blocks/coat-ior.md)]
[!include[](snippets/shader-graph-blocks/coat-mask.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/coat-normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/coat-smoothness.md)]
[!include[](snippets/shader-graph-blocks/coat-thickness.md)]
[!include[](snippets/shader-graph-blocks/depth-offset.md)]
[!include[](snippets/shader-graph-blocks/dielectric-ior.md)]
[!include[](snippets/shader-graph-blocks/diffusion-profile.md)]
[!include[](snippets/shader-graph-blocks/distortion.md)]
[!include[](snippets/shader-graph-blocks/distortion-blur.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]
[!include[](snippets/shader-graph-blocks/hair-strand-direction.md)]
[!include[](snippets/shader-graph-blocks/haze-extent.md)]
[!include[](snippets/shader-graph-blocks/haziness.md)]
[!include[](snippets/shader-graph-blocks/hazy-gloss-max-dielectric-f0.md)]
[!include[](snippets/shader-graph-blocks/eye-ior.md)]
[!include[](snippets/shader-graph-blocks/iridescence-coat-fixup-tir.md)]
[!include[](snippets/shader-graph-blocks/iridescence-coat-fixup-tir-clamp.md)]
[!include[](snippets/shader-graph-blocks/iridescence-mask.md)]
[!include[](snippets/shader-graph-blocks/iridescence-thickness.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/iris-normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/lobe-mix.md)]
[!include[](snippets/shader-graph-blocks/maos-alpha.md)]
[!include[](snippets/shader-graph-blocks/mask.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/normal-object-space.md)]
[!include[](snippets/shader-graph-blocks/normal-world-space.md)]
[!include[](snippets/shader-graph-blocks/normal-alpha.md)]
[!include[](snippets/shader-graph-blocks/refraction-color.md)]
[!include[](snippets/shader-graph-blocks/refraction-distance.md)]
[!include[](snippets/shader-graph-blocks/refraction-index.md)]
[!include[](snippets/shader-graph-blocks/rim-transmission-intensity.md)]
[!include[](snippets/shader-graph-blocks/secondary-smoothness.md)]
[!include[](snippets/shader-graph-blocks/secondary-specular-shift.md)]
[!include[](snippets/shader-graph-blocks/secondary-specular-tint.md)]
[!include[](snippets/shader-graph-blocks/shadow-tint.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/smoothness-b.md)]
[!include[](snippets/shader-graph-blocks/specular-color.md)]
[!include[](snippets/shader-graph-blocks/so-fixup-max-added-roughness.md)]
[!include[](snippets/shader-graph-blocks/so-fixup-strength-factor.md)]
[!include[](snippets/shader-graph-blocks/so-fixup-visibility-ratio-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-screen-space-variance.md)]
[!include[](snippets/shader-graph-blocks/specular-aa-threshold.md)]
[!include[](snippets/shader-graph-blocks/specular-occlusion.md)]
[!include[](snippets/shader-graph-blocks/specular-shift.md)]
[!include[](snippets/shader-graph-blocks/specular-tint.md)]
[!include[](snippets/shader-graph-blocks/subsurface-mask.md)]
[!include[](snippets/shader-graph-blocks/tangent-object-space.md)]
[!include[](snippets/shader-graph-blocks/tangent-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/tangent-world-space.md)]
[!include[](snippets/shader-graph-blocks/thickness.md)]
[!include[](snippets/shader-graph-blocks/transmittance.md)]

</table>
