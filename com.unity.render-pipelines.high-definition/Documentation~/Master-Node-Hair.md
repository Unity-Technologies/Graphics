# Hair master node

You can use a Hair Material to render hair and fur in the High Definition Render Pipeline (HDRP). To create a realistic looking hair effect, it uses layers called hair cards. Each hair card represents a different section of hair. If you use semi-transparent hair cards, you must manually sort them so that they are in back-to-front order from every viewing direction.

The Hair Shader does not have an Inspector implementation, like the [Lit Shader](Lit-Shader.md) does. As such, you need to use the node-based Shader editor, [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html), to configure a Hair Material.

The Hair master node is the destination node for the Hair Shader Graph. It contains ports that you can attach to other Shader Graph nodes so you can edit properties that control the appearance of the Hair Material. To customize the Hair Material, you must override the inputs attached to these slots with your own values.

![](Images/HDRPFeatures-HairShader.png)

## Creating and editing a Hair Material

Hair Materials use a Shader Graph master node which means that you need to use a specific method to create and edit a Hair Material. For information on how to do this, see [Creating and Editing HDRP Shader Graphs](Customizing-HDRP-materials-with-Shader-Graph.md). When you create a Material from the Shader Graph, the properties that you exposed in the Blackboard appear in the **Exposed Properties** section.

## Properties

