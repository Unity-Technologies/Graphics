using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceShadowsSettings
    {
    }

    [DisallowMultipleRendererFeature("Screen Space Shadows")]
    [Tooltip("Screen Space Shadows")]
    [URPHelpURL("renderer-feature-screen-space-shadows")]
    internal class ScreenSpaceShadows : ScriptableRendererFeature
    {
#if UNITY_EDITOR
        [UnityEditor.ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.OpenGLES2)]
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
                bool isDeferredRenderingMode = renderer is UniversalRenderer && ((UniversalRenderer)renderer).renderingModeRequested == RenderingMode.Deferred;

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
            m_SSShadowsPass?.Dispose();
            m_SSShadowsPass = null;
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
            private RTHandle m_RenderTarget;

            internal ScreenSpaceShadowsPass()
            {
                m_CurrentSettings = new ScreenSpaceShadowsSettings();
            }

            public void Dispose()
            {
                m_RenderTarget?.Release();
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
                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                desc.graphicsFormat = RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R8_UNorm, FormatUsage.Linear | FormatUsage.Render)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderTarget, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_ScreenSpaceShadowmapTexture");
                cmd.SetGlobalTexture(m_RenderTarget.name, m_RenderTarget.nameID);

                ConfigureTarget(m_RenderTarget);
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

                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    Blitter.BlitCameraTexture(cmd, m_RenderTarget, m_RenderTarget, m_Material, 0);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
                }
            }
        }

        private class ScreenSpaceShadowsPostPass : ScriptableRenderPass
        {
            // Profiling tag
            private static string m_ProfilerTag = "ScreenSpaceShadows Post";
            private static ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);
            private static readonly RTHandle k_CurrentActive = RTHandles.Alloc(BuiltinRenderTextureType.CurrentActive);

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ConfigureTarget(k_CurrentActive);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = renderingData.commandBuffer;
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
            }
        }
    }
}
