# Output Particle HDRP Lit Decal

The **Output Particle HDRP Lit Decal** Context uses a decal to render a particle system. A decal is a box that the Visual Effect Graph projects a texture into. Unity renders that texture on any geometry that intersects the decal along its xy plane. This means decal particles that don’t intersect any geometry are not visible. When a decal is not visible, it still contributes to the resource intensity required to simulate and render the system.

This Context can project its properties onto a surface using a Base Color map (albedo), a Normal Map, or a Mask Map. It does not support [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest). To use this context, enable **Decals** in the [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/HDRP-Asset.html) and in the [HDRP Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/Frame-Settings.html).

Menu Path : **Context > Output Particle HDRP Lit Decal**

The **Output Particle HDRP Lit Decal** Context can affect the following properties of the surface it projects onto:

- Base Color
- Metalness
- Ambient Occlusion
- Smoothness
- Normal

Below is a list of settings and properties specific to the Output Particle HDRP Lit Decal Context. For information about the generic output settings this Context shares with all other Contexts, see [Output Lit Settings and Properties](Context-OutputLitSettings.md).

# Context Settings

| **Input**                    | **Type** | **Description**                                              |
| ---------------------------- | -------- | ------------------------------------------------------------ |
| **Normal Opacity Channel**   | Enum     | Use this drop-down to select the map that controls the opacity of the normal map opacity:• **Base Color Map Alpha**: Uses the alpha channel of the **Base Map**’s color picker to control the opacity.• **Mask Map Blue**: Uses the blue channel of the **Mask Map** to control opacity. |
| **Mask Opacity Channel**     | Enum     | Use this drop-down to select the source of the **Mask Map** opacity:• **Base Color Map Alpha**: Uses the alpha channel of the **Base Map**’s color picker to control the opacity of the Mask Map.• **Mask Map Blue**: Uses the blue channel of the **Mask Map**  to control its opacity. |
| **Affect Base Color**        | Bool     | Enable this checkbox to make this decal use the **Base Color** properties.  When this property is disabled the decal has no effect on the  Base Color.  HDRP still uses the alpha channel of the base color as an opacity for the other properties when this property is enabled or disabled. |
| **Affect Metal**             | Bool     | Enable the checkbox to make the decal use the metallic property of its **Mask Map**. Otherwise the decal has no metallic effect. Uses the red channel of the **Mask Map**. |
| **Affect Ambient Occlusion** | Bool     | Enable this checkbox to make the decal use the ambient occlusion property of its **Mask Map**. When this property is disabled the decal has no ambient occlusion effect. This property uses the green channel of the **Mask Map**. |
| **Affect Smoothness**        | Bool     | Enable this checkbox to make the decal use the smoothness property of its **Mask Map**.  When this property is disabled the decal has no smoothness effect.  This property uses the alpha channel of the **Mask Map**. |
| **Decal Layer**              | Enum     | The layer that specifies which Materials Unity projects the decal onto.  Unity displays the decal onto any Mesh Renderers or Terrain that use a matching Decal Layer. |



# Context Properties

| **Input**             | **Type** | **Description**                                              |
| --------------------- | -------- | ------------------------------------------------------------ |
| **Fade Factor**       | Float    | Change this value to fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 does not change the overall opacity. |
| **Angle Fade**        | Vector2  | Use the min-max slider to control the fade out range of the decal (in degrees) based on the angle between the Decal backward direction and the vertex normal of the receiving surface.  This property is only available when the [Decal Layers](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/Decal.html) feature is enabled. This value is clamped between 0 and 180 degrees. |
| **Ambient Occlusion** | Float    | Use the slider to set the strength of the ambient occlusion effect of the decal. This property only has an effect when you enable the **Metal and Ambient Occlusion properties** checkbox in your [HDRP Asset](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/manual/HDRP-Asset.html#Decals). |



# Limitations

- This Output does not support Shader Graph.
- For this Context to work, enable Decals in the HDRP Asset and in the HDRP Settings.
- Unity does not consider the **Angle Fade** value when **Decal Layers** is disabled in the HDRP Asset and the HDRP Settings.
- Unity only considers Metalness and Ambient Occlusion if the **Rendering > Decals > Metal** **and Ambient Occlusion Properties** are enabled in the HDRP Asset. When this property is disabled, the Metalness and Ambient Occlusion are visible, but they won’t have any effect.
