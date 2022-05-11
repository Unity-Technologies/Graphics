using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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
        RTHandle m_Source;
        static Material m_BlitMaterial;

        /// <summary>
        /// Creates a new <c>FinalBlitPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="blitMaterial">The <c>Material</c> to use for copying the executing the final blit.</param>
        /// <seealso cref="RenderPassEvent"/>
        public FinalBlitPass(RenderPassEvent evt, Material blitMaterial)
        {
            base.profilingSampler = new ProfilingSampler(nameof(FinalBlitPass));
            base.useNativeRenderPass = false;

            m_BlitMaterial = blitMaterial;
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        [Obsolete("Use RTHandles for colorHandle")]
        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle colorHandle)
        {
            if (m_Source?.nameID != colorHandle.Identifier())
                m_Source = RTHandles.Alloc(colorHandle.Identifier());
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        /// <param name="baseDescriptor"></param>
        /// <param name="colorHandle"></param>
        public void Setup(RenderTextureDescriptor baseDescriptor, RTHandle colorHandle)
        {
            m_Source = colorHandle;
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
            var cmd = renderingData.commandBuffer;

            if (m_Source == cameraData.renderer.GetCameraColorFrontBuffer(cmd))
            {
                m_Source = renderingData.cameraData.renderer.cameraColorTargetHandle;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.FinalBlit)))
            {
                GetActiveDebugHandler(renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref cameraData, true);

                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.LinearToSRGBConversion,
                    cameraData.requireSrgbConversion);

                cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, m_Source.nameID);

                FinalBlitPass.SetupRenderTarget(ref renderingData, cameraTarget);
                FinalBlitPass.ExecutePass(ref renderingData, cameraTarget, m_Source);
#pragma warning disable 0618 // Obsolete usage: RenderTargetIdentifiers required here because of use of RenderTexture cameraData.targetTexture which is not managed by RTHandles
                cameraData.renderer.ConfigureCameraTarget(cameraTarget, cameraTarget);
#pragma warning restore 0618
            }
        }

        private static void SetupRenderTarget(ref RenderingData renderingData, RenderTargetIdentifier cameraTarget)
        {
            var cameraData = renderingData.cameraData;
            var cmd = renderingData.commandBuffer;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    int depthSlice = cameraData.xr.singlePassEnabled ? -1 : cameraData.xr.GetTextureArraySlice();
                    cameraTarget = new RenderTargetIdentifier(cameraData.xr.renderTarget, 0, CubemapFace.Unknown, depthSlice);

                    CoreUtils.SetRenderTarget(
                        cmd,
                        cameraTarget,
                        RenderBufferLoadAction.Load,
                        RenderBufferStoreAction.Store,
                        ClearFlag.None,
                        Color.black);
                }
                else
#endif
                if (cameraData.isSceneViewCamera || cameraData.isDefaultViewport)
                {
                    // This set render target is necessary so we change the LOAD state to DontCare.
                    cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, // color
                        RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare); // depth
                }
                else
                {
                    // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
                    // We need to keep in the pipeline of first render pass to each render target to properly set load/store actions.
                    // meanwhile we set to load so split screen case works.
                    CoreUtils.SetRenderTarget(
                            cmd,
                            cameraTarget,
                            RenderBufferLoadAction.Load,
                            RenderBufferStoreAction.Store,
                            ClearFlag.Depth,
                            Color.black);
                }
        }

        private static void ExecutePass(ref RenderingData renderingData, RenderTargetIdentifier cameraTarget, RTHandle source)
        {
            var cameraData = renderingData.cameraData;
            var cmd = renderingData.commandBuffer;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    int depthSlice = cameraData.xr.singlePassEnabled ? -1 : cameraData.xr.GetTextureArraySlice();
                    cmd.SetViewport(cameraData.pixelRect);

                    // We y-flip if
                    // 1) we are bliting from render texture to back buffer(UV starts at bottom) and
                    // 2) renderTexture starts UV at top
                    bool yflip = SystemInfo.graphicsUVStartsAtTop;
                    Vector4 scaleBias = yflip ? new Vector4(1, -1, 0, 1) : new Vector4(1, 1, 0, 0);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);

                    cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4);
                }
                else
#endif
                if (cameraData.isSceneViewCamera || cameraData.isDefaultViewport)
                {
                    cmd.Blit(source.nameID, cameraTarget, m_BlitMaterial);
                }
                else
                {
                    // TODO: Final blit pass should always blit to backbuffer. The first time we do we don't need to Load contents to tile.
                    // We need to keep in the pipeline of first render pass to each render target to properly set load/store actions.
                    // meanwhile we set to load so split screen case works.

                    Camera camera = cameraData.camera;
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.SetViewport(cameraData.pixelRect);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_BlitMaterial);
                    cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                }
        }

        private class PassData
        {
            internal TextureHandle source;
            internal TextureHandle destination;

            internal int sourceID;

            internal RenderingData renderingData;
        }

        internal void Render(RenderGraph renderGraph, ref RenderingData renderingData, TextureHandle src, TextureHandle dest)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Final Blit", out var passData, base.profilingSampler))
            {
                passData.source = src;
                passData.destination = dest;
                passData.renderingData = renderingData;
                passData.sourceID = ShaderPropertyId.sourceTex;

                builder.UseColorBuffer(passData.destination, 0);
                builder.ReadTexture(passData.source);

                CoreUtils.SetKeyword(renderingData.commandBuffer, ShaderKeywordStrings.LinearToSRGBConversion,
                    renderingData.cameraData.requireSrgbConversion);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    m_BlitMaterial.SetTexture(data.sourceID, data.source);

                    ExecutePass(ref data.renderingData, data.destination, data.source);
                });
            }
        }
    }
}
