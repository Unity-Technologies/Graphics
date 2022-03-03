using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'LocalVolumetricFogArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    struct LocalVolumetricFogEngineData
    {
        public Vector3 scattering;    // [0, 1]
        public float extinction;    // [0, 1]
        public Vector3 textureTiling;
        public int invertFade;    // bool...
        public Vector3 textureScroll;
        public float rcpDistFadeLen;
        public Vector3 rcpPosFaceFade;
        public float endTimesRcpDistFadeLen;
        public Vector3 rcpNegFaceFade;
        public int useVolumeMask; // bool
        public Vector3 atlasOffset;   // coordinates in the atlas in pixels
        public LocalVolumetricFogFalloffMode falloffMode;
        public Vector4 maskSize;      // xyz: atlas size / mask size, w: mask size in pixels

        public static LocalVolumetricFogEngineData GetNeutralValues()
        {
            LocalVolumetricFogEngineData data;

            data.scattering = Vector3.zero;
            data.extinction = 0;
            data.atlasOffset = Vector3.zero;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade = 0;
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;
            data.useVolumeMask = 0;
            data.maskSize = Vector4.zero;
            data.falloffMode = LocalVolumetricFogFalloffMode.Linear;

            return data;
        }
    } // struct VolumeProperties

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesVolumetric
    {
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float _VBufferCoordToViewDirWS[ShaderConfig.k_XRMaxViewsForCBuffer * 16];

        public float _VBufferUnitDepthTexelSpacing;
        public uint _NumVisibleLocalVolumetricFog;
        public float _CornetteShanksConstant;
        public uint _VBufferHistoryIsValid;

        public Vector4 _VBufferSampleOffset;
        public Vector4 _VolumeMaskDimensions;

        [HLSLArray(7, typeof(Vector4))]
        public fixed float _AmbientProbeCoeffs[7 * 4];  // 3 bands of SH, packed, rescaled and convolved with the phase function

        public float _VBufferVoxelSize;
        public float _HaveToPad;
        public float _OtherwiseTheBuffer;
        public float _IsFilledWithGarbage;
        public Vector4 _VBufferPrevViewportSize;
        public Vector4 _VBufferHistoryViewportScale;
        public Vector4 _VBufferHistoryViewportLimit;
        public Vector4 _VBufferPrevDistanceEncodingParams;
        public Vector4 _VBufferPrevDistanceDecodingParams;

        // TODO: Remove if equals to the ones in global CB?
        public uint _NumTileBigTileX;
        public uint _NumTileBigTileY;
        public uint _Pad0_SVV;
        public uint _Pad1_SVV;
    }

    /// <summary></summary>
    [GenerateHLSL]
    public enum LocalVolumetricFogFalloffMode
    {
        /// <summary></summary>
        Linear,
        /// <summary></summary>
        Exponential,
    }

    class VolumeRenderingUtils
    {
        public static float MeanFreePathFromExtinction(float extinction)
        {
            return 1.0f / extinction;
        }

        public static float ExtinctionFromMeanFreePath(float meanFreePath)
        {
            return 1.0f / meanFreePath;
        }

        public static Vector3 AbsorptionFromExtinctionAndScattering(float extinction, Vector3 scattering)
        {
            return new Vector3(extinction, extinction, extinction) - scattering;
        }

        public static Vector3 ScatteringFromExtinctionAndAlbedo(float extinction, Vector3 albedo)
        {
            return extinction * albedo;
        }

        public static Vector3 AlbedoFromMeanFreePathAndScattering(float meanFreePath, Vector3 scattering)
        {
            return meanFreePath * scattering;
        }
    }

    struct LocalVolumetricFogList
    {
        public List<OrientedBBox> bounds;
        public List<LocalVolumetricFogEngineData> density;
    }

    struct VBufferParameters
    {
        public Vector3Int viewportSize;
        public float voxelSize;
        public Vector4 depthEncodingParams;
        public Vector4 depthDecodingParams;

        public VBufferParameters(Vector3Int viewportSize, float depthExtent, float camNear, float camFar, float camVFoV,
                                 float sliceDistributionUniformity, float voxelSize)
        {
            this.viewportSize = viewportSize;
            this.voxelSize = voxelSize;

            // The V-Buffer is sphere-capped, while the camera frustum is not.
            // We always start from the near plane of the camera.

            float aspectRatio = viewportSize.x / (float)viewportSize.y;
            float farPlaneHeight = 2.0f * Mathf.Tan(0.5f * camVFoV) * camFar;
            float farPlaneWidth = farPlaneHeight * aspectRatio;
            float farPlaneMaxDim = Mathf.Max(farPlaneWidth, farPlaneHeight);
            float farPlaneDist = Mathf.Sqrt(camFar * camFar + 0.25f * farPlaneMaxDim * farPlaneMaxDim);

            float nearDist = camNear;
            float farDist = Math.Min(nearDist + depthExtent, farPlaneDist);

            float c = 2 - 2 * sliceDistributionUniformity; // remap [0, 1] -> [2, 0]
            c = Mathf.Max(c, 0.001f);                // Avoid NaNs

            depthEncodingParams = ComputeLogarithmicDepthEncodingParams(nearDist, farDist, c);
            depthDecodingParams = ComputeLogarithmicDepthDecodingParams(nearDist, farDist, c);
        }

        internal Vector3 ComputeViewportScale(Vector3Int bufferSize)
        {
            return new Vector3(HDUtils.ComputeViewportScale(viewportSize.x, bufferSize.x),
                HDUtils.ComputeViewportScale(viewportSize.y, bufferSize.y),
                HDUtils.ComputeViewportScale(viewportSize.z, bufferSize.z));
        }

        internal Vector3 ComputeViewportLimit(Vector3Int bufferSize)
        {
            return new Vector3(HDUtils.ComputeViewportLimit(viewportSize.x, bufferSize.x),
                HDUtils.ComputeViewportLimit(viewportSize.y, bufferSize.y),
                HDUtils.ComputeViewportLimit(viewportSize.z, bufferSize.z));
        }

        internal float ComputeLastSliceDistance(uint sliceCount)
        {
            float d = 1.0f - 0.5f / sliceCount;
            float ln2 = 0.69314718f;

            // DecodeLogarithmicDepthGeneralized(1 - 0.5 / sliceCount)
            return depthDecodingParams.x * Mathf.Exp(ln2 * d * depthDecodingParams.y) + depthDecodingParams.z;
        }

        // See EncodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthEncodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            depthParams.y = 1.0f / Mathf.Log(c * (f - n) + 1, 2);
            depthParams.x = Mathf.Log(c, 2) * depthParams.y;
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }

        // See DecodeLogarithmicDepthGeneralized().
        static Vector4 ComputeLogarithmicDepthDecodingParams(float nearPlane, float farPlane, float c)
        {
            Vector4 depthParams = new Vector4();

            float n = nearPlane;
            float f = farPlane;

            depthParams.x = 1.0f / c;
            depthParams.y = Mathf.Log(c * (f - n) + 1, 2);
            depthParams.z = n - 1.0f / c; // Same
            depthParams.w = 0.0f;

            return depthParams;
        }
    }

    public partial class HDRenderPipeline
    {
        ComputeShader m_VolumeVoxelizationCS = null;
        ComputeShader m_VolumetricLightingCS = null;
        ComputeShader m_VolumetricLightingFilteringCS = null;

        List<OrientedBBox> m_VisibleVolumeBounds = null;
        List<LocalVolumetricFogEngineData> m_VisibleVolumeData = null;
        internal const int k_MaxVisibleLocalVolumetricFogCount = 512;

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        ComputeBuffer m_VisibleVolumeBoundsBuffer = null;
        ComputeBuffer m_VisibleVolumeDataBuffer = null;

        ShaderVariablesVolumetric m_ShaderVariablesVolumetricCB = new ShaderVariablesVolumetric();

        // This size is shared between all cameras to create the volumetric 3D textures
        static Vector3Int s_CurrentVolumetricBufferSize;

        // Is the feature globally disabled?
        bool m_SupportVolumetrics = false;

        Vector4[] m_PackedCoeffs;
        ZonalHarmonicsL2 m_PhaseZH;
        Vector2[] m_xySeq;

        // This is a sequence of 7 equidistant numbers from 1/14 to 13/14.
        // Each of them is the centroid of the interval of length 2/14.
        // They've been rearranged in a sequence of pairs {small, large}, s.t. (small + large) = 1.
        // That way, the running average position is close to 0.5.
        // | 6 | 2 | 4 | 1 | 5 | 3 | 7 |
        // |   |   |   | o |   |   |   |
        // |   | o |   | x |   |   |   |
        // |   | x |   | x |   | o |   |
        // |   | x | o | x |   | x |   |
        // |   | x | x | x | o | x |   |
        // | o | x | x | x | x | x |   |
        // | x | x | x | x | x | x | o |
        // | x | x | x | x | x | x | x |
        float[] m_zSeq = { 7.0f / 14.0f, 3.0f / 14.0f, 11.0f / 14.0f, 5.0f / 14.0f, 9.0f / 14.0f, 1.0f / 14.0f, 13.0f / 14.0f };

        Matrix4x4[] m_PixelCoordToViewDirWS;

        static internal void SafeDestroy(ref RenderTexture rt)
        {
            if (rt != null)
            {
                rt.Release(); // The texture itself is not destroyed: https://docs.unity3d.com/ScriptReference/RenderTexture.Release.html
                Object.DestroyImmediate(rt); // Destroy() may not be called from the Edit mode
            }
        }

        static uint VolumetricFrameIndex(HDCamera hdCamera)
        {
            // Here we do modulo 14 because we need the enable to detect a change every frame, but the accumulation is done on 7 frames (7x2=14)
            return hdCamera.GetCameraFrameCount() % 14;
        }

        static internal Vector3Int ComputeVolumetricViewportSize(HDCamera hdCamera, ref float voxelSize)
        {
            var controller = hdCamera.volumeStack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            int viewportWidth = hdCamera.actualWidth;
            int viewportHeight = hdCamera.actualHeight;

            float screenFraction;
            int sliceCount;
            if (controller.fogControlMode == FogControl.Balance)
            {
                // Evaluate the ssFraction and sliceCount based on the control parameters
                float maxScreenSpaceFraction = (1.0f - controller.resolutionDepthRatio) * (Fog.maxFogScreenResolutionPercentage - Fog.minFogScreenResolutionPercentage) + Fog.minFogScreenResolutionPercentage;
                screenFraction = Mathf.Lerp(Fog.minFogScreenResolutionPercentage, maxScreenSpaceFraction, controller.volumetricFogBudget) * 0.01f;
                float maxSliceCount = Mathf.Max(1.0f, controller.resolutionDepthRatio * Fog.maxFogSliceCount);
                sliceCount = (int)Mathf.Lerp(1.0f, maxSliceCount, controller.volumetricFogBudget);

                // Evaluate the voxel size
                voxelSize = 1.0f / screenFraction;
            }
            else
            {
                screenFraction = controller.screenResolutionPercentage.value * 0.01f;
                sliceCount = controller.volumeSliceCount.value;

                if (controller.screenResolutionPercentage.value == Fog.optimalFogScreenResolutionPercentage)
                    voxelSize = 8;
                else
                    voxelSize = 1.0f / screenFraction; // Does not account for rounding (same function, above)
            }

            int w = Mathf.RoundToInt(viewportWidth * screenFraction);
            int h = Mathf.RoundToInt(viewportHeight * screenFraction);

            // Round to nearest multiple of viewCount so that each views have the exact same number of slices (important for XR)
            int d = hdCamera.viewCount * Mathf.CeilToInt(sliceCount / hdCamera.viewCount);

            return new Vector3Int(w, h, d);
        }

        static internal VBufferParameters ComputeVolumetricBufferParameters(HDCamera hdCamera)
        {
            var controller = hdCamera.volumeStack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            float voxelSize = 0;
            Vector3Int viewportSize = ComputeVolumetricViewportSize(hdCamera, ref voxelSize);

            return new VBufferParameters(viewportSize, controller.depthExtent.value,
                hdCamera.camera.nearClipPlane,
                hdCamera.camera.farClipPlane,
                hdCamera.camera.fieldOfView,
                controller.sliceDistributionUniformity.value,
                voxelSize);
        }

        static internal void ReinitializeVolumetricBufferParams(HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            bool fog = Fog.IsVolumetricFogEnabled(hdCamera);
            bool init = hdCamera.vBufferParams != null;

            if (fog ^ init)
            {
                if (init)
                {
                    // Deinitialize.
                    hdCamera.vBufferParams = null;
                }
                else
                {
                    // Initialize.
                    // Start with the same parameters for both frames. Then update them one by one every frame.
                    var parameters = ComputeVolumetricBufferParameters(hdCamera);
                    hdCamera.vBufferParams = new VBufferParameters[2];
                    hdCamera.vBufferParams[0] = parameters;
                    hdCamera.vBufferParams[1] = parameters;
                }
            }
        }

        // This function relies on being called once per camera per frame.
        // The results are undefined otherwise.
        static internal void UpdateVolumetricBufferParams(HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            Debug.Assert(hdCamera.vBufferParams != null);
            Debug.Assert(hdCamera.vBufferParams.Length == 2);

            var currentParams = ComputeVolumetricBufferParameters(hdCamera);

            int frameIndex = (int)VolumetricFrameIndex(hdCamera);
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            hdCamera.vBufferParams[currIdx] = currentParams;

            // Handle case of first frame. When we are on the first frame, we reuse the value of original frame.
            if (hdCamera.vBufferParams[prevIdx].viewportSize.x == 0.0f && hdCamera.vBufferParams[prevIdx].viewportSize.y == 0.0f)
            {
                hdCamera.vBufferParams[prevIdx] = currentParams;
            }

            // Update size used to create volumetric buffers.
            s_CurrentVolumetricBufferSize = new Vector3Int(Math.Max(s_CurrentVolumetricBufferSize.x, currentParams.viewportSize.x),
                Math.Max(s_CurrentVolumetricBufferSize.y, currentParams.viewportSize.y),
                Math.Max(s_CurrentVolumetricBufferSize.z, currentParams.viewportSize.z));
        }

        // Do not access 'rt.name', it allocates memory every time...
        // Have to manually cache and pass the name.
        static internal void ResizeVolumetricBuffer(ref RTHandle rt, string name, int viewportWidth, int viewportHeight, int viewportDepth)
        {
            Debug.Assert(rt != null);

            int width = rt.rt.width;
            int height = rt.rt.height;
            int depth = rt.rt.volumeDepth;

            bool realloc = (width < viewportWidth) || (height < viewportHeight) || (depth < viewportDepth);

            if (realloc)
            {
                RTHandles.Release(rt);

                width = Math.Max(width, viewportWidth);
                height = Math.Max(height, viewportHeight);
                depth = Math.Max(depth, viewportDepth);

                rt = RTHandles.Alloc(width, height, depth, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                    dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: name);
            }
        }

        class GenerateMaxZMaskPassData
        {
            public ComputeShader generateMaxZCS;
            public int maxZKernel;
            public int maxZDownsampleKernel;
            public int dilateMaxZKernel;

            public Vector2Int intermediateMaskSize;
            public Vector2Int finalMaskSize;
            public Vector2Int minDepthMipOffset;

            public float dilationWidth;
            public int viewCount;

            public TextureHandle depthTexture;
            public TextureHandle maxZ8xBuffer;
            public TextureHandle maxZBuffer;
            public TextureHandle dilatedMaxZBuffer;
        }

        TextureHandle GenerateMaxZPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, HDUtils.PackedMipChainInfo depthMipInfo)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera) || VolumetricCloudsRequireMaxZ(hdCamera))
            {
                if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                {
                    return renderGraph.defaultResources.blackTextureXR;
                }

                using (var builder = renderGraph.AddRenderPass<GenerateMaxZMaskPassData>("Generate Max Z Mask for Volumetric", out var passData))
                {
                    //TODO: move the entire vbuffer to hardware DRS mode. When Hardware DRS is enabled we will save performance
                    // on these buffers, however the final vbuffer will be wasting resolution. This requires a bit of more work to optimize.
                    passData.generateMaxZCS = defaultResources.shaders.maxZCS;
                    passData.generateMaxZCS.shaderKeywords = null;
                    bool planarReflection = hdCamera.camera.cameraType == CameraType.Reflection && hdCamera.parentCamera != null;
                    CoreUtils.SetKeyword(passData.generateMaxZCS, "PLANAR_OBLIQUE_DEPTH", planarReflection);

                    passData.maxZKernel = passData.generateMaxZCS.FindKernel("ComputeMaxZ");
                    passData.maxZDownsampleKernel = passData.generateMaxZCS.FindKernel("ComputeFinalMask");
                    passData.dilateMaxZKernel = passData.generateMaxZCS.FindKernel("DilateMask");

                    passData.intermediateMaskSize.x = HDUtils.DivRoundUp(hdCamera.actualWidth, 8);
                    passData.intermediateMaskSize.y = HDUtils.DivRoundUp(hdCamera.actualHeight, 8);

                    passData.finalMaskSize.x = passData.intermediateMaskSize.x / 2;
                    passData.finalMaskSize.y = passData.intermediateMaskSize.y / 2;

                    passData.minDepthMipOffset.x = depthMipInfo.mipLevelOffsets[4].x;
                    passData.minDepthMipOffset.y = depthMipInfo.mipLevelOffsets[4].y;

                    int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                    var currIdx = frameIndex & 1;

                    if (hdCamera.vBufferParams != null)
                    {
                        var currentParams = hdCamera.vBufferParams[currIdx];
                        float ratio = (float)currentParams.viewportSize.x / (float)hdCamera.actualWidth;
                        passData.dilationWidth = ratio < 0.1f ? 2 :
                            ratio < 0.5f ? 1 : 0;
                    }
                    else
                    {
                        passData.dilationWidth = 1;
                    }

                    passData.viewCount = hdCamera.viewCount;

                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZ8xBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.125f, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask 8x" });
                    passData.maxZBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.125f, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask" });
                    passData.dilatedMaxZBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / 16.0f, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Dilated MaxZ mask" }));

                    builder.SetRenderFunc(
                        (GenerateMaxZMaskPassData data, RenderGraphContext ctx) =>
                        {
                            // Downsample 8x8 with max operator

                            var cs = data.generateMaxZCS;
                            var kernel = data.maxZKernel;

                            int maskW = data.intermediateMaskSize.x;
                            int maskH = data.intermediateMaskSize.y;

                            int dispatchX = maskW;
                            int dispatchY = maskH;

                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.maxZ8xBuffer);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                            ctx.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);

                            // --------------------------------------------------------------
                            // Downsample to 16x16 and compute gradient if required

                            kernel = data.maxZDownsampleKernel;

                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.maxZ8xBuffer);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.maxZBuffer);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                            Vector4 srcLimitAndDepthOffset = new Vector4(
                                maskW,
                                maskH,
                                data.minDepthMipOffset.x,
                                data.minDepthMipOffset.y
                            );
                            ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._SrcOffsetAndLimit, srcLimitAndDepthOffset);
                            ctx.cmd.SetComputeFloatParam(cs, HDShaderIDs._DilationWidth, data.dilationWidth);

                            int finalMaskW = Mathf.CeilToInt(maskW / 2.0f);
                            int finalMaskH = Mathf.CeilToInt(maskH / 2.0f);

                            dispatchX = HDUtils.DivRoundUp(finalMaskW, 8);
                            dispatchY = HDUtils.DivRoundUp(finalMaskH, 8);

                            ctx.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);

                            // --------------------------------------------------------------
                            // Dilate max Z and gradient.
                            kernel = data.dilateMaxZKernel;

                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.maxZBuffer);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.dilatedMaxZBuffer);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);

                            srcLimitAndDepthOffset.x = finalMaskW;
                            srcLimitAndDepthOffset.y = finalMaskH;
                            ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._SrcOffsetAndLimit, srcLimitAndDepthOffset);

                            ctx.cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, data.viewCount);
                        });

                    return passData.dilatedMaxZBuffer;
                }
            }

            return TextureHandle.nullHandle;
        }

        static internal void CreateVolumetricHistoryBuffers(HDCamera hdCamera, int bufferCount)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

            Debug.Assert(hdCamera.volumetricHistoryBuffers == null);

            hdCamera.volumetricHistoryBuffers = new RTHandle[bufferCount];

            // Allocation happens early in the frame. So we shouldn't rely on 'hdCamera.vBufferParams'.
            // Allocate the smallest possible 3D texture.
            // We will perform rescaling manually, in a custom manner, based on volume parameters.
            const int minSize = 4;

            for (int i = 0; i < bufferCount; i++)
            {
                hdCamera.volumetricHistoryBuffers[i] = RTHandles.Alloc(minSize, minSize, minSize, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, // 8888_sRGB is not precise enough
                    dimension: TextureDimension.Tex3D, enableRandomWrite: true, name: string.Format("VBufferHistory{0}", i));
            }

            hdCamera.volumetricHistoryIsValid = false;
        }

        static internal void DestroyVolumetricHistoryBuffers(HDCamera hdCamera)
        {
            if (hdCamera.volumetricHistoryBuffers == null)
                return;

            int bufferCount = hdCamera.volumetricHistoryBuffers.Length;

            for (int i = 0; i < bufferCount; i++)
            {
                RTHandles.Release(hdCamera.volumetricHistoryBuffers[i]);
            }

            hdCamera.volumetricHistoryBuffers = null;
            hdCamera.volumetricHistoryIsValid = false;
        }

        // Must be called AFTER UpdateVolumetricBufferParams.
        static readonly string[] volumetricHistoryBufferNames = new string[2] { "VBufferHistory0", "VBufferHistory1" };
        static internal void ResizeVolumetricHistoryBuffers(HDCamera hdCamera)
        {
            if (!hdCamera.IsVolumetricReprojectionEnabled())
                return;

            Debug.Assert(hdCamera.vBufferParams != null);
            Debug.Assert(hdCamera.vBufferParams.Length == 2);
            Debug.Assert(hdCamera.volumetricHistoryBuffers != null);

            int frameIndex = (int)VolumetricFrameIndex(hdCamera);
            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currentParams = hdCamera.vBufferParams[currIdx];

            // Render texture contents can become "lost" on certain events, like loading a new level,
            // system going to a screensaver mode, in and out of fullscreen and so on.
            // https://docs.unity3d.com/ScriptReference/RenderTexture.html
            if (hdCamera.volumetricHistoryBuffers[0] == null || hdCamera.volumetricHistoryBuffers[1] == null)
            {
                DestroyVolumetricHistoryBuffers(hdCamera);
                CreateVolumetricHistoryBuffers(hdCamera, hdCamera.vBufferParams.Length); // Basically, assume it's 2
            }

            // We only resize the feedback buffer (#0), not the history buffer (#1).
            // We must NOT resize the buffer from the previous frame (#1), as that would invalidate its contents.
            ResizeVolumetricBuffer(ref hdCamera.volumetricHistoryBuffers[currIdx], volumetricHistoryBufferNames[currIdx], currentParams.viewportSize.x,
                currentParams.viewportSize.y,
                currentParams.viewportSize.z);
        }

        internal void CreateVolumetricLightingBuffers()
        {
            Debug.Assert(m_VolumetricLightingCS != null);

            m_VisibleVolumeBounds = new List<OrientedBBox>();
            m_VisibleVolumeData = new List<LocalVolumetricFogEngineData>();
            m_VisibleVolumeBoundsBuffer = new ComputeBuffer(k_MaxVisibleLocalVolumetricFogCount, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleVolumeDataBuffer = new ComputeBuffer(k_MaxVisibleLocalVolumetricFogCount, Marshal.SizeOf(typeof(LocalVolumetricFogEngineData)));
        }

        internal void DestroyVolumetricLightingBuffers()
        {
            CoreUtils.SafeRelease(m_VisibleVolumeDataBuffer);
            CoreUtils.SafeRelease(m_VisibleVolumeBoundsBuffer);

            m_VisibleVolumeData = null; // free()
            m_VisibleVolumeBounds = null; // free()
        }

        void InitializeVolumetricLighting()
        {
            m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            m_VolumeVoxelizationCS = defaultResources.shaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = defaultResources.shaders.volumetricLightingCS;
            m_VolumetricLightingFilteringCS = defaultResources.shaders.volumetricLightingFilteringCS;

            m_PackedCoeffs = new Vector4[7];
            m_PhaseZH = new ZonalHarmonicsL2();
            m_PhaseZH.coeffs = new float[3];

            m_xySeq = new Vector2[7];

            m_PixelCoordToViewDirWS = new Matrix4x4[ShaderConfig.s_XrMaxViews];

            CreateVolumetricLightingBuffers();
        }

        void CleanupVolumetricLighting()
        {
            DestroyVolumetricLightingBuffers();

            m_VolumeVoxelizationCS = null;
            m_VolumetricLightingCS = null;
            m_VolumetricLightingFilteringCS = null;
        }

        static float CornetteShanksPhasePartConstant(float anisotropy)
        {
            float g = anisotropy;

            return (3.0f / (8.0f * Mathf.PI)) * (1.0f - g * g) / (2.0f + g * g);
        }

        void UpdateShaderVariablesGlobalVolumetrics(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
            {
                return;
            }

            // Get the interpolated anisotropy value.
            var fog = hdCamera.volumeStack.GetComponent<Fog>();
            uint frameIndex = hdCamera.GetCameraFrameCount();
            uint currIdx = (frameIndex + 0) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            var cvp = currParams.viewportSize;

            // Adjust slices for XR rendering: VBuffer is shared for all single-pass views
            uint sliceCount = (uint)(cvp.z / hdCamera.viewCount);

            cb._VBufferViewportSize = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
            cb._VBufferSliceCount = sliceCount;
            cb._VBufferRcpSliceCount = 1.0f / sliceCount;
            cb._VBufferLightingViewportScale = currParams.ComputeViewportScale(s_CurrentVolumetricBufferSize);
            cb._VBufferLightingViewportLimit = currParams.ComputeViewportLimit(s_CurrentVolumetricBufferSize);
            cb._VBufferDistanceEncodingParams = currParams.depthEncodingParams;
            cb._VBufferDistanceDecodingParams = currParams.depthDecodingParams;
            cb._VBufferLastSliceDist = currParams.ComputeLastSliceDistance(sliceCount);
            cb._VBufferRcpInstancedViewCount = 1.0f / hdCamera.viewCount;
        }

        LocalVolumetricFogList PrepareVisibleLocalVolumetricFogList(HDCamera hdCamera, CommandBuffer cmd)
        {
            LocalVolumetricFogList localVolumetricFog = new LocalVolumetricFogList();

            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return localVolumetricFog;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PrepareVisibleLocalVolumetricFogList)))
            {
                Vector3 camPosition = hdCamera.camera.transform.position;
                Vector3 camOffset = Vector3.zero;// World-origin-relative

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    camOffset = camPosition; // Camera-relative
                }

                m_VisibleVolumeBounds.Clear();
                m_VisibleVolumeData.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var volumes = LocalVolumetricFogManager.manager.PrepareLocalVolumetricFogData(cmd, hdCamera);

                for (int i = 0; i < Math.Min(volumes.Count, k_MaxVisibleLocalVolumetricFogCount); i++)
                {
                    LocalVolumetricFog volume = volumes[i];

                    // TODO: cache these?
                    var obb = new OrientedBBox(Matrix4x4.TRS(volume.transform.position, volume.transform.rotation, volume.parameters.size));

                    // Handle camera-relative rendering.
                    obb.center -= camOffset;

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    if (GeometryUtils.Overlap(obb, hdCamera.frustum, 6, 8))
                    {
                        // TODO: cache these?
                        var data = volume.parameters.ConvertToEngineData();

                        m_VisibleVolumeBounds.Add(obb);
                        m_VisibleVolumeData.Add(data);
                    }
                }

                m_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
                m_VisibleVolumeDataBuffer.SetData(m_VisibleVolumeData);

                // Fill the struct with pointers in order to share the data with the light loop.
                localVolumetricFog.bounds = m_VisibleVolumeBounds;
                localVolumetricFog.density = m_VisibleVolumeData;

                return localVolumetricFog;
            }
        }

        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesVolumetric cb, HDCamera hdCamera, Fog fog)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_SkyManager.GetAmbientProbe(hdCamera));
            probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, fog.globalLightProbeDimmer.value);
            ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZH, fog.anisotropy.value);
            SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZH));

            SphericalHarmonicMath.PackCoefficients(m_PackedCoeffs, finalSH);
            for (int i = 0; i < 7; i++)
                for (int j = 0; j < 4; ++j)
                    cb._AmbientProbeCoeffs[i * 4 + j] = m_PackedCoeffs[i][j];
        }

        unsafe void UpdateShaderVariableslVolumetrics(ref ShaderVariablesVolumetric cb, HDCamera hdCamera, in Vector4 resolution)
        {
            var fog = hdCamera.volumeStack.GetComponent<Fog>();
            var vFoV = hdCamera.camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            var gpuAspect = HDUtils.ProjectionMatrixAspect(hdCamera.mainViewConstants.projMatrix);
            int frameIndex = (int)VolumetricFrameIndex(hdCamera);

            // Compose the matrix which allows us to compute the world space view direction.
            hdCamera.GetPixelCoordToViewDirWS(resolution, gpuAspect, ref m_PixelCoordToViewDirWS);

            for (int i = 0; i < m_PixelCoordToViewDirWS.Length; ++i)
                for (int j = 0; j < 16; ++j)
                    cb._VBufferCoordToViewDirWS[i * 16 + j] = m_PixelCoordToViewDirWS[i][j];
            cb._VBufferUnitDepthTexelSpacing = HDUtils.ComputZPlaneTexelSpacing(1.0f, vFoV, resolution.y);
            cb._NumVisibleLocalVolumetricFog = (uint)m_VisibleVolumeBounds.Count;
            cb._CornetteShanksConstant = CornetteShanksPhasePartConstant(fog.anisotropy.value);
            cb._VBufferHistoryIsValid = hdCamera.volumetricHistoryIsValid ? 1u : 0u;

            GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = frameIndex % 7;
            Vector4 xySeqOffset = new Vector4();
            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);
            cb._VBufferSampleOffset = xySeqOffset;

            var volumeAtlas = LocalVolumetricFogManager.manager.volumeAtlas.GetAtlas();
            cb._VolumeMaskDimensions = Vector4.zero;
            if (LocalVolumetricFogManager.manager.volumeAtlas.GetAtlas() != null)
            {
                cb._VolumeMaskDimensions.x = volumeAtlas.width; // 1 / number of textures
                cb._VolumeMaskDimensions.y = volumeAtlas.height;
                cb._VolumeMaskDimensions.z = volumeAtlas.volumeDepth;
                cb._VolumeMaskDimensions.w = 0; // Not used
            }

            SetPreconvolvedAmbientLightProbe(ref cb, hdCamera, fog);

            var currIdx = (frameIndex + 0) & 1;
            var prevIdx = (frameIndex + 1) & 1;

            var currParams = hdCamera.vBufferParams[currIdx];
            var prevParams = hdCamera.vBufferParams[prevIdx];

            var pvp = prevParams.viewportSize;

            // The lighting & density buffers are shared by all cameras.
            // The history & feedback buffers are specific to the camera.
            // These 2 types of buffers can have different sizes.
            // Additionally, history buffers can have different sizes, since they are not resized at the same time.
            Vector3Int historyBufferSize = Vector3Int.zero;

            if (hdCamera.IsVolumetricReprojectionEnabled())
            {
                RTHandle historyRT = hdCamera.volumetricHistoryBuffers[prevIdx];
                historyBufferSize = new Vector3Int(historyRT.rt.width, historyRT.rt.height, historyRT.rt.volumeDepth);
            }

            cb._VBufferVoxelSize = currParams.voxelSize;
            cb._VBufferPrevViewportSize = new Vector4(pvp.x, pvp.y, 1.0f / pvp.x, 1.0f / pvp.y);
            cb._VBufferHistoryViewportScale = prevParams.ComputeViewportScale(historyBufferSize);
            cb._VBufferHistoryViewportLimit = prevParams.ComputeViewportLimit(historyBufferSize);
            cb._VBufferPrevDistanceEncodingParams = prevParams.depthEncodingParams;
            cb._VBufferPrevDistanceDecodingParams = prevParams.depthDecodingParams;
            cb._NumTileBigTileX = (uint)GetNumTileBigTileX(hdCamera);
            cb._NumTileBigTileY = (uint)GetNumTileBigTileY(hdCamera);
        }

        class VolumeVoxelizationPassData
        {
            public ComputeShader voxelizationCS;
            public int voxelizationKernel;

            public Vector4 resolution;
            public int viewCount;
            public bool tiledLighting;

            public Texture volumeAtlas;

            public ShaderVariablesVolumetric volumetricCB;
            public ShaderVariablesLightList lightListCB;

            public TextureHandle densityBuffer;
            public ComputeBufferHandle bigTileLightListBuffer;
            public ComputeBuffer visibleVolumeBoundsBuffer;
            public ComputeBuffer visibleVolumeDataBuffer;
        }

        TextureHandle VolumeVoxelizationPass(RenderGraph renderGraph,
            HDCamera hdCamera,
            ComputeBuffer visibleVolumeBoundsBuffer,
            ComputeBuffer visibleVolumeDataBuffer,
            ComputeBufferHandle bigTileLightList)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<VolumeVoxelizationPassData>("Volume Voxelization", out var passData))
                {
                    builder.EnableAsyncCompute(hdCamera.frameSettings.VolumeVoxelizationRunsAsync());

                    int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                    var currIdx = (frameIndex + 0) & 1;
                    var prevIdx = (frameIndex + 1) & 1;

                    var currParams = hdCamera.vBufferParams[currIdx];

                    passData.viewCount = hdCamera.viewCount;
                    passData.tiledLighting = HasLightToCull() && hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
                    bool optimal = currParams.voxelSize == 8;

                    passData.voxelizationCS = m_VolumeVoxelizationCS;
                    passData.voxelizationKernel = (passData.tiledLighting ? 1 : 0) | (!optimal ? 2 : 0);

                    var cvp = currParams.viewportSize;

                    passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
                    passData.volumeAtlas = LocalVolumetricFogManager.manager.volumeAtlas.GetAtlas();

                    if (passData.volumeAtlas == null)
                        passData.volumeAtlas = CoreUtils.blackVolumeTexture;

                    UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, passData.resolution);
                    passData.volumetricCB = m_ShaderVariablesVolumetricCB;
                    passData.lightListCB = m_ShaderVariablesLightListCB;

                    passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
                    passData.visibleVolumeDataBuffer = visibleVolumeDataBuffer;
                    if (passData.tiledLighting)
                        passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightList);

                    passData.densityBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                    { slices = s_CurrentVolumetricBufferSize.z, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferDensity" }));

                    builder.SetRenderFunc(
                        (VolumeVoxelizationPassData data, RenderGraphContext ctx) =>
                        {
                            if (data.tiledLighting)
                                ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs.g_vBigTileLightList, data.bigTileLightListBuffer);

                            ctx.cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VBufferDensity, data.densityBuffer);
                            ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VolumeBounds, data.visibleVolumeBoundsBuffer);
                            ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VolumeData, data.visibleVolumeDataBuffer);
                            ctx.cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VolumeMaskAtlas, data.volumeAtlas);

                            ConstantBuffer.Push(ctx.cmd, data.volumetricCB, data.voxelizationCS, HDShaderIDs._ShaderVariablesVolumetric);
                            ConstantBuffer.Set<ShaderVariablesLightList>(ctx.cmd, data.voxelizationCS, HDShaderIDs._ShaderVariablesLightList);

                            // The shader defines GROUP_SIZE_1D = 8.
                            ctx.cmd.DispatchCompute(data.voxelizationCS, data.voxelizationKernel, ((int)data.resolution.x + 7) / 8, ((int)data.resolution.y + 7) / 8, data.viewCount);
                        });

                    return passData.densityBuffer;
                }
            }
            return TextureHandle.nullHandle;
        }

        // Ref: https://en.wikipedia.org/wiki/Close-packing_of_equal_spheres
        // The returned {x, y} coordinates (and all spheres) are all within the (-0.5, 0.5)^2 range.
        // The pattern has been rotated by 15 degrees to maximize the resolution along X and Y:
        // https://www.desmos.com/calculator/kcpfvltz7c
        static void GetHexagonalClosePackedSpheres7(Vector2[] coords)
        {
            float r = 0.17054068870105443882f;
            float d = 2 * r;
            float s = r * Mathf.Sqrt(3);

            // Try to keep the weighted average as close to the center (0.5) as possible.
            //  (7)(5)    ( )( )    ( )( )    ( )( )    ( )( )    ( )(o)    ( )(x)    (o)(x)    (x)(x)
            // (2)(1)(3) ( )(o)( ) (o)(x)( ) (x)(x)(o) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x) (x)(x)(x)
            //  (4)(6)    ( )( )    ( )( )    ( )( )    (o)( )    (x)( )    (x)(o)    (x)(x)    (x)(x)
            coords[0] = new Vector2(0, 0);
            coords[1] = new Vector2(-d, 0);
            coords[2] = new Vector2(d, 0);
            coords[3] = new Vector2(-r, -s);
            coords[4] = new Vector2(r, s);
            coords[5] = new Vector2(r, -s);
            coords[6] = new Vector2(-r, s);

            // Rotate the sampling pattern by 15 degrees.
            const float cos15 = 0.96592582628906828675f;
            const float sin15 = 0.25881904510252076235f;

            for (int i = 0; i < 7; i++)
            {
                Vector2 coord = coords[i];

                coords[i].x = coord.x * cos15 - coord.y * sin15;
                coords[i].y = coord.x * sin15 + coord.y * cos15;
            }
        }

        class VolumetricLightingPassData
        {
            public ComputeShader volumetricLightingCS;
            public ComputeShader volumetricLightingFilteringCS;
            public int volumetricLightingKernel;
            public int volumetricFilteringKernel;
            public bool tiledLighting;
            public Vector4 resolution;
            public bool enableReprojection;
            public int viewCount;
            public int sliceCount;
            public bool filterVolume;
            public bool filteringNeedsExtraBuffer;
            public ShaderVariablesVolumetric volumetricCB;
            public ShaderVariablesLightList lightListCB;

            public TextureHandle densityBuffer;
            public TextureHandle depthTexture;
            public TextureHandle lightingBuffer;
            public TextureHandle filteringOutputBuffer;
            public TextureHandle maxZBuffer;
            public TextureHandle historyBuffer;
            public TextureHandle feedbackBuffer;
            public ComputeBufferHandle bigTileLightListBuffer;
        }

        TextureHandle VolumetricLightingPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle densityBuffer, TextureHandle maxZBuffer, ComputeBufferHandle bigTileLightListBuffer, ShadowResult shadowResult)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<VolumetricLightingPassData>("Volumetric Lighting", out var passData))
                {
                    int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                    var currIdx = (frameIndex + 0) & 1;
                    var prevIdx = (frameIndex + 1) & 1;

                    var currParams = hdCamera.vBufferParams[currIdx];

                    // Get the interpolated anisotropy value.
                    var fog = hdCamera.volumeStack.GetComponent<Fog>();

                    // Only available in the Play Mode because all the frame counters in the Edit Mode are broken.
                    passData.tiledLighting = hdCamera.frameSettings.IsEnabled(FrameSettingsField.BigTilePrepass);
                    bool volumeAllowsReprojection = ((int)fog.denoisingMode.value & (int)FogDenoisingMode.Reprojection) != 0;
                    passData.enableReprojection = hdCamera.IsVolumetricReprojectionEnabled() && volumeAllowsReprojection;
                    bool enableAnisotropy = fog.anisotropy.value != 0;
                    // The multi-pass integration is only possible if re-projection is possible and the effect is not in anisotropic mode.
                    bool optimal = currParams.voxelSize == 8;
                    passData.volumetricLightingCS = m_VolumetricLightingCS;
                    passData.volumetricLightingFilteringCS = m_VolumetricLightingFilteringCS;
                    passData.volumetricLightingCS.shaderKeywords = null;
                    passData.volumetricLightingFilteringCS.shaderKeywords = null;

                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "LIGHTLOOP_DISABLE_TILE_AND_CLUSTER", !passData.tiledLighting);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_REPROJECTION", passData.enableReprojection);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_ANISOTROPY", enableAnisotropy);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "VL_PRESET_OPTIMAL", optimal);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", !fog.directionalLightsOnly.value);

                    passData.volumetricLightingKernel = passData.volumetricLightingCS.FindKernel("VolumetricLighting");

                    passData.volumetricFilteringKernel = passData.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");

                    var cvp = currParams.viewportSize;

                    passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
                    passData.viewCount = hdCamera.viewCount;
                    passData.filterVolume = ((int)fog.denoisingMode.value & (int)FogDenoisingMode.Gaussian) != 0;
                    passData.sliceCount = (int)(cvp.z);
                    passData.filteringNeedsExtraBuffer = !(SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.LoadStore));

                    UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, passData.resolution);
                    passData.volumetricCB = m_ShaderVariablesVolumetricCB;
                    passData.lightListCB = m_ShaderVariablesLightListCB;

                    if (passData.tiledLighting)
                        passData.bigTileLightListBuffer = builder.ReadComputeBuffer(bigTileLightListBuffer);
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZBuffer = builder.ReadTexture(maxZBuffer);
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                    { slices = s_CurrentVolumetricBufferSize.z, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferLighting" }));

                    if (passData.filterVolume && passData.filteringNeedsExtraBuffer)
                    {
                        passData.filteringOutputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                        { slices = s_CurrentVolumetricBufferSize.z, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferLightingFiltered" }));

                        CoreUtils.SetKeyword(passData.volumetricLightingFilteringCS, "NEED_SEPARATE_OUTPUT", passData.filteringNeedsExtraBuffer);
                    }

                    if (passData.enableReprojection)
                    {
                        passData.feedbackBuffer = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[currIdx]));
                        passData.historyBuffer = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[prevIdx]));
                    }

                    HDShadowManager.ReadShadowResult(shadowResult, builder);

                    builder.SetRenderFunc(
                        (VolumetricLightingPassData data, RenderGraphContext ctx) =>
                        {
                            if (data.tiledLighting)
                                ctx.cmd.SetComputeBufferParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs.g_vBigTileLightList, data.bigTileLightListBuffer);

                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._MaxZMaskTexture, data.maxZBuffer);  // Read

                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);  // Read
                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferDensity, data.densityBuffer);  // Read
                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferLighting, data.lightingBuffer); // Write

                            if (data.enableReprojection)
                            {
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferHistory, data.historyBuffer);  // Read
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferFeedback, data.feedbackBuffer); // Write
                            }

                            ConstantBuffer.Push(ctx.cmd, data.volumetricCB, data.volumetricLightingCS, HDShaderIDs._ShaderVariablesVolumetric);
                            ConstantBuffer.Set<ShaderVariablesLightList>(ctx.cmd, data.volumetricLightingCS, HDShaderIDs._ShaderVariablesLightList);

                            // The shader defines GROUP_SIZE_1D = 8.
                            ctx.cmd.DispatchCompute(data.volumetricLightingCS, data.volumetricLightingKernel, ((int)data.resolution.x + 7) / 8, ((int)data.resolution.y + 7) / 8, data.viewCount);

                            if (data.filterVolume)
                            {
                                ConstantBuffer.Push(ctx.cmd, data.volumetricCB, data.volumetricLightingFilteringCS, HDShaderIDs._ShaderVariablesVolumetric);

                                // The shader defines GROUP_SIZE_1D_XY = 8 and GROUP_SIZE_1D_Z = 1
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingFilteringCS, data.volumetricFilteringKernel, HDShaderIDs._VBufferLighting, data.lightingBuffer);
                                if (data.filteringNeedsExtraBuffer)
                                {
                                    ctx.cmd.SetComputeTextureParam(data.volumetricLightingFilteringCS, data.volumetricFilteringKernel, HDShaderIDs._VBufferLightingFiltered, data.filteringOutputBuffer);
                                }

                                ctx.cmd.DispatchCompute(data.volumetricLightingFilteringCS, data.volumetricFilteringKernel, HDUtils.DivRoundUp((int)data.resolution.x, 8),
                                    HDUtils.DivRoundUp((int)data.resolution.y, 8),
                                    data.sliceCount);
                            }
                        });

                    if (passData.enableReprojection && hdCamera.volumetricValidFrames > 1)
                        hdCamera.volumetricHistoryIsValid = true; // For the next frame..
                    else
                        hdCamera.volumetricValidFrames++;

                    if (passData.filterVolume && passData.filteringNeedsExtraBuffer)
                        return passData.filteringOutputBuffer;
                    else
                        return passData.lightingBuffer;
                }
            }

            return renderGraph.ImportTexture(HDUtils.clearTexture3DRTH);
        }
    }
} // namespace UnityEngine.Rendering.HighDefinition
