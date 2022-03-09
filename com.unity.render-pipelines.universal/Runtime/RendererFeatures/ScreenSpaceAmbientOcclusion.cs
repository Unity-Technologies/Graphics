using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceAmbientOcclusionSettings
    {
        // Parameters
        [SerializeField] internal AOAlgorithmOptions AOAlgorithm = AOAlgorithmOptions.Original;
        [SerializeField] internal bool Downsample = false;
        [SerializeField] internal bool AfterOpaque = false;
        [SerializeField] internal DepthSource Source = DepthSource.DepthNormals;
        [SerializeField] internal NormalQuality NormalSamples = NormalQuality.Medium;
        [SerializeField] internal float Intensity = 3.0f;
        [SerializeField] internal float DirectLightingStrength = 0.25f;
        [SerializeField] internal float Radius = 0.035f;
        [SerializeField] internal int SampleCount = -1;
        [SerializeField] internal AOSampleOption Samples = AOSampleOption._4;

        [SerializeField] internal BlurQualityOptions BlurQuality = BlurQualityOptions.High;
        [SerializeField] internal float Falloff = 100f;
        [SerializeField] internal float Scattering = 0.1f;
        [SerializeField] internal float TimeMultiplier = 100f;
        [SerializeField] internal Texture2D BlueNoiseTexture;

        // Enums
        internal enum DepthSource
        {
            Depth = 0,
            DepthNormals = 1
        }

        internal enum NormalQuality
        {
            Low,
            Medium,
            High
        }

        internal enum AOSampleOption
        {
            _4,
            _6,
            _8,
            _10,
            _12
        }

        internal enum AOAlgorithmOptions
        {
            Original,
            BlueNoise,
        }

        internal enum BlurQualityOptions
        {
            High,   // Bilateral + Bilinear upsampling
            Medium, // Gaussian + Bilinear upsampling
            Low,    // Kawase + Bilinear upsampling
        }
    }

    [DisallowMultipleRendererFeature("Screen Space Ambient Occlusion")]
    [Tooltip("The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other.")]
    [URPHelpURL("post-processing-ssao")]
    internal class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField, HideInInspector] private Shader m_Shader = null;
        [SerializeField, HideInInspector] private Shader m_BlitShader = null;
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private Material m_Material;
        private Material m_BlitMaterial;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

        // Constants
        private const string k_SSAOShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";
        private const string k_BlitShaderName = "Hidden/Universal Render Pipeline/Blit";
        private const string k_AOOriginalKeyword = "_ORIGINAL";
        private const string k_AOBlueNoiseKeyword = "_BLUE_NOISE";
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
        private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
        private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";
        private const string k_SampleCount4Keyword = "_SAMPLE_COUNT4";
        private const string k_SampleCount6Keyword = "_SAMPLE_COUNT6";
        private const string k_SampleCount8Keyword = "_SAMPLE_COUNT8";
        private const string k_SampleCount10Keyword = "_SAMPLE_COUNT10";
        private const string k_SampleCount12Keyword = "_SAMPLE_COUNT12";

        internal bool afterOpaque => m_Settings.AfterOpaque;

        /// <inheritdoc/>
        public override void Create()
        {
            // Create the pass...
            if (m_SSAOPass == null)
            {
                m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
            }

            if (m_Settings.SampleCount > 0)
            {
                if (m_Settings.SampleCount > 11)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption._12;
                else if (m_Settings.SampleCount > 8)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption._10;
                else if (m_Settings.SampleCount > 6)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption._8;
                else if (m_Settings.SampleCount > 4)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption._6;
                else
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption._4;
                m_Settings.SampleCount = -1;
            }

            GetMaterials();
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterials())
            {
                Debug.LogErrorFormat(
                    "{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.",
                    GetType().Name, name);
                return;
            }

            bool shouldAdd = m_SSAOPass.Setup(m_Settings, renderer, m_Material, m_BlitMaterial, m_Settings.BlueNoiseTexture);
            if (shouldAdd)
            {
                renderer.EnqueuePass(m_SSAOPass);
            }
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            m_SSAOPass?.Dispose();
            m_SSAOPass = null;
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_BlitMaterial);
        }

        private bool GetMaterials()
        {
            if (m_Material != null && m_BlitMaterial != null)
            {
                return true;
            }

            if (m_Shader == null)
            {
                m_Shader = Shader.Find(k_SSAOShaderName);
                if (m_Shader == null)
                {
                    return false;
                }
            }

            if (m_BlitShader == null)
            {
                m_BlitShader = Shader.Find(k_BlitShaderName);
                if (m_BlitShader == null)
                {
                    return false;
                }
            }

            m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            m_BlitMaterial = CoreUtils.CreateEngineMaterial(m_BlitShader);

            return m_Material != null && m_BlitMaterial != null;
        }

        // The SSAO Pass
        private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
        {
            // Properties
            private bool isRendererDeferred => m_Renderer != null && m_Renderer is UniversalRenderer && ((UniversalRenderer)m_Renderer).renderingModeRequested == RenderingMode.Deferred;

            // Internal Variables
            internal string profilerTag;

            // Private Variables
            private bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
            private Material m_Material;
            private Material m_BlitMaterial;
            private Texture2D m_BlueNoise;
            private Vector4[] m_CameraTopLeftCorner = new Vector4[2];
            private Vector4[] m_CameraXExtent = new Vector4[2];
            private Vector4[] m_CameraYExtent = new Vector4[2];
            private Vector4[] m_CameraZExtent = new Vector4[2];
            private Matrix4x4[] m_CameraViewProjections = new Matrix4x4[2];
            private ProfilingSampler m_ProfilingSampler = ProfilingSampler.Get(URPProfileId.SSAO);
            private ScriptableRenderer m_Renderer = null;
            private RTHandle m_SSAOTexture1;
            private RTHandle m_SSAOTexture2;
            private RTHandle m_SSAOTexture3;
            private RTHandle m_SSAOTexture4;
            private RTHandle m_SSAOTexture5;
            private RenderTextureDescriptor m_AOPassDescriptor;
            private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;
            private bool m_SinglePassBlur = false;
            private UpsampleTypes m_FinalUpsample = UpsampleTypes.Bilinear;
            private BlurTypes m_BlurType = BlurTypes.Bilateral;

            // Constants
            private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
            private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";

            // Statics
            private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
            private static readonly int s_BlueNoiseTextureID = Shader.PropertyToID("_BlueNoiseTexture");
            private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
            private static readonly int s_SSAOParams2ID = Shader.PropertyToID("_SSAOParams2");
            private static readonly int s_KawaseBlurIterationID = Shader.PropertyToID("_KawaseBlurIteration");
            private static readonly int s_LastKawasePass = Shader.PropertyToID("_LastKawasePass");
            private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
            private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");
            private static readonly int s_SSAOTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");
            private static readonly int s_SSAOTexture4ID = Shader.PropertyToID("_SSAO_OcclusionTexture4");
            private static readonly int s_SSAOTexture5ID = Shader.PropertyToID("_SSAO_OcclusionTexture5");
            private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
            private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
            private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
            private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
            private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
            private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");
            private static readonly int s_ScaleBiasRtID = Shader.PropertyToID("_ScaleBiasRt");

            // Enums
            private enum BlurTypes
            {
                Bilateral,
                Gaussian,
                Kawase,
                DualKawase,
                DualFiltering
            }

            private enum UpsampleTypes
            {
                Bilinear,
                BoxFilter
            }

            private enum ShaderPasses
            {
                AO = 0,
                BlurHorizontal = 1,
                BlurVertical = 2,
                BlurFinal = 3,
                BlurHorizontalVertical = 4,
                BlurHorizontalGaussian = 5,
                BlurVerticalGaussian = 6,
                BlurHorizontalVerticalGaussian = 7,
                Upsample = 8,
                KawaseBlur = 9,
                DualKawaseBlur = 10,
                DualFilteringDownsample = 11,
                DualFilteringUpsample = 12,
                AfterOpaque = 13,
            }

            internal ScreenSpaceAmbientOcclusionPass()
            {
                m_CurrentSettings = new ScreenSpaceAmbientOcclusionSettings();
                m_SSAOTexture1 = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture1");
                m_SSAOTexture2 = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture2");
                m_SSAOTexture3 = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTexture3ID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture3");
                m_SSAOTexture4 = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTexture4ID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture4");
                m_SSAOTexture5 = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTexture5ID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture5");
            }

            internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings, ScriptableRenderer renderer, Material material, Material blitMaterial, Texture2D blueNoiseTexture)
            {
                m_BlueNoise = blueNoiseTexture;
                m_Material = material;
                m_BlitMaterial = blitMaterial;
                m_Renderer = renderer;
                m_CurrentSettings = featureSettings;

                // RenderPass Event + Source Settings (Depth / Depth&Normals
                if (isRendererDeferred)
                {
                    renderPassEvent = m_CurrentSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingGbuffer;
                    m_CurrentSettings.Source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                }
                else
                {
                    // Rendering after PrePasses is usually correct except when depth priming is in play:
                    // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                    // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                    renderPassEvent = m_CurrentSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingPrePasses + 1;
                }

                // Ask for a Depth or Depth + Normals textures
                switch (m_CurrentSettings.Source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth:
                        ConfigureInput(ScriptableRenderPassInput.Depth);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        ConfigureInput(ScriptableRenderPassInput.Normal);// need depthNormal prepass for forward-only geometry
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Blur settings
                switch (m_CurrentSettings.BlurQuality)
                {
                    case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High:
                        m_BlurType = BlurTypes.Bilateral;
                        m_FinalUpsample = UpsampleTypes.Bilinear;
                        m_SinglePassBlur = false;
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium:
                        m_BlurType = BlurTypes.Gaussian;
                        m_FinalUpsample = UpsampleTypes.Bilinear;
                        m_SinglePassBlur = false;
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low:
                        m_BlurType = BlurTypes.Kawase;
                        m_FinalUpsample = UpsampleTypes.Bilinear;
                        m_SinglePassBlur = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }


                return m_Material != null/*
                    && m_CurrentSettings.Intensity > 0.0f
                    && m_CurrentSettings.Radius > 0.0f*/;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;

                // Update SSAO parameters in the material
                Vector4 ssaoParams = new Vector4(
                    m_CurrentSettings.Intensity,     // Intensity
                    m_CurrentSettings.Radius,        // Radius
                    1.0f / downsampleDivider,        // Downsampling
                    m_CurrentSettings.Falloff       // Falloff
                );
                Vector4 ssaoParams2 = new Vector4(
                    0.1f,                               // Min Scattering
                    0.1f + m_CurrentSettings.Scattering,// Max Scattering
                    m_CurrentSettings.TimeMultiplier,   // Unused
                    0.0f                                // Unused
                );

                m_Material.SetVector(s_SSAOParamsID, ssaoParams);
                m_Material.SetVector(s_SSAOParams2ID, ssaoParams2);

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

                m_Material.SetVector(s_ProjectionParams2ID, new Vector4(1.0f / renderingData.cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f));
                m_Material.SetMatrixArray(s_CameraViewProjectionsID, m_CameraViewProjections);
                m_Material.SetVectorArray(s_CameraViewTopLeftCornerID, m_CameraTopLeftCorner);
                m_Material.SetVectorArray(s_CameraViewXExtentID, m_CameraXExtent);
                m_Material.SetVectorArray(s_CameraViewYExtentID, m_CameraYExtent);
                m_Material.SetVectorArray(s_CameraViewZExtentID, m_CameraZExtent);
                m_Material.SetTexture(s_BlueNoiseTextureID, m_BlueNoise);

                // Update keywords
                CoreUtils.SetKeyword(m_Material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);
                CoreUtils.SetKeyword(m_Material, k_AOBlueNoiseKeyword, false);
                CoreUtils.SetKeyword(m_Material, k_AOOriginalKeyword, false);
                switch (m_CurrentSettings.AOAlgorithm)
                {
                    case ScreenSpaceAmbientOcclusionSettings.AOAlgorithmOptions.BlueNoise:
                        CoreUtils.SetKeyword(m_Material, k_AOBlueNoiseKeyword, true);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.AOAlgorithmOptions.Original:
                        CoreUtils.SetKeyword(m_Material, k_AOOriginalKeyword, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CoreUtils.SetKeyword(m_Material, k_SampleCount4Keyword, false);
                CoreUtils.SetKeyword(m_Material, k_SampleCount6Keyword, false);
                CoreUtils.SetKeyword(m_Material, k_SampleCount8Keyword, false);
                CoreUtils.SetKeyword(m_Material, k_SampleCount10Keyword, false);
                CoreUtils.SetKeyword(m_Material, k_SampleCount12Keyword, false);
                switch (m_CurrentSettings.Samples)
                {
                    case ScreenSpaceAmbientOcclusionSettings.AOSampleOption._12:
                        CoreUtils.SetKeyword(m_Material, k_SampleCount12Keyword, true);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.AOSampleOption._10:
                        CoreUtils.SetKeyword(m_Material, k_SampleCount10Keyword, true);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.AOSampleOption._8:
                        CoreUtils.SetKeyword(m_Material, k_SampleCount8Keyword, true);
                        break;
                    case ScreenSpaceAmbientOcclusionSettings.AOSampleOption._6:
                        CoreUtils.SetKeyword(m_Material, k_SampleCount6Keyword, true);
                        break;
                    default:
                        CoreUtils.SetKeyword(m_Material, k_SampleCount4Keyword, true);
                        break;
                }
                CoreUtils.SetKeyword(m_Material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                if (m_CurrentSettings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
                {
                    switch (m_CurrentSettings.NormalSamples)
                    {
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, true);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, true);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, false);
                            break;
                        case ScreenSpaceAmbientOcclusionSettings.NormalQuality.High:
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionLowKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionMediumKeyword, false);
                            CoreUtils.SetKeyword(m_Material, k_NormalReconstructionHighKeyword, true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Set the source keywords...
                switch (m_CurrentSettings.Source)
                {
                    case ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals:
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthKeyword, false);
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthNormalsKeyword, true);
                        break;
                    default:
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthKeyword, true);
                        CoreUtils.SetKeyword(m_Material, k_SourceDepthNormalsKeyword, false);
                        break;
                }

                // Set up the descriptors
                RenderTextureDescriptor descriptor = cameraTargetDescriptor;
                descriptor.msaaSamples = 1;
                descriptor.depthBufferBits = 0;

                // AO PAss
                m_AOPassDescriptor = descriptor;
                m_AOPassDescriptor.width /= downsampleDivider;
                m_AOPassDescriptor.height /= downsampleDivider;
                bool useRedComponentOnly = m_SupportsR8RenderTextureFormat && m_BlurType > BlurTypes.Bilateral;
                m_AOPassDescriptor.colorFormat = useRedComponentOnly ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

                // Get temporary render textures
                cmd.GetTemporaryRT(s_SSAOTexture1ID, m_AOPassDescriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTexture2ID, m_AOPassDescriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTexture3ID, m_AOPassDescriptor, FilterMode.Bilinear);

                // Configure targets and clear color
                ConfigureTarget(m_CurrentSettings.AfterOpaque ? m_Renderer.cameraColorTargetHandle : m_SSAOTexture2);
                ConfigureClear(ClearFlag.None, Color.white);

                // Upsample setup
                m_AOPassDescriptor.width *= downsampleDivider;
                m_AOPassDescriptor.height *= downsampleDivider;
                m_AOPassDescriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

                cmd.GetTemporaryRT(s_SSAOTexture4ID, m_AOPassDescriptor, FilterMode.Bilinear);

                // Configure targets and clear color
                ConfigureTarget(m_SSAOTexture4);
                ConfigureClear(ClearFlag.None, Color.white);

                if (m_BlurType == BlurTypes.DualFiltering)
                {
                    m_AOPassDescriptor.width = cameraTargetDescriptor.width;
                    m_AOPassDescriptor.height = cameraTargetDescriptor.height;
                    m_AOPassDescriptor.width /= downsampleDivider * 2;
                    m_AOPassDescriptor.height /= downsampleDivider * 2;

                    cmd.GetTemporaryRT(s_SSAOTexture5ID, m_AOPassDescriptor, FilterMode.Bilinear);

                    // Configure targets and clear color
                    ConfigureTarget(m_SSAOTexture5);
                    ConfigureClear(ClearFlag.None, Color.white);
                }
            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (m_Material == null || m_BlitMaterial == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }

                var cmd = renderingData.commandBuffer;
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    if (!m_CurrentSettings.AfterOpaque)
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);

                    PostProcessUtils.SetSourceSize(cmd, m_AOPassDescriptor);

                    Vector4 scaleBiasRt = new Vector4(-1, 1.0f, -1.0f, 1.0f);
                    cmd.SetGlobalVector(s_ScaleBiasRtID, scaleBiasRt);

                    // Execute the SSAO
                    Render(cmd, m_SSAOTexture1, ShaderPasses.AO);

                    // Execute the Blur Passes
                    if (m_BlurType == BlurTypes.DualFiltering)
                    {
                        RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture5, ShaderPasses.DualFilteringDownsample);
                        RenderAndSetBaseMap(cmd, m_SSAOTexture5, m_SSAOTexture2, ShaderPasses.DualFilteringUpsample);
                    }
                    else if (m_BlurType == BlurTypes.Kawase || m_BlurType == BlurTypes.DualKawase)
                    {
                        ShaderPasses shaderPass = (m_BlurType == BlurTypes.Kawase) ? ShaderPasses.KawaseBlur : ShaderPasses.DualKawaseBlur;
                        if (m_SinglePassBlur)
                        {
                            cmd.SetGlobalInt(s_LastKawasePass, 1);
                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 0);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture2, shaderPass);
                        }
                        else
                        {
                            //kernels (iterations): { 0, 1, 1, 2, 3 };
                            cmd.SetGlobalInt(s_LastKawasePass, 0);
                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 0);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture3, shaderPass);

                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 1);
                            cmd.SetGlobalInt(s_LastKawasePass, 1);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture3, m_SSAOTexture2, shaderPass);
                        }
                    }
                    else if (m_BlurType == BlurTypes.Gaussian)
                    {
                        if (m_SinglePassBlur)
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture2, ShaderPasses.BlurHorizontalVerticalGaussian);
                        else
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture3, ShaderPasses.BlurHorizontalGaussian);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture3, m_SSAOTexture2, ShaderPasses.BlurVerticalGaussian);
                        }
                    }
                    else if (m_BlurType == BlurTypes.Bilateral)
                    {
                        if (m_SinglePassBlur)
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture2, ShaderPasses.BlurHorizontalVertical);
                        else
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture2, ShaderPasses.BlurHorizontal);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture2, m_SSAOTexture3, ShaderPasses.BlurVertical);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture3, m_SSAOTexture2, ShaderPasses.BlurFinal);
                        }
                    }

                    // if we are downsampling, do an extra upsample pass
                    if (m_CurrentSettings.Downsample)
                    {
                        if (m_FinalUpsample == UpsampleTypes.BoxFilter)
                            RenderAndSetBaseMap(cmd, m_SSAOTexture2, m_SSAOTexture4, ShaderPasses.Upsample);
                        else
                            RenderingUtils.Blit(cmd, m_SSAOTexture2, m_SSAOTexture4, m_BlitMaterial);

                        // Set the global SSAO texture
                        cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTexture4);
                    }
                    else
                    {
                        // Set the global SSAO texture and AO Params
                        cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTexture2);
                    }

                    // Set the global SSAO texture and AO Params
                    //cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTexture4);
                    cmd.SetGlobalVector(k_SSAOAmbientOcclusionParamName, new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));

                    // If true, SSAO pass is inserted after opaque pass and is expected to modulate lighting result now.
                    if (m_CurrentSettings.AfterOpaque)
                    {
                        // SetRenderTarget has logic to flip projection matrix when rendering to render texture. Flip the uv to account for that case.
                        CameraData cameraData = renderingData.cameraData;
                        bool isCameraColorFinalTarget = (cameraData.cameraType == CameraType.Game && m_Renderer.cameraColorTargetHandle.nameID == BuiltinRenderTextureType.CameraTarget && cameraData.camera.targetTexture == null);
                        bool yflip = !isCameraColorFinalTarget;
                        float flipSign = yflip ? -1.0f : 1.0f;
                        scaleBiasRt = (flipSign < 0.0f)
                            ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                            : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                        cmd.SetGlobalVector(s_ScaleBiasRtID, scaleBiasRt);

                        CoreUtils.SetRenderTarget(
                            cmd,
                            m_Renderer.cameraColorTargetHandle,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            m_Renderer.cameraDepthTargetHandle,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            ClearFlag.None,
                            Color.clear
                        );
                        cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, (int)ShaderPasses.AfterOpaque);
                    }
                }
            }

            private void Render(CommandBuffer cmd, RTHandle target, ShaderPasses pass)
            {
                CoreUtils.SetRenderTarget(
                    cmd,
                    target,
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.clear
                );
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, (int)pass);
            }

            private void RenderAndSetBaseMap(CommandBuffer cmd, RTHandle baseMap, RTHandle target, ShaderPasses pass)
            {
                cmd.SetGlobalTexture(s_BaseMapID, baseMap.nameID);
                Render(cmd, target, pass);
            }

            /// <inheritdoc/>
            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                if (cmd == null)
                    throw new ArgumentNullException("cmd");

                if (!m_CurrentSettings.AfterOpaque)
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);

                cmd.ReleaseTemporaryRT(s_SSAOTexture1ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture2ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture3ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture4ID);

                if (m_BlurType == BlurTypes.DualFiltering)
                    cmd.ReleaseTemporaryRT(s_SSAOTexture5ID);
            }

            public void Dispose()
            {
                m_SSAOTexture1.Release();
                m_SSAOTexture2.Release();
                m_SSAOTexture3.Release();
                m_SSAOTexture4.Release();
                m_SSAOTexture5.Release();
            }
        }
    }
}
