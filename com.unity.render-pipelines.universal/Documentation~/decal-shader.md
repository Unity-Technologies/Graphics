# Decal Shader Graph

The [Decal Projector](renderer-feature-decal.md#decal-projector-component) component can project a Material as a decal if the Material uses a Shader Graph with the Decal Material type.

![Shader Graph with the Decal Material type](Images/decal/decal-shader-graph-material-type.png)<br/>*Shader Graph with the Decal Material type*

URP contains the pre-built Decal Shader Graph (`Shader Graphs/Decal`).

![Decal Material properties.](Images/decal/decal-material-properties.png)<br/>*Decal Material properties and advanced options.*

You can assign a Material that uses a Decal Shader Graph to a GameObject directly. For example, you can [use a Quad as the Decal GameObject](renderer-feature-decal.md#decal-gameobject).

The Decal Shader Graph has the following properties:

* **Base Map**: the Base texture of the Material.

* **Normal Map**: the normal texture of the Material.

* **Normal Blend**: this property defines the proportion in which the the normal texture selected in the Normal Map property blends with the normal map of the Material that the decal is projected on. 0: the decal does not affect the Material it's projected on. 1: the normal map of the decal replaces the normal map of the Material it's projected on.

A Material which is assigned a Shader Graph with the Decal Material type has the following options in the **Advanced Options** section.

| __Property__ | __Description__ |
|---|---|
| __Enable GPU&#160;Instancing__ | Enabling this option lets URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__ | Use this slider to determine the chronological rendering order for a Material. URP renders Materials with lower values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. ***This property defines the order in which URP draws decals in the Scene. URP draws decals with lower Priority values first, and draws decals with higher Priority values on top of those with lower values. <br />If there are multiple Decal Materials with the same __Priority__ in the Scene, URP renders them in the order in which the Materials were created. |
| <a name="mesh-bias-type"></a>__Mesh Bias Type__  | Select the Mesh bias type. The Mesh bias lets you prevent z-fighting between the Decal GameObject and the GameObject it overlaps. This property is only applicable for GameObjects with a [Decal Material type assigned directly](renderer-feature-decal.md#decal-gameobject). |
| _View Bias_         | A world-space bias (in meters). When drawing the Decal GameObject, Unity shifts each pixel of the GameObject by this value along the view vector. A positive value shifts pixels closer to the Camera, so that Unity draws the Decal GameObject on top of the overlapping Mesh, which prevents z-fighting. Decal Projectors ignore this property. |
| _Depth Bias_        | When drawing the Decal GameObject, Unity changes the depth value of each pixel of the GameObject by this value. A negative value shifts pixels closer to the Camera, so that Unity draws the Decal GameObject on top of the overlapping Mesh, which prevents z-fighting. Decal Projectors ignore this property. |
