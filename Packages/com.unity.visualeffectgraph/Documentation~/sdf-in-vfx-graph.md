# Signed Distance Fields in the Visual Effect Graph

Signed Distance Fields (SDF) are 3D textures where each texel stores the distance from the surface of an object. By convention, this distance is negative inside the object and positive outside. SDFs are useful for creating particle effects that interact with complex geometry.

## Using SDFs

In the Visual Effect Graph, there are several nodes that use SDFs to create effects:

- [**Position On Signed Distance Field**](Block-SetPositionShape.md): Positions particles either within the volume of the SDF or on its surface.
- [**Attractor Shape Signed Distance Field**](Block-ConformToSignedDistanceField.md): Attracts particles towards an SDF. This is useful for pulling particles towards a complex shape that would be difficult to replicate using other force blocks.
- [**Collision Shape Signed Distance Field**](Block-CollisionShape.md): Simulates collision between particles and an SDF. This is useful when you want particles to collide with complex shapes.
- [**Sample Signed Distance Field**](Operator-SampleSDF.md): Samples an SDF and enables you to create custom behavior with the result.

## Generating SDFs

There are multiple ways to generate an SDF for use in a visual effect:

- You can generate SDFs in the Unity Editor using a window, and generate SDFs at runtime using an API, with the built-in [SDF Bake Tool](sdf-bake-tool.md) tool. For more information, see [SDF Bake Tool](sdf-bake-tool.md)
- You can bake SDFs with the Houdini Volume Exporter bundled with [*VFXToolbox*](https://github.com/Unity-Technologies/VFXToolbox) (located in the /DCC~ folder).

## Limitations and Caveats

The [SDF Bake Tool](sdf-bake-tool.md), both the window and the API, generates normalized SDFs. This means the underlying surface scales such that the largest side of the Texture is of length 1. Remember this if you use Operators such as Sample Signed Distance Field.
