# HDRP Scene view camera settings reference

The High Definition Render Pipeline (HDRP) includes extra customization options for the [Scene view Camera](https://docs.unity3d.com/Manual/SceneViewCamera.html) settings menu. You can use these properties to configure HDRP-specific camera features.

For information on the Scene view Camera settings menu and how to use it, refer to [Scene view Camera](https://docs.unity3d.com/Manual/SceneViewCamera.html).

These settings apply to the individual Scene view they're set on. Unity saves the settings with the Scene view's window layout, and includes them when you use **Copy Settings**, **Paste Settings**, or **Reset Settings** from the Scene Camera settings menu.

## Properties

| **Property**          | **Description**  |
|-----------------------|-----------------|
| **Anti-aliasing**      | Specifies the method the Scene view Camera uses for post-process anti-aliasing. The options are:<ul><li>**No Anti-aliasing:** This Camera can process MSAA but doesn't process any post-process anti-aliasing.</li> <li>**Fast Approximate Anti-aliasing** (FXAA): Smooths edges on a per-pixel level. This is the least resource-intensive anti-aliasing technique in HDRP.</li> <li>**Temporal Anti-aliasing** (TAA): Uses frames from a history buffer to smooth edges more effectively than fast approximate anti-aliasing.</li> <li>**Subpixel Morphological Anti-aliasing** (SMAA): Finds patterns in borders of the image and blends the pixels on these borders according to the pattern.</li></ul><br/>**Note**: Temporal Anti-aliasing in the Scene view only works when you enable **Always Refresh** in the **Effects** dropdown of the Scene view's [View options overlay](https://docs.unity3d.com/Manual/overlays-reference-view-options.html). |
| **Stop NaNs**         | Makes the Scene view Camera replace values that aren't a number (NaN) with a black pixel. This stops certain effects from breaking but is a resource-intensive process. |
| **Override Exposure** | Specifies whether to override the scene's exposure with a specific value. |
| **Scene Exposure** | Sets the exposure value the Scene view Camera uses to override the scene's exposure. This property is available only when **Override Exposure** is enabled.|

