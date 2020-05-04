using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Render all tiled-based deferred lights.
    internal class GBufferPass : ScriptableRenderPass
    {
        public static GraphicsFormat[] kGBufferFormats = new GraphicsFormat[5] {
            GraphicsFormat.R8G8B8A8_SRGB,    // albedo          albedo          albedo          occlusion       (sRGB rendertarget)
            GraphicsFormat.R8G8B8A8_SRGB,    // specular        specular        specular        metallic        (sRGB rendertarget)
            GraphicsFormat.R8G8B8A8_UNorm,   // encoded-normal  encoded-normal  encoded-normal  smoothness
            GraphicsFormat.None,             // Emissive+baked: Most likely B10G11R11_UFloatPack32 or R16G16B16A16_SFloat
            GraphicsFormat.R32_SFloat        // Optional: some mobile platforms are faster reading back depth as color instead of real depth.
        };

        DeferredLights m_DeferredLights;

        ShaderTagId m_ShaderTagId = new ShaderTagId("UniversalGBuffer");
        ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Render GBuffer");

        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;

        public GBufferPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, DeferredLights deferredLights)
        {
            base.renderPassEvent = evt;
            m_DeferredLights = deferredLights;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer gbufferCommands = CommandBufferPool.Get("Render GBuffer");
            using (new ProfilingScope(gbufferCommands, m_ProfilingSampler))
            {
                if (m_DeferredLights.accurateGbufferNormals)
                    gbufferCommands.EnableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
                else
                    gbufferCommands.DisableShaderKeyword(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);

                gbufferCommands.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                // Note: a special case might be required if(renderingData.cameraData.isStereoEnabled) - see reference in ScreenSpaceShadowResolvePass.Execute

                context.ExecuteCommandBuffer(gbufferCommands); // send the gbufferCommands to the scriptableRenderContext - this should be done *before* calling scriptableRenderContext.DrawRenderers
                gbufferCommands.Clear();

                DrawingSettings drawingSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStereoEnabled)
                {
                    context.StartMultiEye(camera, eyeIndex);
                }

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref m_FilteringSettings/*, ref m_RenderStateBlock*/);
            }
            context.ExecuteCommandBuffer(gbufferCommands);
            CommandBufferPool.Release(gbufferCommands);
        }
    }
}
