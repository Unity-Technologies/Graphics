using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceAmbientOcclusionFeature : ScriptableRendererFeature
{
    // Public Variables
    public Settings settings = new Settings();

    // Private Variables
    private Material m_Material;
    private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

    // Constants
    private const string NORMAL_RECONSTRUCTION_LOW_KEYWORD    = "_RECONSTRUCT_NORMAL_LOW";
    private const string NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD = "_RECONSTRUCT_NORMAL_MEDIUM";
    private const string NORMAL_RECONSTRUCTION_HIGH_KEYWORD   = "_RECONSTRUCT_NORMAL_HIGH";

    // Enums
    public enum DepthSource
    {
        Depth,
        //DepthNormals
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
        public int SampleCount              = 8;
    }

    // Called from OnEnable and OnValidate...
    public override void Create()
    {
        if (settings.Shader == null)
        {
            settings.Shader = Shader.Find("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion");
        }

        if (m_Material == null)
        {
            if (settings.Shader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(settings.Shader);
            }
        }

        // Create the pass...
        if (m_SSAOPass == null)
        {
            m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();
        }

        m_SSAOPass.profilerTag = name;
        m_SSAOPass.material = m_Material;
        m_SSAOPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null)
        {
            Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added. Check for missing reference in the renderer resources.", GetType().Name, m_SSAOPass.profilerTag);
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

    // The SSAO Pass
    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        // Public Variables
        public string profilerTag;
        public Material material;

        // Private Variables
        private Settings m_FeatureSettings;
        private ProfilingSampler m_ProfilingSampler = new ProfilingSampler("SSAO.Execute()");
        private RenderTargetHandle m_SSAOTextureHandle;
        private RenderTextureDescriptor m_Descriptor;

        // Constants
        private const string SSAO_TEXTURE_NAME = "_ScreenSpaceOcclusionTexture";
        private static readonly int s_BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int s_SSAOParamsID = Shader.PropertyToID("_SSAOParams");
        private static readonly int s_BlurTexture1ID = Shader.PropertyToID("_SSAO_BlurTexture1");
        private static readonly int s_BlurTexture2ID = Shader.PropertyToID("_SSAO_BlurTexture2");
        private static readonly int s_ScaleBiasId = Shader.PropertyToID("_ScaleBiasRT");

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
            m_SSAOTextureHandle.Init(SSAO_TEXTURE_NAME);
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

            // Keywords
            //if (m_FeatureSettings.DepthSource == DepthSource.Depth)
            {
                switch (m_FeatureSettings.NormalQuality)
                {
                    case QualityOptions.Low:
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_LOW_KEYWORD, true);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD, false);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_HIGH_KEYWORD, false);
                        break;
                    case QualityOptions.Medium:
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_LOW_KEYWORD, false);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD, true);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_HIGH_KEYWORD, false);
                        break;
                    case QualityOptions.High:
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_LOW_KEYWORD, false);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD, false);
                        CoreUtils.SetKeyword(material, NORMAL_RECONSTRUCTION_HIGH_KEYWORD, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Setup descriptors
            m_Descriptor = cameraTargetDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / downsampleDivider;
            m_Descriptor.height = m_Descriptor.height / downsampleDivider;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;

            // Get temporary render textures
            var desc = GetStereoCompatibleDescriptor(cameraTargetDescriptor.width, cameraTargetDescriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(m_SSAOTextureHandle.id, m_Descriptor, FilterMode.Point);

            FilterMode filterMode = m_FeatureSettings.Downsample ? FilterMode.Bilinear : FilterMode.Point;
            cmd.GetTemporaryRT(s_BlurTexture1ID, desc, filterMode);
            cmd.GetTemporaryRT(s_BlurTexture2ID, desc, filterMode);

            // Configure targets and clear color
            ConfigureTarget(m_SSAOTextureHandle.id);
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
            cmd.SetRenderTarget(s_BlurTexture1ID);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, occlusionPass);

            // Horizontal Blur
            cmd.SetGlobalTexture(s_BaseMapID, s_BlurTexture1ID);
            cmd.SetRenderTarget(s_BlurTexture2ID);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, horizontalBlurPass);

            // Vertical Blur
            cmd.SetGlobalTexture(s_BaseMapID, s_BlurTexture2ID);
            cmd.SetRenderTarget(s_BlurTexture1ID);
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, material, 0, verticalPass);

            // Final Composition
            cmd.SetGlobalTexture(s_BaseMapID, s_BlurTexture1ID);
            cmd.SetRenderTarget(m_SSAOTextureHandle.id);
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
            cmd.ReleaseTemporaryRT(m_SSAOTextureHandle.id);
            cmd.ReleaseTemporaryRT(s_BlurTexture1ID);
            cmd.ReleaseTemporaryRT(s_BlurTexture2ID);
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            // Inherit the VR setup from the camera descriptor
            RenderTextureDescriptor desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}
