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
    public enum DepthSource
    {
        Depth,
        DepthNormals
    }

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
        public int BlurPassesCount          = 3;
        public float BlurOffset             = 0.5f;
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
        private Vector4 offsetIncrement = Vector4.zero;
        private Settings m_FeatureSettings;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSAO.Execute()");
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);
        private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);

        // Constants
        private const string SSAO_TEXTURE_NAME = "_ScreenSpaceOcclusionTexture";
        private const RenderBufferLoadAction RBLA_DONT_CARE = RenderBufferLoadAction.DontCare;
        private const RenderBufferStoreAction RBSA_STORE = RenderBufferStoreAction.Store;
        private const RenderBufferStoreAction RBSA_DONT_CARE = RenderBufferStoreAction.DontCare;

        // Statics
        private static readonly int s_BlurOffsetID = Shader.PropertyToID("_BlurOffset");
        private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int s_ScaleBiasId = Shader.PropertyToID("_ScaleBiasRT");
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_SSAOTexture1ID = Shader.PropertyToID("_SSAO_OcclusionTexture1");
        private static readonly int s_SSAOTexture2ID = Shader.PropertyToID("_SSAO_OcclusionTexture2");

        // Enums
        private enum ShaderPass
        {
            OcclusionDepth = 0,
            OcclusionDepthNormals = 1,
            OcclusionGbuffer = 2,
            KawaseBlur = 3,
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
                m_FeatureSettings.BlurPassesCount = volume.BlurPassesCount.value;
            }
            else
            {
                //m_FeatureSettings.DepthSource = featureSettings.DepthSource;
                m_FeatureSettings.NormalQuality = featureSettings.NormalQuality;
                m_FeatureSettings.Downsample = featureSettings.Downsample;
                m_FeatureSettings.Intensity = featureSettings.Intensity;
                m_FeatureSettings.Radius = featureSettings.Radius;
                m_FeatureSettings.SampleCount = featureSettings.SampleCount;
                m_FeatureSettings.BlurPassesCount = featureSettings.BlurPassesCount;
                m_FeatureSettings.BlurOffset = featureSettings.BlurOffset;
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
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / downsampleDivider;
            m_Descriptor.height = m_Descriptor.height / downsampleDivider;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;

            cmd.GetTemporaryRT(s_SSAOTexture1ID, m_Descriptor, FilterMode.Point);
            cmd.GetTemporaryRT(s_SSAOTexture2ID, m_Descriptor, FilterMode.Point);

            // Update the offset increment for Kawase Blur
            offsetIncrement = new Vector4(
                1.0f / m_Descriptor.width,
                1.0f / m_Descriptor.height,
                -1.0f / m_Descriptor.width,
                -1.0f / m_Descriptor.height
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

                // This switch statement will be used once we've exposed render feature requirements.
                // switch (m_FeatureSettings.DepthSource)
                // {
                //     case DepthSource.Depth:
                        ExecuteSSAO(cmd, (int) ShaderPass.OcclusionDepth);
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

                int SSAOTexID = ExecuteKawaseBlur(cmd);
                cmd.SetGlobalTexture(SSAO_TEXTURE_NAME, SSAOTexID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteSSAO(CommandBuffer cmd, int occlusionPass)
        {
            Render(cmd, m_SSAOTexture1Target, occlusionPass);
        }

        private int ExecuteKawaseBlur(CommandBuffer cmd)
        {
            int numOfPasses = m_FeatureSettings.BlurPassesCount;
            if (numOfPasses == 0)
            {
                return s_SSAOTexture1ID;
            }

            int kawasePassID = (int) ShaderPass.KawaseBlur;
            Vector4 offset = 1.5f * offsetIncrement;

            RenderTargetIdentifier lastTarget = m_SSAOTexture1Target;
            RenderTargetIdentifier curTarget = m_SSAOTexture2Target;
            int lastTargetID = s_SSAOTexture1ID;
            int curTargetID = s_SSAOTexture2ID;
            for (int i = 0; i < m_FeatureSettings.BlurPassesCount; i++)
            {
                cmd.SetGlobalFloat("_offset", (0.5f + i));
                cmd.SetGlobalVector(s_BlurOffsetID, offset);
                Render(cmd, lastTargetID, curTarget, kawasePassID);
                offset += offsetIncrement;

                // Ping-Pong
                CoreUtils.Swap(ref curTarget, ref lastTarget);
                CoreUtils.Swap(ref curTargetID, ref lastTargetID);
            }

            return lastTargetID;
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

        private void Render(CommandBuffer cmd, RenderTargetIdentifier target, int pass)
        {
            cmd.SetRenderTarget(target, RBLA_DONT_CARE, RBSA_STORE, target, RBLA_DONT_CARE, RBSA_DONT_CARE);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        }

        private void Render(CommandBuffer cmd, int baseMap, RenderTargetIdentifier target, int pass)
        {
            cmd.SetGlobalTexture(s_BaseMapID, baseMap);
            Render(cmd, target, pass);
        }
    }
}
