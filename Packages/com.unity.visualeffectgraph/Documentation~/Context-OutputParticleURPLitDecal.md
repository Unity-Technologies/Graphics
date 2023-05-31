# Output Particle URP Lit Decal

The **Output Particle URP Lit Decal** Context uses a decal to render a particle system. A decal is a box that the Visual Effect Graph projects a texture into. Unity renders that texture on any geometry that intersects the decal along its xy plane. This means decal particles that don’t intersect any geometry are not visible. When a decal is not visible, it still contributes to the resource intensity required to simulate and render the system.

This Context can project its properties onto a surface using a Base Color map (albedo), a Normal Map, a Metallic Map and/or an Occlusion Map. It does not support [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest). To use this context, assign the [Decal Renderer Feature](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/renderer-feature-decal.html) in your [URP Renderer](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/urp-universal-renderer.html).


Menu Path : **Context > Output Particle URP Lit Decal**

The **Output Particle URP Lit Decal** Context can affect the following properties of the surface it projects onto:

- Base Color
- MAOS (Metal, Ambient Occlusion, Smoothness)
- Normal

Below is a list of settings and properties specific to the Output Particle URP Lit Decal Context. For information about the generic output settings this Context shares with all other Contexts, see [Output Lit Settings and Properties](Context-OutputLitSettings.md).

# Context Settings

| **Input**                  | **Type** | **Description**                                                                                                                                                                                                                                                                               |
|----------------------------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Normal Opacity Channel** | Enum     | Use this drop-down to select the map that controls the opacity of the normal map :<br/>• **Base Color Map Alpha**: Uses the alpha channel of the **Base Map** to control the opacity.<br/>• **Metallic Map Blue**: Uses the blue channel of the **Metallic Map** to control opacity.          |
| **MAOS Opacity Channel**   | Enum     | Use this drop-down to select the source of the **Mask Map** opacity:<br/>• **Base Color Map Alpha**: Uses the alpha channel of the **Base Map** to control the opacity of the Mask Map.<br/>• **Metallic Map Blue**: Uses the blue channel of the **Metallic Map**  to control its opacity.   |
| **Affect Base Color**      | Bool     | Enable this checkbox to make this decal use the **Base Color** properties.  When this property is disabled the decal has no effect on the  Base Color.  HDRP still uses the alpha channel of the base color as an opacity for the other properties when this property is enabled or disabled. |
| **Affect MAOS**            | Bool     | Enable the checkbox to make the decal affect the metallic, ambient occlusion and smoothness values of the surface.                                                                                                                                                                            |
| **Decal Layer**            | Enum     | The layer that specifies which Materials Unity projects the decal onto.  Unity displays the decal onto any Mesh Renderers or Terrain that use a matching Decal Layer.                                                                                                                         |



# Context Properties

| **Input**             | **Type** | **Description**                                                                                                                                                                                                                         |
|-----------------------|----------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Fade Factor**       | Float    | Change this value to fade the decal in and out. A value of 0 makes the decal fully transparent, and a value of 1 does not change the overall opacity.                                                                                   |
| **Angle Fade**        | Vector2  | Use the min-max slider to control the fade out range of the decal (in degrees) based on the angle between the Decal backward direction and the vertex normal of the receiving surface. This value is clamped between 0 and 180 degrees. |
| **Normal Alpha**      | Float    | Use the slider to control the blend factor between the normal of the surface and the normal from the Normal map.                                                                                                                        |
| **Ambient Occlusion** | Float    | Use the slider to scale the Ambient occlusion values contained in the Occlusion Map. This property is only visible if you enabled the **Use Occlusion Map** setting.                                                                    |



# Limitations

- This Output does not support Shader Graph.
- For this Context to work, add the Decal Renderer Feature in your URP Renderer.
- All the limitations inherent to the [Decal Renderer Feature](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/renderer-feature-decal.html). 
