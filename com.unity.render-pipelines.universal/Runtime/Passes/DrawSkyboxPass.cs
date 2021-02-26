
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
            Camera camera = renderingData.cameraData.camera;

#if ENABLE_VR && ENABLE_XR_MODULE
            // XRTODO: Remove this code once Skybox pass is moved to SRP land.
            if (renderingData.cameraData.xr.enabled)
            {
                // Setup Legacy XR buffer states
                if (renderingData.cameraData.xr.singlePassEnabled)
                {
                    // Setup legacy skybox stereo buffer
                    renderingData.cameraData.camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Left, renderingData.cameraData.GetProjectionMatrix(0));
                    renderingData.cameraData.camera.SetStereoViewMatrix(Camera.StereoscopicEye.Left, renderingData.cameraData.GetViewMatrix(0));
                    renderingData.cameraData.camera.SetStereoProjectionMatrix(Camera.StereoscopicEye.Right, renderingData.cameraData.GetProjectionMatrix(1));
                    renderingData.cameraData.camera.SetStereoViewMatrix(Camera.StereoscopicEye.Right, renderingData.cameraData.GetViewMatrix(1));

                    CommandBuffer cmd = CommandBufferPool.Get();

                    // Use legacy stereo instancing mode to have legacy XR code path configured
                    cmd.SetSinglePassStereo(SystemInfo.supportsMultiview ? SinglePassStereoMode.Multiview : SinglePassStereoMode.Instancing);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Calling into built-in skybox pass
                    context.DrawSkybox(renderingData.cameraData.camera);

                    // Disable Legacy XR path
                    cmd.SetSinglePassStereo(SinglePassStereoMode.None);
                    context.ExecuteCommandBuffer(cmd);
                    // We do not need to submit here due to special handling of stereo matrices in core.
                    // context.Submit();
                    CommandBufferPool.Release(cmd);

                    renderingData.cameraData.camera.ResetStereoProjectionMatrices();
                    renderingData.cameraData.camera.ResetStereoViewMatrices();
                }
                else
                {
                    renderingData.cameraData.camera.projectionMatrix = renderingData.cameraData.GetProjectionMatrix(0);
                    renderingData.cameraData.camera.worldToCameraMatrix = renderingData.cameraData.GetViewMatrix(0);

                    context.DrawSkybox(renderingData.cameraData.camera);
                    // Submit and execute the skybox pass before resetting the matrices
                    context.Submit();

                    renderingData.cameraData.camera.ResetProjectionMatrix();
                    renderingData.cameraData.camera.ResetWorldToCameraMatrix();
                }
            }
            else
#endif
            {
                if((DebugHandler != null) && DebugHandler.IsActiveForCamera(ref renderingData.cameraData))
                {
                    CommandBuffer cmd = CommandBufferPool.Get();

                    foreach(DebugRenderSetup debugRenderPass in DebugHandler.CreateDebugRenderSetupEnumerable(context, cmd))
                    {
                        // TODO: The skybox needs to work the same as the other shaders, but until it does we'll not render it under certain circumstances...
                        if(!DebugHandler.IsScreenClearNeeded)
                        {
                            context.DrawSkybox(camera);
                        }
                    }

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
    	        else
        	    {
                	context.DrawSkybox(camera);
            	}
            }
        }
    }
}
