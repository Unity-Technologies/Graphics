# Deferred Rendering Path in URP

This section describes the Deferred Rendering Path, which uses the light stencil volume technique to render light volumes and apply deferred shading. To select the Rendering Path, see [Rendering](../urp-universal-renderer.md#rendering) in the URP Universal Renderer asset.

![Scene rendered with the Deferred Rendering Path](../Images/rendering-deferred/deferred-intro-image.png)<br/>*Sample Scene rendered with the Deferred Rendering Path.*

## <a name="requirements"></a>Requirements

The Deferred Rendering Path has the following requirements and limitations on top of the general system requirements for the Unity Player.

* Minimum Shader Model: Shader Model 4.5.

* Deferred Rendering Path does not support the OpenGL-based graphics API: Desktop&#160;OpenGL, OpenGL&#160;ES&#160;2.0, OpenGL&#160;ES&#160;3.0, WebGL&#160;1.0, WebGL&#160;2.0.

## Implementation details

This section describes the implementation details of this feature, and technical details about how this feature functions.

### G-buffer layout

This section describes how Unity stores material attributes in the G-buffer in the Deferred Rendering Path.

The following illustration shows the data structure for each pixel of the render targets that Unity uses in the Deferred Rendering Path.

![Data structure of the render targets that Unity uses in the Deferred Rendering Path](../Images/rendering-deferred/data-structure-render-targets-g-buffer.png)

The data structure consists of the following components.

**Albedo (sRGB)**

This field contains the albedo color in sRGB format, 24 bits.

**MaterialFlags**

This field is a bit field that contains Material flags:

* Bit 1, **ReceiveShadowsOff**: if set, the pixel does not receive dynamic shadows.

* Bit 2, **SpecularHighlightsOff**: if set, the pixel does not receive specular highlights.

* Bit 4, **SubtractiveMixedLighting**: if set, the pixel uses subtractive mixed lighting.

* Bit 8, **SpecularSetup**: if set, the Material uses the specular workflow.

For more technical details, see the file `/ShaderLibrary/UnityGBuffer.hlsl`.

**Specular**

This field contains the following values:

* SimpleLit Material: RGB specular color stored in 24 bits.

* Lit Material with metallic workflow: reflectivity stored in 8 bits, 16 bits are not used.

* Lit Material with specular workflow: RGB specular color stored in 24 bits.

**Occlusion**

This field contains the baked occlusion value from the baked lighting. For real-time lighting, Unity calculates the ambient occlusion value by combining the baked occlusion value with the SSAO value.

**Normal**

This field contains the world space normals encoded in 24 bits. For information on the encoding of normals, see section [Encoding of normals in G-buffer](#accurate-g-buffer-normals).

**Smoothness**

This field stores the smoothness value for the SimpleLit and Lit materials.

**Emissive/GI/Lighting**

This render target contains the Material emissive output and baked lighting. Unity fills this field during the G-buffer Pass. During the deferred shading pass, Unity stores lighting results in this render target.

Render target format:

* **B10G11R11_UFloatPack32**, unless one of the following conditions is true:

    * In URP Asset, the setting **Quality** > **HDR** is turned on, and the target Player platform does not support HDR.

    * In Player Settings, the setting **PreserveFramebufferAlpha** is true.

* **R16G6B16A16_SFloat**, if Unity cannot use **B10G11R11_UFloatPack32** because of the project settings.

* If Unity cannot use one of the other formats in the list, it uses what the following method returns: `SystemInfo.GetGraphicsFormat(DefaultFormat.HDR)`.

**ShadowMask**

Unity adds this render target to the G-buffer layout when Lighting Mode is set to Subtractive or Shadow mask.

The Subtractive and the Shadow mask modes are optimized for the Forward Rendering Path, and are less efficient in the Deferred Rendering Path. In the Deferred Rendering Path,  avoid using these modes and use the Baked Indirect mode instead to improve GPU performance.

**Rendering Layer Mask**

Unity adds this render target to the G-buffer layout when the Light Layers feature is enabled (URP Asset, **Advanced** > **Light Layers**). The Light Layers feature might have a significant impact on the GPU performance. For more information, see section [Light Layers](#light-layers).

**Depth as Color**

Unity adds this render target to the G-buffer layout when Native Render Pass is enabled on platforms that support it. Unity renders depth as a color into this render target. This render target has the following purpose:

* Improves performance on Vulkan devices.

* Lets Unity get the depth buffer on Metal API, which does not allow fetching the depth from the DepthStencil buffer within the same render pass.

The format of the Depth as Color render target is `GraphicsFormat.R32_SFloat`.

**DepthStencil**

Unity reserves the four highest bits of this render target to mark the Material type. See also [URP Pass tags: UniversalMaterialType](../urp-shaders/urp-shaderlab-pass-tags.md#universalmaterialtype).

For this render target, Unity selects either the `D32F_S8` format, or the `D24S8` format depending on the platform.

### <a name="accurate-g-buffer-normals"></a>Encoding of normals in G-buffer

In the Deferred Rendering Path, Unity stores normals in the G-buffer. Unity encodes each normal as a 24 bit value.

When you select the **Deferred** option in the **Rendering Path** property in the URP Universal Renderer asset, Unity shows the **Accurate G-buffer normals** property.

![The Accurate G-buffer normals property in the URP Universal Renderer asset](../Images/rendering-deferred/urp-renderer-accurate-g-buffer-normals.png)

The **Accurate G-buffer normals** property lets you configure how Unity encodes the normals.

* **Accurate G-buffer normals** off: Unity stores values of normal vectors in the G-buffer in the RGB channel of a normal texture, 8 bit per value (x, y, z). The values are quantized with the loss of accuracy. This option increases performance, especially on mobile GPUs, but might lead to color banding artifacts on smooth surfaces.

* **Accurate G-buffer normals** on: Unity uses the octahedron encoding to store values of normal vectors in the RGB channel of a normal texture. With this encoding, values of normal vectors are more accurate, but the encoding and decoding operations put extra load on the GPU.<br/>The precision of the encoded normal vectors is similar to the precision of the sampled values in the Forward Rendering Path.

The following illustration shows the visual difference between the two options when the Camera is very close to the GameObject:

![Accurate G-buffer normals, visual difference between the two options.](../Images/rendering-deferred/difference-accurate-g-buffer-normals-on-off.png)

**Performance considerations**

With **Accurate G-buffer normals** option turned on, there is extra load on the GPU because of the encoding and decoding operations. This load is insignificant for desktop platforms and consoles, but might be considerable for mobile GPUs.

Turning the option on does not increase the memory footprint. To store the normals, Unity uses the same RGB channel in the normal texture regardless of the encoding.

### <a name="render-passes"></a>Deferred Rendering Path render Passes

The following table shows the sequence of Render Pass events in the Deferred Rendering Path.

<table>
    <thead>
    <tr>
        <th>Render Pass events</th>
        <th>Deferred Rendering Path Passes</th>
        <th>SSAO Renderer Feature Passes</th>
    </tr>
    </thead>
    <tbody>
    <tr>
        <td>BeforeRendering</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingShadows</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingShadows</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingPrePasses</td>
        <td>Depth, or depth and normal prepass (Forward only materials)</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingPrePasses</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingGbuffer</td>
        <td>G-buffer Pass (GBufferPass)</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>&#160;</td>
        <td>Copy G-buffer depth texture</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingGbuffer</td>
        <td>&#160;</td>
        <td>SSAO (optional)</td>
    </tr>
    <tr>
        <td>BeforeRenderingDeferredLights</td>
        <td>&#160;</td>
        <td rowspan="4"><img src="../Images/rendering-deferred/decorator-or.png" /></td>
    </tr>
    <tr>
        <td></td>
        <td>Deferred rendering (stencil)</td>
    </tr>
    <tr>
        <td>AfterRenderingDeferredLights</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingOpaques</td>
        <td>Opaque Forward-only Materials</td>
    </tr>
    <tr>
        <td>AfterRenderingOpaques</td>
        <td>&#160;</td>
        <td>SSAO and blending (optional)</td>
    </tr>
    <tr>
        <td>BeforeRenderingSkybox</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingSkybox</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingTransparents</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingTransparents</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>BeforeRenderingPostProcessing</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRenderingPostProcessing</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tr>
        <td>AfterRendering</td>
        <td>&#160;</td>
        <td>&#160;</td>
    </tr>
    <tbody>
</table>

The following sections describe the Deferred Rendering Path render Passes.

#### Depth, or depth and normal prepass

In the Deferred Rendering Path, in the depth prepass or depth and normal prepass, Unity renders only the Materials that do not support the deferred rendering model. For example, Materials using the Complex Lit shader are such Materials.

In the Deferred Rendering Path, Unity does not use the depth prepass to generate a copy of the depth buffer (this behavior is different in the Forward Rendering Path).

If the Universal Renderer has the SSAO Renderer Feature, Unity executes the depth and normal prepass. SSAO uses the screen-space depth and normal buffers to calculate ambient occlusion.

#### Optional passes: SSAO, SSAO with blending

If the Universal Renderer has the SSAO Renderer Feature, and the **After Opaque** option is disabled (the option is disabled by default), Unity executes the SSAO Pass at the AfterRenderingGbuffer event. The SSAO Renderer Feature calculates the SSAO texture. Unity samples this texture in the Deferred rendering Pass and in the Pass that renders Forward-only Materials.

Using this Pass order, Unity can combine the baked occlusion with the real-time occlusion from the SSAO Renderer Feature, and avoid double-darkening from the baked and the real-time ambient occlusion.

When the After Opaque option is enabled, Unity executes the SSAO and blending Pass at the AfterRenderingOpaques event, after rendering the Forward-only Materials. Unity then executes an extra full screen Pass to overlay the SSAO texture onto the Emissive/GI/Lighting buffer. This causes over-darkening of the areas that receive baked occlusion and real-time occlusion.

**Performance considerations**

On mobile platforms with TBDR architecture, with the **After Opaque** option is disabled, Unity requires an extra render target for load and store operations. This has a significant performance impact.

Enabling the **After Opaque** option on mobile platforms improves GPU performance. On mobile platforms with TBDR architecture, enabling this option avoids extra render target load and store operations.

#### Forward-only Pass

Certain Unity shaders use lighting models that Unity cannot render in the Deferred Rendering Path.

Examples of such shaders:

* **Complex Lit**: the Lighting model of this shader (for example, the Clear Coat effect) is too complex and extra Material properties cannot fit into the G-buffer.

* **Baked Lit** and **Unlit**: these shaders do not calculate real-time lighting, that's why Unity renders them into the Emissive/GI/Lighting buffer directly during the Forward-only pass. This is faster than evaluating the shaders in the Deferred rendering (stencil) pass.

* **Custom shaders**: Unity renders the shaders that do not declare the Pass tags required by the Deferred Rendering Path as Forward-only. The required Pass tags are: `LightMode`, and `UniversalMaterialType`. For more information, see [URP Pass tags](../urp-shaders/urp-shaderlab-pass-tags.md).

Unity renders Materials with such shaders in the Forward Rendering Path. For the SSAO Renderer Feature to be able to calculate ambient occlusion for the Materials using the **Complex Lit** shader, Unity must render such Materials in the depth and normal prepass first. This is because Unity does not render those Materials in the G-buffer pass (GBufferPass). For more information, see [URP Pass tags](../urp-shaders/urp-shaderlab-pass-tags.md).

## ShaderLab Pass tags

To enable Unity to render a shader in the Deferred Rendering Path, the shader must have a Pass with the following tag definition:

`"LightMode" = "UniversalGBuffer"`

Unity executes the shader with such `LightMode` tag during the G-buffer Pass.

To indicate that Unity must render a certain Material in the Forward-only Pass in the Deferred Rendering Path, add the following tags to a shader Pass:

`"LightMode" = "UniversalForwardOnly"`

`"LightMode" = "DepthNormalsOnly"`

To specify the shader lighting model (Lit, SimpleLit), use the `UniversalMaterialType` tag.

For more information, see the section [URP Pass tags: LightMode](../urp-shaders/urp-shaderlab-pass-tags.md#lightmode).
