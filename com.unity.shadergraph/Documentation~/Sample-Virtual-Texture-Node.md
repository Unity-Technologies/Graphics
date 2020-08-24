# Sample Virtual Texture Node

## Description

Samples a [Virtual Texture](Property-Types.md#virtual-texture) and returns up to four Vector 4 color values for use in the shader. You can use the UV input to override the UV coordinate. The Sample Virtual Texture node takes one UV coordinate as the input, and uses that UV coordinate to sample all of the textures in the Virtual Texture.

If you want to use the Sample Virtual Texture node to sample normal maps, navigate to each layer that you want to sample as a normal map, open the **Layer Type** drop-down menu, and select **Normal**. 

By default, you can only use this node in the fragment shader stage. For more information about how to use this node, or how to configure it for use in the vertex shader stage, see [Using Streaming Virtual Texturing in Shader Graph](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-use-in-shader-graph.html).

If you disable Virtual Texturing in your project, this node works the same way as the [Sample 2D Texture Node](Sample-Texture-2D-Node.md), and performs standard 2D sampling on each texture.

You must connect a Sample Virtual Texture node to a Virtual Texture property for the Shader Graph Asset to compile. If you don't connect the node to a property, an error appears, indicating that the node requires a connection.

For information about Streaming Virtual Texturing, see [Streaming Virtual Texturing](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-streaming-virtual-texturing.html). 

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input |	Vector 2    | 	UV	| The UV coordinate. |
| VT | Input |	Virtual Texture | None | The Virtual Texture to sample. Must be connected to a Virtual Texture property. |
| Out	| Output	| Vector 4	| None	| The output value of layer 1 as RGBA. |
| Out2	| Output	| Vector 4	| None	| The output of layer 2 as RGBA. |
| Out3	| Output	| Vector 4	| None	| The output of layer 3 as RGBA. |
| Out4	| Output	| Vector 4	| None	| The output of layer 4 as RGBA. |

## Settings

The Sample Virtual Texture node has several settings available for you to specify its behavior. These settings work in combination with any scripts you might have set up in your project. To view the settings, select the node with the [Graph Inspector](Internal-Inspector) open. For more information, see [Streaming Virtual Texturing](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-streaming-virtual-texturing.html).

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  Lod Mode   | Dropdown | Automatic, Lod Level, Lod Bias, Derivatives | Sets the specific Lod mode to use when sampling the textures. |
|  Quality   | Dropdown | Low, High | Sets the quality mode to use when sampling the textures.  |
|  Manual Streaming  | Toggle | Enabled/Disabled | Determines whether the node should use automatic streaming or manual streaming. |
| Layer 1 Type | Dropdown | Default, Normal | The texture type of layer 1. |
| Layer 2 Type | Dropdown | Default, Normal | The texture type of layer 2. |
| Layer 3 Type | Dropdown | Default, Normal | The texture type of layer 3. This option only appears if the Virtual Texture has at least 3 layers. |
| Layer 4 Type | Dropdown | Default, Normal | The texture type of layer 4. This option only appears if the Virtual Texture has at least 4 layers. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 SampleVirtualTexture(float2 uv, VTPropertyWithTextureType vtProperty, out float4 Layer0)
{
    VtInputParameters vtParams;
    vtParams.uv = uv;
    vtParams.lodOrOffset = 0.0f;
    vtParams.dx = 0.0f;
    vtParams.dy = 0.0f;
    vtParams.addressMode = VtAddressMode_Wrap;
    vtParams.filterMode = VtFilter_Anisotropic;
    vtParams.levelMode = VtLevel_Automatic;
    vtParams.uvMode = VtUvSpace_Regular;
    vtParams.sampleQuality = VtSampleQuality_High;
    #if defined(SHADER_STAGE_RAY_TRACING)
    if (vtParams.levelMode == VtLevel_Automatic || vtParams.levelMode == VtLevel_Bias)
    {
        vtParams.levelMode = VtLevel_Lod;
        vtParams.lodOrOffset = 0.0f;
    }
    #endif
    StackInfo info = PrepareVT(vtProperty.vtProperty, vtParams);
    Layer0 = SampleVTLayerWithTextureType(vtProperty, vtParams, info, 0);
    return GetResolveOutput(info);
}
```