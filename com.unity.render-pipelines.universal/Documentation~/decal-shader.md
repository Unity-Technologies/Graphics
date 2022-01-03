# Decal Shader Graph

The [Decal Projector](renderer-feature-decal.md#decal-projector-component) component can project a Material as a decal if the Material uses a Shader Graph with the Decal Material type.

![Shader Graph with the Decal Material type](Images/decal/decal-shader-graph-material-type.png)<br/>*Shader Graph with the Decal Material type*

Users are encouraged to [make their own Shader Graphs](#making-decal-shaders) when using decals but URP also contains the pre-built Decal Shader (`Shader Graphs/Decal`).

![Decal Material properties.](Images/decal/decal-material-properties.png)<br/>*Decal Material properties and advanced options.*

You can assign a Material that uses a Decal Shader Graph to a GameObject directly. For example, you can [use a Quad as the Decal GameObject](renderer-feature-decal.md#decal-gameobject).

The pre-built Decal Shader has the following properties:

* **Base Map**: the Base texture of the Material.

* **Normal Map**: the normal texture of the Material.

* **Normal Blend**: this property defines the proportion in which the the normal texture selected in the Normal Map property blends with the normal map of the Material that the decal is projected on. 0: the decal does not affect the Material it's projected on. 1: the normal map of the decal replaces the normal map of the Material it's projected on.

The above properties are defined in the shader graph and therefore would vary for any custom decal shader graph. Some properties however are common to any decal shader. These properties reside in the **Advanced Options** section of the material insepctor. 

| __Property__ | __Description__ |
|---|---|
| __Enable GPU&#160;Instancing__ | Enabling this option lets URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__ | This property defines the order in which URP draws decals in the Scene. URP draws decals with lower Priority values first, and draws decals with higher Priority values on top of those with lower values. <br />If there are multiple Decal Materials with the same __Priority__ in the Scene, URP renders them in the order in which the Materials were created. |
| <a name="mesh-bias-type"></a>__Mesh Bias Type__  | Select the Mesh bias type. The Mesh bias lets you prevent z-fighting between the Decal GameObject and the GameObject it overlaps. This property is only applicable for GameObjects with a [Decal Material type assigned directly](renderer-feature-decal.md#decal-gameobject). |
| _View Bias_         | A world-space bias (in meters). When drawing the Decal GameObject, Unity shifts each pixel of the GameObject by this value along the view vector. A positive value shifts pixels closer to the Camera, so that Unity draws the Decal GameObject on top of the overlapping Mesh, which prevents z-fighting. Decal Projectors ignore this property. |
| _Depth Bias_        | When drawing the Decal GameObject, Unity changes the depth value of each pixel of the GameObject by this value. A negative value shifts pixels closer to the Camera, so that Unity draws the Decal GameObject on top of the overlapping Mesh, which prevents z-fighting. Decal Projectors ignore this property. |

## Making Decal Shaders
The provided `Shader Graphs/Decal` shader is meant to be simple and doesn’t expose all of the features supported by decals in URP. In order to take full advantage of decals, users can create custom decal shaders using Shader Graph and the decal subtarget.

A main configuration needed for the decal subtarget is to decide which surface properties the decal effects. Enabling these properties will effectively “override” the equivalent Lit Shader property on the surface. 

![Affecting properties](Images/decal/decal-affect-properties.png)

| __Property__ | __Description__ |
|---|---|
|__Affect BaseColor__ | Affecting base color is useful on almost all cases. An exception is surface damage were you often want to manipulate other properties like normals ![Decal Color](Images/decal/decal-color.png)</br>*From left to right: Only affecting color, affecting everything, not affecting color*|
|__Affect Normal__ | Affecting normal is often used for adding damage to materials like bullet holes or in this case cracks in a road. Normals will give depth to the decal and if carefully authored can even make it seem like the projected object is lying on top like leaves on a forest road or garbage on a street. Blending is used to mask the overriden normal. If the decal normal is not blended, the decal will override the normal all over the projected surface ![Decal Normal](Images/decal/decal-normal.png)</br>*From left to right: No normals, normals without blend, normals with blend* |
|__Affect MAOS__ | MOAS is short for Metallic, Ambient Occlusion and Smoothness. To save memory these properties have been grouped together. The values of the properties can still be set invidually but they can only be blended with a single common alpha value. Overriding smoothness is useful for puddles or wet paint. Overriding a metallic surface with a lower metallic value is useful for rust. Overriding AO is often used to give the decal more depth. ![Decal MAOS](Images/decal/decal-maos.png) </br>*Difference in decal affecting and not affecing MAOS*|
|__Affect Emission__ | Affecting emission is useful for either making surfaces seem like they are emitting light, or to make surfaces seem like they are being lit by light. ![Decal Emission](Images/decal/decal-emission.png) </br>*Decal with Affect Emission turned off an on*|

Even though a decal shader affects a lot of property, it doesn't mean that a texture map is needed for each. Depending on the use case it might make sense to pack data into textures such that less samples are needed and less textures need to be stored. In this example graph, a normal map and mask map are used to drive all properties in the shader. The decal is used for damaged tarmac so a hardcoded roughness of 0 will do.

![Decal Graph](Images/decal/decal-graph.png)

The shader samples the mask and uses the color for setting the Ambient Occlusion (Red Channel), smoothness (Green Channel), Emission intensity (Blue Channel) and alpha for the entire decal. Decals are ofte blended using single alpha values for all properties. The mask map for the examplem tarmac cracks looks like this
![Decal Mask](Images/decal/decal-mask.png)</br>*Exampe of mask map that packs Ambient Occlusion, Smoothness, Emission and aAlpha of a decal atlas into a single texture*
