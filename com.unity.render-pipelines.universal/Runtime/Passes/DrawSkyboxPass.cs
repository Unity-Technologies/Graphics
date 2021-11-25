namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Draw the skybox into the given color buffer using the given depth buffer for depth testing.
    ///
    /// This pass renders the standard Unity skybox.
    /// </summary>
    public class DrawSkyboxPass : ScriptableRenderPass
    {
        public DrawSkyboxPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawSkyboxPass));

            renderPassEvent = evt;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;
            Camera camera = cameraData.camera;

            var activeDebugHandler = GetActiveDebugHandler(renderingData);
            if (activeDebugHandler != null)
            {
                // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it
                // when certain debug modes are active (e.g. wireframe/overdraw modes)
                if (activeDebugHandler.IsScreenClearNeeded)
                {
                    return;
                }
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            // XRTODO: Remove this code once Skybox pass is moved to SRP land.
            if (cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (cameraData.xr.singlePassEnabled)
                {
                    // Setup legacy skybox stereo buffer
                    camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, cameraData.GetProjectionMatrix(0));
                    camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, cameraData.GetViewMatrix(0));
                    camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, cameraData.GetProjectionMatrix(1));
                    camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, cameraData.GetViewMatrix(1));

                    CommandBuffer cmd = CommandBufferPool.Get();

                    // Use legacy stereo instancing mode to have legacy XR code path configured
                    cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Calling into built-in skybox pass
                    context.DrawSkybox(camera);

                    // Disable Legacy XR path
                    cmd.SetSinglePassStereo(SinglePassStereoMode.None);
                    context.ExecuteCommandBuffer(cmd);
                    // We do not need to submit here due to special handling of stereo matrices in core.
                    // context.Submit();
                    CommandBufferPool.Release(cmd);

                    camera.ResetStereoProjectionMatrices();
                    camera.ResetStereoViewMatrices();
                }
                else
                {
                    camera.projectionMatrix = cameraData.GetProjectionMatrix(0);
                    camera.worldToCameraMatrix = cameraData.GetViewMatrix(0);

                    context.DrawSkybox(camera);

                    // XRTODO: remove this call because it creates issues with nested profiling scopes
                    // See examples in UniversalRenderPipeline.RenderSingleCamera() and in ScriptableRenderer.Execute()
                    context.Submit(); // Submit and execute the skybox pass before resetting the matrices

                    camera.ResetProjectionMatrix();
                    camera.ResetWorldToCameraMatrix();
                }
            }
            else
#endif
            {
                context.DrawSkybox(camera);
            }
        }
    }
}
