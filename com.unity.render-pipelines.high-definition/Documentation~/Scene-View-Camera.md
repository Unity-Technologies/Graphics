# Scene view camera

The High Definition Render Pipeline (HDRP) includes extra customization options for the [Scene view Camera](https://docs.unity3d.com/Manual/SceneViewCamera.html) settings menu. You can use these properties to configure HDRP specific camera features.

For information on the Scene view Camera settings menu and how to use it, see the [Scene view Camera documentation](https://docs.unity3d.com/Manual/SceneViewCamera.html).

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Camera Anti-aliasing** | Specifies the method the Scene view Camera uses for post-process anti-aliasing. The options are:<br/>&#8226; **No Anti-aliasing**: This Camera can process MSAA but does not process any post-process anti-aliasing.<br/>&#8226; **Fast Approximate Anti-aliasing** (FXAA): Smooths edges on a per-pixel level. This is the least resource-intensive anti-aliasing technique in HDRP.<br/>&#8226; **Temporal Anti-aliasing** (TAA): Uses frames from a history buffer to smooth edges more effectively than fast approximate anti-aliasing.<br/>&#8226; **Subpixel Morphological Anti-aliasing** (SMAA): Finds patterns in borders of the image and blends the pixels on these borders according to the pattern. |
| **Camera Stop NaNs**     | Makes the Scene view Camera replace values that are not a number (NaN) with a black pixel. This stops certain effects from breaking but is a resource-intensive process. |
| **Override Exposure**    | Specifies whether to override the scene's exposure with a specific value. |
| - Scene Exposure         | The exposure value the Scene view Camera uses to override the scene's exposure.<br/>This property only appears when you enable **Override Exposure**. |