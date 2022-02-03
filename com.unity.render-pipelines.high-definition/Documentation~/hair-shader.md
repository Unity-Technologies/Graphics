# Hair shader
The Hair shader is your starting point for rendering hair and fur in the High Definition Render Pipeline (HDRP). To create a realistic looking hair effect, it uses layers called hair cards. Each hair card represents a different section of hair. If you use semi-transparent hair cards, you must manually sort them so that they are in back-to-front order from every viewing direction.

![](Images/HDRPFeatures-HairShader.png)

Under the hood, the Hair shader is a pre-configured Shader Graph. To learn more about the Hair shader implementation, or to create your own Hair shader variant, see the Shader Graph documentation about the [Hair Master Node](master-stack-hair.md).

## Importing the Hair Sample

HDRP comes with a Hair Material sample to further help you get started. To find this Sample:

1. Go to **Windows > Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.
4. In the Asset window, go to **Samples > High Definition RP > 11.0** and open the Hair scene. Here you will see the hair sample material set up in-context with a scene, and available for you to use.

## Creating a Hair Material

New Materials in HDRP use the [Lit shader](Lit-Shader.md) by default. To create a Hair Material from scratch, create a Material and then make it use the Hair shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.

3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP > Hair**.



## Properties

[!include[](snippets/shader-properties/surface-options/lit-surface-options.md)]

### Exposed Properties

| **Property**                      | **Description**                                              |
| --------------------------------- | ------------------------------------------------------------ |
| **Base Color Map**                | Assign a Texture that controls both the color and opacity of your Material. |
| **Base Color**                    | Set the color of the Material. If you do not assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Alpha Cutoff**                  | Set the alpha value limit that HDRP uses to determine whether it should render each pixel. If the alpha value of the pixel is equal to or higher than the limit then HDRP renders the pixel. If the value is lower than the limit then HDRP does not render the pixel. This property only appears when you enable the **Alpha Clipping** setting. |
| **Alpha Cutoff Prepass**          | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth prepass.<br/>This property only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Cutoff Postpass**         | Set the alpha value limit that HDRP uses to determine whether it should discard the fragment from the depth postpass.<br/>This property only appears when you enable the **Alpha Clipping** and **Transparent Depth Postpass** settings. |
| **Alpha Cutoff Shadows**          | Set the alpha value limit that HDRP uses to determine whether it should render shadows for a fragment.<br/>This property only appears when you enable the **Use Shadow Threshold** settings. |
| **Base UV Scale Transform**       | Sets the tiling rate (xy) and offsets (zw) for Base Color, Normal, and AO maps. |
| **Normal Map**                    | Assign a Texture that defines the normal map for this Material in tangent space. |
| **Normal Strength**               | Modulates the normal intensity between 0 and 8.              |
| **AO Map**                        | Assign a Texture that defines the ambient occlusion for this material. |
| **AO Use Lightmap UV**            | Set the UV channel used to sample the AO Map. When enabled, UV1 channel will be used. This is useful in the case of overlapping UVs (which is often for hair cards). |
| **Smoothness Mask**               | Assign a Texture that defines the smoothness for this material. |
| **Smoothness UV Scale Transform** | Sets the tiling rate (xy) and offsets (zw) for the Smoothness Mask Map. |
| **Smoothness Min**                | Set the minimum smoothness for this Material.                |
| **Smoothness Max**                | Set the maximum smoothness for this Material.                |
| **Specular Color**                | Set the representative color of the highlight that Unity uses to drive both the primary specular highlight color, which is mainly monochrome, and the secondary specular highlight color, which is chromatic.|
| **Specular Multiplier**           | Modifies the primary specular highlight by this multiplier.  |
| **Specular Shift**                | Modifies the position of the primary specular highlight.     |
| **Secondary Specular Multiplier** | Modifies the secondary specular highlight by this multiplier. |
| **Secondary Specular Shift**      | Modifies the position of the secondary specular highlight    |
| **Transmission Color**            | Set the fraction of specular lighting that penetrates the hair from behind. This is on a per-color channel basis so you can use this property to set the color of penetrating light. Set this to (0, 0, 0) to stop any light from penetrating through the hair. Set this to (1, 1, 1) to have a strong effect with a lot of white light transmitting through the hair. |
| **Transmission Rim**              | Set the intensity of back lit hair around the edge of the hair. Set this to 0 to completely remove the transmission effect. |

[!include[](snippets/shader-properties/advanced-options/lit-advanced-options.md)]
