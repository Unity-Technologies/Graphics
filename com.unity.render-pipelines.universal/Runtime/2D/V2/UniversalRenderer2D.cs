using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class UniversalRenderer2D : ScriptableRenderer, IRenderPass2D
    {
        private Renderer2DData m_RendererData;
        private Light2DCullResult m_LightCullResult;
        private FinalBlitPass m_FinalBlitPass;
        private Material m_ClearMaterial;
        private GBuffers m_GBuffers;

        public class GBuffers
        {
            public static readonly int k_NormalTexture = 4;
            public static readonly int k_ColorTexture = 5;

            public RTHandle[] buffers { get; }
            public GraphicsFormat[] formats { get; }
            public bool[] transients { get; }

            public RTHandle[] lightBuffers { get; }

            public RTHandle colorAttachment { get; private set; }

            public RTHandle depthAttachment { get; private set; }

            public RTHandle normalAttachment { get; private set; }

            public GBuffers()
            {
                buffers = new RTHandle[
                    4
                    + 1 // normal
                    + 1 // color
                ];
                formats = new GraphicsFormat[buffers.Length];
                transients = new[] {true, true, true, true, true};
                lightBuffers = new RTHandle[4];
            }

            public void Init(ref RenderingData renderingData, Renderer2DData rendererData)
            {
                ref var cameraData = ref renderingData.cameraData;
                ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

                var renderTextureScale = 1.0f;//Mathf.Clamp(rendererData.lightRenderTextureScale, 0.01f, 1.0f);
                var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
                var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

                var gbufferSlice = cameraTargetDescriptor;
                gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                gbufferSlice.stencilFormat = GraphicsFormat.None;
                gbufferSlice.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                gbufferSlice.msaaSamples = 1;
                gbufferSlice.useMipMap = false;
                gbufferSlice.autoGenerateMips = false;

                gbufferSlice.width = width;
                gbufferSlice.height = height;

                for (var i = 0; i < buffers.Length; i++)
                {
                    if (i == k_ColorTexture)
                    {
                        var colorDescriptor = cameraTargetDescriptor;
                        colorDescriptor.depthBufferBits = 0;
                        colorDescriptor.width = width;
                        colorDescriptor.height = height;

                        var rt = this.buffers[i];
                        RenderingUtils.ReAllocateIfNeeded(ref rt, colorDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: Render2DLightingPass.k_ShapeLightTextureNames[i]);
                        buffers[i] = rt;
                        formats[i] = colorDescriptor.graphicsFormat;
                        colorAttachment = rt;
                    }
                    else
                    {
                        formats[i] = gbufferSlice.graphicsFormat;
                        buffers[i] = RTHandles.Alloc(Render2DLightingPass.k_ShapeLightTextureNames[i], Render2DLightingPass.k_ShapeLightTextureNames[i]);
                    }
                }

                {
                    for (var i = 0; i < lightBuffers.Length; i++)
                        lightBuffers[i] = buffers[i];
                }

                // {
                //     var colorDescriptor = cameraTargetDescriptor;
                //     colorDescriptor.depthBufferBits = 0;
                //     var rt = this.colorAttachment;
                //     RenderingUtils.ReAllocateIfNeeded(ref rt, colorDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_CameraColorAttachment");
                //     this.colorAttachment = rt;
                // }

                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    depthDescriptor.width = width;
                    depthDescriptor.height = height;

                    if (!cameraData.resolveFinalTarget && true)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                    var rt = this.depthAttachment;
                    RenderingUtils.ReAllocateIfNeeded(ref rt, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                    this.depthAttachment = rt;
                }

                {
                    this.normalAttachment = buffers[k_NormalTexture];

                    // var colorDescriptor = cameraTargetDescriptor;
                    // colorDescriptor.depthBufferBits = 0;
                    // var rt = this.normalAttachment;
                    // RenderingUtils.ReAllocateIfNeeded(ref rt, colorDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_NormalAttachment");
                    // this.normalAttachment = rt;
                }
            }

            public void Release()
            {
                if (buffers != null)
                {
                    foreach(var gb in buffers)
                        gb?.Release();
                }

                colorAttachment?.Release();
                depthAttachment?.Release();
            }
        }

        public UniversalRenderer2D(Renderer2DData data) : base(data)
        {
            m_RendererData = data;
            m_LightCullResult = new Light2DCullResult();
            m_RendererData.lightCullResult = m_LightCullResult;
            m_ClearMaterial = CoreUtils.CreateEngineMaterial(data.clearShader);
            var blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, blitMaterial);
            m_GBuffers = new GBuffers();
            useRenderPassEnabled = true;
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_GBuffers.Init(ref renderingData, m_RendererData);
            ConfigureCameraTarget(m_GBuffers.colorAttachment, m_GBuffers.depthAttachment);

            var layerBatches = LayerUtility.CalculateBatches(m_RendererData.lightCullResult, out var batchCount);
            for (var i = 0; i < layerBatches.Length; i += 1)
            {
                ref var layerBatch = ref layerBatches[i];
                layerBatch.FilterLights(m_RendererData.lightCullResult.visibleLights);

                var drawGlobalLightPass = new DrawGlobalLight2DPass(m_ClearMaterial, layerBatch, m_GBuffers);
                drawGlobalLightPass.specialId = i;
                EnqueuePass(drawGlobalLightPass);

                if (layerBatch.lightStats.totalNormalMapUsage > 0)
                {
                    var normalPass = new DrawNormal2DPass(layerBatch, m_GBuffers);
                    normalPass.specialId = i;
                    EnqueuePass(normalPass);
                }

                var drawLightPass = new DrawLight2DPass(m_RendererData, layerBatch, m_GBuffers);
                drawLightPass.specialId = i;
                EnqueuePass(drawLightPass);

                var drawObjectPass = new DrawRenderer2DPass(m_RendererData, layerBatch, m_GBuffers);
                drawObjectPass.specialId = i;
                EnqueuePass(drawObjectPass);
            }

            ref var cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;
            m_FinalBlitPass.Setup(cameraTargetDescriptor, m_GBuffers.colorAttachment);
            EnqueuePass(m_FinalBlitPass);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
            m_LightCullResult.SetupCulling(ref cullingParameters, cameraData.camera);
        }

        protected override void Dispose(bool disposing)
        {
            m_GBuffers?.Release();
        }

        public Renderer2DData rendererData => m_RendererData;
    }
}
