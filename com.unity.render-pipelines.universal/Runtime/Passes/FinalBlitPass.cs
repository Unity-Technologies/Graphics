namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    public class FinalBlitPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "Final Blit Pass";
        RenderTargetHandle m_Source;
        Material m_BlitMaterial;
        TextureDimension m_TargetDimension;

        public FinalBlitPass(RenderPassEvent evt, Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
        {
            m_Source = colorHandle;
            m_TargetDimension = baseDescriptor.dimension;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_BlitMaterial, GetType().Name);
                return;
            }

            // Note: We need to get the cameraData.targetTexture as this will get the targetTexture of the camera stack.
            // Overlay cameras need to output to the target described in the base camera while doing camera stack.
            ref CameraData cameraData = ref renderingData.cameraData;
            RenderTargetIdentifier cameraTarget = (cameraData.targetTexture != null) ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;

            bool requiresSRGBConvertion = Display.main.requiresSrgbBlitToBackbuffer;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            // For stereo case, eye texture always want color data in sRGB space.
            // If eye texture color format is linear, we do explicit sRGB convertion
#if ENABLE_VR && ENABLE_VR_MODULE
            if (cameraData.isStereoEnabled)
                requiresSRGBConvertion = !XRGraphics.eyeTextureDesc.sRGB;
#endif
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            if (requiresSRGBConvertion)
                cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            else
                cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            // Use default blit for XR as we are not sure the UniversalRP blit handles stereo.
            // The blit will be reworked for stereo along the XRSDK work.
            Material blitMaterial = (cameraData.isStereoEnabled) ? null : m_BlitMaterial;
            cmd.SetGlobalTexture("_BlitTex", m_Source.Identifier());
            if (cameraData.isStereoEnabled || isSceneViewCamera || cameraData.isDefaultViewport)
            {
                // This set render target is necessary so we change the LOAD state to DontCare.
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,     // color
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                cmd.Blit(m_Source.Identifier(), cameraTarget, blitMaterial);
            }
            else
            {
                // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
                // We need to keep in the pipeline of first render pass to each render target to propertly set load/store actions.
                // meanwhile we set to load so split screen case works.
                SetRenderTarget(
                    cmd,
                    cameraTarget,
                    RenderBufferLoadAction.Load,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.black,
                    m_TargetDimension);

                Camera camera = cameraData.camera;
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(cameraData.pixelRect);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, blitMaterial);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
