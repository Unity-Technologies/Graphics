# Decal Shader Graph

The [Decal Projector](renderer-feature-decal.md#decal-projector-component) component can project a Material as a decal if the Material uses a Shader Graph with the Decal Material type.

![Shader Graph with the Decal Material type](Images/decal/decal-shader-graph-material-type.png)<br/>*Shader Graph with the Decal Material type*

URP contains the pre-built Decal Shader Graph (`Shader Graphs/Decal`).

![Decal Material properties.](Images/decal/decal-material-properties.png)<br/>*Decal Material properties and advanced options.*

You can assign a Material that uses a Shader Graph with the Decal Material type to a GameObject directly. In this case Unity projects the decal on objects that the GameObject occludes (taking depth bias into account).

The Decal Shader Graph has the following properties:

* **Base Map**: the Base texture of the Material.

* **Normal Map**: the normal texture of the Material.

* **Normal Blend**: this property defines the proportion in which the the normal texture selected in the Normal Map property blends with the normal map of the Material that the decal is projected on. 0: the decal does not affect the Material it's projected on. 1: the normal map of the decal replaces the normal map of the Material it's projected on.

A Material which is assigned a Shader Graph with the Decal Material type has the following options in the **Advanced Options** section.

| __Property__ | __Description__ |
|---|---|
| __Enable GPU&#160;Instancing__ | Enabling this option lets URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__ | Use this slider to determine the chronological rendering order for a Material. URP renders Materials with higher values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. ***This property defines the order in which URP draws decals in the Scene. URP draws decals with lower Priority values first, and draws decals with higher Priority values on top of those with lower values. <br />If there are multiple Decal Materials with the same __Priority__ in the Scene, URP renders them in the order in which the Materials were created. |
| __Mesh Bias Type__  | This property is only applicable when a Decal Material type is assigned to a GameObject directly (not projected by a Decal Projector). Determines the type of bias that URP applies to the Mesh of the GameObject with the Decal Material type. The bias lets you determine how Unity draws the decal relative to other GameObjects. |
| _View Bias_         | A world-space bias (in meters) that URP applies to the Mesh along the view vector. A positive value draws the decal in front of any overlapping Mesh, a negative value offsets the decal away from the Camera. Decal Projectors ignore this property. |
| _Depth Bias_        | A depth bias that URP applies to the Mesh. A negative value draws the decal in front of any overlapping Mesh, a positive value offsets the decal further behind. Decal Projectors ignore this property. |
