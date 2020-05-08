using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;

public class ScreenSpaceAmbientOcclusionFeature : ScriptableRendererFeature
{
    // Public Variables
    public Settings settings = new Settings();

    // Private Variables
    private Material m_Material;
    private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

    // Constants
    private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";
    private const string k_OrthographicCameraKeyword    = "_ORTHOGRAPHIC";
    private const string k_NormalReconstructionLowKeyword    = "_RECONSTRUCT_NORMAL_LOW";
    private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
    private const string k_NormalReconstructionHighKeyword   = "_RECONSTRUCT_NORMAL_HIGH";

    // Enums
    //public enum DepthSource
    //{
    //    Depth,
    //    DepthNormals
    //}

    public enum QualityOptions
    {
        Low,
        Medium,
        High
    }

    // Classes
    [Serializable]
    public class Settings
    {
        public Shader Shader                = null;
        public bool UseVolumes              = false;
        //public DepthSource DepthSource    = DepthSource.Depth;
        public QualityOptions NormalQuality = QualityOptions.Medium;
        public bool Downsample              = true;
        public float Intensity              = 0.0f;
        public float Radius                 = 0.05f;
        public int SampleCount              = 4;
    }

    // Called from OnEnable and OnValidate...
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

        if (settings.Shader == null)
        {
            settings.Shader = Shader.Find(k_ShaderName);
            if (settings.Shader == null)
            {
                return false;
            }
        }

