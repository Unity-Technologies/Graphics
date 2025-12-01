# Water Decal Master Stack reference

You can modify the properties of a Water Decal Shader Graph in the Water Decal Master Stack.

Refer to [Water Decals](introduction-to-water-decals.md) for more information.

## Contexts

[!include[](snippets/master-stacks-contexts-intro.md)]

### Vertex Context

The Vertex context represents the vertex stage of this shader. Unity executes any block you connect to this context in the vertex function of this shader. For more information, refer to [Master Stack](https://docs.unity3d.com/Packages/com.unity.shadergraph@16.0/manual/Master-Stack.html).

Vertex blocks aren't compatible with the Water Decal Master Stack.

### Fragment Context

Depending on the [Graph Settings](#graph-settings) you choose, the Fragment Context displays the following blocks.

| **Property** | **Description** | **Setting Dependency** | **Default Value** |
| ------------ | --------------- | ---------------------- | -------------- |
| **Deformation** | Sets height offset in the decal area to simulate surface displacement. | **Affect Deformation** enabled. | 0.0 |
| **Horizontal Deformation** | Sets the horizontal offset in UV space to simulate water flow direction. | **Affect Deformation** enabled. | (0,0) |
| **SurfaceFoam** | Sets the foam intensity on the water surface in the decal area. | **Affect Foam** enabled. | 0.0 |
| **DeepFoam** | Sets the foam intensity for deeper water zones in the decal area. | **Affect Foam** enabled. | 0.0 |
| **SimulationMask** | Sets the mask that defines areas affected by water simulation (for example, waves and currents). | **Affect Simulation** enabled. | (1,1,1) |
| **SimulationFoamMask** | Sets the mask that defines areas where simulation-generated foam appears. | **Affect Foam** enabled. | 1.0 |
| **LargeCurrent** | Defines large-scale water current direction and strength. | **Affect Large Current** enabled. | (0,0) |
| **LargeCurrentInfluence** | Controls how much the large current affects the decal area. | **Affect Large Current** enabled. | 1.0 |
| **RipplesCurrent** | Defines small ripple current direction and strength. | **Affect Ripples Current** enabled. |  (0,0) |
| **RipplesCurrentInfluence** | Controls how much ripple currents affect the decal area. | **Affect Ripples Current** enabled. | 1.0 |

## Graph Settings

Explore the shader graph settings you can use to customize the Fragment Context.

| **Setting**  | **Description** | 
| ------------ | --------------- | 
| **Affect Deformation** | Enables customizing deformation-related properties. When you enable this setting, Unity adds the **Deformation** and **Horizontal Deformation** Blocks to the Fragment context. |
| **Affect Foam** | Enables customizing foam-related properties. When you enable this setting, Unity adds the **SurfaceFoam** and **DeepFoam** Blocks to the Fragment context. |
| **Affect Simulation Mask** | Enables customizing simulation-related properties. When you enable this setting, Unity adds the **SimulationMask** and **SimulationFoamMask** Blocks to the Fragment context. |
| **Affect Large Current** | Enables customizing current-related properties. When you enable this setting, Unity adds the **LargeCurrent** and **LargeCurrentInfluence** Blocks to the Fragment context. |
| **Affect Ripples Current** | Enables customizing ripple-related properties. When you enable this setting, Unity adds the **RipplesCurrent** and **RipplesCurrentInfluence** Blocks to the Fragment context. |
| **Custom Editor GUI** | Renders a custom editor GUI in the Inspector window of the material. Enter the name of the GUI class in the field. A Custom Editor GUI class might replace default properties. For more information, refer to [Custom material Inspectors](custom-material-inspectors.md).|

## Additional resources

- [Water surface fluctuations](water-decals-masking-landing.md)