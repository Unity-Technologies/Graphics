using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceAmbientOcclusionSettings
    {
        // Parameters
        [SerializeField] internal bool Downsample = false;
        [SerializeField] internal DepthSource Source = DepthSource.DepthNormals;
        [SerializeField] internal NormalQuality NormalSamples = NormalQuality.Medium;
        [SerializeField] internal float Intensity = 3.0f;
        [SerializeField] internal float DirectLightingStrength = 0.25f;
        [SerializeField] internal float Radius = 0.035f;
        [SerializeField] internal int SampleCount = 6;
        [SerializeField] internal int BlurPasses = 2;

        // Enums
        internal enum DepthSource
        {
            Depth = 0,
            DepthNormals = 1,
            //GBuffer = 2
        }

        internal enum NormalQuality
        {
            Low,
            Medium,
            High
        }
    }

    [DisallowMultipleRendererFeature]
    internal class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField, HideInInspector] private Shader shader = null;
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";

        /// <inheritdoc/>
        public override void Create()
        {
            // Create the pass...
            if (m_SSAOPass == null)
            {
                m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
            }

            GetMaterial();
            m_SSAOPass.profilerTag = name;
            m_SSAOPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, m_SSAOPass.profilerTag);
                return;
            }

            bool shouldAdd = m_SSAOPass.Setup(settings);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_SSAOPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(m_Material);
        }

        private bool GetMaterial()
        {
            if (m_Material != null)
            {
                return true;
            }

            if (shader == null)
            {
                shader = Shader.Find(k_ShaderName);
                if (shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(shader);
            m_SSAOPass.material = m_Material;
            return m_Material != null;
        }

        // The SSAO Pass
        private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
        {
            // Public Variables
            internal string profilerTag;
            internal Material material;

            // Private Variables
            private Vector4 m_offsetIncrement = Vector4.zero;
            private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;
            private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSAO.Execute()");
            private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);
            private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);
            private RenderTextureDescriptor m_Descriptor;

            // Constants
            private const int k_KawaseBlurShaderPassID = 3;
            private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";
            private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";

            // Statics
            private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
            private static readonly int s_ScaleBiasId = Shader.PropertyToID("_ScaleBiasRT");
            private static readonly int s_BlurOffsetID = Shader.PropertyToID("_BlurOffset");
            private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
            private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
            private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");

            internal ScreenSpaceAmbientOcclusionPass()
            {
                m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
            }

            internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings)
            {
                m_CurrentSettings = featureSettings;
                switch (m_CurrentSettings.Source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth:
                        ConfigureInput(ScriptableRenderPassInput.Depth);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        ConfigureInput(ScriptableRenderPassInput.Normal);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return material != null
                       &&  m_CurrentSettings.Intensity > 0.0f
                       &&  m_CurrentSettings.Radius > 0.0f
                       &&  m_CurrentSettings.SampleCount > 0;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;

                // Update SSAO parameters in the material
                Vector4 ssaoParams = new Vector4(
                    1.0f / downsampleDivider,      // Downsampling
                    m_CurrentSettings.Intensity,   // Intensity
                    m_CurrentSettings.Radius,      // Radius
                    m_CurrentSettings.SampleCount  // Sample count
                );
                material.SetVector(s_SSAOParamsID, ssaoParams);

                // Update keywords
                CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
                {
                    switch (m_CurrentSettings.NormalSamples)
                    {
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low:
                            CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, true);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium:
                            CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, true);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.High:
                            CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Get temporary render textures
                m_Descriptor = cameraTargetDescriptor;
                m_Descriptor.msaaSamples = 1;
                m_Descriptor.depthBufferBits = 0;
                m_Descriptor.width = m_Descriptor.width / downsampleDivider;
                m_Descriptor.height = m_Descriptor.height / downsampleDivider;
                m_Descriptor.colorFormat = RenderTextureFormat.R8;

                cmd.GetTemporaryRT(s_SSAOTexture1ID, m_Descriptor, FilterMode.Point);
                cmd.GetTemporaryRT(s_SSAOTexture2ID, m_Descriptor, FilterMode.Point);

                // Update the offset increment for Kawase Blur
                m_offsetIncrement = new Vector4(
                    -1.0f / m_Descriptor.width,
                    -1.0f / m_Descriptor.height,
                    1.0f / m_Descriptor.width,
                    1.0f / m_Descriptor.height
                );

                // Configure targets and clear color
                ConfigureTarget(s_SSAOTexture1ID);
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

                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);

                    // Blit has logic to flip projection matrix when rendering to render texture.
                    // Currently the y-flip is handled in CopyDepthPass.hlsl by checking _ProjectionParams.x
                    // If you replace this Blit with a Draw* that sets projection matrix double check
                    // to also update shader.
                    // scaleBias.x = flipSign
                    // scaleBias.y = scale
                    // scaleBias.z = bias
                    // scaleBias.w = unused
                    float flipSign = (renderingData.cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                    Vector4 scaleBias = (flipSign < 0.0f) ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f) : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(s_ScaleBiasId, scaleBias);

                    // Execute the SSAO
                    ExecuteSSAO(cmd, (int) m_CurrentSettings.Source);

                    // Set the global SSAO texture and AO Params
                    int SSAOTexID = ExecuteKawaseBlur(cmd);
                    cmd.SetGlobalTexture(k_SSAOTextureName, SSAOTexID);
                    cmd.SetGlobalVector(k_SSAOAmbientOcclusionParamName, new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));
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

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
                cmd.ReleaseTemporaryRT(s_SSAOTexture1ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture2ID);
            }

            private void ExecuteSSAO(CommandBuffer cmd, int occlusionPass)
            {
                Render(cmd, m_SSAOTexture1Target, occlusionPass);
            }

            private int ExecuteKawaseBlur(CommandBuffer cmd)
            {
                RenderTargetIdentifier lastTarget = m_SSAOTexture1Target;
                RenderTargetIdentifier curTarget = m_SSAOTexture2Target;
                int lastTargetID = s_SSAOTexture1ID;
                int curTargetID = s_SSAOTexture2ID;
                Vector4 offset = 0.5f * m_offsetIncrement;
                int numOfPasses = m_CurrentSettings.BlurPasses;
                for (int i = 0; i < numOfPasses; i++)
                {
                    cmd.SetGlobalVector(s_BlurOffsetID, offset);
                    Render(cmd, lastTargetID, curTarget, k_KawaseBlurShaderPassID);
                    offset += m_offsetIncrement;

                    // Ping-Pong
                    CoreUtils.Swap(ref curTarget, ref lastTarget);
                    CoreUtils.Swap(ref curTargetID, ref lastTargetID);
                }

                return lastTargetID;
            }

            private void Render(CommandBuffer cmd, RenderTargetIdentifier target, int pass)
            {
                cmd.SetRenderTarget(
                    target,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    target,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.DontCare
                );
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
            }

            private void Render(CommandBuffer cmd, int baseMap, RenderTargetIdentifier target, int pass)
            {
                cmd.SetGlobalTexture(s_BaseMapID, baseMap);
                Render(cmd, target, pass);
            }
        }
    }
}
