using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenSpaceAmbientOcclusionFeature : ScriptableRendererFeature
{
    // Public Variables
    public Settings settings = new Settings();
    public override bool HasRenderingRequirements => true;
    
    // Private Variables
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
        public bool highQualityNormals = true;
        public DepthSource depthSource = DepthSource.Depth;
        public bool downScale = false;
        public float intensity = 1.0f;
        public float radius = 0.1f;
        public int sampleCount = 16;
    }
    
    // Called from OnEnable and OnValidate...
    public override void Create()
    {
        // Retrieve the array of renderers...
        ScriptableRendererData[] rendererDataArray = UniversalRenderPipeline.asset.m_RendererDataList;
        if (rendererDataArray == null || rendererDataArray[0] == null)
        {
            Debug.LogError("Unable to find renderer data in \"UniversalRenderPipeline.asset.m_RendererDataList\" !");
            return;
        }
        
        // Try to retrieve the forward renderer...
        ForwardRendererData forwardData = rendererDataArray[0] as ForwardRendererData;
        if (forwardData == null)
        {
            Debug.LogError("Unable to find ForwardRendererData in \"UniversalRenderPipeline.asset.m_RendererDataList[0]\" !");
            return;
        }
        
        // Create the pass...
        m_SSAOPass = new ScreenSpaceAmbientOcclusionPass(name, forwardData.shaders.screenSpaceAmbientOcclusionPS, RenderPassEvent.AfterRenderingPrePasses);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_SSAOPass.Setup(name, settings);
        renderer.EnqueuePass(m_SSAOPass);
    }
    
    public override RenderFeatureRequirements GetRenderingRequirements()
    {
        return m_SSAOPass.GetFeatureRequirements();
    }
    
    public override void Cleanup()
    {
        if (m_SSAOPass != null)
        {
            m_SSAOPass.Cleanup();
        }
    }
    
    // The SSAO Pass
    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        private int m_SampleCount;
        private int m_DownScale;
        private float m_Intensity;
        private float m_Radius;
        private string m_ProfilerTag;
        private Material m_Material;
        private RenderTargetHandle m_Texture;
        private DepthSource m_DepthSource;
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier screenSpaceOcclusionTexture;
        
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
        
        public ScreenSpaceAmbientOcclusionPass(string profilerTag, Shader screenSpaceAmbientOcclusionShader, RenderPassEvent rpEvent)
        {
            m_ProfilerTag = profilerTag;
            m_Material = CoreUtils.CreateEngineMaterial(screenSpaceAmbientOcclusionShader);            
            m_Texture.Init(m_TextureName);
            screenSpaceOcclusionTexture = m_Texture.Identifier();
            renderPassEvent = rpEvent;
        }
        
        public RenderFeatureRequirements GetFeatureRequirements()
        {
            return new RenderFeatureRequirements()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses,
                depthTexture = m_DepthSource == DepthSource.Depth,
                depthNormalTexture = m_DepthSource == DepthSource.DepthNormals,
                depthCreationOption = DepthCreationOption.Reuse
            };
        }
        
        public void Setup(string profilerTag, Settings settings)
        {
            m_Radius = settings.radius;
            m_DownScale = settings.downScale ? 2 : 1;
            m_Intensity = settings.intensity;
            m_SampleCount = settings.sampleCount;
            m_DepthSource = settings.depthSource;
            m_ProfilerTag = profilerTag;
        }
        
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Override the settings if there are either global volumes or local volumes near the camera
            ScreenSpaceAmbientOcclusionVolume volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolume>();
            if (volume != null && volume.IsActive())
            {
                m_Radius = volume.radius.value;
                m_DownScale = volume.downSample.value ? 2 : 1;
                m_Intensity = volume.intensity.value;
                m_SampleCount = volume.sampleCount.value;
                m_DepthSource = volume.depthSource.value;
            }

            // Material settings
            m_Material.SetFloat("_SSAO_DownScale", 1.0f / m_DownScale);
            m_Material.SetFloat("_SSAO_Intensity", m_Intensity);
            m_Material.SetFloat("_SSAO_Radius", m_Radius);
            m_Material.SetInt("_SSAO_Samples", m_SampleCount);
            
            // Setup descriptors
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / m_DownScale;
            m_Descriptor.height = m_Descriptor.height / m_DownScale;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;
            cmd.GetTemporaryRT(m_Texture.id, m_Descriptor, FilterMode.Point);
            
            var desc = GetStereoCompatibleDescriptor(m_Descriptor.width * m_DownScale, m_Descriptor.height * m_DownScale, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(_TempRenderTexture1, desc, FilterMode.Point);
            cmd.GetTemporaryRT(_TempRenderTexture2, desc, FilterMode.Point);
            
            // Configure targets and clear color
            ConfigureTarget(screenSpaceOcclusionTexture);
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

            switch (m_DepthSource)
            {
                case DepthSource.Depth:
                    ExecuteSSAO(
                        context,
                        ref renderingData,
                        (int) ShaderPass.OcclusionDepth,
                        (int) ShaderPass.HorizontalBlurDepth,
                        (int) ShaderPass.VerticalBlurDepth,
                        (int) ShaderPass.FinalComposition
                    );
                    break;
                case DepthSource.DepthNormals:
                    ExecuteSSAO(
                        context,
                        ref renderingData,
                        (int) ShaderPass.OcclusionDepthNormals,
                        (int) ShaderPass.HorizontalBlurDepthNormals,
                        (int) ShaderPass.VerticalBlurDepthNormals,
                        (int) ShaderPass.FinalComposition
                    );
                    break;
            }
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
            cmd.Blit(_TempRenderTexture1, screenSpaceOcclusionTexture, m_Material, finalPass);
            
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

        public void Cleanup()
        {
            CoreUtils.Destroy(m_Material);
        }
    }
}