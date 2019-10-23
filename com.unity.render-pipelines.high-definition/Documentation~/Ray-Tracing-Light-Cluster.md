# Light Cluster

To compute light bounces for ray-traced effects, such as [Reflections](Ray-Traced-Reflections.html), [Global Illumination](Ray-Traced-Global-Illumination.html), [Recursive Rendering](Ray-Tracing-Recursive-Rendering.html), or Path Tracing. HDRP uses a structure to store the set of [Lights](Light-Component.html) that affect each region. In rasterization, HDRP uses the tile structure for opaque objects and the cluster structure for transparent objects. The main difference between these two structures and this one used for ray tracing is that this one is not based on the Camera frustum.
For ray tracing, HDRP builds an axis-aligned grid which, in each cell, stores the list of Lights to fetch if an intersection occurs in that cell. Use this [Volume Override](Volume-Components.html) to change how HDRP builds this structure.

![](Images/RayTracingLightCluster1.png)

**Light Cluster [Debug Mode](Ray-Tracing-Debug.html)**: The red regions show where the number of lights per cell is equal to the maximum number of lights per cell.

## Using a Light Cluster

**Light Clusters** use the [Volume](Volumes.html) framework, so to enable this feature, and modify its properties, you need to add a **Light Cluster** override to a [Volume](Volumes.html) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and click on **Light Cluster**.

## Properties

| **Property**                | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Maximum Lights Per Cell** | Sets the maximum number of Lights that an individual cell can store. |
| **Camera Cluster Range**    | Sets the range of the cluster grid. The cluster grid itself has its center on the Camera's position and extends in all directions because an intersection may occur outside of the Camera frustum. |

