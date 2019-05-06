using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    internal class FinalPostProcessPass : ScriptableRenderPass
    {
        RenderTargetHandle m_Source;

        readonly Material m_Material;
        readonly PostProcessData m_Data;

        int m_DitheringTextureIndex;

        const string k_RenderPostProcessingTag = "Render Final PostProcessing Pass";

        public FinalPostProcessPass(RenderPassEvent evt, PostProcessData data)
        {
            renderPassEvent = evt;

            var shader = data.shaders.finalPostPassPS;
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. FinalPostProcessPass render pass will not execute. Check for missing reference in the renderer resources.");
                return;
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_Data = data;
        }

        public void Setup(in RenderTargetHandle source)
        {
            m_Source = source;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);

            if (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
                m_Material.EnableKeyword(ShaderKeywordStrings.Fxaa);
            else
                m_Material.DisableKeyword(ShaderKeywordStrings.Fxaa);

            if (cameraData.isDitheringEnabled)
            {
                m_DitheringTextureIndex = RenderingUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    cameraData.camera,
                    m_Material,
                    ShaderConstants._BlueNoise_Texture,
                    ShaderConstants._Dithering_Params
                );
                m_Material.EnableKeyword(ShaderKeywordStrings.Dithering);
            }
            else
                m_Material.DisableKeyword(ShaderKeywordStrings.Dithering);

            Blit(cmd, m_Source.Identifier(), RenderTargetHandle.CameraTarget.Identifier(), m_Material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _BlueNoise_Texture = Shader.PropertyToID("_BlueNoise_Texture");
            public static readonly int _Dithering_Params  = Shader.PropertyToID("_Dithering_Params");
        }
    }
}
