# Ray tracing and animation

If your Project uses ray-traced effects, you may need to perform further steps to display particular animations correctly.

## Alembic

If you use an [alembic](https://docs.unity3d.com/Packages/com.unity.formats.alembic@latest) animated mesh, the animation may not appear to play in ray-traced effects. To fix this:

1. Select the GameObject with the alembic animated mesh.
2. In the Inspector, find the **Mesh Renderer** component.
3. In the **Ray Tracing** section, set **Ray Tracing Mode** to **Dynamic Geometry**.

This allows the alembic mesh to animate in ray-traced effects.

## Skinned Mesh Renderer

If you use a [Skinned Mesh Renderer](<https://docs.unity3d.com/Manual/class-SkinnedMeshRenderer.html>) and the mesh is culled by the camera, the animation may not appear to play in ray-traced effects. To fix this:

1. Find and select the [Volume](Volumes.md) that contains your [Ray Tracing Settings](Ray-Tracing-Settings.md).
2. In the Inspector, find the **Volume** component.
3. Override and enable the **Extend Camera Culling** property.

This allows the Skinned Mesh Renderer to animate in ray-traced effects.

