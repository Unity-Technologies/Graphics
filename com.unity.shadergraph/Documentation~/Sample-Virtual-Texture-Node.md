# Sample Virtual Texture Node

## Description

Samples a [Virtual Texture](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-use-in-shader-graph.html) and returns up to 4 Vector 4 color values for use in the shader. You can override the UV coordinates using the UV input. The UV coordinate provided is used to sample all of the textures in the **Virtual Texture**. 


By default, this Node can only be used in the Fragment Shader Stage. For more information about how to use this node, or how to use this node in the Vertex Shader Stage, see [Using Streaming Virtual Texturing in Shader Graph](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-use-in-shader-graph.html). 

If `Virtual Texturing` is disabled in your project, this node will function the same as [Sample Texture 2D Node](Sample-Texture-2D-Node) and perform standard 2D sampling on each texture. 

A **Sample Virtual Texture Node** must be connected to a `Virtual Texture` property to compile. If no property is connected, an error will display indicating that a connection is required.  


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input |	Vector 2    | 	UV	| UV coordinates |
| VT | Input |	Virtual Texture | None | The Virtual Texture to sample. |
| Out	| Output	| Vector 4	| None	| Output value of layer 1 as RGBA |
| Out2	| Output	| Vector 4	| None	| Output value of layer 2 as RGBA |
| Out3	| Output	| Vector 4	| None	| Output value of layer 3 as RGBA |
| Out4	| Output	| Vector 4	| None	| Output value of layer 4 as RGBA |

## Settings

The **Sample Virtual Texture Node** has several settings to specify the behavior of the node. These work in combination with any scripts or [Streaming Virtual Texture](https://docs.unity3d.com/2020.1/Documentation/Manual/svt-streaming-virtual-texturing.html) settings you may have set up in your project. To view the settings, select the node with the [Graph Inspector](Internal-Inspector) open.

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  Lod Mode   | Dropdown | Automatic, Lod Level, Lod Bias, Derivatives | Sets the specific Lod mode to use when sampling the textures. |
|  Quality   | Dropdown | Low, High | Sets the quality mode to use when sampling the textures.  |
|  Manual Streaming  | Toggle | Enabled/Disabled | Sets whether the node should use automatic streaming or manual streaming.   |

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