using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given depth buffer into the given destination depth buffer.
    ///
    /// You can use this pass to copy a depth buffer to a destination,
    /// so you can use it later in rendering. If the source texture has MSAA
    /// enabled, the pass uses a custom MSAA resolve. If the source texture
    /// does not have MSAA enabled, the pass uses a Blit or a Copy Texture
    /// operation, depending on what the current platform supports.
    /// </summary>
    public class CopyDepthPass : ScriptableRenderPass
    {
        private RTHandle source { get; set; }
        private RTHandle destination { get; set; }
        internal int MssaSamples { get; set; }
        // In some cases (Scene view, XR and etc.) we actually want to output to depth buffer
        // So this variable needs to be set to true to enable the correct copy shader semantic
        internal bool CopyToDepth { get; set; }
        Material m_CopyDepthMaterial;

        internal bool m_CopyResolvedDepth;
        internal bool m_ShouldClear;

        public CopyDepthPass(RenderPassEvent evt, Material copyDepthMaterial, bool shouldClear = false)
        {
            base.profilingSampler = new ProfilingSampler(nameof(CopyDepthPass));
            CopyToDepth = false;
            m_CopyDepthMaterial = copyDepthMaterial;
            renderPassEvent = evt;
            m_CopyResolvedDepth = false;
            m_ShouldClear = shouldClear;
        }

        /// <summary>
        /// Configure the pass with the source and destination to execute on.
        /// </summary>
        /// <param name="source">Source Render Target</param>
        /// <param name="destination">Destination Render Target</param>
        public void Setup(RTHandle source, RTHandle destination)
        {
            this.source = source;
            this.destination = destination;
            this.MssaSamples = -1;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            var isDepth = (destination.rt && destination.rt.graphicsFormat == GraphicsFormat.None);
            descriptor.graphicsFormat = isDepth ? GraphicsFormat.D32_SFloat_S8_UInt : GraphicsFormat.R32_SFloat;
            descriptor.msaaSamples = 1;
#if UNITY_EDITOR
            ConfigureTarget(destination, destination, GraphicsFormat.R32_SFloat, descriptor.width, descriptor.height, descriptor.msaaSamples);
#else
            ConfigureTarget(destination, descriptor.graphicsFormat, descriptor.width, descriptor.height, descriptor.msaaSamples, isDepth);
#endif
            if (m_ShouldClear)
                ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CopyDepthMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_CopyDepthMaterial, GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.CopyDepth)))
            {
                int cameraSamples = 0;
                if (MssaSamples == -1)
                {
                    RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
                    cameraSamples = descriptor.msaaSamples;
                }
                else
                    cameraSamples = MssaSamples;

                // When depth resolve is supported or multisampled texture is not supported, set camera samples to 1
                if (SystemInfo.supportsMultisampledTextures == 0 || m_CopyResolvedDepth)
                    cameraSamples = 1;

                CameraData cameraData = renderingData.cameraData;

                switch (cameraSamples)
                {
                    case 8:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 4:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    case 2:
                        cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;

                    // MSAA disabled, auto resolve supported or ms textures not supported
                    default:
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                        cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                        break;
                }

                if (CopyToDepth || destination.rt.graphicsFormat == GraphicsFormat.None)
                    cmd.EnableShaderKeyword("_OUTPUT_DEPTH");
                else
                    cmd.DisableShaderKeyword("_OUTPUT_DEPTH");

                cmd.SetGlobalTexture("_CameraDepthAttachment", source.nameID);


#if ENABLE_VR && ENABLE_XR_MODULE
                // XR uses procedural draw instead of cmd.blit or cmd.DrawFullScreenMesh
                if (renderingData.cameraData.xr.enabled)
                {
                    // XR flip logic is not the same as non-XR case because XR uses draw procedure
                    // and draw procedure does not need to take projection matrix yflip into account
                    // We y-flip if
                    // 1) we are bliting from render texture to back buffer and
                    // 2) renderTexture starts UV at top
                    // XRTODO: handle scalebias and scalebiasRt for src and dst separately
                    bool isRenderToBackBufferTarget = destination.nameID == cameraData.xr.renderTarget && !cameraData.xr.renderTargetIsRenderTexture;
                    bool yflip = isRenderToBackBufferTarget && SystemInfo.graphicsUVStartsAtTop;
                    float flipSign = (yflip) ? -1.0f : 1.0f;
                    Vector4 scaleBiasRt = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);

                    cmd.DrawProcedural(Matrix4x4.identity, m_CopyDepthMaterial, 0, MeshTopology.Quads, 4);
                }
                else
#endif
                {
                    // Blit has logic to flip projection matrix when rendering to render texture.
                    // Currently the y-flip is handled in CopyDepthPass.hlsl by checking _ProjectionParams.x
                    // If you replace this Blit with a Draw* that sets projection matrix double check
                    // to also update shader.
                    // scaleBias.x = flipSign
                    // scaleBias.y = scale
                    // scaleBias.z = bias
                    // scaleBias.w = unused
                    // In game view final target acts as back buffer were target is not flipped
                    bool isGameViewFinalTarget = (cameraData.cameraType == CameraType.Game && destination.nameID == k_CameraTarget.nameID);
                    bool yflip = (cameraData.IsCameraProjectionMatrixFlipped()) && !isGameViewFinalTarget;
                    float flipSign = yflip ? -1.0f : 1.0f;
                    Vector4 scaleBiasRt = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                    if (isGameViewFinalTarget)
                        cmd.SetViewport(cameraData.pixelRect);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_CopyDepthMaterial);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            destination = k_CameraTarget;
        }
    }
}
