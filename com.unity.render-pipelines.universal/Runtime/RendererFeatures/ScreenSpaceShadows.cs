using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceShadowsSettings
    {
    }

    [DisallowMultipleRendererFeature]
    internal class ScreenSpaceShadows : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField, HideInInspector] private Shader m_Shader = null;
        [SerializeField] private ScreenSpaceShadowsSettings m_Settings = new ScreenSpaceShadowsSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceShadowsPass m_SSShadowsPass = null;
        private RestoreShadowKeywordsPass m_RestoreShadowKeywordsPass = null;

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceShadows";

        /// <inheritdoc/>
        public override void Create()
        {
            if (m_SSShadowsPass == null)
                m_SSShadowsPass = new ScreenSpaceShadowsPass();
            if (m_RestoreShadowKeywordsPass == null)
                m_RestoreShadowKeywordsPass = new RestoreShadowKeywordsPass();

            LoadMaterial();

            m_SSShadowsPass.profilerTag = name;
            m_SSShadowsPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            m_RestoreShadowKeywordsPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!LoadMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, m_SSShadowsPass.profilerTag);
                return;
            }

            bool allowMainLightShadows = renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1;
            bool shouldEnqueue = m_SSShadowsPass.Setup(m_Settings) && allowMainLightShadows;

            if (shouldEnqueue)
            {
                renderer.EnqueuePass(m_SSShadowsPass);
                renderer.EnqueuePass(m_RestoreShadowKeywordsPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

        private bool LoadMaterial()
        {
            if (m_Material != null)
            {
                return true;
            }

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            m_SSShadowsPass.material = m_Material;

            return m_Material != null;
        }

        private class ScreenSpaceShadowsPass : ScriptableRenderPass
        {
            // Public Variables
            internal string profilerTag;
            internal Material material;

            // Private Variables
            private ScreenSpaceShadowsSettings m_CurrentSettings;
            private RenderTextureDescriptor m_RenderTextureDescriptor;
            private RenderTargetHandle m_RenderTarget;

            // Constants
            private const string k_SSShadowsTextureName = "_ScreenSpaceShadowmapTexture";

            internal ScreenSpaceShadowsPass()
            {
                m_CurrentSettings = new ScreenSpaceShadowsSettings();
                m_RenderTarget.Init(k_SSShadowsTextureName);
            }

            internal bool Setup(ScreenSpaceShadowsSettings featureSettings)
            {
                m_CurrentSettings = featureSettings;
                ConfigureInput(ScriptableRenderPassInput.Depth);

                return material != null;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                m_RenderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                m_RenderTextureDescriptor.depthBufferBits = 0;
                m_RenderTextureDescriptor.msaaSamples = 1;
                m_RenderTextureDescriptor.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;

                cmd.GetTemporaryRT(m_RenderTarget.id, m_RenderTextureDescriptor, FilterMode.Point);

                RenderTargetIdentifier renderTargetTexture = m_RenderTarget.Identifier();
                ConfigureTarget(renderTargetTexture);
                ConfigureClear(ClearFlag.None, Color.white);
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. {1} render pass will not execute. Check for missing reference in the renderer resources.", GetType().Name, profilerTag);
                    return;
                }

                Camera camera = renderingData.cameraData.camera;

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.ResolveShadows)))
                {
                    if (!renderingData.cameraData.xr.enabled)
                    {
                        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material);
                        cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                    }
                    else
                    {
                        // Avoid setting and restoring camera view and projection matrices when in stereo.
                        RenderTargetIdentifier screenSpaceShadowTexture = m_RenderTarget.Identifier();
                        cmd.Blit(null, screenSpaceShadowTexture, material);
                    }

                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// <inheritdoc/>
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                {
                    throw new ArgumentNullException("cmd");
                }

                cmd.ReleaseTemporaryRT(m_RenderTarget.id);
            }
        }

        private class RestoreShadowKeywordsPass : ScriptableRenderPass
        {
            const string m_ProfilerTag = "Restore Shadow Keywords Pass";
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ConfigureTarget(BuiltinRenderTextureType.CurrentActive);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    ShadowData shadowData = renderingData.shadowData;
                    int cascadesCount = shadowData.mainLightShadowCascadesCount;

                    bool mainLightShadows = renderingData.shadowData.supportsMainLightShadows;
                    bool receiveShadowsNoCascade = mainLightShadows && cascadesCount == 1;
                    bool receiveShadowsCascades = mainLightShadows && cascadesCount > 1;

                    // Before transparent object pass, force screen space shadow for main light to disable
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, false);

                    // then enable main light shadows with or without cascades
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, receiveShadowsNoCascade);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, receiveShadowsCascades);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
