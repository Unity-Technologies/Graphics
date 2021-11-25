using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceAmbientOcclusionSettings
    {
        // Parameters
        [SerializeField] internal bool Downsample = false;
        [SerializeField] internal bool AfterOpaque = false;
        [SerializeField] internal DepthSource Source = DepthSource.DepthNormals;
        [SerializeField] internal NormalQuality NormalSamples = NormalQuality.Medium;
        [SerializeField] internal float Intensity = 3.0f;
        [SerializeField] internal float DirectLightingStrength = 0.25f;
        [SerializeField] internal float Radius = 0.035f;
        [SerializeField] internal int SampleCount = 4;

        [SerializeField] internal bool SinglePassBlur = false;
        [SerializeField] internal UpsampleTypes FinalUpsample = UpsampleTypes.None;
        [SerializeField] internal BlurTypes BlurType = BlurTypes.Bilateral;

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

        internal enum BlurTypes
        {
            Bilateral,
            Gaussian,
            Kawase,
            DualKawase,
            DualFiltering
        }

        internal enum UpsampleTypes
        {
            None,
            Bilinear,
            BoxFilter
        }
    }

    [DisallowMultipleRendererFeature]
    [Tooltip("The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other.")]
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
        private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
        private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
        private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";
        private const string k_SourceDepthKeyword = "_SOURCE_DEPTH";
        private const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";

        internal bool afterOpaque => m_Settings.AfterOpaque;

        /// <inheritdoc/>
        public override void Create()
        {
            // Create the pass...
            if (m_SSAOPass == null)
            {
                m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
            }

            GetMaterial();
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

            bool shouldAdd = m_SSAOPass.Setup(m_Settings, renderer, m_Material);
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
            m_SSAOPass.material = m_Material;
            m_SSAOPass.blitMaterial = m_BlitMaterial;

            return m_Material != null && m_BlitMaterial != null;
        }

        // The SSAO Pass
        private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
        {
            // Properties
            private bool isRendererDeferred => m_Renderer != null && m_Renderer is UniversalRenderer && ((UniversalRenderer)m_Renderer).renderingMode == RenderingMode.Deferred;
            // Public Variables
            internal string profilerTag;
            internal Material material;
            internal Material blitMaterial;

            internal bool blurFinalUpsample = false;

            // Private Variables
            private bool m_SupportsR8RenderTextureFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8);
            private Material m_Material;
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
            private RTHandle m_SSAOTextureFinal;
            private RenderTextureDescriptor m_AOPassDescriptor;
            private RenderTextureDescriptor m_BlurPassesDescriptor;
            private RenderTextureDescriptor m_FinalDescriptor;
            private ScreenSpaceAmbientOcclusionSettings m_CurrentSettings;

            // Constants
            private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
            private const string k_SSAOAmbientOcclusionParamName = "_AmbientOcclusionParam";

            // Statics
            private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
            private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
            private static readonly int s_KawaseBlurIterationID = Shader.PropertyToID("_KawaseBlurIteration");
            private static readonly int s_LastKawasePass = Shader.PropertyToID("_LastKawasePass");
            private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
            private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");
            private static readonly int s_SSAOTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");
            private static readonly int s_SSAOTextureFinalID = Shader.PropertyToID("_SSAO_OcclusionTexture");
            private static readonly int s_CameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent");
            private static readonly int s_CameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent");
            private static readonly int s_CameraViewZExtentID = Shader.PropertyToID("_CameraViewZExtent");
            private static readonly int s_ProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2");
            private static readonly int s_CameraViewProjectionsID = Shader.PropertyToID("_CameraViewProjections");
            private static readonly int s_CameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner");

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
                m_SSAOTextureFinal = RTHandles.Alloc(new RenderTargetIdentifier(s_SSAOTextureFinalID, 0, CubemapFace.Unknown, -1), "_SSAO_OcclusionTexture");
            }

            public void Dispose()
            {
                m_SSAOTexture1.Release();
                m_SSAOTexture2.Release();
                m_SSAOTexture3.Release();
                m_SSAOTextureFinal.Release();
            }

            internal bool Setup(ScreenSpaceAmbientOcclusionSettings featureSettings, ScriptableRenderer renderer, Material material)
            {
                m_Material = material;
                m_Renderer = renderer;
                m_CurrentSettings = featureSettings;

                ScreenSpaceAmbientOcclusionSettings.DepthSource source;
                if (isRendererDeferred)
                {
                    renderPassEvent = featureSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingGbuffer;
                    source = ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                }
                else
                {
                    // Rendering after PrePasses is usually correct except when depth priming is in play:
                    // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSAO.
                    // Hence we set the event to RenderPassEvent.AfterRenderingPrePasses + 1 at the earliest.
                    renderPassEvent = featureSettings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingPrePasses + 1;
                    source = m_CurrentSettings.Source;
                }


                switch (source)
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
                return m_Material != null
                    && m_CurrentSettings.Intensity > 0.0f
                    && m_CurrentSettings.Radius > 0.0f
                    && m_CurrentSettings.SampleCount > 0;
            }

            /// <inheritdoc/>
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
            {
                RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                int downsampleDivider = m_CurrentSettings.Downsample ? 2 : 1;
                blurFinalUpsample = m_CurrentSettings.Downsample && (m_CurrentSettings.FinalUpsample != ScreenSpaceAmbientOcclusionSettings.UpsampleTypes.None);

                // Update SSAO parameters in the material
                Vector4 ssaoParams = new Vector4(
                    m_CurrentSettings.Intensity,   // Intensity
                    m_CurrentSettings.Radius,      // Radius
                    1.0f / downsampleDivider,      // Downsampling
                    m_CurrentSettings.SampleCount  // Sample count
                );
                m_Material.SetVector(s_SSAOParamsID, ssaoParams);

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

                // Update keywords
                CoreUtils.SetKeyword(m_Material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

                ScreenSpaceAmbientOcclusionSettings.DepthSource source = this.isRendererDeferred
                    ? ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals
                    : m_CurrentSettings.Source;

                if (source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth)
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

                switch (source)
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

                m_AOPassDescriptor = descriptor;
                m_AOPassDescriptor.width /= downsampleDivider;
                m_AOPassDescriptor.height /= downsampleDivider;
                m_AOPassDescriptor.colorFormat = RenderTextureFormat.ARGB32;

                m_BlurPassesDescriptor = descriptor;
                m_BlurPassesDescriptor.colorFormat = RenderTextureFormat.ARGB32;

                m_FinalDescriptor = descriptor;
                m_FinalDescriptor.colorFormat = m_SupportsR8RenderTextureFormat ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

                // Get temporary render textures
                cmd.GetTemporaryRT(s_SSAOTexture1ID, m_AOPassDescriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTexture2ID, m_BlurPassesDescriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTexture3ID, m_BlurPassesDescriptor, FilterMode.Bilinear);
                cmd.GetTemporaryRT(s_SSAOTextureFinalID, m_FinalDescriptor, FilterMode.Bilinear);

                // Configure targets and clear color
                ConfigureTarget(m_CurrentSettings.AfterOpaque ? m_Renderer.cameraColorTargetHandle : m_SSAOTexture2);
                ConfigureClear(ClearFlag.None, Color.white);

                if (blurFinalUpsample)
                {
                    m_Descriptor.width *= downsampleDivider;
                    m_Descriptor.height *= downsampleDivider;
                    m_Descriptor.colorFormat = RenderTextureFormat.R8;

                    cmd.GetTemporaryRT(s_SSAOTexture4ID, m_Descriptor, FilterMode.Bilinear);

                    // Configure targets and clear color
                    ConfigureTarget(s_SSAOTexture4ID);
                    ConfigureClear(ClearFlag.None, Color.white);
                }

                if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.DualFiltering)
                {
                    m_Descriptor.width = cameraTargetDescriptor.width;
                    m_Descriptor.height = cameraTargetDescriptor.height;
                    m_Descriptor.width /= downsampleDivider * 2;
                    m_Descriptor.height /= downsampleDivider * 2;

                    cmd.GetTemporaryRT(s_SSAOTexture5ID, m_Descriptor, FilterMode.Bilinear);

                    // Configure targets and clear color
                    ConfigureTarget(s_SSAOTexture5ID);
                    ConfigureClear(ClearFlag.None, Color.white);
                }

            }

            /// <inheritdoc/>
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || blitMaterial == null)
                {
                    Debug.LogErrorFormat("{0}.Execute(): Missing material. ScreenSpaceAmbientOcclusion pass will not execute. Check for missing reference in the renderer resources.", GetType().Name);
                    return;
                }

                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    if (!m_CurrentSettings.AfterOpaque)
                    {
                        CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, true);
                    }
                    PostProcessUtils.SetSourceSize(cmd, m_AOPassDescriptor);

                    // Execute the SSAO
                    Render(cmd, m_SSAOTexture1, ShaderPasses.AO);

                    if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.DualFiltering)
                    {
                        RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture5Target, ShaderPasses.DualFilteringDownsample);
                        RenderAndSetBaseMap(cmd, m_SSAOTexture5Target, m_SSAOTexture2Target, ShaderPasses.DualFilteringUpsample);
                    }
                    else if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.Kawase || m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.DualKawase)
                    {
                        ShaderPasses shaderPass = (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.Kawase) ? ShaderPasses.KawaseBlur : ShaderPasses.DualKawaseBlur;
                        if (m_CurrentSettings.SinglePassBlur)
                        {
                            cmd.SetGlobalInt(s_LastKawasePass, 1);
                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 0);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture2Target, shaderPass);
                        }
                        else
                        {
                            //kernels (iterations): { 0, 1, 1, 2, 3 };

                            cmd.SetGlobalInt(s_LastKawasePass, 0);
                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 0);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture3Target, shaderPass);

                            cmd.SetGlobalFloat(s_KawaseBlurIterationID, 1);
                            cmd.SetGlobalInt(s_LastKawasePass, 1);

                            RenderAndSetBaseMap(cmd, m_SSAOTexture3Target, m_SSAOTexture2Target, shaderPass);
                        }
                    }
                    else if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.Gaussian)
                    {
                        if (m_CurrentSettings.SinglePassBlur)
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontalVerticalGaussian);
                        }
                        else
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture3Target, ShaderPasses.BlurHorizontalGaussian);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture3Target, m_SSAOTexture2Target, ShaderPasses.BlurVerticalGaussian);
                        }
                    }
                    else if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.Bilateral)
                    {
                        if (m_CurrentSettings.SinglePassBlur)
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontalVertical);
                        }
                        else
                        {
                            RenderAndSetBaseMap(cmd, m_SSAOTexture1Target, m_SSAOTexture2Target, ShaderPasses.BlurHorizontal);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture2Target, m_SSAOTexture3Target, ShaderPasses.BlurVertical);
                            RenderAndSetBaseMap(cmd, m_SSAOTexture3Target, m_SSAOTexture2Target, ShaderPasses.BlurFinal);
                        }
                    }

                    // Execute the Blur Passes


                    // if we are downsampling, do an extra upsample pass
                    if (blurFinalUpsample)
                    {
                        if (m_CurrentSettings.FinalUpsample == ScreenSpaceAmbientOcclusionSettings.UpsampleTypes.BoxFilter)
                            RenderAndSetBaseMap(cmd, m_SSAOTexture2Target, m_SSAOTexture4Target, ShaderPasses.Upsample);
                        else if (m_CurrentSettings.FinalUpsample == ScreenSpaceAmbientOcclusionSettings.UpsampleTypes.Bilinear)
                            RenderingUtils.Blit(cmd, m_SSAOTexture2Target, m_SSAOTexture4Target, blitMaterial);
                    }


                    // Set the global SSAO texture and AO Params
                    cmd.SetGlobalTexture(k_SSAOTextureName, blurFinalUpsample ? m_SSAOTexture4Target : m_SSAOTexture2Target);
                    RenderAndSetBaseMap(cmd, m_SSAOTexture1, m_SSAOTexture2, ShaderPasses.BlurHorizontal);

                    PostProcessUtils.SetSourceSize(cmd, m_BlurPassesDescriptor);
                    RenderAndSetBaseMap(cmd, m_SSAOTexture2, m_SSAOTexture3, ShaderPasses.BlurVertical);
                    RenderAndSetBaseMap(cmd, m_SSAOTexture3, m_SSAOTextureFinal, ShaderPasses.BlurFinal);

                    // Set the global SSAO texture and AO Params
                    cmd.SetGlobalTexture(k_SSAOTextureName, m_SSAOTextureFinal);
                    cmd.SetGlobalVector(k_SSAOAmbientOcclusionParamName, new Vector4(0f, 0f, 0f, m_CurrentSettings.DirectLightingStrength));

                    // If true, SSAO pass is inserted after opaque pass and is expected to modulate lighting result now.
                    if (m_CurrentSettings.AfterOpaque)
                    {
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

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
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
                {
                    throw new ArgumentNullException("cmd");
                }

                if (!m_CurrentSettings.AfterOpaque)
                {
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
                }

                cmd.ReleaseTemporaryRT(s_SSAOTexture1ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture2ID);
                cmd.ReleaseTemporaryRT(s_SSAOTexture3ID);
                cmd.ReleaseTemporaryRT(s_SSAOTextureFinalID);

                if (blurFinalUpsample)
                    cmd.ReleaseTemporaryRT(s_SSAOTexture4ID);

                if (m_CurrentSettings.BlurType == ScreenSpaceAmbientOcclusionSettings.BlurTypes.DualFiltering)
                    cmd.ReleaseTemporaryRT(s_SSAOTexture5ID);
            }
        }
    }
}
