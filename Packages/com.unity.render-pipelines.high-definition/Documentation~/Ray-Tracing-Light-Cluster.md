# Calculate light bounces for ray-traced effects

HDRP uses a structure to store the set of [Lights](Light-Component.md) that affect each region to compute light bounces for ray-traced effects, such as [Reflections](Ray-Traced-Reflections.md), [Global Illumination](Ray-Traced-Global-Illumination.md), [Recursive Rendering](Ray-Tracing-Recursive-Rendering.md), or path tracing. This structure is called a light cluster. 

To create a light cluster HDRP builds an axis-aligned grid which, in each cell, stores the list of Lights to fetch if an intersection occurs in that cell. Use this [Volume Override](volume-component.md) to change how HDRP builds this structure.

In the rasterization rendering step, HDRP uses the tile structure for opaque objects and the cluster structure for transparent objects. The main difference between these two structures and this one used for ray tracing is that the light cluster structure is not based on the Camera frustum.

![](Images/RayTracingLightCluster1.png)

**Light Cluster [Debug Mode](Ray-Tracing-Debug.md#debug-modes)**

## Set up a Light Cluster

**Light Clusters** use the [Volume](understand-volumes.md) framework, so to enable this feature, and modify its properties, you need to add a **Light Cluster** override to a [Volume](understand-volumes.md) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Ray Tracing** and select **Light Cluster**.

## Properties

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Camera Cluster Range** | The range of the cluster grid in meters. The cluster grid itself has its center on the Camera's position and extends in all directions because an intersection may occur outside of the Camera frustum. |
