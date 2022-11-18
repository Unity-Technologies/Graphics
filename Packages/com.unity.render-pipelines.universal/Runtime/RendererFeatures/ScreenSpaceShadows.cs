using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceShadowsSettings
    {
    }

    [DisallowMultipleRendererFeature]
    [Tooltip("Screen Space Shadows")]
    internal class ScreenSpaceShadows : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.SelectIf(true, keywordNames: ShaderKeywordStrings.MainLightShadowScreen)]
        private const bool k_RequiresScreenSpaceShadowsKeyword = true;
#endif

        // Serialized Fields
        [SerializeField, HideInInspector] private Shader m_Shader = null;
        [SerializeField] private ScreenSpaceShadowsSettings m_Settings = new ScreenSpaceShadowsSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceShadowsPass m_SSShadowsPass = null;
        private ScreenSpaceShadowsPostPass m_SSShadowsPostPass = null;

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceShadows";

        /// <inheritdoc/>
        public override void Create()
        {
            if (m_SSShadowsPass == null)
                m_SSShadowsPass = new ScreenSpaceShadowsPass();
            if (m_SSShadowsPostPass == null)
                m_SSShadowsPostPass = new ScreenSpaceShadowsPostPass();

            LoadMaterial();

            m_SSShadowsPass.renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
            m_SSShadowsPostPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!LoadMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool allowMainLightShadows = renderingData.shadowData.supportsMainLightShadows && renderingData.lightData.mainLightIndex != -1;
            bool shouldEnqueue = allowMainLightShadows && m_SSShadowsPass.Setup(m_Settings, m_Material);

            if (shouldEnqueue)
            {
                bool isDeferredRenderingMode = renderer is UniversalRenderer && ((UniversalRenderer)renderer).renderingMode == RenderingMode.Deferred;

                m_SSShadowsPass.renderPassEvent = isDeferredRenderingMode
                    ? RenderPassEvent.AfterRenderingGbuffer
                    : RenderPassEvent.AfterRenderingPrePasses;

                renderer.EnqueuePass(m_SSShadowsPass);
                renderer.EnqueuePass(m_SSShadowsPostPass);
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

            return m_Material != null;
        }

        private class ScreenSpaceShadowsPass : ScriptableRenderPass
        {
            // Profiling tag
            private static string m_ProfilerTag = "ScreenSpaceShadows";
            private static ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

            // Private Variables
            private Material m_Material;
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

            internal bool Setup(ScreenSpaceShadowsSettings featureSettings, Material material)
            {
                m_CurrentSettings = featureSettings;
                m_Material = material;
                ConfigureInput(ScriptableRenderPassInput.Depth);

                return m_Material != null;
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
                if (m_Material == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceShadows pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }

                Camera camera = renderingData.cameraData.camera;

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    if (!renderingData.cameraData.xr.enabled)
                    {
                        cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
                        cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                    }
                    else
                    {
                        Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                        Vector4 scaleBiasRt = new Vector4(1, 1, 0, 0);
                        cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);
                        cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                        // Avoid setting and restoring camera view and projection matrices when in stereo.
                        RenderTargetIdentifier screenSpaceShadowTexture = m_RenderTarget.Identifier();
                        cmd.SetRenderTarget(new RenderTargetIdentifier(screenSpaceShadowTexture, 0, CubemapFace.Unknown, -1),
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                        cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Quads, 4, 1, null);
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

        private class ScreenSpaceShadowsPostPass : ScriptableRenderPass
        {
            // Profiling tag
            private static string m_ProfilerTag = "ScreenSpaceShadows Post";
            private static ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

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

                    // Before transparent object pass, force to disable screen space shadow of main light
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
