using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class BloomPostProcessPass : PostProcessPass
    {
        public const int k_MaxPyramidSize = 16;

        Material m_Material;
        Material[] m_MaterialPyramid;

        // Cached bloom params from previous frame to avoid unnecessary material updates
        MaterialParams m_PrevParams;

        BloomMipPyramid m_MipPyramid;

        bool m_IsValid;

        public BloomMipPyramid mipPyramid => m_MipPyramid;

        const string k_PassNameKawase = "Blit Bloom Mipmaps (Kawase)";
        const string k_PassNameDual = "Blit Bloom Mipmaps (Dual)";
        ProfilingSampler m_ProfilingSamplerKawase;
        ProfilingSampler m_ProfilingSamplerDual;

        public BloomPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit Bloom Mipmaps");
            m_ProfilingSamplerKawase = new ProfilingSampler(k_PassNameKawase);
            m_ProfilingSamplerDual = new ProfilingSampler(k_PassNameDual);

            m_Material = PostProcessUtils.LoadShader(shader, passName);

            // Array for bloom pyramid materials.
            // Materials are references in the command buffer, so we need a separate material for each mip level.
            m_MaterialPyramid = new Material[k_MaxPyramidSize];
            for (uint i = 0; i < k_MaxPyramidSize; ++i)
                m_MaterialPyramid[i] = PostProcessUtils.LoadShader(shader, passName);

            // Check if the pass init was successful.
            m_IsValid = m_Material != null;
            for(int i = 0; i < k_MaxPyramidSize; i++)
                m_IsValid &= m_MaterialPyramid[i] != null;

            m_MipPyramid = new BloomMipPyramid(k_MaxPyramidSize);
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            for(int i = 0; i < k_MaxPyramidSize; i++)
                CoreUtils.Destroy(m_MaterialPyramid[i]);
        }

        private class BloomPassData
        {
            internal Material material;
            internal Material[] mipMaterials;

            internal TextureHandle sourceTexture;

            internal BloomMipPyramid mipPyramid;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            var bloom = volumeStack.GetComponent<Bloom>();
            if (!bloom.IsActive())
                return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var sourceTexture = resourceData.cameraColor;
            var sourceDesc = sourceTexture.GetDescriptor(renderGraph);

            // Setup
            // Materials are set up beforehand.
            // We rely on the fact that they're private and separate for each blit.
            // They should remain unchanged between graph build and execution.
            using(new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_BloomSetup)))
            {
                m_MipPyramid.Update(renderGraph, bloom, in sourceDesc);
                int mipCount = m_MipPyramid.mipCount;

                // Pre-filtering parameters
                float clamp = bloom.clamp.value;
                float threshold = Mathf.GammaToLinearSpace(bloom.threshold.value);
                float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

                // Material setup
                float scatter = Mathf.Lerp(0.05f, 0.95f, bloom.scatter.value);   // Blend factor between low/hi mip on upsample.
                float kawaseScatter = Mathf.Clamp01(bloom.scatter.value);          // Blend factor between linear and blurred sample. 1.0 for strict Kawase blur.
                float dualScatter = Mathf.Lerp(0.3f, 1.3f, bloom.scatter.value); // Dual upsample filter scale. Scatter default == 0.7 --> 1.0 filter scale.

                MaterialParams newParams = new MaterialParams();
                newParams.parameters = new Vector4(scatter, clamp, threshold, thresholdKnee);
                newParams.parameters2 = new Vector4(0.5f, kawaseScatter, dualScatter, 0.5f * dualScatter);
                newParams.bloomFilter = bloom.filter.value;
                newParams.highQualityFiltering = bloom.highQualityFiltering.value;
                newParams.enableAlphaOutput = cameraData.isAlphaOutputEnabled;

                // Setting keywords can be somewhat expensive on low-end platforms.
                // Previous params are cached to avoid setting the same keywords every frame.
                bool paramsDirty = !m_PrevParams.Equals(ref newParams);
                bool isParamsPropertySet = m_Material.HasProperty(ShaderConstants._Params);
                if (paramsDirty || !isParamsPropertySet)
                {
                    m_Material.SetVector(ShaderConstants._Params, newParams.parameters);
                    m_Material.SetVector(ShaderConstants._Params2, newParams.parameters2);
                    CoreUtils.SetKeyword(m_Material, ShaderKeywordStrings.BloomHQ, newParams.highQualityFiltering);
                    CoreUtils.SetKeyword(m_Material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, newParams.enableAlphaOutput);

                    // These materials are duplicate just to allow different bloom blits to use different textures.
                    for (uint i = 0; i < k_MaxPyramidSize; ++i)
                    {
                        var materialPyramid = m_MaterialPyramid[i];
                        materialPyramid.SetVector(ShaderConstants._Params, newParams.parameters);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings.BloomHQ, newParams.highQualityFiltering);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, newParams.enableAlphaOutput);

                        // TODO: investigate suggested quality improvement trick in more detail:
                        // Kawase5: 0, 1, 2, 2, 3
                        // Kawase9: 0, 1, 2, 3, 4, 4, 5, 6, 7
                        // ? -> KawaseN: duplicate pass at N/2 (See, Bandwidth-Efficient Rendering, siggraph2015)
                        float kawaseDist = 0.5f + ((i > mipCount / 2) ? (i - 1) : i);

                        Vector4 params2 = newParams.parameters2;
                        params2.x = kawaseDist;
                        materialPyramid.SetVector(ShaderConstants._Params2, params2);
                    }

                    m_PrevParams = newParams;
                }
            }

            switch (bloom.filter.value)
            {
                case BloomFilterMode.Dual:
                    resourceData.bloom = BloomDual(renderGraph, sourceTexture);
                break;
                case BloomFilterMode.Kawase:
                    resourceData.bloom = BloomKawase(renderGraph, sourceTexture);
                break;
                case BloomFilterMode.Gaussian: goto default;
                default:
                    resourceData.bloom = BloomGaussian(renderGraph, sourceTexture);
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Vector2Int CalcBloomResolution(Bloom bloom, in TextureDesc bloomSourceDesc)
        {
            // Start at half-res
            int downres = 1;
            switch (bloom.downscale.value)
            {
                case BloomDownscaleMode.Half:
                    downres = 1;
                    break;
                case BloomDownscaleMode.Quarter:
                    downres = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //We should set the limit the downres result to ensure we dont turn 1x1 textures, which should technically be valid
            //into 0x0 textures which will be invalid
            int tw = Mathf.Max(1, bloomSourceDesc.width >> downres);
            int th = Mathf.Max(1, bloomSourceDesc.height >> downres);

            return new Vector2Int(tw, th);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int CalcBloomMipCount(Bloom bloom, in Vector2Int bloomResolution)
        {
            // Determine the iteration count
            int maxSize = Mathf.Max(bloomResolution.x, bloomResolution.y);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, bloom.maxIterations.value);
            return mipCount;
        }

        TextureHandle BloomGaussian(RenderGraph renderGraph, in TextureHandle source)
        {
            using (var builder = renderGraph.AddUnsafePass<BloomPassData>(passName, out var passData, profilingSampler))
            {
                passData.sourceTexture = source;
                passData.material = m_Material;
                passData.mipMaterials = m_MaterialPyramid;
                passData.mipPyramid = m_MipPyramid;

                int mipCount = m_MipPyramid.mipCount;

                builder.UseTexture(source, AccessFlags.Read);
                for (int i = 0; i < mipCount; i++)
                {
                    builder.UseTexture(m_MipPyramid.mipDownTextures[i], AccessFlags.ReadWrite);
                    builder.UseTexture(m_MipPyramid.mipUpTextures[i], AccessFlags.ReadWrite);
                }

                builder.SetRenderFunc(static (BloomPassData data, UnsafeGraphContext context) =>
                {
                    // TODO: can't call BlitTexture with unsafe command buffer
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var material = data.material;
                    int mipCount = data.mipPyramid.mipCount;
                    TextureHandle[] mipDownTextures = data.mipPyramid.mipDownTextures;
                    TextureHandle[] mipUpTextures = data.mipPyramid.mipUpTextures;

                    var loadAction = RenderBufferLoadAction.DontCare; // Blit - always write all pixels
                    var storeAction = RenderBufferStoreAction.Store; // Blit - always read by then next Blit

                    // Prefilter
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, mipDownTextures[0], loadAction,
                            storeAction, material, ShaderPass.k_Prefilter);
                    }

                    // Downsample - gaussian pyramid
                    // Classic two pass gaussian blur - use mipUp as a temporary target
                    //   First pass does 2x downsampling + 9-tap gaussian
                    //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        TextureHandle lastDown = mipDownTextures[0];
                        for (int i = 1; i < mipCount; i++)
                        {
                            TextureHandle mipDown = mipDownTextures[i];
                            TextureHandle mipUp = mipUpTextures[i];

                            Blitter.BlitCameraTexture(cmd, lastDown, mipUp, loadAction, storeAction, material, ShaderPass.k_BlurHorizontal);
                            Blitter.BlitCameraTexture(cmd, mipUp, mipDown, loadAction, storeAction, material, ShaderPass.k_BlurVertical);

                            lastDown = mipDown;
                        }
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomUpsample)))
                    {
                        // Upsample (bilinear by default, HQ filtering does bicubic instead
                        for (int i = mipCount - 2; i >= 0; i--)
                        {
                            TextureHandle lowMip =
                                (i == mipCount - 2) ? mipDownTextures[i + 1] : mipUpTextures[i + 1];
                            TextureHandle highMip = mipDownTextures[i];
                            TextureHandle dst = mipUpTextures[i];

                            // We need a separate material for each upsample pass because setting the low texture mip source
                            // gets overriden by the time the render func is executed.
                            // Material is a reference, so all the blits would share the same material state in the cmdbuf.
                            // NOTE: another option would be to use cmd.SetGlobalTexture().
                            var upMaterial = data.mipMaterials[i];
                            upMaterial.SetTexture(ShaderConstants._SourceTexLowMip, lowMip);

                            Blitter.BlitCameraTexture(cmd, highMip, dst, loadAction, storeAction, upMaterial, ShaderPass.k_Upsample);
                        }
                    }
                });

                // 1st mip is the prefilter.
                m_MipPyramid.resultTexture = mipCount == 1 ? m_MipPyramid.mipDownTextures[0] : m_MipPyramid.mipUpTextures[0];
                return m_MipPyramid.resultTexture;
            }
        }

        TextureHandle BloomKawase(RenderGraph renderGraph, in TextureHandle source)
        {
            using (var builder = renderGraph.AddUnsafePass<BloomPassData>(k_PassNameKawase, out var passData, m_ProfilingSamplerKawase))
            {
                passData.sourceTexture = source;
                passData.material = m_Material;
                passData.mipMaterials = m_MaterialPyramid;
                passData.mipPyramid = m_MipPyramid;

                int mipCount = m_MipPyramid.mipCount;

                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(m_MipPyramid.mipDownTextures[0], AccessFlags.ReadWrite);
                builder.UseTexture(m_MipPyramid.mipUpTextures[0], AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (BloomPassData data, UnsafeGraphContext context) =>
                {
                    // TODO: can't call BlitTexture with unsafe command buffer
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var material = data.material;
                    int mipCount = data.mipPyramid.mipCount;
                    TextureHandle[] mipDownTextures = data.mipPyramid.mipDownTextures;
                    TextureHandle[] mipUpTextures = data.mipPyramid.mipUpTextures;

                    var loadAction = RenderBufferLoadAction.DontCare;   // Blit - always write all pixels
                    var storeAction = RenderBufferStoreAction.Store;    // Blit - always read by then next Blit

                    // Prefilter
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, mipDownTextures[0], loadAction, storeAction, material, ShaderPass.k_Prefilter);
                    }

                    // Kawase blur passes
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        for (int i = 0; i < mipCount; i++)
                        {
                            TextureHandle src = ((i & 1) == 0) ? mipDownTextures[0] : mipUpTextures[0];
                            TextureHandle dst = ((i & 1) == 0) ? mipUpTextures[0] : mipDownTextures[0];
                            Material mat = data.mipMaterials[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, mat, ShaderPass.k_Kawase);
                        }
                    }
                });

                m_MipPyramid.resultTexture = (((mipCount - 1) & 1) == 0) ? m_MipPyramid.mipUpTextures[0] : m_MipPyramid.mipDownTextures[0];
                return m_MipPyramid.resultTexture;
            }
        }


        //  Dual Filter, Bandwidth-Efficient Rendering, siggraph2015
        TextureHandle BloomDual(RenderGraph renderGraph, in TextureHandle source)
        {
            using (var builder = renderGraph.AddUnsafePass<BloomPassData>(k_PassNameDual, out var passData, m_ProfilingSamplerDual))
            {
                passData.sourceTexture = source;
                passData.material = m_Material;
                passData.mipMaterials = m_MaterialPyramid;
                passData.mipPyramid = m_MipPyramid;

                int mipCount = m_MipPyramid.mipCount;

                builder.UseTexture(source, AccessFlags.Read);
                for (int i = 0; i < mipCount; i++)
                {
                    builder.UseTexture(m_MipPyramid.mipDownTextures[i], AccessFlags.ReadWrite);
                    builder.UseTexture(m_MipPyramid.mipUpTextures[i], AccessFlags.ReadWrite);
                }

                builder.SetRenderFunc(static (BloomPassData data, UnsafeGraphContext context) =>
                {
                    // TODO: can't call BlitTexture with unsafe command buffer
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var material = data.material;
                    int mipCount = data.mipPyramid.mipCount;
                    TextureHandle[] mipDownTextures = data.mipPyramid.mipDownTextures;
                    TextureHandle[] mipUpTextures = data.mipPyramid.mipUpTextures;

                    var loadAction = RenderBufferLoadAction.DontCare;   // Blit - always write all pixels
                    var storeAction = RenderBufferStoreAction.Store;    // Blit - always read by then next Blit

                    // Prefilter
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, mipDownTextures[0], loadAction, storeAction, material, ShaderPass.k_Prefilter);
                    }

                    // ARM: Bandwidth-Efficient Rendering, siggraph2015
                    // Downsample - dual pyramid, fixed Kawase0 blur on shrinking targets.
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        TextureHandle lastDown = mipDownTextures[0];
                        for (int i = 1; i < mipCount; i++)
                        {
                            TextureHandle src = mipDownTextures[i - 1];
                            TextureHandle dst = mipDownTextures[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, material, ShaderPass.k_DualDownsample);
                        }
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomUpsample)))
                    {
                        for (int i = mipCount - 2; i >= 0; i--)
                        {
                            TextureHandle src = (i == mipCount - 2) ? mipDownTextures[i + 1] : mipUpTextures[i + 1];
                            TextureHandle dst = mipUpTextures[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, material, ShaderPass.k_DualUpsample);
                        }
                    }
                });
                // 1st mip is the prefilter.
                m_MipPyramid.resultTexture = mipCount == 1 ? m_MipPyramid.mipDownTextures[0] : m_MipPyramid.mipUpTextures[0];
                return m_MipPyramid.resultTexture;
            }
        }

        // Helper class to communicate bloom mip results with external passes in a consistent way.
        public class BloomMipPyramid
        {
            int m_MipCount;

            TextureHandle[] m_MipDownPyramidTextures;
            TextureHandle[] m_MipUpPyramidTextures;

            string[] m_MipDownPyramidNames;
            string[] m_MipUpPyramidNames;

            BloomFilterMode m_BloomFilterMode;

            public int mipCapacity => m_MipDownPyramidTextures.Length;
            public int mipCount => m_MipCount;

            public TextureHandle resultTexture { get; internal set; }

            public TextureHandle GetResultMip(int index)
            {
                if (m_BloomFilterMode == BloomFilterMode.Kawase)
                    return resultTexture;   // No mips.

                if (mipCount == 1)
                    return mipDownTextures[0];  // Prefilter only.

                int i = Mathf.Max(Mathf.Min(index, mipCount - 1), 0);
                return mipUpTextures[i];    // Upsampled results.
            }

            internal TextureHandle[] mipDownTextures => m_MipDownPyramidTextures;
            internal TextureHandle[] mipUpTextures => m_MipUpPyramidTextures;

            internal BloomMipPyramid(int size = k_MaxPyramidSize)
            {
                // Arrays for Bloom pyramid TextureHandle names.
                m_MipDownPyramidNames = new string[size];
                m_MipUpPyramidNames = new string[size];

                for (int i = 0; i < size; i++)
                {
                    m_MipDownPyramidNames[i] = "_BloomMipDown" + i;
                    m_MipUpPyramidNames[i] = "_BloomMipUp" + i;
                }

                // Arrays for Bloom pyramid TextureHandles.
                m_MipDownPyramidTextures = new TextureHandle[size];
                m_MipUpPyramidTextures = new TextureHandle[size];
            }

            // Create bloom mip pyramid textures
            internal void Update(RenderGraph renderGraph, Bloom bloom, in TextureDesc bloomSourceDesc)
            {
                Vector2Int bloomResolution = CalcBloomResolution(bloom, in bloomSourceDesc);
                m_MipCount = CalcBloomMipCount(bloom, in bloomResolution);
                int tw = bloomResolution.x;
                int th = bloomResolution.y;

                var desc = PostProcessUtils.GetCompatibleDescriptor(bloomSourceDesc, tw, th, bloomSourceDesc.colorFormat);
                m_MipDownPyramidTextures[0] = PostProcessUtils.CreateCompatibleTexture(renderGraph, desc, m_MipDownPyramidNames[0], false, FilterMode.Bilinear);
                m_MipUpPyramidTextures[0] = PostProcessUtils.CreateCompatibleTexture(renderGraph, desc, m_MipUpPyramidNames[0], false, FilterMode.Bilinear);

                m_BloomFilterMode = bloom.filter.value;
                if (m_BloomFilterMode != BloomFilterMode.Kawase)
                {
                    for (int i = 1; i < mipCount; i++)
                    {
                        tw = Mathf.Max(1, tw >> 1);
                        th = Mathf.Max(1, th >> 1);
                        ref TextureHandle mipDown = ref m_MipDownPyramidTextures[i];
                        ref TextureHandle mipUp = ref m_MipUpPyramidTextures[i];

                        desc.width = tw;
                        desc.height = th;

                        mipDown = PostProcessUtils.CreateCompatibleTexture(renderGraph, desc, m_MipDownPyramidNames[i], false, FilterMode.Bilinear);
                        mipUp = PostProcessUtils.CreateCompatibleTexture(renderGraph, desc, m_MipUpPyramidNames[i], false, FilterMode.Bilinear);
                    }
                }
            }
        }

        // Cache params to avoid setting each property in each material every frame.
        internal struct MaterialParams
        {
            internal Vector4 parameters;
            internal Vector4 parameters2;
            internal BloomFilterMode bloomFilter;
            internal bool highQualityFiltering;
            internal bool enableAlphaOutput;

            internal bool Equals(ref MaterialParams other)
            {
                return parameters == other.parameters &&
                       parameters2 == other.parameters2 &&
                       highQualityFiltering == other.highQualityFiltering &&
                       enableAlphaOutput == other.enableAlphaOutput &&
                       bloomFilter == other.bloomFilter;
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _Params2 = Shader.PropertyToID("_Params2");

            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
        }

        public static class ShaderPass
        {
            public const int k_Prefilter = 0;
            public const int k_BlurHorizontal = 1;
            public const int k_BlurVertical = 2;
            public const int k_Upsample = 3;
            public const int k_Kawase = 4;
            public const int k_DualDownsample = 5;
            public const int k_DualUpsample = 6;
        }
    }
}
