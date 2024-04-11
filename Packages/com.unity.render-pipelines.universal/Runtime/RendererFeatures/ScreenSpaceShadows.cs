using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

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
                    : RenderPassEvent.AfterRenderingPrePasses + 1; // We add 1 to ensure this happens after depth priming depth copy pass that might be scheduled

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
            private int m_ScreenSpaceShadowmapTextureID;
            private PassData m_PassData;

            internal ScreenSpaceShadowsPass()
            {
                m_CurrentSettings = new ScreenSpaceShadowsSettings();
                m_ScreenSpaceShadowmapTextureID = Shader.PropertyToID("_ScreenSpaceShadowmapTexture");
                m_PassData = new PassData();
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
                // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
                // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
                desc.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Blend)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;

                RenderingUtils.ReAllocateIfNeeded(ref m_RenderTarget, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_ScreenSpaceShadowmapTexture");
                cmd.SetGlobalTexture(m_RenderTarget.name, m_RenderTarget.nameID);

                ConfigureTarget(m_RenderTarget);
                ConfigureClear(ClearFlag.None, Color.white);
            }

            private class PassData
            {
                internal TextureHandle target;
                internal RenderingData renderingData;
                internal Material material;
                internal int shadowmapID;
            }

            /// <summary>
            /// Initialize the shared pass data.
            /// </summary>
            /// <param name="passData"></param>
            private void InitPassData(ref RenderingData renderingData, ref PassData passData)
            {
                passData.renderingData = renderingData;
                passData.material = m_Material;
                passData.shadowmapID = m_ScreenSpaceShadowmapTextureID;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
            {
                if (m_Material == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceShadows pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }

                var desc = renderingData.cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
                // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
                desc.graphicsFormat = SystemInfo.IsFormatSupported(GraphicsFormat.R8_UNorm, GraphicsFormatUsage.Blend)
                    ? GraphicsFormat.R8_UNorm
                    : GraphicsFormat.B8G8R8A8_UNorm;
                TextureHandle color = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_ScreenSpaceShadowmapTexture", true);

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space Shadows Pass", out var passData, m_ProfilingSampler))
                {
                    passData.target = builder.UseTextureFragment(color, 0, IBaseRenderGraphBuilder.AccessFlags.Write);

                    InitPassData(ref renderingData, ref passData);
                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                    {
                        ExecutePass(rgContext.cmd, data, data.target, ref data.renderingData);
                    });
                }

                RenderGraphUtils.SetGlobalTexture(renderGraph, m_ScreenSpaceShadowmapTextureID, color);
            }

            private static void ExecutePass(RasterCommandBuffer cmd, PassData data, RTHandle target, ref RenderingData renderingData)
            {
                Blitter.BlitTexture(cmd, target, Vector2.one, data.material, 0);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, false);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowScreen, true);
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_Material == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceShadows pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }

                InitPassData(ref renderingData, ref m_PassData);
                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, m_RenderTarget, ref renderingData);
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


            private static void ExecutePass(RasterCommandBuffer cmd, ref RenderingData renderingData)
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

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), ref renderingData);
                }
            }

            internal class PassData
            {
                internal ScreenSpaceShadowsPostPass pass;
                internal RenderingData renderingData;
            }
            public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Screen Space Shadow Post Pass", out var passData, m_ProfilingSampler))
                {
                    UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;
                    TextureHandle color = renderer.activeColorTexture;
                    builder.UseTextureFragment(color, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    passData.renderingData = renderingData;
                    passData.pass = this;

                    builder.AllowGlobalStateModification(true);

                    builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                    {
                        ExecutePass(rgContext.cmd, ref data.renderingData);
                    });
                }
            }
        }
    }
}
