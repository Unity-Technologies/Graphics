using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal partial class PostProcessPass : ScriptableRenderPass
    {
        #region RenderGraph
        public class StopNaNsPassData
        {
            public TextureHandle targeTexture;
            public TextureHandle sourceTexture;
            public RenderingData renderingData;
            public Material stopNaN;
        }

        public void RenderStopNaN(in TextureHandle activeCameraColor, out TextureHandle destination, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var graph = renderingData.renderGraph;

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                cameraTargetDescriptor.graphicsFormat,
                DepthBits.None);

            destination = UniversalRenderer.CreateRenderGraphTexture(graph, desc, "_StopNaNsTarget", true);

            bool useStopNan = cameraData.isStopNaNEnabled && m_Materials.stopNaN != null;
            // Optional NaN killer before post-processing kicks in
            // stopNaN may be null on Adreno 3xx. It doesn't support full shader level 3.5, but SystemInfo.graphicsShaderLevel is 35.
            if (useStopNan)
            {
                using (var builder = graph.AddRenderPass<StopNaNsPassData>("Stop NaNs", out var passData, ProfilingSampler.Get(URPProfileId.StopNaNs)))
                {


                    passData.targeTexture = builder.UseColorBuffer(destination, 0);
                    passData.sourceTexture = builder.ReadTexture(activeCameraColor);
                    passData.renderingData = renderingData;
                    passData.stopNaN = m_Materials.stopNaN;

                    //  TODO RENDERGRAPH: culling? force culluing off for testing
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((StopNaNsPassData data, RenderGraphContext context) =>
                    {
                        var cmd = data.renderingData.commandBuffer;
                        RenderingUtils.Blit(
                            cmd, data.sourceTexture, data.targeTexture, data.stopNaN, 0, data.renderingData.cameraData.xr.enabled,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
                    });

                    return;
                }
            }
        }

        public class SMAAPassData
        {
            public TextureHandle destinationTexture;
            public TextureHandle sourceTexture;
            public TextureHandle blendTexture;
            public RenderingData renderingData;
            public Material material;
        }

        public void RenderSMAA(in TextureHandle source, out TextureHandle destination, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var graph = renderingData.renderGraph;

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.useMipMap = false;
            cameraTargetDescriptor.autoGenerateMips = false;

            var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                cameraTargetDescriptor.graphicsFormat,
                DepthBits.None);
            destination = UniversalRenderer.CreateRenderGraphTexture(graph, desc, "_SMAATarget", true);

            // TODO RENDERGRAPH: look into depth target as stencil buffer case, in RenderGraph, it is not passible to use same RT as both color and depth. That is not supported.
            var edgeTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                m_SMAAEdgeFormat,
                DepthBits.None);
            var edgeTexture = UniversalRenderer.CreateRenderGraphTexture(graph, edgeTextureDesc, "_EdgeTexture", true);

            var blendTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                GraphicsFormat.R8G8B8A8_UNorm,
                DepthBits.None);
            var blendTexture = UniversalRenderer.CreateRenderGraphTexture(graph, blendTextureDesc, "_BlendTexture", true);

            bool useSubPixelMorpAA = cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            // Anti-aliasing
            if (useSubPixelMorpAA)
            {
                var material = m_Materials.subpixelMorphologicalAntialiasing;
                const int kStencilBit = 64;

                // Globals
                material.SetVector(ShaderConstants._Metrics, new Vector4(1f / cameraTargetDescriptor.width, 1f / cameraTargetDescriptor.height, cameraTargetDescriptor.width, cameraTargetDescriptor.height));
                material.SetTexture(ShaderConstants._AreaTexture, m_Data.textures.smaaAreaTex);
                material.SetTexture(ShaderConstants._SearchTexture, m_Data.textures.smaaSearchTex);
                material.SetFloat(ShaderConstants._StencilRef, (float)kStencilBit);
                material.SetFloat(ShaderConstants._StencilMask, (float)kStencilBit);

                // Quality presets
                material.shaderKeywords = null;

                switch (cameraData.antialiasingQuality)
                {
                    case AntialiasingQuality.Low:
                        material.EnableKeyword(ShaderKeywordStrings.SmaaLow);
                        break;
                    case AntialiasingQuality.Medium:
                        material.EnableKeyword(ShaderKeywordStrings.SmaaMedium);
                        break;
                    case AntialiasingQuality.High:
                        material.EnableKeyword(ShaderKeywordStrings.SmaaHigh);
                        break;
                }

                using (var builder = graph.AddRenderPass<SMAAPassData>("SMAA Edge Detection", out var passData, ProfilingSampler.Get(URPProfileId.SMAA)))
                {
                    //passData.destinationTexture = builder.UseDepthBuffer(edgeTexture, DepthAccess.Write);
                    passData.destinationTexture = builder.UseColorBuffer(edgeTexture, 0);

                    passData.sourceTexture = builder.ReadTexture(source);
                    passData.renderingData = renderingData;
                    passData.material = material;

                    //  TODO RENDERGRAPH: culling? force culluing off for testing
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((SMAAPassData data, RenderGraphContext context) =>
                    {
                        var pixelRect = data.renderingData.cameraData.pixelRect;
                        var material = data.material;

                        var cmd = data.renderingData.commandBuffer;

                        // Prepare for manual blit
                        cmd.SetViewport(pixelRect);

                        // Pass 1: Edge detection
                        cmd.ClearRenderTarget(RTClearFlags.ColorStencil, Color.clear, 1.0f, 0);
                        cmd.SetGlobalTexture(ShaderConstants._ColorTexture, passData.sourceTexture);
                        DrawFullscreenMesh(cmd, material, 0, data.renderingData.cameraData.xr.enabled);
                    });
                }

                using (var builder = graph.AddRenderPass<SMAAPassData>("SMAA Blend weights", out var passData, ProfilingSampler.Get(URPProfileId.SMAA)))
                {
                    passData.destinationTexture = builder.UseColorBuffer(blendTexture, 0);
                    //passData.destinationTexture = builder.UseDepthBuffer(edgeTexture, DepthAccess.Read);
                    passData.sourceTexture = builder.ReadTexture(edgeTexture);
                    passData.renderingData = renderingData;
                    passData.material = material;

                    //  TODO RENDERGRAPH: culling? force culluing off for testing
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((SMAAPassData data, RenderGraphContext context) =>
                    {
                        var pixelRect = data.renderingData.cameraData.pixelRect;
                        var material = data.material;

                        var cmd = data.renderingData.commandBuffer;

                        // Prepare for manual blit
                        cmd.SetViewport(pixelRect);

                        // Pass 2: Blend weights
                        cmd.ClearRenderTarget(false, true, Color.clear);
                        cmd.SetGlobalTexture(ShaderConstants._ColorTexture, passData.sourceTexture);
                        DrawFullscreenMesh(cmd, material, 1, data.renderingData.cameraData.xr.enabled);
                    });
                }

                using (var builder = graph.AddRenderPass<SMAAPassData>("SMAA Neighborhood blending", out var passData, ProfilingSampler.Get(URPProfileId.SMAA)))
                {
                    passData.destinationTexture = builder.UseColorBuffer(destination, 0);
                    passData.sourceTexture = builder.ReadTexture(source);
                    passData.blendTexture = builder.ReadTexture(blendTexture);
                    passData.renderingData = renderingData;
                    passData.material = material;

                    //  TODO RENDERGRAPH: culling? force culluing off for testing
                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc((SMAAPassData data, RenderGraphContext context) =>
                    {
                        var pixelRect = data.renderingData.cameraData.pixelRect;
                        var material = data.material;
                        var cmd = data.renderingData.commandBuffer;

                        // Prepare for manual blit
                        cmd.SetViewport(pixelRect);

                        // Pass 3: Neighborhood blending
                        cmd.ClearRenderTarget(false, true, Color.clear);
                        cmd.SetGlobalTexture(ShaderConstants._ColorTexture, passData.sourceTexture);
                        cmd.SetGlobalTexture(ShaderConstants._BlendTexture, passData.blendTexture);
                        DrawFullscreenMesh(cmd, material, 2, data.renderingData.cameraData.xr.enabled);
                    });
                }
            }
        }

        public class DoFPassData
        {
            public TextureHandle cocTexture;
            public TextureHandle colorTexture;
            public TextureHandle sourceTexture;
            public RenderingData renderingData;
            public Material material;
        }

        public void RenderDoF(in TextureHandle source, out TextureHandle destination, ref RenderingData renderingData)
        {
            // TODO RENDERGRAPH: use helper function for setting up member vars
            var stack = VolumeManager.instance.stack;
            m_DepthOfField = stack.GetComponent<DepthOfField>();

            var cameraData = renderingData.cameraData;
            var graph = renderingData.renderGraph;

            var dofMaterial = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian ? m_Materials.gaussianDepthOfField : m_Materials.bokehDepthOfField;
            bool useDepthOfField = m_DepthOfField.IsActive() && !renderingData.cameraData.isSceneViewCamera && dofMaterial != null;

            // TODO RENDERGRAPH: use member variable instead
            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            cameraTargetDescriptor.useMipMap = false;
            cameraTargetDescriptor.autoGenerateMips = false;

            var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                cameraTargetDescriptor.graphicsFormat,
                DepthBits.None);
            destination = UniversalRenderer.CreateRenderGraphTexture(graph, desc, "_DoFTarget", true);

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            if (useDepthOfField)
            {
                var markerName = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian
                    ? URPProfileId.GaussianDepthOfField
                    : URPProfileId.BokehDepthOfField;

                if (m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian)
                {
                    int downSample = 2;
                    var material = dofMaterial;
                    int wh = cameraTargetDescriptor.width / downSample;
                    int hh = cameraTargetDescriptor.height / downSample;
                    float farStart = m_DepthOfField.gaussianStart.value;
                    float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianEnd.value);
                    var cmd = renderingData.commandBuffer;

                    // Assumes a radius of 1 is 1 at 1080p
                    // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
                    // very high resolutions (4K+).
                    float maxRadius = m_DepthOfField.gaussianMaxRadius.value * (wh / 1080f);
                    maxRadius = Mathf.Min(maxRadius, 2f);

                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.HighQualitySampling, m_DepthOfField.highQualitySampling.value);
                    material.SetVector(ShaderConstants._CoCParams, new Vector3(farStart, farEnd, maxRadius));

                    // Temporary textures
                    var fullCoCTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, cameraTargetDescriptor.width, cameraTargetDescriptor.height, m_GaussianCoCFormat);
                    var fullCoCTexture = UniversalRenderer.CreateRenderGraphTexture(graph, fullCoCTextureDesc, "_FullCoCTexture", true);
                    var halfCoCTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, wh, hh, m_GaussianCoCFormat);
                    var halfCoCTexture = UniversalRenderer.CreateRenderGraphTexture(graph, halfCoCTextureDesc, "_HalfCoCTexture", true);
                    var pingTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, wh, hh, m_DefaultHDRFormat);
                    var pingTexture = UniversalRenderer.CreateRenderGraphTexture(graph, pingTextureDesc, "_PingTexture", true);
                    var pongTextureDesc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor, wh, hh, m_DefaultHDRFormat);
                    var pongTexture = UniversalRenderer.CreateRenderGraphTexture(graph, pongTextureDesc, "_PongTexture", true);

                    // TODO RENDERGRAPH: this line is for postFX dynamic resolution without RTHandle, we should consider remove this line in favor of RTHandles
                    PostProcessUtils.SetSourceSize(cmd, cameraTargetDescriptor);
                    cmd.SetGlobalVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / downSample, 1.0f / downSample, downSample, downSample));

                    using (var builder = graph.AddRenderPass<DoFPassData>("Depth of Field - Compute CoC", out var passData, ProfilingSampler.Get(markerName)))
                    {
                        builder.UseColorBuffer(fullCoCTexture, 0);
                        passData.sourceTexture = builder.ReadTexture(source);
                        passData.renderingData = renderingData;
                        passData.material = material;

                        //  TODO RENDERGRAPH: culling? force culluing off for testing
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((DoFPassData data, RenderGraphContext context) =>
                        {
                            var material = data.material;
                            var cmd = data.renderingData.commandBuffer;

                            // Compute CoC
                            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, data.sourceTexture);
                            DrawFullscreenMesh(cmd, material, 0, data.renderingData.cameraData.xr.enabled);
                        });
                    }

                    using (var builder = graph.AddRenderPass<DoFPassData>("Depth of Field - Downscale & Prefilter Color + CoC", out var passData, ProfilingSampler.Get(markerName)))
                    {
                        builder.UseColorBuffer(halfCoCTexture, 0);
                        builder.UseColorBuffer(pingTexture, 1);
                        // TODO RENDERGRAPH: investigate - Setting MRTs without a depth buffer is not supported.
                        builder.UseDepthBuffer(halfCoCTexture, DepthAccess.ReadWrite);

                        passData.sourceTexture = builder.ReadTexture(source);
                        passData.cocTexture = builder.ReadTexture(fullCoCTexture);

                        passData.renderingData = renderingData;
                        passData.material = material;

                        //  TODO RENDERGRAPH: culling? force culluing off for testing
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((DoFPassData data, RenderGraphContext context) =>
                        {
                            var material = data.material;
                            var cmd = data.renderingData.commandBuffer;
                            var pixelRect = data.renderingData.cameraData.pixelRect;

                            // Downscale & prefilter color + coc
                            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                            //  TODO RENDERGRAPH: figure out why setViewport will break rendering, looks like cmd.SetRenderTarget in non RG pass calls setViewport implicitly
                            //cmd.SetViewport(pixelRect);
                            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, data.sourceTexture);
                            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, data.cocTexture);
                            DrawFullscreenMesh(cmd, material, 1, data.renderingData.cameraData.xr.enabled);
                            cmd.SetViewProjectionMatrices(data.renderingData.cameraData.camera.worldToCameraMatrix, data.renderingData.cameraData.camera.projectionMatrix);
                        });
                    }


                    using (var builder = graph.AddRenderPass<DoFPassData>("Depth of Field - Blur H", out var passData, ProfilingSampler.Get(markerName)))
                    {
                        builder.UseColorBuffer(pongTexture, 0);
                        passData.sourceTexture = builder.ReadTexture(pingTexture);
                        passData.cocTexture = builder.ReadTexture(halfCoCTexture);

                        passData.renderingData = renderingData;
                        passData.material = material;

                        //  TODO RENDERGRAPH: culling? force culluing off for testing
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((DoFPassData data, RenderGraphContext context) =>
                        {
                            var material = data.material;
                            var cmd = data.renderingData.commandBuffer;

                            // Downscale & prefilter color + coc
                            cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, data.cocTexture);
                            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, data.sourceTexture);
                            DrawFullscreenMesh(cmd, material, 2, data.renderingData.cameraData.xr.enabled);
                        });
                    }

                    using (var builder = graph.AddRenderPass<DoFPassData>("Depth of Field - Blur V", out var passData, ProfilingSampler.Get(markerName)))
                    {
                        builder.UseColorBuffer(pingTexture, 0);
                        passData.sourceTexture = builder.ReadTexture(pongTexture);
                        passData.cocTexture = builder.ReadTexture(halfCoCTexture);

                        passData.renderingData = renderingData;
                        passData.material = material;

                        //  TODO RENDERGRAPH: culling? force culluing off for testing
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((DoFPassData data, RenderGraphContext context) =>
                        {
                            var material = data.material;
                            var cmd = data.renderingData.commandBuffer;

                            // Downscale & prefilter color + coc
                            cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, data.cocTexture);
                            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, data.sourceTexture);
                            DrawFullscreenMesh(cmd, material, 3, data.renderingData.cameraData.xr.enabled);
                        });
                    }

                    using (var builder = graph.AddRenderPass<DoFPassData>("Depth of Field - Composite", out var passData, ProfilingSampler.Get(markerName)))
                    {
                        builder.UseColorBuffer(destination, 0);
                        passData.sourceTexture = builder.ReadTexture(source);
                        passData.cocTexture = builder.ReadTexture(fullCoCTexture);
                        passData.colorTexture = builder.ReadTexture(pingTexture);

                        passData.renderingData = renderingData;
                        passData.material = material;

                        //  TODO RENDERGRAPH: culling? force culluing off for testing
                        builder.AllowPassCulling(false);

                        builder.SetRenderFunc((DoFPassData data, RenderGraphContext context) =>
                        {
                            var material = data.material;
                            var cmd = data.renderingData.commandBuffer;

                            // Downscale & prefilter color + coc
                            cmd.SetGlobalTexture(ShaderConstants._ColorTexture, data.colorTexture);
                            cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, data.cocTexture);
                            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, data.sourceTexture);
                            DrawFullscreenMesh(cmd, material, 4, data.renderingData.cameraData.xr.enabled);
                        });
                    }
                }
                else if (m_DepthOfField.mode.value == DepthOfFieldMode.Bokeh)
                {
                    // DoBokehDepthOfField(cmd, source, destination, pixelRect);
                }
            }
        }
        #endregion
    }
}
