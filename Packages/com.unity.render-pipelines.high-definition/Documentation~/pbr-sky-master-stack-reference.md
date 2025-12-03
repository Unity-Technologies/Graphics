# PBR Sky Master Stack reference

You can modify the properties of a Physically Based Rendering (PBR) Sky Shader Graph in the PBR Sky Master Stack.

Refer to [Create a physically based sky](create-a-physically-based-sky.md) for more information.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

The Vertex context represents the vertex stage of this shader. Unity executes any block you connect to this context in the vertex function of this shader. For more information, refer to [Master Stack](https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Master-Stack.html).

Vertex blocks aren't compatible with the PBR Sky Master Stack.

### Fragment Context

Depending on the [Graph Settings](#graph-settings) you choose, the Fragment Context displays the following blocks.

| **Property** | **Description** | **Setting Dependency** | **Default Value** |
| ------------ | --------------- | ---------------------- | ----------------- |
| **Space Color** | Sets the base color of the sky. | None. | `Color.black` |
| **Ground Color** | Sets the diffuse color of the ground surface. | **Ground Shading** enabled. | `Color.white` |
| **Ground Emission** | Sets the emissive color of the ground surface.  | **Ground Shading** enabled. | `Color.black` |
| **Ground Smoothness** | Controls the specular reflection sharpness of the ground surface. Choose a high value for sharper, mirror-like reflections, or a low value to produce rougher, more diffuse reflections. | **Ground Shading** enabled. | 0.0 |
| **Ground Normal (Tangent Space)** | Sets the ground normal in tangent space. | **Ground Shading** enabled and **Fragment Normal Space** set to **Tangent**. | `CoordinateSpace.Tangent` |
| **Ground Normal (Object Space)** | Sets the ground normal in object space. | **Ground Shading** enabled and **Fragment Normal Space** set to **Object**. | `CoordinateSpace.Object` |
| **Ground Normal (World Space)** | Sets the ground normal in world space. | **Ground Shading** enabled and **Fragment Normal Space** set to **World**. | `CoordinateSpace.World` |

## Graph Settings

Explore the shader graph settings you can use to customize the Fragment Context.

| **Setting**  | **Description** | 
| ------------ | --------------- | 
| **Fragment Normal Space** | Specifies the coordinate space for the normal input in the Fragment Context. The options are:<ul><li>**Tangent**: Normals are relative to the mesh surface’s UV orientation.</li><li>**Object**: Normals are relative to the mesh’s local coordinate system.</li><li>**World**: Normals are relative to the global scene coordinate system.</li></ul>When you change this setting, Unity adds the corresponding **Normal** Block to the Fragment context. |
| **Ground Shading** | Enables rendering of a physically based ground surface or horizon beneath the sky. When you enable this setting, Unity adds the **Ground Color**, **Ground Emission**, **Ground Smoothness**, and **Ground Normal** Blocks to the Fragment context. |
| **Custom Editor GUI** | Renders a custom editor GUI in the Inspector window of the material. Enter the name of the GUI class in the field. A Custom Editor GUI class might replace default properties. For more information, refer to [Custom material Inspectors](custom-material-inspectors.md).|