There are properties on the master node as well as properties on the Materials that use it. [Material properties](#MaterialProperties) are in the Inspector for Materials that use this Shader, and the master node properties are inside the Shader Graph itself in two sections:

- **[Master node input ports](#InputPorts)**: Shader Graph input ports on the master node itself that you can connect to the output of other nodes or, in some cases, add your own values to.
- **[Master node settings menu](#SettingsMenu)**: Settings you can use to customize your master node and expose more input ports.

<a name="InputPorts"></a>

### Master node input ports

![](Images/MasterNodeHair1.png)

The following table describes the input ports on an Hair Master node, including the property type and Shader stage used for each port. For more information on Shader stages, see [Shader stage](https://docs.unity3d.com/Packages/com.unity.shadergraph@6.9/manual/Shader-Stage.html).

| **Property**                              | **Type** | **Stage** | **Description**                                              |
| ----------------------------------------- | -------- | --------- | ------------------------------------------------------------ |
| **Position**                              | Vector 3 | Vertex    | The vertex position of the hair card or spline.              |
| **Diffuse Color**                         | Vector 3 | Fragment  | The color of the pixel. To assign a map, connect a sampled Texture2D. |
| **Specular Occlusion**                    | Float    | Fragment  | A multiplier for the intensity of specular global illumination. This port only appears when you select **Custom** from the **Specular Occlusion Mode** drop-down. |
| **Normal**                                | Vector 3 | Fragment  | The normal of the hair card.                                 |
| **Bent Normal**                           | Vector 3 | Fragment  | The [bent normal](Glossary.md#BentNormalMap) of the hair card. |
| **Smoothness**                            | Float    | Fragment  | Set the appearance of the primary specular highlight. Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a perfectly smooth surface that reflects light like a mirror, set this to a value of 1. For a rougher surface, set this to a lower value. |
| **Ambient Occlusion**                     | Float    | Fragment  | A multiplier for the intensity of diffuse global illumination. Set this to 0 to remove all global illumination. |
| **Transmittance**                         | Vector 3 | Fragment  | Set the fraction of specular lighting that penetrates the hair from behind. This is on a per-color channel basis so you can use this property to set the color of penetrating light. Set this to (0, 0, 0) to stop any light from penetrating through the hair. Set this to (1, 1, 1) to have a strong effect with a lot of white light transmitting through the hair. |
| **Rim Transmission Intensity**            | Float    | Fragment  | Set the intensity of back lit hair around the edge of the hair. Set this to 0 to completely remove the transmission effect. |
| **Hair Strand Direction**                 | Vector 3 | Fragment  | Set the direction that the hair flows (from root to tip) when modelling with hair cards. |
| **Alpha**                                 | Float    | Fragment  | Set the coverage/opacity of the hair. Set this to 0 to make the hair fully transparent. Set this to 1 to make the hair fully opaque. |
| **Alpha Clip Threshold**                  | Float    | Fragment  | Set the alpha value limit that HDRP uses to determine whether it should render each pixel. If the alpha value of the pixel is equal to or higher than the limit then HDRP renders the pixel. If the value is lower than the limit then HDRP does not render the pixel. This port only appears when you enable the **Alpha Clipping** setting. |
| **Alpha Clip Threshold (Depth Prepass)**  | Float    | Fragment  | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth prepass.<br/>This port only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Clip Threshold (Depth Postpass)** | Float    | Fragment  | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth postpass.<br/>This port only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Clip Threshold (Shadow Pass)**    | Float    | Fragment  | Set the alpha value limit that HDRP uses to determine whether it should render shadows for a fragment.<br/>This port only appears when you enable the **Use Shadow Threshold** settings. |
| **Specular AA Screen Space Variance**     | Float    | Fragment  | Set the strength of the [geometric specular anti-aliasing](Geometric-Specular-Anti-Aliasing.md) effect between 0 and 1. Higher values produce a blurrier result with less aliasing.<br/>This port only appears when you enable the **Geometric Specular AA** setting. |
| **Specular AA Threshold**                 | Float    | Fragment  | Set the maximum value that HDRP subtracts from the smoothness value to reduce artifacts.<br/>This port only appears when you enable the **Geometric Specular AA** setting. |
| **Specular Tint**                         | Vector 3 | Fragment  | Set the color of the primary specular highlight.             |
| **Specular Shift**                        | Float    | Fragment  | Modifies the position of the primary specular highlight.     |
| **Secondary Specular Tint**               | Vector 3 | Fragment  | Set the color of the secondary specular highlight.           |
| **Secondary Smoothness**                  | Float    | Fragment  | Controls the appearance of the secondary specular highlight. Increase this value to make the highlight smaller. |
| **Secondary Specular Shift**              | Float    | Fragment  | Modifies the position of the secondary specular highlight    |
| **Baked GI**                              | Vector 3 | Fragment  | Replaces the built-in diffuse GI solution with a value that you can set. This is for the front [face](Glossary.md#Face) of the Mesh only.<br/>This port only appears when you enable the **Override Baked GI** setting. |
| **Baked Back GI**                         | Vector 3 | Fragment  | Replaces the built-in diffuse GI solution with a value that you can set. This is for the back [face](Glossary.md#Face) of the Mesh only.<br/>This port only appears when you enable the **Override Baked GI** setting. |
| **Depth offset**                          | Float    | Fragment  | Set the value that the Shader uses to increase the depth of the fragment by. This pushes the fragment away from the Camera and helps to reduce the flat appearance of hair cards.<br/>This port only appears when you enable the **Depth Offset** setting. |

<a name="SettingsMenu"></a>

### Master node settings menu

To view these properties, click the cog icon in the top right of the master node.

![](Images/MasterNodeHair2.png)

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Precision**                    | Select the precision of the calculations the Shader processes to work out how to render hair. Lower precision calculations are faster but can cause issues, such as incorrect intensity for specular highlights.<br/>&#8226; **Inherit**: Uses global precision settings. This is the highest precision setting so using it does not result in any precision issues.<br/>&#8226; **Float**: Uses single-precision floating-point instructions. This makes each calculation less resource intensive which speeds up calculations in the Hair Shader. This lower precision level might cause some issues, specifically for specular highlights which can have incorrect intensities or harsh transitions.<br/>&#8226; **Half**: Uses half-precision floating-point instructions. This is the fastest precision level which means that calculations that use it are the least resource intensive to process. This precision settings is the most likely one to result in issues. **Half** precision is currently experimental for the Hair Shader. |
| **Surface Type**                 | Use the drop-down to define whether your Material supports transparency or not. Materials with the **Transparent Surface Type** are more resource intensive to render than Materials with the **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.md). |
| **Double-Sided**                 | Enable this setting to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.md). |
| **Alpha Clipping**               | Enable the checkbox to make this Material act like a [Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html). Enabling this feature exposes more properties. For more information about the feature and for the  list of properties this feature exposes, see the [Alpha Clipping documentation](Alpha-Clipping.md). |
| **- Use Shadow Threshold**       | Enable this setting to set another threshold value for alpha clipping shadows. |
| **Receive Decals**               | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR (Transparent)**    | Enable this setting to make HDRP include this Material when it processes the screen space reflection pass. There is a separate option for transparent Surface Type.|
| **Add Precomputed Velocity** | Enable this setting to use precomputed velocity information stored in an Alembic file. |
| **Geometric Specular AA**        | Enable this setting to make HDRP perform geometric anti-aliasing on this Material. This modifies the smoothness values on surfaces of curved geometry to remove specular artifacts. For more information about the feature and for the list of properties this feature exposes, see the [Geometric Specular Anti-aliasing documentation](Geometric-Specular-Anti-Aliasing.md). |
| **Specular Occlusion Mode**      | Set the mode that HDRP uses to calculate specular occlusion.<br/>&#8226; **Off**: Disables specular occlusion.<br/>&#8226; **From AO**: Calculates specular occlusion from the ambient occlusion map and the Camera's view vector.<br/>&#8226; **From AO and Bent Normal**: Calculates specular occlusion from the ambient occlusion map, the bent normal map, and the Camera's view vector.<br/>&#8226; **Custom**: Allows you to specify your own specular occlusion values. |
| **Override Baked GI**            | Enable this setting to expose two baked GI [input ports](#InputPorts). This makes this Materials ignore global illumination in your Scene and, instead, allows you to provide your own global illumination values and customize the way this Material looks. |
| **Depth Offset**                 | Enable this setting to expose the DepthOffset [InputPort](#InputPorts) which you can use to increase the depth value of the fragment and push it away from the Camera. You can use this to reduce the flat appearance of hair cards. |
| **Use Light Facing Normal**      | Enable this setting to make the hair normals always face towards light. This mimics the behavior of hair. |
| **Override ShaderGUI**           | Lets you override the [ShaderGUI](https://docs.unity3d.com/ScriptReference/ShaderGUI.html) that this Shader Graph uses. If `true`, the **ShaderGUI** property appears, which lets you specify the ShaderGUI to use. |
| **- ShaderGUI**                    |  The full name of the ShaderGUI class to use, including the class path. |

<a name="MaterialProperties"></a>

### Material Inspector

These properties are in the **Exposed Properties** section of the Inspector and sit alongside the properties that you exposed in the Shader Graph's Blackboard. If you set **Override ShaderGUI** to `true`, the Material Properties section does not appear, and instead, the ShaderGUI you specified appears.

| **Property**                           | **Description**                                              |
| -------------------------------------- | ------------------------------------------------------------ |
| **Enable GPU Instancing**              | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you cannot [static-batch](https://docs.unity3d.com/Manual/DrawCallBatching.html) GameObjects that have an animation based on the object pivot, but the GPU can instance them. |
| **Emission**                           | Toggles whether emission affects global illumination. |
| **- Global Illumination**              | Use the drop-down to choose how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |
| **Motion Vector For Vertex Animation** | Enable the checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause. |
