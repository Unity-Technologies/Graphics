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
        public DepthSource DepthSource      = DepthSource.Depth;
        public QualityOptions NormalQuality = QualityOptions.Medium;
        public bool DownScale               = false;
        public float Intensity              = 0.0f;
        public float Radius                 = 0.05f;
        public int SampleCount              = 10;

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
        m_SSAOPass = new ScreenSpaceAmbientOcclusionPass(name, m_Material, RenderPassEvent.AfterRenderingPrePasses); ;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Material == null)
        {
            return;
        }
        m_SSAOPass.Setup(settings);
        renderer.EnqueuePass(m_SSAOPass);
    }

    public override void Dispose()
    {
        CoreUtils.Destroy(m_Material);
    }

    // The SSAO Pass
    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        // Private Variables
        private string m_ProfilerTag;
        private Material m_Material;
        private RenderTargetHandle m_Texture;
        //private DepthSource m_DepthSource;
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_ScreenSpaceOcclusionTexture;
        private Settings m_FeatureSettings;

        // Constants
        private const string SSAO_TEXTURE_NAME = "_ScreenSpaceAmbientOcclusionTexture";
        private static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int s_TempRenderTexture1ID = Shader.PropertyToID("_TempRenderTexture1");
        private static readonly int s_TempRenderTexture2ID = Shader.PropertyToID("_TempRenderTexture2");

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

        public ScreenSpaceAmbientOcclusionPass(string profilerTag, Material material, RenderPassEvent rpEvent)
        {
            m_ProfilerTag = profilerTag;
            m_Material = material;
            m_Texture.Init(SSAO_TEXTURE_NAME);
            m_ScreenSpaceOcclusionTexture = m_Texture.Identifier();
            renderPassEvent = rpEvent;
        }

        public void Setup(Settings featureSettings)
        {
            m_FeatureSettings = featureSettings;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            bool downScale;
            int sampleCount;
            float intensity;
            float radius;
            QualityOptions reconstructionQualityOptions;

            // Override the settings if there are either global volumes or local volumes near the camera
            ScreenSpaceAmbientOcclusionVolume volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolume>();
            if (m_FeatureSettings.UseVolumes && volume != null)
            {
                radius = volume.Radius.value;
                downScale = volume.DownScale.value;
                intensity = volume.Intensity.value;
                sampleCount = volume.SampleCount.value;
                reconstructionQualityOptions = volume.NormalQuality.value;
                //m_DepthSource = volume.depthSource.value;
            }
            else
            {
                radius = m_FeatureSettings.Radius;
                downScale = m_FeatureSettings.DownScale;
                intensity = m_FeatureSettings.Intensity;
                sampleCount = m_FeatureSettings.SampleCount;
                reconstructionQualityOptions = m_FeatureSettings.NormalQuality;
                //m_DepthSource = m_FeatureSettings.depthSource;
            }

            int downScaleDivider = downScale ? 2 : 1;
            FilterMode filterMode = downScale ? FilterMode.Bilinear : FilterMode.Point;

            // Material settings
            m_Material.SetFloat("_SSAO_DownScale", 1.0f / downScaleDivider);
            m_Material.SetFloat("_SSAO_Intensity", intensity);
            m_Material.SetFloat("_SSAO_Radius", radius);
            m_Material.SetInt("_SSAO_Samples", sampleCount);

            m_Material.DisableKeyword(NORMAL_RECONSTRUCTION_LOW_KEYWORD);
            m_Material.DisableKeyword(NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD);
            m_Material.DisableKeyword(NORMAL_RECONSTRUCTION_HIGH_KEYWORD);
            //if (m_DepthSource == DepthSource.Depth)
            {
                switch (reconstructionQualityOptions)
                {
                    case QualityOptions.Low:
                        m_Material.EnableKeyword(NORMAL_RECONSTRUCTION_LOW_KEYWORD);
                        break;
                    case QualityOptions.Medium:
                        m_Material.EnableKeyword(NORMAL_RECONSTRUCTION_MEDIUM_KEYWORD);
                        break;
                    case QualityOptions.High:
                        m_Material.EnableKeyword(NORMAL_RECONSTRUCTION_HIGH_KEYWORD);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Setup descriptors
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / downScaleDivider;
            m_Descriptor.height = m_Descriptor.height / downScaleDivider;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;


            var desc = GetStereoCompatibleDescriptor(cameraTextureDescriptor.width, cameraTextureDescriptor.height, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(m_Texture.id, m_Descriptor, FilterMode.Point);
            cmd.GetTemporaryRT(s_TempRenderTexture1ID, desc, filterMode);
            cmd.GetTemporaryRT(s_TempRenderTexture2ID, desc, filterMode);

            // Configure targets and clear color
            ConfigureTarget(m_ScreenSpaceOcclusionTexture);
            ConfigureClear(ClearFlag.All, Color.white);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null)
            {
                Debug.LogErrorFormat(
                    "Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.",
                    m_Material, GetType().Name);
                return;
            }

            // Don't execute the SSAO passes if it doesn't contribute to the final image...
            if (m_FeatureSettings.Intensity <= 0.0f)
            {
                return;
            }

            ExecuteSSAO(
                context,
                ref renderingData,
                (int)ShaderPass.OcclusionDepth,
                (int)ShaderPass.HorizontalBlurDepth,
                (int)ShaderPass.VerticalBlurDepth,
                (int)ShaderPass.FinalComposition
            );

            // This path will be used once we've exposed render feature requirements.
            //DepthSource currentSource = m_DepthSource;
            //if (m_RequirementsSummary.needsDepthNormals)
            //{
            //    currentSource = DepthSource.DepthNormals;
            //}
            //
            //switch (currentSource)
            //{
            //    case DepthSource.Depth:
            //        ExecuteSSAO(
            //            context,
            //            ref renderingData,
            //            (int) ShaderPass.OcclusionDepth,
            //            (int) ShaderPass.HorizontalBlurDepth,
            //            (int) ShaderPass.VerticalBlurDepth,
            //            (int) ShaderPass.FinalComposition
            //        );
            //        break;
            //    case DepthSource.DepthNormals:
            //        ExecuteSSAO(
            //            context,
            //            ref renderingData,
            //            (int) ShaderPass.OcclusionDepthNormals,
            //            (int) ShaderPass.HorizontalBlurDepthNormals,
            //            (int) ShaderPass.VerticalBlurDepthNormals,
            //            (int) ShaderPass.FinalComposition
            //        );
            //        break;
            //}
        }

        private void ExecuteSSAO(ScriptableRenderContext context, ref RenderingData renderingData, int occlusionPass, int horizonalBlurPass, int verticalPass, int finalPass)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceAmbientOcclusion, true);

            // Occlusion pass
            cmd.Blit(s_TempRenderTexture1ID, s_TempRenderTexture1ID, m_Material, occlusionPass);

            // Horizontal Blur
            cmd.SetGlobalTexture(s_BaseMap, s_TempRenderTexture1ID);
            cmd.Blit(s_TempRenderTexture1ID, s_TempRenderTexture2ID, m_Material, horizonalBlurPass);

            // Vertical Blur
            cmd.SetGlobalTexture(s_BaseMap, s_TempRenderTexture2ID);
            cmd.Blit(s_TempRenderTexture2ID, s_TempRenderTexture1ID, m_Material, verticalPass);

            // Final Composition
            cmd.SetGlobalTexture(s_BaseMap, s_TempRenderTexture1ID);
            cmd.Blit(s_TempRenderTexture1ID, m_ScreenSpaceOcclusionTexture, m_Material, finalPass);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceAmbientOcclusion, false);
            cmd.ReleaseTemporaryRT(m_Texture.id);
            cmd.ReleaseTemporaryRT(s_TempRenderTexture1ID);
            cmd.ReleaseTemporaryRT(s_TempRenderTexture2ID);
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
