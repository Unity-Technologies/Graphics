# Output ShaderGraph Strip

Menu Path : **Context > Output Particle ShaderGraph Strip**

[!include[](Snippets/Context-OutputShaderGraph-InlineIntro.md)]

This output is similar to Output ParticleStrip Quad.

## Context settings

| Setting | Type | Description |
| ------- | ---- | ----------- |
| **Shader Graph** | ShaderGraphVfxAsset | Specifies the [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest) Unity uses to render particles this output produces. When you are assigning a Shader Graph to this property, the Inspector exposes all the Surface Options from the Shader Graph which allows you to edit the Shader Graph properties inside the Inspector for the context.<br />The context properties will be populated with compatible exposed input from the shaderGraph.<br />For more information on the Surface Options this adds to the Inspector, see the documentation for the type of Shader Graph you assigned. For example, if you assigned an HDRP Lit Shader Graph, see the documentation for the [Lit Shader Graph](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/master-stack-lit.html). |
| **Tiling Mode** | Enum | Specifies how the output generates texture coordinates within a strip. The options are:<br/>&#8226; **Stretch**: Stretches the mapping along the whole strip.<br/>&#8226; **Repeat Per Segment**: Restarts the mapping for every segment of the strip.<br/>&#8226; **Custom**: Manually provides the reference texture coordinate.<br/> |
| **Swap UV** | Bool | Invert the two channels of texture coordinates. |

## Context properties

| **Input**     | **Type** | **Description**                                              |
| ------------- | -------- | ------------------------------------------------------------ |
| **Tex Coord** | float    | Custom texture coordinate that acts as a reference for each segment of a strip.<br/>This property only appears if you set **Tiling Mode** to **Custom**. |

[!include[](Snippets/Context-OutputShaderGraph-InlineNotes.md)]

