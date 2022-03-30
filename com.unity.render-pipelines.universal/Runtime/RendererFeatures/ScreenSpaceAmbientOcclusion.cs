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
        [SerializeField, HideInInspector] private Shader m_Shader = null;
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

        // Constants
        private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
        private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
        private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";
        private const string k_SourceGBufferKeyword = "_SOURCE_GBUFFER";

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

            bool shouldAdd = m_SSAOPass.Setup(m_Settings);
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

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_ShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
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
            private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;
            private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
            private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
            private Vector4[] m_CameraXExtent = new Vector4[2];
            private Vector4[] m_CameraYExtent = new Vector4[2];
            private Vector4[] m_CameraZExtent = new Vector4[2];
            private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.SSAO);
            private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);
            private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);
            private RenderTargetIdentifier m_SSAOTexture3Target = new RenderTargetIdentifier(s_SSAOTexture3ID, 0, CubemapFace.Unknown, -1);
            private RenderTextureDescriptor m_Descriptor;

            // Constants
            private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";
            private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";

            // Statics
            private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
            private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
            private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
            private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
            private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
            private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
            private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
            private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
            private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
            private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");
            private static readonly int s_SSAOTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");

            private enum ShaderPasses
            {
                AO = 0,
                BlurHorizontal = 1,
                BlurVertical = 2,
                BlurFinal = 3
            }

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
                    m_CurrentSettings.Intensity,   // Intensity
                    m_CurrentSettings.Radius,      // Radius
                    1.0f / downsampleDivider,      // Downsampling
                    m_CurrentSettings.SampleCount  // Sample count
                );
                material.SetVector(s_SSAOParamsID, ssaoParams);

#if ENABLE_VR && ENABLE_XR_MODULE
                int eyeCount = renderingData.cameraData.xr.enabled && renderingData.cameraData.xr.singlePassEnabled ? 2 : 1;
#else
                int eyeCount = 1;
#endif
                for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
                {
                    Matrix4x4 view = renderingData.cameraData.GetViewMatrix(eyeIndex);
                    Matrix4x4 proj = renderingData.cameraData.GetProjectionMatrix(eyeIndex);
                    m_CameraViewProjections[eyeIndex] = proj * view;

                    // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                    Matrix4x4 cview = view;
                    cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                    Matrix4x4 cviewProj = proj * cview;
                    Matrix4x4 cviewProjInv = cviewProj.inverse;

                    Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                    Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                    Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                    Vector4 farCentre = cviewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                    m_CameraTopLeftCorner[eyeIndex] = topLeftCorner;
                    m_CameraXExtent[eyeIndex] = topRightCorner - topLeftCorner;
                    m_CameraYExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                    m_CameraZExtent[eyeIndex] = farCentre;
                }

                material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
                material.SetMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
                material.SetVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
                material.SetVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
                material.SetVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
                material.SetVectorArray(s_CameraViewZExtentID, m_CameraZExtent);

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

                switch (m_CurrentSettings.Source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        CoreUtils.SetKeyword(material, k_SourceDepthKeyword, false);
                        CoreUtils.SetKeyword(material, k_SourceDepthNormalsKeyword, true);
                        CoreUtils.SetKeyword(material, k_SourceGBufferKeyword, false);
                        break;
                    default:
                        CoreUtils.SetKeyword(material, k_SourceDepthKeyword, true);
                        CoreUtils.SetKeyword(material, k_SourceDepthNormalsKeyword, false);
                        CoreUtils.SetKeyword(material, k_SourceGBufferKeyword, false);
                        break;
                }

                // Get temporary render textures
                m_Descriptor = cameraTargetDescriptor;
                m_Descriptor.msaaSamples = 1;
                m_Descriptor.depthBufferBits = 0;
                m_Descriptor.width /= downsampleDivider;
                m_Descriptor.height /= downsampleDivider;
                m_Descriptor.colorFormat = RenderTextureFormat.ARGB32;
                cmd.GetTemporaryRT(s_SSAOTexture1ID, m_Descriptor, FilterMode.Bilinear);

                m_Descriptor.width *= downsampleDivider;
                m_Descriptor.height *= downsampleDivider;
                cmd.GetTemporaryRT(s_SSAOTexture2ID, m_Descriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTexture3ID, m_Descriptor, FilterMode.Bilinear);

                // Configure targets and clear color
                ConfigureTarget(s_SSAOTexture2ID);
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

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
                    PostProcessUtils.SetSourceSize(cmd, m_Descriptor);

                    // Execute the SSAO
                    Render(cmd, m_SSAOTexture1Target, ShaderPasses.AO);

                    // Execute the Blur Passes
                    RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontal);
                    RenderAndSetBaseMap(cmd, m_SSAOTexture2Target, m_SSAOTexture3Target, ShaderPasses.BlurVertical);
                    RenderAndSetBaseMap(cmd, m_SSAOTexture3Target, m_SSAOTexture2Target, ShaderPasses.BlurFinal);

                    // Set the global SSAO texture and AO Params
                    cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTexture2Target);
                    cmd.SetGlobalVector(k_SSAOAmbientOcclusionParamName, new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            private void Render(CommandBuffer cmd, RenderTargetIdentifier target, ShaderPasses pass)
            {
                cmd.SetRenderTarget(
                    target,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    target,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.DontCare
                );
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, (int) pass);
            }

            private void RenderAndSetBaseMap(CommandBuffer cmd, RenderTargetIdentifier baseMap, RenderTargetIdentifier target, ShaderPasses pass)
            {
                cmd.SetGlobalTexture(s_BaseMapID, baseMap);
                Render(cmd, target, pass);
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
                cmd.ReleaseTemporaryRT(s_SSAOTexture3ID);
            }
        }
    }
}
