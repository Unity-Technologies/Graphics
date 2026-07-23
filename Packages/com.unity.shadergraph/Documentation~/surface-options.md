# Modify surface options without changing your graph

## Description

Enable **Allow Material Override** to modify a specific set of properties for Universal Render Pipeline Lit and Unlit Shader Graphs and for Built-In Render Pipeline Shader Graphs in the Material Inspector.

[!include[birp-deprecation-message](snippets/birp-deprecation-message.md)]

| Property | Behavior | 
| :--- | :--- |
| **Workflow Mode** | Refer to the URP documentation for the [Lit URP](https://docs.unity3d.com/Manual/urp/lit-shader) Shader. <br/>**Note**: Not applicable to URP Unlit and the Built-In Render Pipeline. |
| **Receive Shadows** | Refer to the URP documentation for the [Lit URP](https://docs.unity3d.com/Manual/urp/lit-shader) Shader. <br/>**Note**: Not applicable to URP Unlit and the Built-In Render Pipeline. |
| **Cast Shadows** | This property is only exposed if **Allow Material Override** is enabled for this Shader Graph. Enable this property to make it possible for a GameObject using this shader to cast shadows onto itself and other GameObjects. This corresponds to the [SubShader Tag ForceNoShadowCasting](https://docs.unity3d.com/Manual/SL-SubShaderTags.html).<br/>**Note**: Not applicable to the Built-In Render Pipeline. |
| **Surface Type** | Refer to the URP documentation for the [Lit URP](https://docs.unity3d.com/Manual/urp/lit-shader.html) and [Unlit URP](https://docs.unity3d.com/Manual/urp/unlit-shader.html) shaders. <br/>**Note**: In the Built-In Render Pipeline, this feature has the same behavior as in URP. |
| **Render Face** | Specifies which faces of the mesh the shader renders. The options are:<ul><li>**Front**: Renders only front-facing triangles. This is the default.</li><li>**Back**: Renders only back-facing triangles.</li><li>**Both**: Renders both front and back faces in a single pass.</li><li>**Back To Front**: Renders back faces first, then front faces, in two passes.</li><li>**Front To Back**: Renders front faces first, then back faces, in two passes.</li></ul>**Note**: Use **Both** instead of **Back To Front** or **Front To Back**, unless draw order between faces matters (for example, for transparent blending or stencil shadow volumes).<br/>**Note**: **Back To Front** and **Front To Back** are available only in URP. **Front**, **Back**, and **Both** behave the same in the Built-In Render Pipeline. |
| **Alpha Clipping** | Refer to the URP documentation for the [Lit URP](https://docs.unity3d.com/Manual/urp/lit-shader.html) and [Unlit URP](https://docs.unity3d.com/Manual/urp/unlit-shader.html) shaders. <br/>**Note**: In the Built-In Render Pipeline, this feature has the same behavior as in URP. |
| **Override Depth** | Enables explicit control over depth writes and the depth test for this Shader Graph. When disabled, depth follows the default behavior for the surface type: enabled for opaque, disabled for transparent. When enabled, **Write Depth** and **Depth Test** become editable. <br/>**Note**: Not applicable to the Built-In Render Pipeline. |
| **Write Depth** | Determines whether the GPU writes pixels to the [depth buffer](https://en.wikipedia.org/wiki/Z-buffering) when it uses this shader to render geometry. This option's functionality corresponds to the command [ZWrite](https://docs.unity3d.com/Manual/SL-ZWrite.html) in [ShaderLab](https://docs.unity3d.com/Manual/SL-Reference.html). To override this setting in a [RenderStateBlock](https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock.html), set the [depthState](https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock-depthState.html). <br/>This property is only available if **Override Depth** is enabled. <br/>**Note**: Not applicable to the Built-In Render Pipeline. |
| **Depth Test** | Sets the conditions under which pixels pass or fail depth testing. The GPU does not draw pixels that fail a depth test. If you choose anything other than **LEqual** (the default), consider also changing the rendering order of this material. The options are:<ul><li>**LEqual** (default): Unity draws the pixel if its depth value is less than or equal to the value in the depth texture.</li><li>**Never**: Unity never draws pixels of the affected surface.</li><li>**Less**: Unity draws pixels if their depth coordinates are less than the current depth buffer value.</li><li>**Greater**: Unity draws pixels if their depth coordinates are greater than the current depth buffer value.</li><li>**GEqual**: Unity draws pixels if their depth coordinates are greater than or equal to the current depth buffer value.</li><li>**Equal**: Unity draws pixels if their depth coordinates are equal to the current depth buffer value.</li><li>**NotEqual**: Unity draws pixels if their depth coordinates are not the same as the current depth buffer value.</li><li>**Always**: Unity draws this surface to your screen regardless of its z-coordinate.</li></ul>This option's functionality corresponds to the command [ZTest](https://docs.unity3d.com/Manual/SL-ZTest.html) in ShaderLab. To override this setting in a [RenderStateBlock](https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock.html), set the [depthState](https://docs.unity3d.com/ScriptReference/Rendering.RenderStateBlock-depthState.html) property. <br/>This property is only available if **Override Depth** is enabled. <br/>**Note**: Not applicable to the Built-In Render Pipeline. |
| **Override Stencil** | Processes and overrides the Stencil buffer values. When enabled, the [**Stencil** sub-properties](#stencil-subproperties) are available. For more information about how stencil works in shaders, refer to [ShaderLab command: Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html). <br/>**Note**: Not applicable to the Built-In Render Pipeline. |
| **Override Passes** | Controls which passes the depth and stencil state overrides apply to. Toggle each pass independently:<ul><li>**Color Pass**: Apply during the main forward color pass (opaque or transparent).</li><li>**PrePass**: Apply during the depth or depth-normals prepass. Opaque only.</li><li>**Shadow Pass**: Apply during the shadow caster pass. Requires the renderer's **Shadowmap Stencil** setting to be enabled, otherwise the shadowmap has no stencil bits to write to.</li></ul>This property is shown for Opaque surfaces and any time **Allow Material Override** is on. <br/>**Note**: Not applicable to the Built-In Render Pipeline. |

## Stencil override sub-properties

The properties in this section are only available if **Override Stencil** is enabled. The Shader Graph stencil controls map directly to the [ShaderLab Stencil command](https://docs.unity3d.com/Manual/SL-Stencil.html).

**Note**: Not applicable to the Built-In Render Pipeline.

When **Render Face** is set to **Both**, **Back To Front**, or **Front To Back**, the per-face properties (**Compare Function**, **Pass**, **Fail**, **Z Fail**) appear separately under **Front face** and **Back face** sub-sections. Otherwise they appear once and apply to whichever face is being rendered.

| Property | Behavior |
| :--- | :--- |
| **Stencil Ref** | Sets the reference value that **Compare Function** compares against the stencil buffer value for each pixel. Unity writes this value to the buffer if **Pass** is set to **Replace**. Edit the value by typing a decimal number or by clicking individual bit cells (MSB on the left). The valid range matches the user-accessible portion of the stencil byte that the URP renderer reserves for shaders. |
| **Read Mask** | Sets the binary AND mask applied to stencil values before comparison. The default is `0xFF`. |
| **Write Mask** | Sets the binary AND mask applied to stencil values before the stencil write operation (**Pass**, **Fail**, **Z Fail**). The default is `0xFF`. |
| **Compare Function** | Sets the function Unity uses to compare the **Stencil Ref** value with the value in the stencil buffer, for each pixel. For the list of available functions, refer to [CompareFunction](https://docs.unity3d.com/ScriptReference/Rendering.CompareFunction.html). |
| **Pass** | Sets the stencil operation Unity performs on the stencil value when the stencil test passes. For the list of available operations, refer to [StencilOp](https://docs.unity3d.com/ScriptReference/Rendering.StencilOp.html). |
| **Fail** | Sets the stencil operation Unity performs on the stencil value when the stencil test fails. For the list of available operations, refer to [StencilOp](https://docs.unity3d.com/ScriptReference/Rendering.StencilOp.html) |
| **Z Fail** | Sets the stencil operation Unity performs on the stencil value when the stencil test passes but the Z test fails. For the list of available operations, refer to [StencilOp](https://docs.unity3d.com/ScriptReference/Rendering.StencilOp.html) |

## Write Color toggle (URP Unlit only)

The Unlit Shader Graph has an additional **Write Color** toggle. The toggle is enabled by default. Disable it to reduce the shader to a utility that writes only to the depth and stencil buffers and never to color. Use this to mask geometry into the depth or stencil buffer without affecting the rendered image.

**Note**: Not applicable to the Built-In Render Pipeline.

When **Write Color** is disabled, the following changes apply:
- The **Base Color** block is removed from the master node.
- **Surface Type** is forced to **Opaque** and is hidden in the Graph Inspector and Material Inspector.
- **Blending Mode** is hidden.
- **Additional Motion Vectors** and **Alembic Motion Vectors** are hidden.
- The shader is treated as forward-only (similar to the **Complex Lit** shader). It does not participate in the deferred GBuffer phase. The depth prepass writes depth before the GBuffer phase, so occlusion of deferred objects still works.
- All other surface options (including **Render Face**, **Alpha Clipping**, **Override Stencil**, **Cast Shadows**, **Receive Shadows**) remain available.

## How to use

To use the Material Override feature:
1. Create a new graph in Shader Graph.
2. Save this graph.
3. Open the [Graph Inspector](Internal-Inspector.md).
4. Set **Active Targets** to **Universal** or **Built In**.
5. In the Graph Inspector's **Universal** or **Built In** section, enable **Allow Material Override**.
6. Create or select a Material or GameObject which uses your Shader Graph.
7. In the Material Inspector, modify **Surface Options** for the target Material or GameObject.