        m_Material = CoreUtils.CreateEngineMaterial(settings.Shader);
        m_SSAOPass.material = m_Material;
        return m_Material != null;
    }

    // The SSAO Pass
    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        // Public Variables
        public string profilerTag;
        public Material material;

        // Private Variables
        private Settings m_FeatureSettings;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSAO.Execute()");
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_OcclusionTexture1Target = new RenderTargetIdentifier(s_OcclusionTexture1ID, 0, CubemapFace.Unknown, -1);
        private RenderTargetIdentifier m_OcclusionTexture2Target = new RenderTargetIdentifier(s_OcclusionTexture2ID, 0, CubemapFace.Unknown, -1);
        private RenderTargetIdentifier m_OcclusionTexture3Target = new RenderTargetIdentifier(s_OcclusionTexture3ID, 0, CubemapFace.Unknown, -1);
        private RenderTargetIdentifier m_ScreenSpaceOcclusionTextureTarget = new RenderTargetIdentifier(s_ScreenSpaceOcclusionTextureID, 0, CubemapFace.Unknown, -1);

        // Constants
        private const string SSAO_TEXTURE_NAME = "_ScreenSpaceOcclusionTexture";
        private const RenderBufferLoadAction RBLA_DONT_CARE = RenderBufferLoadAction.DontCare;
        private const RenderBufferStoreAction RBSA_STORE = RenderBufferStoreAction.Store;
        private const RenderBufferStoreAction RBSA_DONT_CARE = RenderBufferStoreAction.DontCare;

        // Statics
        private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int s_ScaleBiasId = Shader.PropertyToID("_ScaleBiasRT");
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_OcclusionTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
        private static readonly int s_OcclusionTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");
        private static readonly int s_OcclusionTexture3ID = Shader.PropertyToID("_SSAO_OcclusionTexture3");
        private static readonly int s_ScreenSpaceOcclusionTextureID = Shader.PropertyToID(SSAO_TEXTURE_NAME);

        // Enums
        private enum ShaderPass
        {
            OcclusionDepth = 0,
            HorizontalBlurDepth = 1,
            VerticalBlurDepth = 2,
            //OcclusionDepthNormals = 3,
            //HorizontalBlurDepthNormals = 4,
            //VerticalBlurDepthNormals = 5,
            //OcclusionGbuffer = 6,
            //HorizontalBlurGBuffer = 7,
            //VerticalBlurGBuffer = 8,
            FinalComposition = 9,
            //FinalCompositionGBuffer = 10,
        }

        public ScreenSpaceAmbientOcclusionPass()
        {
            m_FeatureSettings = new Settings();
        }

        public bool Setup(Settings featureSettings)
        {
            // Override the settings if there are either global volumes or local volumes near the camera
            ScreenSpaceAmbientOcclusionVolume volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolume>();
            if (featureSettings.UseVolumes && volume != null)
            {
                //m_FeatureSettings.DepthSource = volume.DepthSource.value;
                m_FeatureSettings.NormalQuality = volume.NormalQuality.value;
                m_FeatureSettings.Downsample = volume.Downsample.value;
                m_FeatureSettings.Intensity = volume.Intensity.value;
                m_FeatureSettings.Radius = volume.Radius.value;
                m_FeatureSettings.SampleCount = volume.SampleCount.value;
            }
            else
            {
                //m_FeatureSettings.DepthSource = featureSettings.DepthSource;
                m_FeatureSettings.NormalQuality = featureSettings.NormalQuality;
                m_FeatureSettings.Downsample = featureSettings.Downsample;
                m_FeatureSettings.Intensity = featureSettings.Intensity;
                m_FeatureSettings.Radius = featureSettings.Radius;
                m_FeatureSettings.SampleCount = featureSettings.SampleCount;
            }

            return material != null
               &&  m_FeatureSettings.Intensity > 0.0f
               &&  m_FeatureSettings.Radius > 0.0f
               &&  m_FeatureSettings.SampleCount > 0;
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            int downsampleDivider = m_FeatureSettings.Downsample ? 2 : 1;

            // Update SSAO parameters in the material
            Vector4 ssaoParams = new Vector4(
                1.0f / downsampleDivider,      // Downsampling
                m_FeatureSettings.Intensity,   // Intensity
                m_FeatureSettings.Radius,      // Radius
                m_FeatureSettings.SampleCount  // Sample count
            );
            material.SetVector(s_SSAOParamsID, ssaoParams);

            // Update keywords
            CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

            //if (m_FeatureSettings.DepthSource == DepthSource.Depth)
            {
                switch (m_FeatureSettings.NormalQuality)
                {
                    case QualityOptions.Low:
                        CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, true);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, false);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                        break;
                    case QualityOptions.Medium:
                        CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, false);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, true);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                        break;
                    case QualityOptions.High:
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
            FilterMode filterMode = FilterMode.Point;
            RenderTextureFormat format = RenderTextureFormat.ARGB32;

            RenderTextureDescriptor occlusionTex1Descriptor = GetStereoCompatibleDescriptor(m_Descriptor.width / downsampleDivider, m_Descriptor.height / downsampleDivider, format);
            cmd.GetTemporaryRT(s_OcclusionTexture1ID, occlusionTex1Descriptor, filterMode);

            RenderTextureDescriptor occlusionTex2Descriptor = GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, format);
            cmd.GetTemporaryRT(s_OcclusionTexture2ID, occlusionTex2Descriptor, filterMode);

            RenderTextureDescriptor occlusionTex3Descriptor = GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, format);
            cmd.GetTemporaryRT(s_OcclusionTexture3ID, occlusionTex3Descriptor, filterMode);

            RenderTextureDescriptor resultsDescriptor = GetStereoCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, RenderTextureFormat.R8);
            cmd.GetTemporaryRT(s_ScreenSpaceOcclusionTextureID, resultsDescriptor, filterMode);

            // Configure targets and clear color
            ConfigureTarget(s_ScreenSpaceOcclusionTextureID);
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

                // This switch statement will be used once we've exposed render feature requirements.
                // switch (m_FeatureSettings.DepthSource)
                // {
                //     case DepthSource.Depth:
                        ExecuteSSAO(
                            cmd,
                            (int) ShaderPass.OcclusionDepth,
                            (int) ShaderPass.HorizontalBlurDepth,
                            (int) ShaderPass.VerticalBlurDepth,
                            (int) ShaderPass.FinalComposition
                        );
                //         break;
                //     case DepthSource.DepthNormals:
                //         ExecuteSSAO(
                //             cmd,
                //             (int) ShaderPass.OcclusionDepthNormals,
                //             (int) ShaderPass.HorizontalBlurDepthNormals,
                //             (int) ShaderPass.VerticalBlurDepthNormals,
                //             (int) ShaderPass.FinalComposition
                //         );
                //         break;
                // }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteSSAO(CommandBuffer cmd, int occlusionPass, int horizontalBlurPass, int verticalPass, int finalPass)
        {
            // Occlusion pass
            cmd.SetRenderTarget(m_OcclusionTexture1Target, RBLA_DONT_CARE, RBSA_STORE, m_OcclusionTexture1Target, RBLA_DONT_CARE, RBSA_DONT_CARE);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, occlusionPass);

            // Horizontal Blur
            cmd.SetGlobalTexture(s_BaseMapID, s_OcclusionTexture1ID);
            cmd.SetRenderTarget(m_OcclusionTexture2Target, RBLA_DONT_CARE, RBSA_STORE, m_OcclusionTexture2Target, RBLA_DONT_CARE, RBSA_DONT_CARE);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, horizontalBlurPass);

            // Vertical Blur
            cmd.SetGlobalTexture(s_BaseMapID, s_OcclusionTexture2ID);
            cmd.SetRenderTarget(m_OcclusionTexture3Target, RBLA_DONT_CARE, RBSA_STORE, m_OcclusionTexture3Target, RBLA_DONT_CARE, RBSA_DONT_CARE);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, verticalPass);

            // Final Composition
            cmd.SetGlobalTexture(s_BaseMapID, s_OcclusionTexture3ID);
            cmd.SetRenderTarget(m_ScreenSpaceOcclusionTextureTarget, RBLA_DONT_CARE, RBSA_STORE, m_ScreenSpaceOcclusionTextureTarget, RBLA_DONT_CARE, RBSA_DONT_CARE);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, finalPass);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceOcclusion, false);
            cmd.ReleaseTemporaryRT(s_ScreenSpaceOcclusionTextureID);
            cmd.ReleaseTemporaryRT(s_OcclusionTexture1ID);
            cmd.ReleaseTemporaryRT(s_OcclusionTexture2ID);
            cmd.ReleaseTemporaryRT(s_OcclusionTexture3ID);
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, RenderTextureFormat format, int depthBufferBits = 0)
        {
            RenderTextureDescriptor desc = m_Descriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = depthBufferBits;
            desc.width = width;
            desc.height = height;
            desc.colorFormat = format;
            return desc;
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            RenderTextureDescriptor desc = m_Descriptor;
            desc.msaaSamples = 1;
            desc.depthBufferBits = depthBufferBits;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}
