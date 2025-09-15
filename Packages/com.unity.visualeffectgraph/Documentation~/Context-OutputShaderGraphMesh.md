# Output ShaderGraph Mesh

Menu Path : **Context > Output Particle ShaderGraph Mesh**

You can use custom Shader Graphs in dedicated Shader Graph Outputs. Refer to [Working with Shader Graph](sg-working-with.md) for more information about the general Shader Graph workflow.

This output is similar to [Output Particle Mesh](Context-OutputParticleMesh.md).

## Context settings

| Setting | Type | Description |
| ------- | ---- | ----------- |
| **Shader Graph** | ShaderGraphVfxAsset | Specifies the [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest) Unity uses to render particles this output produces. When you are assigning a Shader Graph to this property, the Inspector exposes all the Surface Options from the Shader Graph which allows you to edit the Shader Graph properties inside the Inspector for the Context.<br />The Context properties are populated with compatible exposed input from the Shader Graph.<br />For more information on the Surface Options this adds to the Inspector, see the documentation for the type of Shader Graph you assigned. For example, if you assigned an HDRP Lit Shader Graph, see the documentation for the [Lit Shader Graph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html). |
| **Mesh Count**   | uint (slider)       | **(Inspector)** The number of different meshes to use with this output (from 1 to 4). You can select a mesh for a particle by index. This uses the particle's *meshIndex* attribute. |
| **Lod**          | bool                | **(Inspector)** Indicates whether the particle mesh uses [levels of details](https://docs.unity3d.com/Manual/LevelOfDetail.html) (LOD).If you enable this setting, the Context bases mesh selection on the particle's apparent size on screen. To specify values for the LOD mesh selection, use the **Lod Values** property. |

## Context properties

| **Input**             | **Type**    | **Description**                                              |
| --------------------- | ----------- | ------------------------------------------------------------ |
| **Mesh [N]**          | Mesh        | The meshes to use to render particles. The number of mesh input fields depends on the **Mesh Count** setting. |
| **Sub Mesh Mask [N]** | uint (mask) | The sub mesh masks to use for each mesh. The number of sub mesh mask fields depends on the **Mesh Count** setting. |
| **Lod Values**        | Vector4     | The threshold values the Context uses to choose between LOD levels. The values represent a percentage of the viewport along one dimension (For instance, a Value of 10.0 means 10% of the screen). The Context tests values from left to right (0 to n) and selects the LOD level only if the percentage of the particle on screen is above the threshold. This means you have to specify LOD values in decreasing order. Note that you can also use LOD with a mesh count of 1 to cull small particles on screen. This property only appears if you enable the **LOD** setting. |
| **Radius Scale**      | float       | The scale to apply when selecting the LOD level per particle. By default, the LOD system assumes mesh bounding boxes are unit boxes. If your mesh bounding box is smaller/bigger than the unit box, you can use this property to apply a scale so that LOD thresholds are consistent with apparent size. Frustum culling also uses this scale to compute the mesh size, when enabled. This property only appears if you enable the **LOD** or **Frustum Culling** settings. |

[!include[](Snippets/Context-OutputShaderGraph-InlineNotes.md)]

