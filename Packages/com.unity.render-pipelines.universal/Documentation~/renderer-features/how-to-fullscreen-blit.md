# Example of a complete Scriptable Renderer Feature

The example on this page performs a full-screen blit that tints the screen green.

To use the examples, follow these steps:

1. To create the custom render pass, create a new C# script called `ColorBlitPass.cs`, then paste in the code from the [Example custom render pass](#render-pass) section.

    **Note:** The example uses the `Blitter` API. Don't use the `CommandBuffer.Blit` API in URP. Refer to [Blit](../customize/blit-overview) for more information.

2. To create the Scriptable Renderer Feature that adds the custom render pass to the render loop, create a new C# script called `ColorBlitRendererFeature.cs`, then paste in the code from the [Example Scriptable Renderer Feature](#scriptable-renderer-feature) section.

3. To create the shader code that tints the pixels green, [create a shader file](https://docs.unity3d.com/2022.3/Documentation/Manual/class-Shader.html), then paste in the code from the [Example shader](#shader) section.

4. Add the `ColorBlitRendererFeature` to the current URP Renderer asset. For more information, refer to [Add a Renderer Feature to a URP Renderer](../urp-renderer-feature.md).

To change the brightness, adjust the **Intensity** property in the **Color Blit Renderer Feature** component.

**Note:** To visualize the example if your project uses XR, install the [MockHMD XR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.mock-hmd@latest/) package in your project, then set the **Render Mode** property to **Single Pass Instanced**.

<a name="render-pass"></a>
## Example custom render pass

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
    Material m_Material;
    RTHandle m_CameraColorTarget;
    float m_Intensity;

    public ColorBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void SetTarget(RTHandle colorHandle, float intensity)
    {
        m_CameraColorTarget = colorHandle;
        m_Intensity = intensity;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(m_CameraColorTarget);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            m_Material.SetFloat("_Intensity", m_Intensity);
            Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}
```

<a name="scriptable-renderer-feature"></a>
## Example Scriptable Renderer Feature

The Scriptable Renderer Feature adds the render pass to the render loop.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRendererFeature : ScriptableRendererFeature
{
    public Shader m_Shader;
    public float m_Intensity;

    Material m_Material;

    ColorBlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_RenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                        in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle, m_Intensity);
        }
    }

    public override void Create()
    {
        m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
        m_RenderPass = new ColorBlitPass(m_Material);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
```

<a name="shader"></a>
## Example shader

The shader performs the GPU side of the rendering. It samples the color texture from the camera, then outputs the color with the green value set to the chosen intensity.

**Note:** The shader you use with the `Blitter` API must be a hand-coded shader. [Shader Graph](xref:um-shader-graph) shaders aren't compatible with the `Blitter` API.

```c++
Shader "ColorBlit"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // The Blit.hlsl file provides the vertex shader (Vert),
            // the input structure (Attributes) and the output structure (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            // Set the color texture from the camera as the input texture
            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            // Set up an intensity parameter
            float _Intensity;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample the color from the input texture
                float4 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.texcoord);

                // Output the color from the texture, with the green value set to the chosen intensity
                return color * float4(0, _Intensity, 0, 1);
            }
            ENDHLSL
        }
    }
}
```

## Additional resources

- [Custom render pass workflow](../renderer-features/custom-rendering-pass-workflow-in-urp.md)
- [Injecting a render pass via a Scriptable Renderer Feature](../renderer-features/scriptable-renderer-features/scriptable-renderer-features-landing.md)
- [Writing custom shaders in URP](../writing-custom-shaders-urp.md)
