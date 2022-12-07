# Full Screen Pass Renderer Feature

The Full Screen Pass Renderer Feature lets you inject full screen render passes at pre-defined injection points to create full screen effects.

You can use this Renderer Feature to create [custom post-processing effects](../post-processing/custom-post-processing.md).

## How to use the feature

To add the Renderer Feature to your Scene:

1. [Add the Full Screen Pass Renderer Feature](urp-renderer-feature-how-to-add.md) to the URP Renderer.

Refer to the following page for an example of how to use this feature:

* [How to create a custom post-processing effect](../post-processing/post-processing-custom-effect-low-code.md).

## Properties

The Full Screen Pass Renderer Feature contains the following properties.

| Property | Description |
| -------- | ----------- |
| **Name** | Name of the Full Screen Pass Renderer Feature. |
| **Pass Material** | The Material the Renderer Feature uses to render the effect. |
| **Injection Point** | Select when the effect is rendered:<ul><li>**Before Rendering Transparents**: Add the effect after the skybox pass and before the transparents pass.</li><li>**Before Rendering Post Processing**: Add the effect after the transparents pass and before the post-processing pass.</li><li>**After Rendering Post Processing**: Add the effect after the post-processing pass and before AfterRendering pass.</li></ul>**After Rendering Post Processing** is the default setting. |
| **Requirements** | Select one or more of the following passes for the Renderer Feature to use:<ul><li>**None**: Add no additional passes.</li><li>**Everything**: Adds all additional passes available (Depth, Normal, Color, and Motion).</li><li>**Depth**: Adds a depth prepass to enable the use of depth values.</li><li>**Normal**: Enables the use of normal vector data.</li><li>**Color**: Copies color data of a screen to the _BlitTexture texture inside the shader.</li><li>**Motion**: Enables the use of motion vectors.</li></ul>**Color** is the default setting. |
| **Pass Index** | Select a specific pass inside the Pass Material's shader for the Pass Material to use.<br/><br/>This option is hidden by default. To access this option, click &#8942; in the Renderer Feature section of the Inspector and select **Show Additional Properties**. |