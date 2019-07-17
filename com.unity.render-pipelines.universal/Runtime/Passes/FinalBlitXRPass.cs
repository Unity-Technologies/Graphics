namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    internal class FinalBlitXRPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "Final Blit XR Pass";
        RenderTargetIdentifier m_Source;
        RenderTargetIdentifier m_Dest;
        RenderTextureDescriptor m_srcDesc;
        RenderTextureDescriptor m_dstDesc;
        int m_Targetslice;

        Material m_BlitMaterial;

        public FinalBlitXRPass(RenderPassEvent evt, Material blitMaterial)
        {
            m_BlitMaterial = blitMaterial;
            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor srcDescriptor, RenderTargetIdentifier srcHandle, RenderTextureDescriptor dstDescriptor, RenderTargetIdentifier dstHandle, int targetslice)
        {
            m_Source = srcHandle;
            m_Dest = dstHandle;
            m_srcDesc = srcDescriptor;
            m_dstDesc = dstDescriptor;
            m_Targetslice = targetslice;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_BlitMaterial, GetType().Name);
                return;
            }

            bool requiresSRGBConvertion = m_dstDesc.sRGB;
            bool killAlpha = renderingData.killAlphaInFinalBlit;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);

            if (requiresSRGBConvertion)
                cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
            else
                cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            if (killAlpha)
                cmd.EnableShaderKeyword(ShaderKeywordStrings.KillAlpha);
            else
                cmd.DisableShaderKeyword(ShaderKeywordStrings.KillAlpha);

            {
                cmd.SetGlobalTexture("_BlitTex", m_Source);

                // Perform y-flip in the final blit pass.
                cmd.SetGlobalVector("_BlitScaleBias", new Vector4(1,-1,0,1));

                if (m_dstDesc.dimension == TextureDimension.Tex2DArray)
                    cmd.SetRenderTarget(m_Dest, 0, CubemapFace.Unknown, m_Targetslice);
                else
                    cmd.SetRenderTarget(m_Dest, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                // Emit 3 vertex draw with empty vbo and ibo. VS will generate full screen triangle
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Triangles, 3, 1);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
