namespace UnityEngine.Rendering.Universal
{
    internal class LightingPass : ScriptableRenderPass, IRenderPass2D
    {
        private static readonly int k_HDREmulationScaleID = Shader.PropertyToID("_HDREmulationScale");
        private static readonly int k_InverseHDREmulationScaleID = Shader.PropertyToID("_InverseHDREmulationScale");
        private static readonly int k_UseSceneLightingID = Shader.PropertyToID("_UseSceneLighting");
        private static readonly int k_RendererColorID = Shader.PropertyToID("_RendererColor");
        private readonly Renderer2DData m_Renderer2DData;

        Renderer2DData IRenderPass2D.rendererData
        {
            get { return m_Renderer2DData; }
        }

        public LightingPass(Renderer2DData rendererData)
        {
            profilingSampler = new ProfilingSampler(nameof(LightingPass));
            m_Renderer2DData = rendererData;
        }

        public void Setup()
        {
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            this.ReleaseRenderTextures(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var rtCount = 0U;
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, profilingSampler))
            {
                ref var layerBatch = ref m_Renderer2DData.layerBatches[m_Renderer2DData.currLayerBatch];

                var blendStyleMask = layerBatch.lightStats.blendStylesUsed;
                var blendStyleCount = 0U;
                while (blendStyleMask > 0)
                {
                    blendStyleCount += blendStyleMask & 1;
                    blendStyleMask >>= 1;
                }

                rtCount += blendStyleCount;

                if (rtCount > LayerUtility.maxTextureCount)
                    return;

                var isLitView = true;

#if UNITY_EDITOR
                if (renderingData.cameraData.isSceneViewCamera)
                    isLitView = UnityEditor.SceneView.currentDrawingSceneView.sceneLighting;

                if (renderingData.cameraData.camera.cameraType == CameraType.Preview)
                    isLitView = false;
#endif

                cmd.SetGlobalFloat(k_HDREmulationScaleID, m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_InverseHDREmulationScaleID, 1.0f / m_Renderer2DData.hdrEmulationScale);
                cmd.SetGlobalFloat(k_UseSceneLightingID, isLitView ? 1.0f : 0.0f);
                cmd.SetGlobalColor(k_RendererColorID, Color.white);
                this.SetShapeLightShaderGlobals(cmd);

                var desc = this.GetBlendStyleRenderTextureDesc(renderingData);
                this.RenderLights(renderingData, cmd, ref layerBatch, ref desc);

                ++m_Renderer2DData.currLayerBatch;
            }
        }
    }
}
