# Scene Color node

The Scene Color node samples the color buffer of the current camera, using the screen space coordinates you input. 

If you use the Universal Render Pipeline (URP), the node samples the opaque texture, which is a copy of the color buffer before Unity renders transparent objects. For more information, refer to [Universal Render Pipeline asset reference](https://docs.unity3d.com/Manual/urp/universalrp-asset.html).

To make sure the Scene Color node outputs the correct values, follow these steps:

1. Connect the node to the fragment [shader stage](Shader-Stage.md). The Scene Color node doesn't support the vertex shader stage.
2. In the **Graph Settings** tab of the [**Graph Inspector**](Internal-inspector.md) window, set **Surface Type** to **Transparent**. Otherwise, the node samples the color buffer before Unity renders all the opaque contents in the scene.

## Render pipeline support 

The Scene Color node supports the following render pipelines:

- Universal Render Pipeline (URP)
- High Definition Render Pipeline (HDRP)

If you use the Scene Color node with an unsupported pipeline, it returns 0 (black).

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:-----|:----------|:---------|:----------------|:------------|
| **UV** | Input | Vector 4 | Screen position | The normalized screen space coordinates to sample from. |
| **Out** | Output | Vector 3 | None | The color value from the color buffer at the **UV** coordinates. |

## Generated code example

The HLSL code this node generates depends on the render pipeline you use. If you use your own custom render pipeline, you must define the behavior of the node yourself. Otherwise, the node returns a value of 0 (black).

The following example code represents one possible outcome of this node.

```
void Unity_SceneColor_float(float4 UV, out float3 Out)
{
    Out = SHADERGRAPH_SAMPLE_SCENE_COLOR(UV);
}
```
