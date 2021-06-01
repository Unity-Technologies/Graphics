# SDF Bake Tool

The SDF Bake Tool is a utility that enables you to bake Signed Distance Fields (SDFs) to use in visual effects that rely on complex geometry. The tool takes an input [Mesh](https://docs.unity3d.com/Manual/class-Mesh.html) and generates a 3D texture representation of it, which you can use in a visual effect.

For information on what SDFs are and what you can use them for, see [Signed Distance Fields in the Visual Effect Graph](sdf-in-vfx-graph.md).

There are two ways to use the SDF Bake Tool:

- A window interface that bakes an SDF in the Unity Editor. For more information, see [The SDF Bake Tool window](sdf-bake-tool-window.md).
- An API that enables you to bake SDFs at runtime and in the Unity Editor. For more information, see [The SDF Bake Tool API](sdf-bake-tool-api.md).

## Baking box

To generate an SDF representation of a Mesh, the SDF Bake Tool places the Mesh into an axis-aligned bounding box called a baking box. The baking box represents the shape of the resulting 3D texture. To capture the SDF further away from the Mesh, you can enlarge the baking box. To only bake a particular part of the Mesh, make the baking box smaller than the Mesh and position its center so it encapsulates the part of the Mesh you want it to.
