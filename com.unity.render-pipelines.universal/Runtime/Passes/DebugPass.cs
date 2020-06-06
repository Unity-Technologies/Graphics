namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    internal class DebugPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "Debug Pass";
        RenderTargetIdentifier m_Source;
        Material m_BlitMaterial;
        TextureDimension m_TargetDimension;
        bool m_ClearBlitTarget;
        bool m_IsMobileOrSwitch;
        Rect m_PixelRect;
        int m_DebugMode;
        float m_NearPlane; 
        float m_FarPlane;

        public DebugPass(RenderPassEvent evt, Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        /// <param name="clearBlitTarget"></param>
        /// <param name="pixelRect"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetIdentifier colorIdentifier, int debugMode, 
            float nearPlane, float farPlane, bool clearBlitTarget = false, Rect pixelRect = new Rect())
        {
            m_Source = colorIdentifier;
            m_TargetDimension = baseDescriptor.dimension;
            m_ClearBlitTarget = clearBlitTarget;
            m_IsMobileOrSwitch = Application.isMobilePlatform || Application.platform == RuntimePlatform.Switch;
            m_PixelRect = pixelRect;
            m_DebugMode = debugMode;
            m_NearPlane = nearPlane;
            m_FarPlane = farPlane;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_BlitMaterial, GetType().Name);
                return;
            }

            bool requiresSRGBConvertion = Display.main.requiresSrgbBlitToBackbuffer;
            
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            if (requiresSRGBConvertion)
                cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            else
                cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            ref CameraData cameraData = ref renderingData.cameraData;
            {
                cmd.SetGlobalTexture("_BlitTex", m_Source);
                cmd.SetGlobalInt("_DebugMode", m_DebugMode);
                cmd.SetGlobalFloat("_NearPlane", m_NearPlane);
                cmd.SetGlobalFloat("_FarPlane", m_FarPlane);

                CoreUtils.SetRenderTarget(
                    cmd,
                    BuiltinRenderTextureType.CameraTarget,
                    RenderBufferLoadAction.Load,
                    RenderBufferStoreAction.Store,
                    ClearFlag.None,
                    Color.black);

                Camera camera = cameraData.camera;
                cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                cmd.SetViewport(m_PixelRect != Rect.zero ? m_PixelRect : cameraData.camera.pixelRect);
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
