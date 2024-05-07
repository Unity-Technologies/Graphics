# Make a GameObject compatible with the GPU Resident Drawer

To make a GameObject compatible with the [GPU Resident Drawer](gpu-resident-drawer.md), check it has the following properties:

- Has a [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html).
- In the Mesh Renderer component, **Light Probes** isn't set to **Use Proxy Volume**.
- Uses only static global illumination, not real time global illumination.
- Uses a shader that supports DOTS instancing. Refer to [Supporting DOTS Instancing](https://docs.unity3d.com/Manual/dots-instancing-shaders.html) for more information.
- Doesn't move position after one camera finishes rendering and before another camera starts rendering.
- Doesn't use the `MaterialPropertyBlock` API.
- Doesn't have a script that uses a per-instance callback, for example `OnRenderObject`.

## Exclude a GameObject from the GPU Resident Drawer

To exclude a GameObject from the GPU Resident Drawer, add a **Disallow GPU Driven Rendering** component to the GameObject.

1. Select the GameObject.
2. In the **Inspector** window, select **Add Component**.
3. Select **Disallow GPU Driven Rendering**.

Select **Apply to Children Recursively** to exclude both the GameObject and its children.

## Additional resources

- [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html)

