---
uid: sample-element-texture-node
---

# Sample Element Texture node

[!include[](include_note_uitk.md)]

The Sample Element Texture node samples a texture at specific UV coordinates. You can use this node to get multiple samples of the texture assigned to the element, for example to create complex visual effects or manipulate the texture.

Sampling multiple times with this node is more efficient than using several separate nodes for individual samples. This is because each node introduces overhead by traversing internal branches to select the correct texture slot. By combining multiple samples into a single node, you reduce this overhead and improve performance. Use the **Sample Count** setting to control how many samples the node takes.

## Limitations

[!include[nodes-sample-fragment-lod](./snippets/sample-nodes/nodes-sample-fragment-lod.md)]

## Ports

The node has a pair of **UV** input and **Color** output ports for each sample. The number of pairs matches the **Sample Count** setting, and the ports are numbered from `0` to **Sample Count** &minus; 1. By default, the node has four samples (**UV 0** to **UV 3** and **Color 0** to **Color 3**).

| Name           | Direction | Type          | Description                                                                                                           |
|----------------|-----------|---------------|-----------------------------------------------------------------------------------------------------------------------|
| UV *n*         | Input     | Vector2       | The UV coordinates for sample *n*.                                                                                   |
| Sampler        | Input     | Sampler State | The Sampler State to use when sampling. If you leave this port unconnected, the node uses the element texture's own sampler. |
| LOD            | Input     | Float         | The mip level to sample. This port only appears when **Mip Sampling Mode** is **LOD**.                              |
| Color *n*      | Output    | Color         | The color sampled at **UV *n***.                                                                                    |

## Additional node settings

The Sample Element Texture [!include[nodes-additional-settings](./snippets/nodes-additional-settings.md)]

| Name                  | Description                                                                                                                                                                                                                                                                                       |
|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Sample Count**      | The number of samples the node takes, between 1 and 32. The node displays one **UV** input port and one **Color** output port per sample.                                                                                                                                                        |
| **Mip Sampling Mode** | Controls how the node selects the texture mip level. The options are:<ul><li>**Standard**: The GPU calculates the mip level from screen-space derivatives. You can only use this mode in the **Fragment** Context.</li><li>**LOD**: You specify the mip level explicitly through the **LOD** input port. Use this mode to sample the texture in the **Vertex** Context.</li></ul> |

## Additional resources

- [Introduction to UI Shader Graph](xref:uie-introduction-to-ui-shader-graph)
