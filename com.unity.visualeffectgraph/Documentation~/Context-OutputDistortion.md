# Output Distortion

Menu Path : **Context > Output [Strip/Particle] Distortion [Quad/Mesh]**

*(Output Strip Distortion Quad, Output Particle Distortion Quad, Output Particle Distortion Mesh)*

**Distortion** outputs are Context nodes specific to the High Definition Render Pipeline (HDRP). They utilize HDRP's Distortion pass to simulate the bending of light. The Distortion output is available for both particles and particle strip data types, and particle distortion Contexts support both quad and mesh distortion. Distortion outputs are useful for simulating effects like heat distortion from fire.

![](F:/Graphics/com.unity.visualeffectgraph/Documentation~/Images/Context-OutputDistortion.png)

Below is a list of settings and properties specific to the Distortion Context. For information about the generic output settings this Context shares with all other Contexts, see [Global Output Settings and Properties](Context-OutputSharedSettings.md).

## Context settings

| **Setting**         | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Distortion Mode** | Enum     | Specifies how to process distortion. The options are:<br/>&#8226; **Screen Space**: Processes distortion as a screen-space effect.<br/>&#8226; **Normal Based**: Uses a normal map, in tangent space, to calculate distortion. This normal map represents the surface orientation of the distortion quad or mesh. |

## Context properties

| **Input**               | **Type** | **Description**                                              |
| ----------------------- | -------- | ------------------------------------------------------------ |
| **Scale By Distance**   | Bool     | Indicates whether this Context should scale the distortion effect by the distance to the camera to maintain a consistent visual look. |
| **Distortion Blur Map** | Texture  | The map to use for the distortion. The **R** and **G** channels (centered on 0.5) map to the distortion’s X and Y offset, and the **B** channel is a mask for the distortion blur.<br/>Note, for this Texture to work correctly, you must disable **sRGB** in the textures Import Settings.<br/>This property only appears if you set **Distortion Mode** to **Screen Space**. |
| **Normal Map**          | Texture  | The normal map to use for the distortion.<br/>This property only appears if you set **Distortion Mode** to **Normal Based**. |
| **Smoothness Map**      | Texture  | The texture that controls the blur of the distortion. The mask uses this Texture’s alpha channel.<br/>This property only appears if you set **Distortion Mode** to **Normal Based**. |
| **Alpha Mask**          | Texture  | The texture that scales the distortion vectors. The mask uses the Texture’s alpha channel. This property only appears if you set **Distortion Mode** to **Normal Based**. |
| **Distortion Scale**    | Vector2  | The per-axis scale to use for the screen-space distortion. The x-axis corresponds to the horizontal scale and the y-axis corresponds to the vertical scale.<br/>This property only appears if you set **Distortion Mode** to **Screen Space**. |
| **Distortion Scale**    | Float    | The scale to use for the normal-based distortion. This is screen-space when using **Screen Space** mode and world-space when using **Normal Based** mode.<br/>This property only appears if you set **Distortion Mode** to **Normal Based**. |
| **Blur Scale**          | Float    | The scale of the blur.                                       |