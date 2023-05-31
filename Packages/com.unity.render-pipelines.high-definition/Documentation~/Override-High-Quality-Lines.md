# High Quality Line Rendering

Rendering line topology via traditional hardware rasterization, for example to represent hair or fur, can easily suffer from image quality problems.

Use the **High Quality Line Rendering** override to draw line geometry with analytic anti-aliasing and transparent sorting.

![](Images/HQLines-HW.png)
An example of hardware lines.

![](Images/HQLines-SW.png)
An example of high quality lines.

## How High Quality Line Rendering works

The High Quality Line Renderer is a line segment software rasterizer designed for fast, high quality transparency and anti-aliasing.

Outlined below are the general steps of this raster algorithm.

1. The camera's view frustum is divided into many clusters. A single cluster is 8 pixels wide by 8 pixels high. The length is derived from the **Cluster Count**.
2. Each cluster computes a list of the visible line segments that intersect with it.
3. Clusters are processed in front-to-back order. For each cluster, the segment list is unpacked and sorted from front-to-back, and the **Sorting Quality** determines the maximum amount of segments sorted in a single cluster.
4. The sorted segment list is processed. For each segment pixel, HDRP computes the shading contribution, computes the analytically anti-aliased coverage mask, and blends the fragment behind the final tile result.
5. HDRP computes the average opacity of the tile. If the average opacity is greater than the **Tile Opacity Threshold**, the tile is complete. A threshold lower than 1.0 can greatly improve performance. 
6. HDRP repeats steps 3 to 5 until all tiles have been processed.

## Enable High Quality Line Rendering

[!include[](snippets/Volume-Override-Enable-Override.md)]

* To enable High Quality Line Rendering in your HDRP Asset go to **Rendering** > **High Quality Line Rendering**.
* To enable High Quality Line Rendering in your Frame Settings go to **Edit** > **Project Settings** > **Graphics** > **HDRP Global Settings** > **Frame Settings (Default Values)** > **Camera** > **Rendering** > **High Quality Line Rendering**.

## Using High Quality Line Rendering

Once you've enabled High Quality Line Rendering in your project, follow these steps to render high quality lines in your scene:

1. In the Scene or Hierarchy window, select a GameObject that contains a [Volume](Volumes.md) component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override** > **Rendering** and select **High Quality Lines**. HDRP now applies High Quality Line Rendering to any Camera this Volume affects.
3. In the Scene or Hierarchy view, select a GameObject that contains a [Mesh Renderer component](https://docs.unity3d.com/2023.1/Documentation/Manual/class-MeshRenderer.html).
4. In the Inspector, navigate to **Add Component**, then find and add the [Mesh Renderer Extension component](Mesh-Renderer-Extension.md).

Unity warns you if there is any configuration issue with your GameObject that prevents High Quality Line Rendering:

- If you're warned about the Topology, ensure that the mesh connected to your MeshFilter component is composed of LineTopology.
- If you're warned about the Material, open your Material's shader graph and enable **Support High Quality Line Rendering** in the Master Stack.

You can use the [Rendering Debugger](Render-Pipeline-Debug-Window.md) to visualize the underlying data used to calculate the high quality lines.

## Properties

| **Property** || **Description** |
|--|--|--|
| **Composition Mode** || Determine when in the render pipeline lines are rendered into the main frame. |
|| **Before Color Pyramid** | Use this setting if you want lines to appear in transparency effects. |
|| **After Temporal Anti-Aliasing** | Use this setting if you use [Temporal Anti-Aliasing](Anti-Aliasing.html#temporal-antialiasing-taa), so HDRP uses a stable depth buffer. |
|| **After Depth Of Field** | Use this setting if the lines will be in focus against a blurrier scene. |
| **Cluster Count**          || Set the number of clusters in a tile, between the camera's near and far plane. |
| **Sorting Quality**        || Set the quality of the line rendering, which affects the maximum number of segments HDRP can sort within a cluster. The options are **Low**, **Medium**, **High** and **Ultra**. The higher the quality, the more memory high quality line rendering uses. |
| **Tile Opacity Threshold** || Set the opacity value that qualifies as an opaque tile. A threshold lower than 1.0 can greatly improve performance. |
