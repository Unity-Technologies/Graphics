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

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceShadows";

        /// <inheritdoc/>
        public override void Create()
        {
            if (m_SSShadowsPass == null)
                m_SSShadowsPass = new ScreenSpaceShadowsPass();

            LoadMaterial();
            m_SSShadowsPass.profilerTag = name;
            m_SSShadowsPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
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

            bool shouldAdd = m_SSShadowsPass.Setup(m_Settings) && renderingData.shadowData.supportsMainLightShadows;
            if (shouldAdd)
                renderer.EnqueuePass(m_SSShadowsPass);
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
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSShadows.Execute()");
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
                ConfigureClear(ClearFlag.All, Color.white);
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
                ShadowData shadowData = renderingData.shadowData;

                int cascadesCount = shadowData.mainLightShadowCascadesCount;

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, cascadesCount == 1);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowsCascades, cascadesCount > 1);

                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material);
                    cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);

                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowsCascades, false);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowsScreen, true);
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
    }
}
