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
            var filmGrain = VolumeManager.instance.stack.GetComponent<FilmGrain>();

            m_Material.shaderKeywords = null;

            // FXAA setup
            if (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing)
                m_Material.EnableKeyword(ShaderKeywordStrings.Fxaa);

            // Film Grain setup
            if (filmGrain.IsActive())
            {
                m_Material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Data,
                    filmGrain,
                    cameraData.camera,
                    m_Material
                );
            }

            // Dithering setup
            if (cameraData.isDitheringEnabled)
            {
                m_Material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    cameraData.camera,
                    m_Material
                );
            }

            Blit(cmd, m_Source.Identifier(), RenderTargetHandle.CameraTarget.Identifier(), m_Material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
