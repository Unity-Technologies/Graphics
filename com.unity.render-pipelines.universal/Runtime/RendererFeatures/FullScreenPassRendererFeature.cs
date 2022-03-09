using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class FullScreenPassRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Material postProcessMaterial;
    [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    [SerializeField] private ScriptableRenderPassInput requirements = ScriptableRenderPassInput.Color;
    [SerializeField] private int passIndex;

    private FullscreenRenderPass fullScreenPass;

    public override void Create()
    {
        fullScreenPass = new FullscreenRenderPass();
        fullScreenPass.renderPassEvent = injectionPoint;
        fullScreenPass.ConfigureInput(requirements);
        fullScreenPass.profilerTag = name;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (postProcessMaterial == null)
        {
            Debug.LogWarningFormat("Missing Post Processing effect Material. {0} Fullscreen pass will not execute. Check for missing reference in the assigned renderer.", GetType().Name);
            return;
        }

        // TODO Add logic here to choose what source to send?
        fullScreenPass.Setup(postProcessMaterial, passIndex);
        renderer.EnqueuePass(fullScreenPass);
    }

    class FullscreenRenderPass : ScriptableRenderPass
    {
        public string profilerTag = "Fullscreen Render Pass";
        private Material passMaterial;
        private int passIndex;

        private RTHandle source;
        private RTHandle destination;

        // _CameraNormalsTexture
        static class ShaderConstants
        {
            public static int _TempTarget = Shader.PropertyToID("_TempTarget");

            public static readonly int _MainTexture = Shader.PropertyToID("_MainTex");
            public static readonly int _DepthTexture = Shader.PropertyToID("_DepthTexture");
            public static readonly int _NormalTexture = Shader.PropertyToID("_CameraNormalsTexture");
        }

        public void Setup(Material mat, int index)
        {
            passMaterial = mat;
            passIndex = index;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        private void Swap(CommandBuffer cmd, in ScriptableRenderer r)
        {
            r.SwapColorBuffer(cmd);
            //source = r.cameraColorTargetHandle;
            //destination = r.GetCameraColorFrontBuffer(cmd);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passMaterial == null) return; // should not happen as we check it in feature

            if (renderingData.cameraData.isPreviewCamera)
            {
                return; // TODO handle this, otherwise RT throws an error...
            }

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            cmd.SetRenderTarget(renderingData.cameraData.renderer.GetCameraColorFrontBuffer(cmd));
            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
            cmd.SetViewport(renderingData.cameraData.pixelRect);
            if ((input & ScriptableRenderPassInput.Color) != 0)
            {
                source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                passMaterial.SetTexture(ShaderConstants._MainTexture, source);
            }
            cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, passMaterial, 0, passIndex);

            Swap(cmd, renderingData.cameraData.renderer);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

    }

}


