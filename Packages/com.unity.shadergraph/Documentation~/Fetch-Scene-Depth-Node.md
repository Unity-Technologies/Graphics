# Fetch Scene Depth node

The Fetch Scene Depth node reads the depth buffer that a previous render pass wrote to on-chip memory, using it as an input attachment ([framebuffer fetch](https://docs.unity3d.com/Manual/urp/render-graph-framebuffer-fetch.html)). Unlike the [Scene Depth](Scene-Depth-Node.md) node, it doesn't sample the camera depth texture, so it avoids a round trip to video memory, especially on mobile devices that use tile-based deferred rendering, such as most mobile and XR devices.

The node reads depth only at the current pixel position. It has no UV input, so you can't sample depth at any screen coordinate. For texture-based depth sampling at any coordinate, use the [Scene Depth](Scene-Depth-Node.md) node instead.

> [!NOTE]
> This node works only in the fragment shader stage.

> [!NOTE]
> Depth input attachments are supported only in URP, and only on DirectX 12 and Vulkan. To check support at runtime, use `SystemInfo.supportsDepthAttachmentAsInputAttachment`. 

This node returns a value of 0 (black) if depth input attachments aren't enabled or supported on the target platform. There's no automatic fallback to sampling the depth texture.

To make the Fetch Scene Depth node return a valid depth value, you must render the material in a pass that reads the depth buffer as an input attachment. For example, use a [Render Objects Renderer Feature](https://docs.unity3d.com/Manual/urp/renderer-features/renderer-feature-render-objects.html) with **Set As Input Attachment** enabled.

For more information, refer to [Get the current depth buffer from GPU memory in URP](https://docs.unity3d.com/Manual/urp/read-depth-input-attachment.html).

## Render pipeline support

This node supports the following render pipelines:

- Universal Render Pipeline (URP)

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **Out** | Output | Float | None | The depth value at the current pixel position. |

## Sampling modes

| **Name** | **Description** |
|----------|------------------------------------|
| **Linear 01** | Returns the linear depth value. The range is from 0 to 1. 0 is the near clipping plane of the camera, and 1 is the far clipping plane of the camera. |
| **Raw** | Returns the non-linear depth value. The range is from 0 to 1. 0 is the near clipping plane of the camera, and 1 is the far clipping plane of the camera. |
| **Eye** | Returns the depth value as the distance from the camera in meters. |

For more information about clipping planes, refer to [Introduction to the camera view](https://docs.unity3d.com/Manual/UnderstandingFrustum.html).

## Generated code example

The following example code represents one possible outcome of this node.

```
void Unity_FetchSceneDepth_Raw_float(float4 clipPos, out float Out)
{
    #if defined(_DEPTH_AS_INPUT_ATTACHMENT) || defined(_DEPTH_AS_INPUT_ATTACHMENT_MSAA)
        // Fetches raw depth from on-chip memory using an input attachment.
        Out = shadergraph_LWFetchSceneDepth(clipPos.xy);
    #else
        // There is explicitly no fallback here.
        Out = 0;
    #endif
}
```

## Additional resources

- [Scene Depth](Scene-Depth-Node.md)
- [Get the current depth buffer from GPU memory in URP](https://docs.unity3d.com/Manual/urp/read-depth-input-attachment.html)
- [Render Objects Renderer Feature reference](https://docs.unity3d.com/Manual/urp/renderer-features/renderer-feature-render-objects.html)
