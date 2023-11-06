# Mesh Renderer Extension reference

Use the Mesh Renderer Extension component to activate and control [High Quality Line Rendering](Override-High-Quality-Lines.md) on a Mesh Renderer.

A GameObject must have a [Mesh Filter component](https://docs.unity3d.com/2023.1/Documentation/Manual/class-MeshFilter.html) and a [Mesh Renderer component](https://docs.unity3d.com/2023.1/Documentation/Manual/class-MeshRenderer.html) for you to be able to add and use a Mesh Render Extension component. 

## High Quality Line Rendering

### Properties

| **Property**           | **Description**          |
|------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Enable**             | Enable the high quality line rendering path. |
| **Group**              | Set the sorting group that the Mesh Renderer belongs to. HDRP sorts Mesh Renderers in the same group together correctly, for proper transparency. You can add the Mesh Renderer to one of eight groups. |
| **LOD Mode**           | Set the method used for culling line segments. The options are **None**, **Camera Distance**, and **Fixed**.|
| **Camera Distance LOD** | Set the curve that defines the percentage of lines (the y axis) HDRP draws based on the distance to the camera in meters (the x axis). This property appears only if you set **LOD  Mode** to **Camera Distance**.  |
| **Fixed LOD**          | Set the amount of lines in the mesh HDRP draws. A value of `1` means HDRP draws all the lines. This property appears only if you set **LOD Mode** to **Fixed**. |
| **Shading Fraction**   | Set the amount of the vertices in the mesh HDRP uses as positions for shading samples in this frame. A value of `1` means HDRP uses all of the mesh's vertices. |
