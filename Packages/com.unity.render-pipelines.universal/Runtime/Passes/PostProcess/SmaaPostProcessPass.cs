using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class SmaaPostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const string k_TargetName = "_SMAATarget";

        Material m_Material;
        bool m_IsValid;

        // Static resource textures for SMAA technique
        Texture2D m_AreaTexture;
        Texture2D m_SearchTexture;

        Experimental.Rendering.GraphicsFormat m_SMAAEdgeFormat;

        // Settings
        public AntialiasingQuality antialiasingQuality { get; set; }

        // Input
        public TextureHandle sourceTexture { get; set; }

        // Output
        public TextureHandle destinationTexture { get; set; }

        public SmaaPostProcessPass(Shader shader, Texture2D smaaAreaTexture, Texture2D smaaSearchTexture)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;

            m_AreaTexture = smaaAreaTexture;
            m_SearchTexture = smaaSearchTexture;

            // SMAA
            // Only two components are needed for edge render texture, but on some vendors four components may be faster.
            if (SystemInfo.IsFormatSupported(Experimental.Rendering.GraphicsFormat.R8G8_UNorm, Experimental.Rendering.GraphicsFormatUsage.Render) && SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("arm"))
                m_SMAAEdgeFormat = Experimental.Rendering.GraphicsFormat.R8G8_UNorm;
            else
                m_SMAAEdgeFormat = Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        private class SMAASetupPassData
        {
            internal Vector4 metrics;
            internal Texture2D areaTexture;
            internal Texture2D searchTexture;
            internal float stencilRef;
            internal float stencilMask;
            internal AntialiasingQuality antialiasingQuality;
            internal Material material;
            internal TextureHandle sourceTexture;
        }

        private class SMAAPassData
        {
            internal TextureHandle sourceTexture;
            internal TextureHandle blendTexture;
            internal Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), "Source texture must be set for SmaaPostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), "Destination texture must be set for SmaaPostProcessPass.");

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var destDesc = renderGraph.GetTextureDesc(sourceTexture);

            destDesc.clearColor = Color.black;
            destDesc.clearColor.a = 0.0f;

            var edgeTextureDesc = destDesc;
            edgeTextureDesc.format = m_SMAAEdgeFormat;
            var edgeTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, edgeTextureDesc, "_EdgeStencilTexture", true, FilterMode.Bilinear);

            var edgeTextureStencilDesc = destDesc;
            edgeTextureStencilDesc.format = Experimental.Rendering.GraphicsFormatUtility.GetDepthStencilFormat(24);
            var edgeTextureStencil = PostProcessUtils.CreateCompatibleTexture(renderGraph, edgeTextureStencilDesc, "_EdgeTexture", true, FilterMode.Bilinear);

            var blendTextureDesc = destDesc;
            blendTextureDesc.format = Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
            var blendTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, blendTextureDesc, "_BlendTexture", true, FilterMode.Point);

            using (var builder = renderGraph.AddRasterRenderPass<SMAASetupPassData>("SMAA Edge Detection", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAAEdgeDetection)))
            {
                // Material setup
                const int kStencilBit = 64;
                // TODO RENDERGRAPH: handle dynamic scaling
                passData.metrics = new Vector4(1f / destDesc.width, 1f / destDesc.height, destDesc.width, destDesc.height);

                // TODO: These are constant for every frame
                passData.areaTexture = m_AreaTexture;
                passData.searchTexture = m_SearchTexture;
                passData.stencilRef = (float)kStencilBit;
                passData.stencilMask = (float)kStencilBit;

                passData.antialiasingQuality = antialiasingQuality;
                passData.material = m_Material;

                builder.SetRenderAttachment(edgeTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(edgeTextureStencil, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepth ,AccessFlags.Read);

                builder.SetRenderFunc(static (SMAASetupPassData data, RasterGraphContext context) =>
                {
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 0: Material Setup
                    SetupMaterial(data);

                    // Pass 1: Edge detection
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, ShaderPass.k_EdgeDetection);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<SMAAPassData>("SMAA Blend weights", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAABlendWeight)))
            {
                builder.SetRenderAttachment(blendTexture, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(edgeTextureStencil, AccessFlags.Read);
                passData.sourceTexture = edgeTexture;
                builder.UseTexture(edgeTexture, AccessFlags.Read);
                passData.material = m_Material;

                builder.SetRenderFunc(static (SMAAPassData data, RasterGraphContext context) =>
                {
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 2: Blend weights
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, ShaderPass.k_BlendWeightsCalculation);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<SMAAPassData>("SMAA Neighborhood blending", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAANeighborhoodBlend)))
            {
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                passData.blendTexture = blendTexture;
                builder.UseTexture(blendTexture, AccessFlags.Read);
                passData.material = m_Material;

                builder.SetRenderFunc(static (SMAAPassData data, RasterGraphContext context) =>
                {
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 3: Neighborhood blending
                    SMAAMaterial.SetTexture(ShaderConstants._BlendTexture, data.blendTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, ShaderPass.k_NeighborhoodBlending);
                });
            }
        }

        static void SetupMaterial(SMAASetupPassData data)
        {
            using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_SMAAMaterialSetup)))
            {
                // Common for all passes
                data.material.SetVector(ShaderConstants._Metrics, data.metrics);
                data.material.SetTexture(ShaderConstants._AreaTexture, data.areaTexture);
                data.material.SetTexture(ShaderConstants._SearchTexture, data.searchTexture);
                data.material.SetFloat(ShaderConstants._StencilRef, data.stencilRef);
                data.material.SetFloat(ShaderConstants._StencilMask, data.stencilMask);

                // Quality presets
                data.material.shaderKeywords = null;

                switch (data.antialiasingQuality)
                {
                    case AntialiasingQuality.Low:
                        data.material.EnableKeyword(ShaderKeywordStrings.SmaaLow);
                        break;
                    case AntialiasingQuality.Medium:
                        data.material.EnableKeyword(ShaderKeywordStrings.SmaaMedium);
                        break;
                    case AntialiasingQuality.High:
                        data.material.EnableKeyword(ShaderKeywordStrings.SmaaHigh);
                        break;
                }
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");
            //public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");
        }

        public static class ShaderPass
        {
            public const int k_EdgeDetection = 0;
            public const int k_BlendWeightsCalculation = 1;
            public const int k_NeighborhoodBlending = 2;
        }
    }
}
