namespace UnityEngine.Rendering.Universal
{
    // TODO: xmldoc
    internal class VolumeBlendingPass : ScriptableRenderPass
    {
        const string k_VolumeBlendingTag = "Volume Blending";

        public VolumeBlendingPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            overrideCameraTarget = true;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            var cmd = CommandBufferPool.Get(k_VolumeBlendingTag);

            // TODO: Send a command buffer for texture blending ops
            VolumeManager.instance.Update(cameraData.volumeTrigger, cameraData.volumeLayerMask);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
