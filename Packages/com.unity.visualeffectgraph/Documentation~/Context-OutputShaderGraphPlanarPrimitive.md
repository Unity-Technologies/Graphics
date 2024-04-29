# Output ShaderGraph Quad

Menu Path : **Context > Output Particle ShaderGraph Quad**

[!include[](Snippets/Context-OutputShaderGraph-InlineIntro.md)]

This output is similar to [Output Particle Quad](Context-OutputPrimitive.md).

## Context settings

| Setting | Type | Description |
| ------- | ---- | ----------- |
| **Shader Graph** | ShaderGraphVfxAsset | Specifies the [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest) that Unity uses to render particles produced by this output. When you are assigning a Shader Graph to this field, the Inspector exposes all the Surface Options from the Shader Graph, which allows you to edit the Shader Graph properties inside the Inspector for the Context.<br />The Context properties are populated with compatible exposed inputs from the Shader Graph.<br />For more information on the Surface Options this setting adds to the Inspector, see the documentation for the type of Shader Graph you assigned. For example, if you assigned an HDRP Lit Shader Graph, see the documentation for the [Lit Shader Graph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html). |
| **Primitive Type** | Enum | **(Inspector)** Specifies the primitive this Context uses to render each particle. The options are:<br/>&#8226; **Quad**: Renders each particle as a quad.<br/>&#8226; **Triangle**: Renders each particle as a triangle.<br/>&#8226; **Octagon**: Renders each particle as an octagon.<br /> |

## Context properties

| **Input**       | **Type** | **Description**                                              |
| --------------- | -------- | ------------------------------------------------------------ |
| **Crop Factor** | float    | The amount by which to crop the octagonal particle shape. This eliminates transparent pixels which allows for a tighter fit and reduces potential overdraw.<br/>This property only appears if you set **Primitive Type** to **Octagon**. |

[!include[](Snippets/Context-OutputShaderGraph-InlineNotes.md)]

