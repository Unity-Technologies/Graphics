# Graph Settings tab reference

Use the **Graph Settings** tab in the [Graph Inspector](Internal-Inspector.md) window to change settings that affect the current shader graph as a whole.

## General properties

| Property | Description |
| :--- | :--- |
| **Precision** | Select a default [Precision Mode](Precision-Modes.md) for the entire graph. You can override the precision mode at the node level in your graph. |
| **Preview** | Select your preferred preview mode for the nodes that support preview. The options are: <ul><li>**Inherit**:  The Unity Editor automatically selects the preview mode to use.</li><li>**Preview 2D**: Renders the output of the sub graph as a flat two-dimensional preview.</li><li>**Preview 3D**: Renders the output of the sub graph on a three-dimensional object such as a sphere.</li></ul> This property is available only in [sub graphs](Sub-graph.md).  |

## Target Settings

Add or remove graph targets to the current shader graph and set target properties according to the selected material type.

### Active Targets

A list that contains the [graph targets](Graph-Target.md) selected for the current shader graph. Select the **Add (+)** and **Remove (&minus;)** buttons to add or remove **Active Targets**.

Shader Graph supports the following target types:
* **Custom Render Texture**: Shaders for updating [Custom Render Textures](Custom-Render-Texture.md).
* **Built-in**: Shaders for Unity’s [Built-In Render Pipeline](xref:built-in-render-pipeline).
* **Universal**: Shaders for the [Universal Render Pipeline (URP)](xref:um-universal-render-pipeline), available only if your project uses URP.
* **HDRP**: Shaders for the [High Definition Render Pipeline (HDRP)](xref:high-definition-render-pipeline), available only if your project uses HDRP.

### Target properties

Each graph target added in the list of **Active Targets** has its own set of properties.

| Property | Description |
| :--- | :--- |
| **Material** | Selects a material type for the target. The available options depend on the current target type. |
| Other properties (contextual) | A set of material and shader related properties that correspond to the current target type and the **Material** you select for the target.<ul><li>For Universal (URP) target properties, refer to [Shader graph material Inspector window reference for URP](xref:um-shaders-in-universalrp-reference).</li><li>For HDRP target properties, refer to HDRP's [Shader Graph materials reference](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/shader-graph-materials-reference.html).</li></ul> |
| **Custom Editor GUI** | Renders a custom editor GUI in the Inspector window of the material. Enter the name of the GUI class in the field. For more information, refer to [Control material properties in the Inspector window](xref:um-writing-shader-display-types) and [Custom Editor block in ShaderLab reference](xref:um-sl-custom-editor). |
| **Support VFX Graph** | Enables this shader graph to support the [Visual Effect Graph](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@latest) to render particles.<br />**Note**: This option is only available for certain material types. |

## Additional resources

- [Precision Modes](Precision-Modes.md)
- [Graph targets](Graph-Target.md)
