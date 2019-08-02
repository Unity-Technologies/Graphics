using System;
using Exerimental = UnityEngine.Experimental;

namespace UnityEngine.Rendering.LWRP
{
    internal class VTFeedbackPass : ScriptableRenderPass
    {
        const int scale = 16; //== 1/16 res

        RenderTargetHandle m_FeedbackAttachmentHandle;
        RenderTextureDescriptor m_Descriptor;

        FilteringSettings m_FilteringSettings;
        string m_ProfilerTag = "VT feedback";
        ShaderTagId m_ShaderTagId = new ShaderTagId("VTFeedback");

        Exerimental.VirtualTextureResolver m_Resolver;
        int m_VirtualConstantPatchID = Shader.PropertyToID("VT_ResolveConstantPatch");

        public VTFeedbackPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_Resolver = new Exerimental.VirtualTextureResolver();
            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            int w = baseDescriptor.width / scale;
            int h = baseDescriptor.height / scale;
            m_Descriptor = new RenderTextureDescriptor(w, h, Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm, 32, 1);
            m_Descriptor.useDynamicScale = false;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Descriptor.enableRandomWrite = false;

            m_FeedbackAttachmentHandle.Init("VTFeedbackRT");

            m_Resolver.Init(baseDescriptor.width, baseDescriptor.height, w, h);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
             cmd.GetTemporaryRT(m_FeedbackAttachmentHandle.id, m_Descriptor, FilterMode.Point);
             ConfigureTarget(m_FeedbackAttachmentHandle.Identifier());
             ConfigureClear(ClearFlag.All, Color.white);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (m_VirtualConstantPatchID < 0)
                {
                    Debug.LogError("Material is used with VT feedback but did not expose the VT_ResolveConstantPatch variable.");
                }

                cmd.SetGlobalVector(m_VirtualConstantPatchID, m_Resolver.VirtualConstantPatch);

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStereoEnabled)
                    context.StartMultiEye(camera);

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

                m_Resolver.Process(m_FeedbackAttachmentHandle.Identifier(), cmd);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

             if (m_FeedbackAttachmentHandle != RenderTargetHandle.CameraTarget)
             {
                 cmd.ReleaseTemporaryRT(m_FeedbackAttachmentHandle.id);
                 m_FeedbackAttachmentHandle = RenderTargetHandle.CameraTarget;
             }
        }
    }
}
