using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
{
    // Public Variables
    public Settings settings = new Settings();
    public override bool HasFeatureRequirements => true;

    // Private Variables
    private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

    [SerializeField]
    public enum DepthTextureSource
    {
        Depth,
        DepthNormals,
        GBuffer
    }


    [Serializable]
    public class Settings
    {
        public DepthTextureSource depthTextureSource = DepthTextureSource.Depth;
    }


    public ScreenSpaceAmbientOcclusion()
    {
        
    }

    // Called from OnEnable and OnValidate...
    public override void Create()
    {
        // Get the array of renderers...
        ScriptableRendererData[] rendererDataArray = UniversalRenderPipeline.asset.m_RendererDataList;
        if (rendererDataArray == null || rendererDataArray[0] == null)
        {
            Debug.LogError("Unable to find renderer data in \"UniversalRenderPipeline.asset.m_RendererDataList\" !");
            return;
        }

        // Try to get data from the forward renderer...
        ForwardRendererData forwardData = null;
        forwardData = UniversalRenderPipeline.asset.m_RendererDataList[0] as ForwardRendererData;
        if (forwardData == null)
        {
            Debug.LogError("Unable to find ForwardRendererData in \"UniversalRenderPipeline.asset.m_RendererDataList[0]\" !");
            return;
        }

        // TODO: Deferred...
        // When implemented we need to to get the data from it.

        // Create the pass if we haven't already done so...
        if (m_SSAOPass == null)
        {
            m_SSAOPass = new ScreenSpaceAmbientOcclusionPass(name, forwardData.shaders.screenSpaceAmbientOcclusionPS, RenderPassEvent.AfterRenderingPrePasses);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_SSAOPass.Setup(name, ref renderingData, settings.depthTextureSource);
        renderer.EnqueuePass(m_SSAOPass);
    }

    public override RenderFeatureRequirements GetFeatureRequirements()
    {
        return new RenderFeatureRequirements()
        {
            depthTexture = settings.depthTextureSource == DepthTextureSource.Depth,
            depthNormalTexture = settings.depthTextureSource == DepthTextureSource.DepthNormals,
        };
    }

    public override void Cleanup()
    {
        if (m_SSAOPass != null)
        {
            m_SSAOPass.Cleanup();
        }
    }



    private class ScreenSpaceAmbientOcclusionPass : ScriptableRenderPass
    {
        private Material m_Material;
        private RenderTargetHandle m_Texture;
        private RenderTextureDescriptor m_Descriptor;
        private RenderTargetIdentifier screenSpaceOcclusionTexture;

        private Shader m_Shader;
        private string m_ProfilerTag;
        private DepthTextureSource m_DepthTextureMode = DepthTextureSource.Depth;

        private const string m_TextureName = "_ScreenSpaceAOTexture";
        private static readonly int _TempRenderTexture1 = Shader.PropertyToID("_TempRenderTexture1");
        private static readonly int _TempRenderTexture2 = Shader.PropertyToID("_TempRenderTexture2");

        private enum SSAOShaderPasses
        {
            OcclusionDepth = 0,
            OcclusionDepthNormals = 1,
            OcclusionGbuffer = 2,
            HorizontalBlurDepth = 3,
            HorizontalBlurDepthNormals = 4,
            HorizontalBlurGBuffer = 5,
            VerticalBlur = 6,
            FinalComposition = 7,
        }

        public ScreenSpaceAmbientOcclusionPass(string profilerTag, Shader screenSpaceAmbientOcclusionShader, RenderPassEvent rpEvent)
        {
            m_ProfilerTag = profilerTag;
            m_Shader = screenSpaceAmbientOcclusionShader;

            m_Texture.Init(m_TextureName);
            renderPassEvent = rpEvent;
        }

        public void Setup(string profilerTag, ref RenderingData renderingData, DepthTextureSource depthTextureMode)
        {
            m_ProfilerTag = profilerTag;
            m_DepthTextureMode = depthTextureMode;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_Material == null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            }

            AmbientOcclusion ambientOcclusion = VolumeManager.instance.stack.GetComponent<AmbientOcclusion>();
            int downScale = 1;
            float intensity = 1.0f;
            float radius = 0.1f;
            int sampleCount = 12;

            if (ambientOcclusion != null)
            {
                downScale = ambientOcclusion.downSample.value ? 2 : 1;
                intensity = ambientOcclusion.intensity.value;
                radius = ambientOcclusion.radius.value;
                sampleCount = ambientOcclusion.sampleCount.value;
            }


            // Setup descriptor
            m_Descriptor = cameraTextureDescriptor;
            m_Descriptor.msaaSamples = 1;
            m_Descriptor.depthBufferBits = 0;
            m_Descriptor.width = m_Descriptor.width / downScale;
            m_Descriptor.height = m_Descriptor.height / downScale;
            m_Descriptor.colorFormat = RenderTextureFormat.R8;

            // SSAO settings
            m_Material.SetFloat("_SSAO_DownScale", 1.0f / downScale);
            m_Material.SetFloat("_SSAO_Intensity", intensity);
            m_Material.SetFloat("_SSAO_Radius", radius);
            m_Material.SetInt("_SSAO_Samples", sampleCount);

            var desc = GetStereoCompatibleDescriptor(m_Descriptor.width * downScale, m_Descriptor.height * downScale, GraphicsFormat.R8G8B8A8_UNorm);
            cmd.GetTemporaryRT(m_Texture.id, m_Descriptor, FilterMode.Point);
            cmd.GetTemporaryRT(_TempRenderTexture1, desc, FilterMode.Point);
            cmd.GetTemporaryRT(_TempRenderTexture2, desc, FilterMode.Point);

            screenSpaceOcclusionTexture = m_Texture.Identifier();

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

            Camera camera = renderingData.cameraData.camera;
            m_Material.SetMatrix("ProjectionMatrix", camera.projectionMatrix);
            bool isStereo = renderingData.cameraData.isStereoEnabled;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.ScreenSpaceAmbientOcclusion, true);
            if (!isStereo)
            {
                switch (m_DepthTextureMode)
                {
                    case DepthTextureSource.Depth:
                        ExecuteDepthTexture(camera, cmd);
                        break;
                    case DepthTextureSource.DepthNormals:
                        ExecuteDepthNormalTexture(camera, cmd);
                        break;
                    case DepthTextureSource.GBuffer:
                        ExecuteGBuffer(camera, cmd);
                        break;
                }
            }
            else
            {
                // ????
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void ExecuteGBuffer(Camera camera, CommandBuffer cmd)
        {
            Debug.LogError("GBUFFER is not Implemented!!!!");
        }

        private void ExecuteDepthTexture(Camera camera, CommandBuffer cmd)
        {
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // Occlusion pass
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture1, m_Material, (int)SSAOShaderPasses.OcclusionDepth);

            // Horizontal Blur
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture2, m_Material, (int)SSAOShaderPasses.HorizontalBlurDepth);

            // Vertical Blur
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture2);
            cmd.Blit(_TempRenderTexture2, _TempRenderTexture1, m_Material, (int)SSAOShaderPasses.VerticalBlur);

            // Final Composition
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, screenSpaceOcclusionTexture, m_Material, (int)SSAOShaderPasses.FinalComposition);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
        }

        private void ExecuteDepthNormalTexture(Camera camera, CommandBuffer cmd)
        {
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);

            // Occlusion pass
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture1, m_Material, (int)SSAOShaderPasses.OcclusionDepthNormals);

            // Horizontal Blur
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, _TempRenderTexture2, m_Material, (int)SSAOShaderPasses.HorizontalBlurDepthNormals);

            // Vertical Blur
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture2);
            cmd.Blit(_TempRenderTexture2, _TempRenderTexture1, m_Material, (int)SSAOShaderPasses.VerticalBlur);

            // Final Composition
            cmd.SetGlobalTexture("_MainTex", _TempRenderTexture1);
            cmd.Blit(_TempRenderTexture1, screenSpaceOcclusionTexture, m_Material, (int)SSAOShaderPasses.FinalComposition);

            cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
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

