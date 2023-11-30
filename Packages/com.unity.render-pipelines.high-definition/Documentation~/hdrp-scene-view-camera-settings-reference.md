# HDRP Scene view camera settings reference

The High Definition Render Pipeline (HDRP) includes extra customization options for the [Scene view Camera](https://docs.unity3d.com/Manual/SceneViewCamera.html) settings menu. You can use these properties to configure HDRP-specific camera features.

For information on the Scene view Camera settings menu and how to use it, refer to [Scene view Camera](https://docs.unity3d.com/Manual/SceneViewCamera.html).

## Properties

<table>
<thead>
<tr>
<th colspan="1"><strong>Property</strong></th>
<th colspan="2"><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Camera Anti-aliasing</strong></td>
<td colspan="2">Specifies the method the Scene view Camera uses for post-process anti-aliasing. The options are:<br/>&#8226; <strong>No Anti-aliasing</strong>: This Camera can process MSAA but doesn't process any post-process anti-aliasing.<br/>&#8226; <strong>Fast Approximate Anti-aliasing</strong> (FXAA): Smooths edges on a per-pixel level. This is the least resource-intensive anti-aliasing technique in HDRP.<br/>&#8226; <strong>Temporal Anti-aliasing</strong> (TAA): Uses frames from a history buffer to smooth edges more effectively than fast approximate anti-aliasing.<br/>&#8226; <strong>Subpixel Morphological Anti-aliasing</strong> (SMAA): Finds patterns in borders of the image and blends the pixels on these borders according to the pattern.</td>
</tr>
<tr>
<td><strong>Camera Stop NaNs</strong></td>
<td colspan="2">Makes the Scene view Camera replace values that aren't a number (NaN) with a black pixel. This stops certain effects from breaking but is a resource-intensive process.</td>
</tr>
<tr>
<td rowspan="2"><strong>Override Exposure</strong></td>
<td colspan="2">Specifies whether to override the scene's exposure with a specific value.</td>
</tr>
<tr>
<td>Scene Exposure</td>
<td>The exposure value the Scene view Camera uses to override the scene's exposure.<br/>This property only appears when you enable <strong>Override Exposure</strong>.</td>
</tr>
</tbody>
</table>
