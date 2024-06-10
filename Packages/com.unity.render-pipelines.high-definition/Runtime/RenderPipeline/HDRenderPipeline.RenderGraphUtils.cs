using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;


// Resove the ambiguity in the RendererList name (pick the in-engine version)
using RendererList = UnityEngine.Rendering.RendererList;
using RendererListDesc = UnityEngine.Rendering.RendererUtils.RendererListDesc;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        class GenerateMipmapsPassData
        {
            public TextureHandle texture;
        }

        internal static void GenerateMipmaps(RenderGraph renderGraph, TextureHandle texture)
        {
            using (var builder = renderGraph.AddRenderPass<GenerateMipmapsPassData>("Generate Mipmaps", out var passData))
            {
                passData.texture = builder.ReadWriteTexture(texture);

                builder.SetRenderFunc(
                    (GenerateMipmapsPassData data, RenderGraphContext context) =>
                    {
                        RTHandle tex = data.texture;
                        Debug.Assert(tex.rt.autoGenerateMips == false);
                        context.cmd.GenerateMips(tex);
                    });
            }
        }

        class SetGlobalTexturePassData
        {
            public int shaderID;
            public Texture texture;
        }

        internal static void SetGlobalTexture(RenderGraph renderGraph, int shaderID, Texture texture)
        {
            using (var builder = renderGraph.AddRenderPass<SetGlobalTexturePassData>("SetGlobalTexture", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.shaderID = shaderID;
                passData.texture = texture;

                builder.SetRenderFunc(
                    (SetGlobalTexturePassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(data.shaderID, data.texture);
                    });
            }
        }

        class SetGlobalBufferPassData
        {
            public int shaderID;
            public GraphicsBuffer buffer;
        }

        internal static void SetGlobalBuffer(RenderGraph renderGraph, int shaderID, GraphicsBuffer buffer)
        {
            using (var builder = renderGraph.AddRenderPass<SetGlobalBufferPassData>("SetGlobalBuffer", out var passData))
            {
                builder.AllowPassCulling(false);

                passData.shaderID = shaderID;
                passData.buffer = buffer;

                builder.SetRenderFunc(
                    (SetGlobalBufferPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalBuffer(data.shaderID, data.buffer);
                    });
            }
        }

        static internal void DrawOpaqueRendererList(in RenderGraphContext context, in FrameSettings frameSettings, in RendererList rendererList)
        {
            DrawOpaqueRendererList(context.renderContext, context.cmd, frameSettings, rendererList);
        }

        static void DrawTransparentRendererList(in RenderGraphContext context, in FrameSettings frameSettings, RendererList rendererList)
        {
            DrawTransparentRendererList(context.renderContext, context.cmd, frameSettings, rendererList);
        }

        internal static int SampleCountToPassIndex(MSAASamples samples)
        {
            switch (samples)
            {
                case MSAASamples.None:
                    return s_ColorResolve1XPassIndex;
                case MSAASamples.MSAA2x:
                    return s_ColorResolve2XPassIndex;
                case MSAASamples.MSAA4x:
                    return s_ColorResolve4XPassIndex;
                case MSAASamples.MSAA8x:
                    return s_ColorResolve8XPassIndex;
            }
            ;
            return 0;
        }

        bool NeedClearColorBuffer(HDCamera hdCamera)
        {
            if (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Color ||
                // If the luxmeter is enabled, the sky isn't rendered so we clear the background color
                m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter ||
                // If the matcap view is enabled, the sky isn't updated so we clear the background color
                m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera) ||
                // If we want the sky but the sky don't exist, still clear with background color
                (hdCamera.clearColorMode == HDAdditionalCameraData.ClearColorMode.Sky && !m_SkyManager.IsVisualSkyValid(hdCamera)) ||
                // Special handling for Preview we force to clear with background color (i.e black)
                // Note that the sky use in this case is the last one setup. If there is no scene or game, there is no sky use as reflection in the preview
                HDUtils.IsRegularPreviewCamera(hdCamera.camera))
            {
                return true;
            }

            return false;
        }

        Color GetColorBufferClearColor(HDCamera hdCamera)
        {
            Color clearColor = hdCamera.backgroundColorHDR;

            // We set the background color to black when the luxmeter is enabled to avoid picking the sky color
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuxMeter ||
                m_CurrentDebugDisplaySettings.DebugHideSky(hdCamera))
                clearColor = Color.black;

            if (hdCamera.CameraIsSceneFiltering())
                clearColor.a = 0.0f;

            // Get the background color from preferences if preview camera
