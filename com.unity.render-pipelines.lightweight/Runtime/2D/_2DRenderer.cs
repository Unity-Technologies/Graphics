using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class _2DRenderer : ScriptableRenderer
    {
        Render2DLightingPass m_Render2DLightingPass;
        FinalBlitPass m_FinalBlitPass;

        public _2DRenderer(_2DRendererData data) : base(data)
        {
            m_Render2DLightingPass = new Render2DLightingPass(data);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, CoreUtils.CreateEngineMaterial(data.blitShader));
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderTargetHandle colorTargetHandle = RenderTargetHandle.CameraTarget;
            bool useOffscreenColorTexture = true;

            if (useOffscreenColorTexture)
                colorTargetHandle = CreateOffscreenColorTexture(context, ref renderingData.cameraData);

            ConfigureCameraTarget(colorTargetHandle.Identifier(), BuiltinRenderTextureType.CameraTarget);

            m_Render2DLightingPass.ConfigureTarget(colorTargetHandle.Identifier());
            EnqueuePass(m_Render2DLightingPass);

            if (useOffscreenColorTexture)
            {
                m_FinalBlitPass.Setup(renderingData.cameraData.cameraTargetDescriptor, colorTargetHandle);
                EnqueuePass(m_FinalBlitPass);
            }
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
        }

        RenderTargetHandle CreateOffscreenColorTexture(ScriptableRenderContext context, ref CameraData cameraData)
        {
            RenderTargetHandle colorTextureHandle = new RenderTargetHandle();
            colorTextureHandle.Init("_CameraColorTexture");

            var colorDescriptor = cameraData.cameraTargetDescriptor;
            colorDescriptor.depthBufferBits = 32;

            CommandBuffer cmd = CommandBufferPool.Get("Create Camera Textures");
            cmd.GetTemporaryRT(colorTextureHandle.id, colorDescriptor, FilterMode.Bilinear);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            return colorTextureHandle;
        }
    }
}
