# Custom post-processing

The Universal Render Pipeline (URP) provides a variety of [post-processing effects](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@15.0/manual/integration-with-post-processing.html) that you can alter to create a particular visual effect or style. But to create a custom visual effect you must create a custom post-processing effect with the Full Screen Render Pass Renderer Feature. You can then use the effect to change a scene’s appearance, such as using a grayscale effect to indicate when a player has run out of health.

![Scene with no post-processing effects.](../Images/post-proc/custom-effect/no-custom-effect.png)
<br/>*Scene with no post-processing effects.*

![Scene with grayscale custom post-processing effect.](../Images/post-proc/custom-effect/grayscale-custom-effect.png)
<br/>*Scene with grayscale custom post-processing effect.*

## Create a custom post-processing effect

Refer to [How to create a custom post-processing effect](post-processing-custom-effect-zero-code.md).

## Properties

The Full Screen Render Pass Renderer Feature contains the following properties.

| Property | Description |
| -------- | ----------- |
| **Name** | Name of the Full Screen Pass Renderer Feature. |
| **Pass Material** | The Material the Renderer Feature uses to render the effect. |
| **Injection Point** | Select when the effect is rendered:<br/>&#8226; **Before Rendering Transparents**: Add the effect after the skybox pass and before the transparents pass.<br/>&#8226; **Before Rendering Post Processing**: Add the effect after the transparents pass and before the post-processing pass.<br/>&#8226; **After Rendering Post Processing**: Add the effect after the post-processing pass and before AfterRendering pass.<br/><br/>**After Rendering Post Processing** is the default setting. |
| **Requirements** | Select one or more of the following passes for the Renderer Feature to use:<br/>&#8226; **None**: Add no additional passes.<br/>&#8226; **Everything**: Adds all additional passes available (Depth, Normal, Color, and Motion).<br/>&#8226; **Depth**: Adds a depth prepass to enable the use of depth values.<br/>&#8226; **Normal**: Enables the use of normal vector data.<br/>&#8226; **Color**: Copies color data of a screen to the _BlitTexture texture inside the shader.<br/>&#8226; **Motion**: Enables the use of motion vectors.<br/><br/>**Color** is the default setting. |
| **Pass Index** | Select a specific pass inside the Pass Material's shader for the Pass Material to use.<br/><br/>This option is hidden by default. To access this option, click &#8942; in the Renderer Feature section of the Inspector and select **Show Additional Properties**. |
