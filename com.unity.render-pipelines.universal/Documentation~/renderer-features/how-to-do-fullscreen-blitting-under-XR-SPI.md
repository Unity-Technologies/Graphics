# Example: How to create a full screen blitting feature under Single Pass Instanced rendering in XR

The example on this page describes how to create a custom scriptable rendering feature doing full screen blit in XR

## Example overview

This example adds a ScriptableRenderPass that blits ScriptableRenderPassInput.Color to the CameraColorTarget. It uses the command buffer to draw a full screen mesh for both eyes.
The example also includes a shader used to perform the GPU side of the rendering, which works by sampling the color buffer using XR sampler macros.

## Prerequisites

This example requires the following:

* A Unity project with the URP package installed.

* The **Scriptable Render Pipeline Settings** property refers to a URP asset (**Project Settings** > **Graphics** > **Scriptable Render Pipeline Settings**).

## Create example Scene and GameObjects<a name="example-objects"></a>

To follow the steps in this example, create a new Scene with the following GameObjects:

1. Create a Cube.

    ![Scene Cube](../Images/how-to-blit-in-xr/renderobj-cube.png)

Now you have the setup necessary to follow the steps in this example.

## Example implementation

This section assumes that you created a Scene as described in section [Example Scene and GameObjects](#example-objects).

The example implementation uses Scriptable Renderer Features: to draw color buffer information on screen.

### Create a Renderer Feature and configure both input and output

Follow these steps to create a Renderer Feature

1. Create a new feature under the project asset folder. In this example, we name the file `ColorBlitRendererFeature.cs`.
```cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRendererFeature : ScriptableRendererFeature
{
    public Shader m_Shader;
    public float m_Intensity;

    Material m_Material;

    ColorBlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTarget, m_Intensity);
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public override void Create()
    {
        if (m_Shader != null)
            m_Material = new Material(m_Shader);

        m_RenderPass = new ColorBlitPass(m_Material);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}

```
2. Create the scriptable render pass that issue the custom blit draw call. In this example, we name the file `ColorBlitPass.cs`
In this example, we use `cmd.DrawMesh` to draw a fullscreen quad that performs the blit operation.
Do **Not** use the `cmd.Blit` in URP XR as it has serveral compatibility issues with URP XR integration and it may enable/disable XR shader keyword behind the scene which breaks XR SPI rendering.
```cs
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
    Material m_Material;
    RenderTargetIdentifier m_CameraColorTarget;
    float m_intensity;

    public ColorBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void SetTarget(RenderTargetIdentifier colorHandle, float intensity)
    {
        m_CameraColorTarget = colorHandle;
        m_intensity = intensity;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            m_Material.SetFloat("_Intensity", m_intensity);
            cmd.SetRenderTarget(new RenderTargetIdentifier(m_CameraColorTarget, 0, CubemapFace.Unknown, -1));
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}
```
3. Create the shader that performs the blit operation. In this example, we name the file `ColorBlit.shader`. The vertex stage outputs the fullscreen quad position. The fragment stage samples color buffer and writes out the `color*intencity` value to the render target.
```hlsl
Shader "ColorBlit"
{
        SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionHCS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                float2  uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Note: The pass is setup with a mesh already in CS
                // Therefore, we can just output vertex position
                output.positionCS = float4(input.positionHCS.xyz, 1.0);

                #if UNITY_UV_STARTS_AT_TOP
                output.positionCS.y *= -1;
                #endif

                output.uv = input.uv;
                return output;
            }

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            float _Intensity;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.uv);
                return color * _Intensity;
            }
            ENDHLSL
        }
    }
}
```
4. Select forward renderer.

    ![Select Forward Renderer](../Images/how-to-blit-in-xr/forward-renderer-asset.png)

7. Configure forward renderer to use the new render feature and set the intencity to 1.5.

    ![Configure New Renderer Feature](../Images/how-to-blit-in-xr/new-render-feature.png)

6. Configure project to use XRSDK. Add mock hmd provider and select single pass instanced as rendering mode.

    ![Configure MockHMD](../Images/how-to-blit-in-xr/mock-hmd-render-mode.png)

The example is complete. When running in playmode, color buffer is displayed.
![Color Output](../Images/how-to-blit-in-xr/render-obj-cube-color-output.PNG)