#if UNITY_EDITOR
            if (HDUtils.IsRegularPreviewCamera(hdCamera.camera) && hdCamera.camera.clearFlags != CameraClearFlags.SolidColor)
            {
                return CoreRenderPipelinePreferences.previewBackgroundColor;
            }
#endif


            return clearColor;
        }

        // XR Specific
        class XRRenderingPassData
        {
            public XRPass xr;
        }

        internal static void StartXRSinglePass(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (hdCamera.xr.enabled)
            {
                using (var builder = renderGraph.AddRenderPass<XRRenderingPassData>("Start XR single-pass", out var passData))
                {
                    passData.xr = hdCamera.xr;

                    builder.SetRenderFunc(
                        (XRRenderingPassData data, RenderGraphContext context) =>
                        {
                            data.xr.StartSinglePass(context.cmd);
                        });
                }
            }
        }

        internal static void StopXRSinglePass(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (hdCamera.xr.enabled)
            {
                using (var builder = renderGraph.AddRenderPass<XRRenderingPassData>("Stop XR single-pass", out var passData))
                {
                    passData.xr = hdCamera.xr;

                    builder.SetRenderFunc(
                        (XRRenderingPassData data, RenderGraphContext context) =>
                        {
                            data.xr.StopSinglePass(context.cmd);
                        });
                }
            }
        }

        class RenderOcclusionMeshesPassData
        {
            public HDCamera hdCamera;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public Color clearColor;
        }

        void RenderXROcclusionMeshes(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (hdCamera.xr.hasValidOcclusionMesh && m_Asset.currentPlatformRenderPipelineSettings.xrSettings.occlusionMesh)
            {
                using (var builder = renderGraph.AddRenderPass<RenderOcclusionMeshesPassData>("XR Occlusion Meshes", out var passData))
                {
                    passData.hdCamera = hdCamera;
                    passData.colorBuffer = builder.WriteTexture(colorBuffer);
                    passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Write);
                    passData.clearColor = GetColorBufferClearColor(hdCamera);

                    builder.SetRenderFunc(
                        (RenderOcclusionMeshesPassData data, RenderGraphContext ctx) =>
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.colorBuffer, data.depthBuffer, ClearFlag.None, data.clearColor, 0, CubemapFace.Unknown, -1);

                            ctx.cmd.SetGlobalVector(HDShaderIDs._ClearColor, data.clearColor);

                            data.hdCamera.xr.RenderOcclusionMesh(ctx.cmd);
                        });
                }
            }
        }

        class BlitCameraTextureData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public float mipLevel;
            public bool bilinear;
        }

        static internal void BlitCameraTexture(RenderGraph renderGraph, TextureHandle source, TextureHandle destination, float mipLevel = 0.0f, bool bilinear = false)
        {
            using (var builder = renderGraph.AddRenderPass<BlitCameraTextureData>("Blit Camera Texture", out var passData))
            {
                passData.source = builder.ReadTexture(source);
                passData.destination = builder.WriteTexture(destination);
                passData.mipLevel = mipLevel;
                passData.bilinear = bilinear;
                builder.SetRenderFunc(
                    (BlitCameraTextureData data, RenderGraphContext ctx) =>
                    {
                        HDUtils.BlitCameraTexture(ctx.cmd, data.source, data.destination, data.mipLevel, data.bilinear);
                    });
            }
        }
    }

    internal struct XRSinglePassScope : System.IDisposable
    {
        readonly RenderGraph m_RenderGraph;
        readonly HDCamera m_HDCamera;

        bool m_Disposed;

        public XRSinglePassScope(RenderGraph renderGraph, HDCamera hdCamera)
        {
            m_RenderGraph = renderGraph;
            m_HDCamera = hdCamera;
            m_Disposed = false;

            HDRenderPipeline.StartXRSinglePass(renderGraph, hdCamera);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        // Protected implementation of Dispose pattern.
        void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            // As this is a struct, it could have been initialized using an empty constructor so we
            // need to make sure `cmd` isn't null to avoid a crash. Switching to a class would fix
            // this but will generate garbage on every frame (and this struct is used quite a lot).
            if (disposing)
            {
                HDRenderPipeline.StopXRSinglePass(m_RenderGraph, m_HDCamera);
            }

            m_Disposed = true;
        }
    }
}
