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

    // Enums
    public enum DepthSource
    {
        Depth,
        DepthNormals
    }

    // Classes
    [Serializable]
    public class Settings
    {
        public Shader shader;
        public bool useVolumes = false;
        public DepthSource depthSource = DepthSource.Depth;
        public bool downScale = false;
        public float intensity = 1.0f;
        public float radius = 0.1f;
        public int sampleCount = 10;
    }
    
    // Called from OnEnable and OnValidate...
    public override void Create()
    {
        if (settings.shader == null)
        {
            settings.shader = Shader.Find("Hidden/Universal Render Pipeline/ScreenSpaceAmbientOcclusion");
        }

        if (m_Material == null)
        {
            if (settings.shader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(settings.shader);
            }
        }

        // Create the pass...
        m_SSAOPass = new ScreenSpaceAmbientOcclusionPass(name, m_Material, RenderPassEvent.AfterRenderingPrePasses);
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

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
    
    // The SSAO Pass
    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        private string m_ProfilerTag;
        private Material m_Material;
        private RenderTargetHandle m_Texture;
        //private DepthSource m_DepthSource;
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier m_ScreenSpaceOcclusionTexture;
        private Settings m_FeatureSettings;

        private const string m_TextureName = "_ScreenSpaceAOTexture";
        private static readonly int _BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int _TempRenderTexture1 = Shader.PropertyToID("_TempRenderTexture1");
        private static readonly int _TempRenderTexture2 = Shader.PropertyToID("_TempRenderTexture2");
        
        private enum ShaderPass
        {
            OcclusionDepth = 0,
            HorizontalBlurDepth = 1,
            VerticalBlurDepth = 2,
            OcclusionDepthNormals = 3,
            HorizontalBlurDepthNormals = 4,
            VerticalBlurDepthNormals = 5,
            OcclusionGbuffer = 6,
            HorizontalBlurGBuffer = 7,
            VerticalBlurGBuffer = 8,
            FinalComposition = 9,
            FinalCompositionGBuffer = 10,
        }
        
        public ScreenSpaceAmbientOcclusionPass(string profilerTag, Material material, RenderPassEvent rpEvent)
        {
            m_ProfilerTag = profilerTag;
            m_Material = material;
            m_Texture.Init(m_TextureName);
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
            int downScaleDivider;
            int sampleCount;
            float intensity;
            float radius;

            // Override the settings if there are either global volumes or local volumes near the camera
            ScreenSpaceAmbientOcclusionVolume volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolume>();
            if (m_FeatureSettings.useVolumes && volume != null)
            {
                radius = volume.radius.value;
                downScale = volume.downSample.value;
                intensity = volume.intensity.value;
                sampleCount = volume.sampleCount.value;
                //m_DepthSource = volume.depthSource.value;
            }
            else
            {
                radius = m_FeatureSettings.radius;
                downScale = m_FeatureSettings.downScale;
                intensity = m_FeatureSettings.intensity;
                sampleCount = m_FeatureSettings.sampleCount;
                //m_DepthSource = m_FeatureSettings.depthSource;
            }

            downScaleDivider = downScale ? 2 : 1;
            FilterMode filterMode = downScale ? FilterMode.Bilinear : FilterMode.Point;

            // Material settings
            m_Material.SetFloat("_SSAO_DownScale", 1.0f / downScaleDivider);
            m_Material.SetFloat("_SSAO_Intensity", intensity);
            m_Material.SetFloat("_SSAO_Radius", radius);
            m_Material.SetInt("_SSAO_Samples", sampleCount);

            // Setup descriptors
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / downScaleDivider;
            m_Descriptor.height = m_Descriptor.height / downScaleDivider;

            m_Descriptor.colorFormat = RenderTextureFormat.R8;
            cmd.GetTemporaryRT(m_Texture.id, m_Descriptor, FilterMode.Point);

            var desc = GetStereoCompatibleDescriptor(m_Descriptor.width * downScaleDivider, m_Descriptor.height * downScaleDivider, GraphicsFormat.R8G8B8A8_UNorm);
            
            cmd.GetTemporaryRT(_TempRenderTexture1, desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(_TempRenderTexture2, desc, FilterMode.Bilinear);

            /*RenderTextureDescriptor desc = GetStereoCompatibleDescriptor(m_Descriptor, m_Descriptor.width * downScaleDivider, m_Descriptor.height * downScaleDivider, m_Descriptor.graphicsFormat);
            cmd.GetTemporaryRT(_TempRenderTexture1, desc, filterMode);
            cmd.GetTemporaryRT(_TempRenderTexture2, desc, filterMode);

            m_Descriptor = GetStereoCompatibleDescriptor(m_Descriptor, m_Descriptor.width / downScaleDivider, m_Descriptor.height / downScaleDivider, GraphicsFormat.R8G8B8A8_UNorm);
            m_Descriptor.colorFormat = RenderTextureFormat.R8;
            cmd.GetTemporaryRT(m_Texture.id, m_Descriptor, filterMode);*/



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
            Camera camera = renderingData.cameraData.camera;
            m_Material.SetMatrix("ProjectionMatrix", camera.projectionMatrix);
            bool isStereo = renderingData.cameraData.isStereoEnabled;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceAmbientOcclusion, true);
            
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            
            // Occlusion pass
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture1, m_Material, occlusionPass);
            
            // Horizontal Blur
            cmd.SetGlobalTexture(_BaseMap, _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture2, m_Material, horizonalBlurPass);
            
            // Vertical Blur
            cmd.SetGlobalTexture(_BaseMap, _TempRenderTexture2);
            cmd.Blit(_TempRenderTexture2, _TempRenderTexture1, m_Material, verticalPass);
            
            // Final Composition
            cmd.SetGlobalTexture(_BaseMap, _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, m_ScreenSpaceOcclusionTexture, m_Material, finalPass);
            
            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            
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
            cmd.ReleaseTemporaryRT(_TempRenderTexture1);
            cmd.ReleaseTemporaryRT(_TempRenderTexture2);
        }

        RenderTextureDescriptor GetStereoCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            // Inherit the VR setup from the camera descriptor
            var desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }
    }
}
