using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
{
    // Serialized Fields
    [SerializeField] private Settings settings = new Settings();

    // Private Fields
    private Material m_Material;
    private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

    // Constants
    internal const Quality k_QualityDefault = Quality.Medium;
    internal const bool k_DownsampleDefault = true;
    internal const NormalSamples k_NormalSamplesDefault = NormalSamples.Five;
    internal const int k_SampleCountDefault = 6;
    internal const int k_SampleCountMin = 4;
    internal const int k_SampleCountMax = 12;
    internal const int k_BlurPassesDefault = 3;
    internal const int k_BlurPassesMin = 2;
    internal const int k_BlurPassesMax = 12;
    internal const float k_IntensityDefault = 0.0f;
    internal const float k_IntensityMin = 0.0f;
    internal const float k_IntensityMax = 10.0f;
    internal const float k_RadiusDefault = 0.1f;
    internal const float k_RadiusMin = 0.0f;
    internal const float k_RadiusMax = 10.0f;
    private const string k_ShaderName = "Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion";
    private const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
    private const string k_NormalReconstructionLowKeyword = "_RECONSTRUCT_NORMAL_LOW";
    private const string k_NormalReconstructionMediumKeyword = "_RECONSTRUCT_NORMAL_MEDIUM";
    private const string k_NormalReconstructionHighKeyword = "_RECONSTRUCT_NORMAL_HIGH";

    // Enums
    internal enum DepthSource
    {
        Depth,
        DepthNormals
    }

    internal enum Quality
    {
        Low,
        Medium,
        High,
        Custom
    }

    internal enum NormalSamples
    {
        One,
        Five,
        Nine
    }

    // Classes
    [Serializable]
    internal class Settings
    {
        public Shader Shader = null;
        public Quality Quality = k_QualityDefault;
        public Parameters Params = new Parameters(Quality.Medium);
    }

    [Serializable]
    internal struct Parameters
    {
        public bool Downsample;
        public NormalSamples NormalSamples;
        public float Intensity;
        public float Radius;
        public int SampleCount;
        public int BlurPasses;

        public Parameters(Parameters copyFrom)
        {
            Downsample = copyFrom.Downsample;
            NormalSamples = copyFrom.NormalSamples;
            Intensity = copyFrom.Intensity;
            Radius = copyFrom.Radius;
            SampleCount = copyFrom.SampleCount;
            BlurPasses = copyFrom.BlurPasses;
        }

        public Parameters(Quality quality)
        {
            switch (quality)
            {
                case Quality.Low:
                    Downsample = true;
                    NormalSamples = NormalSamples.One;
                    Intensity = 1;
                    Radius = 0.1f;
                    SampleCount = 4;
                    BlurPasses = 3;
                    break;
                case Quality.High:
                    Downsample = false;
                    NormalSamples = NormalSamples.Nine;
                    Intensity = 1;
                    Radius = 0.1f;
                    SampleCount = 10;
                    BlurPasses = 3;
                    break;

                // Medium, Custom, Default
                default:
                    Downsample = true;
                    NormalSamples = NormalSamples.Five;
                    Intensity = 1;
                    Radius = 0.1f;
                    SampleCount = 7;
                    BlurPasses = 3;
                    break;
            }
        }
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
        private Vector4 m_offsetIncrement = Vector4.zero;
        private Settings m_FeatureSettings;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSAO.Execute()");
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_SSAOTexture1Target = new RenderTargetIdentifier(s_SSAOTexture1ID, 0, CubemapFace.Unknown, -1);
        private RenderTargetIdentifier m_SSAOTexture2Target = new RenderTargetIdentifier(s_SSAOTexture2ID, 0, CubemapFace.Unknown, -1);

        // Constants
        private const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";
        private const RenderBufferLoadAction k_RBLADontCare = RenderBufferLoadAction.DontCare;
        private const RenderBufferStoreAction k_RBLAStore = RenderBufferStoreAction.Store;
        private const RenderBufferStoreAction k_RBSADontCare = RenderBufferStoreAction.DontCare;

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

        internal ScreenSpaceAmbientOcclusionPass()
        {
            m_FeatureSettings = new Settings();
        }

        internal bool Setup(Settings featureSettings)
        {
            // Override the settings if there are either global volumes or local volumes near the camera
            ScreenSpaceAmbientOcclusionVolume volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolume>();
            m_FeatureSettings.Quality = volume.Quality.overrideState ? volume.Quality.value : featureSettings.Quality;

            bool takeSettingsFromAsset = m_FeatureSettings.Quality != Quality.Custom;
            if (takeSettingsFromAsset)
            {
                Parameters settings = UniversalRenderPipeline.asset.GetSSAOParameters(m_FeatureSettings.Quality);
                //m_FeatureSettings.DepthSource = settings.DepthSource;
                m_FeatureSettings.Params.Downsample = settings.Downsample;
                m_FeatureSettings.Params.NormalSamples = settings.NormalSamples;
                m_FeatureSettings.Params.Intensity = settings.Intensity;
                m_FeatureSettings.Params.Radius = settings.Radius;
                m_FeatureSettings.Params.SampleCount = settings.SampleCount;
                m_FeatureSettings.Params.BlurPasses = settings.BlurPasses;
            }
            else
            {
                //m_FeatureSettings.DepthSource = volume.DepthSource.value;
                m_FeatureSettings.Params.Downsample = volume.Downsample.value;
                m_FeatureSettings.Params.NormalSamples = volume.NormalSamples.value;
                m_FeatureSettings.Params.Intensity = volume.Intensity.value;
                m_FeatureSettings.Params.Radius = volume.Radius.value;
                m_FeatureSettings.Params.SampleCount = volume.SampleCount.value;
                m_FeatureSettings.Params.BlurPasses = volume.BlurPasses.value;
            }

            return material != null
               &&  m_FeatureSettings.Params.Intensity > 0.0f
               &&  m_FeatureSettings.Params.Radius > 0.0f
               &&  m_FeatureSettings.Params.SampleCount > 0;
        }

        /// <inheritdoc/>
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            int downsampleDivider = m_FeatureSettings.Params.Downsample ? 2 : 1;

            // Update SSAO parameters in the material
            Vector4 ssaoParams = new Vector4(
                1.0f / downsampleDivider,      // Downsampling
                m_FeatureSettings.Params.Intensity,   // Intensity
                m_FeatureSettings.Params.Radius,      // Radius
                m_FeatureSettings.Params.SampleCount  // Sample count
            );
            material.SetVector(s_SSAOParamsID, ssaoParams);


            // Update keywords
            CoreUtils.SetKeyword(material, k_OrthographicCameraKeyword, renderingData.cameraData.camera.orthographic);

            //if (m_FeatureSettings.DepthSource == DepthSource.Depth)
            {
                switch (m_FeatureSettings.Params.NormalSamples)
                {
                    case NormalSamples.One:
                        CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, true);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, false);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                        break;
                    case NormalSamples.Five:
                        CoreUtils.SetKeyword(material, k_NormalReconstructionLowKeyword, false);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionMediumKeyword, true);
                        CoreUtils.SetKeyword(material, k_NormalReconstructionHighKeyword, false);
                        break;
                    case NormalSamples.Nine:
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
                //          ExecuteSSAO(cmd, (int) ShaderPass.OcclusionDepthNormals);
                //         break;
                // }

                int SSAOTexID = ExecuteKawaseBlur(cmd);
                cmd.SetGlobalTexture(k_SSAOTextureName, SSAOTexID);
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
            int numOfPasses = m_FeatureSettings.Params.BlurPasses;
            if (numOfPasses == 0)
            {
                return s_SSAOTexture1ID;
            }

            int kawasePassID = (int) ShaderPass.KawaseBlur;
            Vector4 offset = 0.5f * m_offsetIncrement;

            RenderTargetIdentifier lastTarget = m_SSAOTexture1Target;
            RenderTargetIdentifier curTarget = m_SSAOTexture2Target;
            int lastTargetID = s_SSAOTexture1ID;
            int curTargetID = s_SSAOTexture2ID;
            for (int i = 0; i < m_FeatureSettings.Params.BlurPasses; i++)
            {
                cmd.SetGlobalVector(s_BlurOffsetID, offset);
                Render(cmd, lastTargetID, curTarget, kawasePassID);
                offset += m_offsetIncrement;

                // Ping-Pong
                CoreUtils.Swap(ref curTarget, ref lastTarget);
                CoreUtils.Swap(ref curTargetID, ref lastTargetID);
            }

            return lastTargetID;
        }

        private void Render(CommandBuffer cmd, RenderTargetIdentifier target, int pass)
        {
            cmd.SetRenderTarget(target, k_RBLADontCare, k_RBLAStore, target, k_RBLADontCare, k_RBSADontCare);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, pass);
        }

        private void Render(CommandBuffer cmd, int baseMap, RenderTargetIdentifier target, int pass)
        {
            cmd.SetGlobalTexture(s_BaseMapID, baseMap);
            Render(cmd, target, pass);
        }
    }
}
