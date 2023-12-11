# Decal Master Stack reference

You can modify the properties of a Decal Shader Graph in the Decal Master Stack.

Refer to [Decals](decals.md) for more information.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

#### Default

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/vertex-position.md)]
[!include[](snippets/shader-graph-blocks/vertex-normal.md)]
[!include[](snippets/shader-graph-blocks/vertex-tangent.md)]

</table>

#### Relevant

This Master Stack material type adds all its Vertex Blocks to the Vertex Context by default and has no extra relevant Blocks.

### Fragment Context

#### Default

When you create a new Decal Master Stack, the Fragment Context contains the following Blocks by default:

<table>
<tr>
<th>Property</th>
<th>Description</th>
<th>Setting Dependency</th>
<th>Default Value</th>
</tr>

[!include[](snippets/shader-graph-blocks/base-color.md)]
[!include[](snippets/shader-graph-blocks/alpha.md)]
[!include[](snippets/shader-graph-blocks/normal-tangent-space.md)]
[!include[](snippets/shader-graph-blocks/normal-alpha.md)]
[!include[](snippets/shader-graph-blocks/metallic.md)]
[!include[](snippets/shader-graph-blocks/ambient-occlusion.md)]
[!include[](snippets/shader-graph-blocks/smoothness.md)]
[!include[](snippets/shader-graph-blocks/maos-alpha.md)]
[!include[](snippets/shader-graph-blocks/emission.md)]

</table>

#### Relevant

This Master Stack material type adds all its Fragment Blocks to the Fragment Context by default and has no extra relevant Blocks.

## Graph Settings

### Surface Options

| **Setting**                  | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Affect BaseColor**         | Indicates whether the decal shader affects the base color of Materials it's projected or placed on. |
| **Affect Normal**            | Indicates whether the decal shader affects the normals of GameObjects it's projected or placed on. When enabled, the shader uses the **Normal** Block to override the receiving Material's normals. |
| **Affect Metal**             | Indicates whether the decal shader affects the metallic property of Materials it's projected or placed on. When enabled, the shader uses the **Metallic** Block to override the receiving Material's metallic property. |
| **Affect Ambient Occlusion** | Indicates whether the decal shader affects the ambient occlusion property of Materials it's projected or placed on. When enabled, the shader uses the **Ambient Occlusion** Block to override the receiving Material's ambient occlusion. |
| **Affect Smoothness**        | Indicates whether the decal shader affects the smoothness property of materials it's projected or placed on. When enabled, the shader uses the **Smoothness** Block to override the receiving Material's smoothness property. |
| **Affect Emissive**          | Indicates whether the decal shader affects the emission property of Materials it's projected or placed on. When enabled, the shader uses the **Emission** Block to override the receiving Material's emission property. Emissive Materials appear self-illuminated and act as a visible source of light. This property doesn't work with receiving Materials that are transparent. |

### Other Settings
| **Setting**                       | **Description**                                              |
| --------------------------------- | ------------------------------------------------------------ |
| **Transparent Dynamic Update**    | When enabled, the textures in the decal atlas are updated every frame. This property only has an impact if **Affects Transparent** is enabled in the decal projector. Enabling this has a performance impact. |

### Affects Transparent

In case the master stack material is being used with a decal projector that has **Affects Transparent** enabled the surface options determine which textures are being added to the texture atlas. Based on these settings textures are being added for:

- The BaseColor texture is always added regardless of settings. This is so the alpha value of the texture is always available.
- The Normal texture is added if **Affect Normal** is enabled.
- The Mask texture is added if **Affect Metal**, **Ambient Occlusion**, or **Smoothness** are enabled.
- Emissive is not supported.

#### Dynamic Update

By default the decal only updates if the material changes or its resolution has been changed. If the master stack material is supposed to be updated dynamically for example when using a Time node it has to be enabled explicitly through the **Transparent Dynamic Update** setting.
Decals with the same material will share the same space in the texture atlas. To create a different output, a new material has to be created.
It is possible to call DecalProjector.UpdateTransparentShaderGraphTextures through a script. This will force an update for all textures of the decal projector in the atlas. It has the advantage that a dynamic update of every frame can be avoided while still being able to update the atlas texture if needed.

#### Limitations

In general the same limitations as for decal shaders apply when enabling **Affects Transparent**. These additional limitations have to be considered for the master stack decal:

- Vertex inputs such as position, normal, or tangent are not supported. If the shader graph modifies these the modification will be ignored when the decal is applied on transparent geometry.
- Geometry, scene, and buffer inputs are not supported.
