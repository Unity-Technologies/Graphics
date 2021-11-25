# Output Mesh

Menu Path : **Context > Output Mesh**

The **Output Mesh** Context renders a regular static mesh and functions entirely separately to a particle system. An effect is often composed of different elements such as particle systems and meshes. This output allows you to render meshes directly within a Visual Effect Asset and control their shader and transform properties using nodes.

To specify a shader for this mesh, assign a [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest) to the **Shader** setting to the Context. The Visual Effect Graph then converts all exposed shader properties to input ports and displays them below the standard properties. Note that Operators that require GPU evaluation, for example [Sample Texture2D](Operator-SampleTexture2D.md) and the other sample texture Operators, are not compatible with ports on this output Context.

## Context settings

| **Setting**      | **Type**           | **Description**                                              |
| ---------------- | ------------------ | ------------------------------------------------------------ |
| **Shader**       | Shader Graph Asset | The Shader Graph to render the mesh with. When you assign a Shader Graph asset to this property, the Visual Effect Graph converts all exposed properties to input ports and displays them in this Context. |
| **Cast Shadows** | bool               | **(Inspector)** Indicates whether the mesh casts shadows. |

## Context properties

| **Input**         | **Type**                       | **Description**                                              |
| ----------------- | ------------------------------ | ------------------------------------------------------------ |
| **Mesh**          | Mesh                           | The mesh this output renders.                                |
| **Transform**     | [Transform](Type-Transform.md) | The transform to apply to the mesh. The transform can either be in world or local space. |
| **Sub Mesh Mash** | uint                           | The sub mesh mask the output uses to render the mesh. The output only renders the sub meshes with their bit set. |
