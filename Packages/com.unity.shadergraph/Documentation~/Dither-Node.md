# Dither node

The Dither node adds a structured form of noise to the input. Use the Dither node to reduce the color bands that might appear if you move from a high number of colors to a low number (quantizing), or to simulate transparency by adding random alpha pixels to an opaque object. 

The Dither node applies dithering in screen space to ensure a uniform distribution of the pattern. To change the space the node uses, connect another node to the **Screen Position** input, such as a [UV node](UV-Nodes.md).

To use a dither pattern for transparency, connect the Dither node to the **Alpha Clip Threshold** input in the [Master Stack](Master-Stack.md). As a result, when you adjust the overall alpha value of the material, some pixels are discarded because the alpha value is lower than their alpha clip threshold. This technique is useful for creating geometry that appears to be transparent but has the advantages of rendering as opaque, such as writing to the depth buffer or rendering using a deferred [rendering path](https://docs.unity3d.com/Manual/built-in-rendering-paths.html).

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **In** | Input | Dynamic vector | None | The input to dither. The noise stays within the overall minimum and maximum range of the input values. |
| **Screen Position** | Input | Vector 4 | Screen Position | The coordinates Unity uses to calculate the dither pattern. For more information about the options, refer to the [Screen Position node](Screen-Position-Node.md). |
| **Out** | Output | Dynamic vector | None | The dithered output. |

## Generated code example

The following example code represents one possible outcome of this node.

```
void Unity_Dither_float4(float4 In, float4 ScreenPosition, out float4 Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
}
```

