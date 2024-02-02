# Use high quality line rendering

[!include[](snippets/Volume-Override-Enable-Override.md)]

* To enable High Quality Line Rendering in your HDRP Asset go to **Rendering** > **High Quality Line Rendering**.
* To enable High Quality Line Rendering in your Frame Settings go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP** > **Frame Settings (Default Values)** > **Camera** > **Rendering** > **High Quality Line Rendering**.

## Using High Quality Line Rendering

Once you've enabled High Quality Line Rendering in your project, follow these steps to render high quality lines in your scene:

1. In the Scene or Hierarchy window, select a GameObject that contains a [Volume](understand-volumes.md) component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override** > **Rendering** and select **High Quality Lines**. HDRP now applies High Quality Line Rendering to any Camera this Volume affects.
3. In the Scene or Hierarchy view, select a GameObject that contains a [Mesh Renderer component](https://docs.unity3d.com/2023.1/Documentation/Manual/class-MeshRenderer.html).
4. In the Inspector, navigate to **Add Component**, then find and add the [Mesh Renderer Extension component](Mesh-Renderer-Extension.md).

Unity warns you if there is any configuration issue with your GameObject that prevents High Quality Line Rendering:

- If you're warned about the Topology, ensure that the mesh connected to your MeshFilter component is composed of LineTopology.
- If you're warned about the Material, open your Material's shader graph and enable **Support High Quality Line Rendering** in the Master Stack.

You can use the [Rendering Debugger](use-the-rendering-debugger.md) to visualize the underlying data used to calculate the high quality lines.

Refer to the following for more information:

- [High Quality Line Rendering Volume Override reference](Override-High-Quality-Lines.md)
- [Mesh Renderer Extension](Mesh-Renderer-Extension.md)
