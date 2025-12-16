# Terrain Texture Node

## Description

Explore the properties in the Terrain Texture node to sample textures, masks and other properties from the Terrain Layers of a terrain material, so you can adjust them and input them into the Contexts of a Terrain Lit shader graph.

The Terrain Texture node is compatible only with Terrain Lit shader graphs. You can't use it with other types of shaders.

For more information about how Unity creates render passes for terrain layers, refer to [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html).

## Ports

| Name          | Direction           | Type  | Description |
|:------------  |:-------------|:-----|:---|
| Index         | Input                 | float      | Sets the Terrain Layer to input. For more information, refer to [Terrain layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html).<br/><br/>If you set the [Controls](#controls) dropdown to **Layer Mask**, you can only use a value of 0, or 1 if you use the Universal Render Pipeline (URP) and the terrain has 5 or more Terrain Layers. </br/><br/>The value is a float literal, which means the value must be known at shader compile time. Use a float value, or a float property from the Blackboard in a subgraph. You can't use a float property from the Blackboard in the main graph, or a float output from another node. |
| Texture       | Output                | Texture    | The texture from the Terrain Layer. This output depends on the texture you select in the [Controls](#controls). |
| Available     | Output                | boolean    | Whether the texture or mask you select in the [Controls](#controls) is present in the Terrain Layer. The value is also `false` if the **Index** value is out of bounds. |
| Normal Scale              | Output    | float      | The scaling factor for the normal values in the normal map. For more information, refer to [Terrain layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html). |
| Metallic Default          | Output    | float      | The overall metallic value of the Terrain Layer. |
| Smoothness Default        | Output    | float      | The overall smoothness value of the Terrain Layer. |
| Color Tint                | Output    | float3     | The **Color Tint** value from the Terrain Layer. |
| Opacity As Density        | Output    | float      | Whether the Terrain Layer renders using the value stored in the alpha channel of the diffuse texture, instead of the usual splatmap weight or the height value from the mask map. For more information, refer to [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html). |
| Channel Remapping Offset  | Output    | float4     | The offsets Unity uses to remap values in each channel of the mask map texture. For more information, refer to [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html). |
| Channel Remapping Scale   | Output    | float4     | The scales Unity uses to remap values in each channel of the mask map texture. For more information, refer to [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html). |

<a name="controls"></a>
## Controls

The dropdown in the Terrain Texture node sets the texture or mask that the node inputs.

| **Value** | **Description** |
|----------|--------------|
|**Diffuse Map**|Sets the input as the diffuse texture of the **Input** Terrain Layer.|
|**Normal Map**|Sets the input as the normal map of the **Input** Terrain Layer. If there's no normal map in the Terrain Layer, the node uses normals pointing upwards.|
|**Mask Map**|Sets the input as the mask map of the **Input** Terrain Layer. The channels are usually the following:<ul><li>Red: Metallic</li><li>Green: Ambient occlusion</li><li>Blue: Height</li><li>Alpha: Smoothness</li></ul>|
|**Layer Mask**|Sets the input as the masks Unity applies to the output of the Terrain Layers.|
|**Holes**|Sets the input as the holes texture in the **Input** Terrain Layer. The holes are in the red channel.|

For more information, refer to [Terrain Layers](https://docs.unity3d.com/Manual/class-TerrainLayer.html).
