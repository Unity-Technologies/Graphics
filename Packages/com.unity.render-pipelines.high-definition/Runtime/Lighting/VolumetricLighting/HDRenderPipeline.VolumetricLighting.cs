using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Collections;
using UnityEngine.Rendering.RendererUtils;

namespace UnityEngine.Rendering.HighDefinition
{
    // Optimized version of 'LocalVolumetricFogArtistParameters'.
    // TODO: pack better. This data structure contains a bunch of UNORMs.
    [GenerateHLSL]
    struct LocalVolumetricFogEngineData
    {
        public Vector3 scattering;    // [0, 1]
        public LocalVolumetricFogFalloffMode falloffMode;

        public Vector3 textureTiling;
        public int invertFade;    // bool...

        public Vector3 textureScroll;
        public float rcpDistFadeLen;

        public Vector3 rcpPosFaceFade;
        public float endTimesRcpDistFadeLen;

        public Vector3 rcpNegFaceFade;
        public LocalVolumetricFogBlendingMode blendingMode;


        public static LocalVolumetricFogEngineData GetNeutralValues()
        {
            LocalVolumetricFogEngineData data;

            data.scattering = Vector3.zero;
            data.textureTiling = Vector3.one;
            data.textureScroll = Vector3.zero;
            data.rcpPosFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.rcpNegFaceFade = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            data.invertFade = 0;
            data.rcpDistFadeLen = 0;
            data.endTimesRcpDistFadeLen = 1;
            data.falloffMode = LocalVolumetricFogFalloffMode.Linear;
            data.blendingMode = LocalVolumetricFogBlendingMode.Additive;

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
        public uint _MaxSliceCount;
        public float _MaxVolumetricFogDistance;

        // Voxelization data
        public Vector4 _CameraRight;

        public Matrix4x4 _CameraInverseViewProjection_NO;

        public uint _VolumeCount;
        public uint _IsObliqueProjectionMatrix;
        public uint _Padding1;
        public uint _Padding2;
    }

    /// <summary>Falloff mode for the local volumetric fog blend distance.</summary>
    [GenerateHLSL]
    public enum LocalVolumetricFogFalloffMode
    {
        /// <summary>Fade using a linear function.</summary>
        Linear,
        /// <summary>Fade using an exponential function.</summary>
        Exponential,
    }

    /// <summary>Local volumetric fog blending mode.</summary>
    [GenerateHLSL]
    public enum LocalVolumetricFogBlendingMode
    {
        /// <summary>Replace the current fog, it is similar to disabling the blending.</summary>
        Overwrite   = 0,
        /// <summary>Additively blend fog volumes. This is the default behavior.</summary>
        Additive    = 1,
        /// <summary>Multiply the fog values when doing the blending. This is useful to make the fog density relative to other fog volumes.</summary>
        Multiply    = 2,
        /// <summary>Performs a minimum operation when blending the volumes.</summary>
        Min         = 3,
        /// <summary>Performs a maximum operation when blending the volumes.</summary>
        Max         = 4,
    }

    [GenerateHLSL(needAccessors = false)]
    unsafe struct VolumetricMaterialRenderingData
    {
        public Vector4 viewSpaceBounds;
        public uint startSliceIndex;
        public uint sliceCount;
        public uint padding0;
        public uint padding1;
        [HLSLArray(8, typeof(Vector4))]
        public fixed float obbVertexPositionWS[8 * 4];
    }

    // TODO: 16bit floats, Mathf.FloatToHalf
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    struct VolumetricMaterialDataCBuffer
    {
        public Vector4 _VolumetricMaterialObbRight;
        public Vector4 _VolumetricMaterialObbUp;
        public Vector4 _VolumetricMaterialObbExtents;
        public Vector4 _VolumetricMaterialObbCenter;
        public Vector4 _VolumetricMaterialRcpPosFaceFade;
        public Vector4 _VolumetricMaterialRcpNegFaceFade;

        public float _VolumetricMaterialInvertFade;
        public float _VolumetricMaterialRcpDistFadeLen;
        public float _VolumetricMaterialEndTimesRcpDistFadeLen;
        public float _VolumetricMaterialFalloffMode;
    }

    /// <summary>Select which mask mode to use for the local volumetric fog.</summary>
    public enum LocalVolumetricFogMaskMode
    {
        /// <summary>Use a 3D texture as mask.</summary>
        Texture,
        /// <summary>Use a material as mask. The material must use the "Fog Volume" material type in Shader Graph.</summary>
        Material,
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

        float EncodeLogarithmicDepthGeneralized(float z, Vector4 encodingParams)
        {
            return encodingParams.x + encodingParams.y * Mathf.Log(Mathf.Max(0, z - encodingParams.z), 2);
        }

        float DecodeLogarithmicDepthGeneralized(float d, Vector4 decodingParams)
        {
            return decodingParams.x * Mathf.Pow(2, d * decodingParams.y) + decodingParams.z;
        }

        internal int ComputeSliceIndexFromDistance(float distance, int maxSliceCount)
        {
            // Avoid out of bounds access
            distance = Mathf.Clamp(distance, 0f, ComputeLastSliceDistance((uint)maxSliceCount));

            float vBufferNearPlane = DecodeLogarithmicDepthGeneralized(0, depthDecodingParams);

            // float dt = (distance - vBufferNearPlane) * 2;
            float dt = distance + vBufferNearPlane;
            float e1 = EncodeLogarithmicDepthGeneralized(dt, depthEncodingParams);
            float rcpSliceCount = 1.0f / (float)maxSliceCount;

            float slice = (e1 - rcpSliceCount) / rcpSliceCount;

            return (int)slice;
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
        List<int> m_GlobalVolumeIndices = null;
        List<LocalVolumetricFog> m_VisibleLocalVolumetricFogVolumes = null;
        NativeArray<uint> m_VolumetricFogSortKeys;
        NativeArray<uint> m_VolumetricFogSortKeysTemp;
        internal const int k_MaxVisibleLocalVolumetricFogCount = 1024;

        // DrawProceduralIndirect with an index buffer takes 5 parameters instead of 4
        const int k_VolumetricMaterialIndirectArgumentCount = 5;
        const int k_VolumetricMaterialIndirectArgumentByteSize = k_VolumetricMaterialIndirectArgumentCount * sizeof(uint);
        const int k_VolumetricFogPriorityMaxValue = 1048576; // 2^20 because there are 20 bits in the volumetric fog sort key

        // Static keyword is required here else we get a "DestroyBuffer can only be called from the main thread"
        ComputeBuffer m_VisibleVolumeBoundsBuffer = null;
        GraphicsBuffer m_VisibleVolumeGlobalIndices = null;

        ShaderVariablesVolumetric m_ShaderVariablesVolumetricCB = new ShaderVariablesVolumetric();

        // This size is shared between all cameras to create the volumetric 3D textures
        static Vector3Int s_CurrentVolumetricBufferSize;

        static readonly ShaderTagId[] s_VolumetricFogPassNames = { HDShaderPassNames.s_VolumetricFogVFXName, HDShaderPassNames.s_FogVolumeVoxelizeName };

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

        static void ComputeVolumetricFogSliceCountAndScreenFraction(Fog fog, out int sliceCount, out float screenFraction)
        {
            if (fog.fogControlMode == FogControl.Balance)
            {
                // Evaluate the ssFraction and sliceCount based on the control parameters
                float maxScreenSpaceFraction = (1.0f - fog.resolutionDepthRatio) * (Fog.maxFogScreenResolutionPercentage - Fog.minFogScreenResolutionPercentage) + Fog.minFogScreenResolutionPercentage;
                screenFraction = Mathf.Lerp(Fog.minFogScreenResolutionPercentage, maxScreenSpaceFraction, fog.volumetricFogBudget) * 0.01f;
                float maxSliceCount = Mathf.Max(1.0f, fog.resolutionDepthRatio * Fog.maxFogSliceCount);
                sliceCount = (int)Mathf.Lerp(1.0f, maxSliceCount, fog.volumetricFogBudget);
            }
            else
            {
                screenFraction = fog.screenResolutionPercentage.value * 0.01f;
                sliceCount = fog.volumeSliceCount.value;
            }
        }

        static internal Vector3Int ComputeVolumetricViewportSize(HDCamera hdCamera, ref float voxelSize)
        {
            var controller = hdCamera.volumeStack.GetComponent<Fog>();
            Debug.Assert(controller != null);

            int viewportWidth = hdCamera.actualWidth;
            int viewportHeight = hdCamera.actualHeight;

            ComputeVolumetricFogSliceCountAndScreenFraction(controller, out var sliceCount, out var screenFraction);
            if (controller.fogControlMode == FogControl.Balance)
            {
                // Evaluate the voxel size
                voxelSize = 1.0f / screenFraction;
            }
            else
            {
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
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                using (var builder = renderGraph.AddRenderPass<GenerateMaxZMaskPassData>("Generate Max Z Mask for Volumetric", out var passData))
                {
                    //TODO: move the entire vbuffer to hardware DRS mode. When Hardware DRS is enabled we will save performance
                    // on these buffers, however the final vbuffer will be wasting resolution. This requires a bit of more work to optimize.
                    passData.generateMaxZCS = runtimeShaders.maxZCS;
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
                    { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask 8x" });
                    passData.maxZBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.125f, true, true)
                    { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "MaxZ mask" });
                    passData.dilatedMaxZBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one / 16.0f, true, true)
                    { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Dilated MaxZ mask" }));

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

            int maxLocalVolumetricFogs = asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxLocalVolumetricFogOnScreen;
            m_VisibleVolumeBounds = new List<OrientedBBox>();
            m_VisibleVolumeData = new List<LocalVolumetricFogEngineData>();
            m_GlobalVolumeIndices = new List<int>(maxLocalVolumetricFogs);
            m_VisibleLocalVolumetricFogVolumes = new List<LocalVolumetricFog>();
            m_VisibleVolumeBoundsBuffer = new ComputeBuffer(maxLocalVolumetricFogs, Marshal.SizeOf(typeof(OrientedBBox)));
            m_VisibleVolumeGlobalIndices = new GraphicsBuffer(GraphicsBuffer.Target.Raw, maxLocalVolumetricFogs, Marshal.SizeOf(typeof(uint)));
            m_VolumetricFogSortKeys = new NativeArray<uint>(maxLocalVolumetricFogs, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_VolumetricFogSortKeysTemp = new NativeArray<uint>(maxLocalVolumetricFogs, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        internal void DestroyVolumetricLightingBuffers()
        {
            CoreUtils.SafeRelease(m_VisibleVolumeBoundsBuffer);
            CoreUtils.SafeRelease(m_VisibleVolumeGlobalIndices);

            if (m_VolumetricFogSortKeys.IsCreated)
                m_VolumetricFogSortKeys.Dispose();
            if (m_VolumetricFogSortKeysTemp.IsCreated)
                m_VolumetricFogSortKeysTemp.Dispose();

            m_VisibleVolumeData = null; // free()
            m_VisibleVolumeBounds = null; // free()
            m_VisibleLocalVolumetricFogVolumes = null;
        }

        void InitializeVolumetricLighting()
        {
            m_SupportVolumetrics = asset.currentPlatformRenderPipelineSettings.supportVolumetrics;

            if (!m_SupportVolumetrics)
                return;

            m_VolumeVoxelizationCS = runtimeShaders.volumeVoxelizationCS;
            m_VolumetricLightingCS = runtimeShaders.volumetricLightingCS;
            m_VolumetricLightingFilteringCS = runtimeShaders.volumetricLightingFilteringCS;

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
            cb._FogGIDimmer = fog.globalLightProbeDimmer.value;
            cb._VBufferRcpSliceCount = 1.0f / sliceCount;
            cb._VBufferLightingViewportScale = currParams.ComputeViewportScale(s_CurrentVolumetricBufferSize);
            cb._VBufferLightingViewportLimit = currParams.ComputeViewportLimit(s_CurrentVolumetricBufferSize);
            cb._VBufferDistanceEncodingParams = currParams.depthEncodingParams;
            cb._VBufferDistanceDecodingParams = currParams.depthDecodingParams;
            cb._VBufferLastSliceDist = currParams.ComputeLastSliceDistance(sliceCount);
            cb._VBufferRcpInstancedViewCount = 1.0f / hdCamera.viewCount;
        }

        uint PackFogVolumeSortKey(LocalVolumetricFog fog, int index)
        {
            // 12 bit index, 20 bit priority
            int halfMaxPriority = k_VolumetricFogPriorityMaxValue / 2;
            int clampedPriority = Mathf.Clamp(fog.parameters.priority, -halfMaxPriority, halfMaxPriority) + halfMaxPriority;
            uint priority = (uint)(clampedPriority & 0xFFFFF);
            uint fogIndex = (uint)(index & 0xFFF);
            return (priority << 12) | (fogIndex << 0);
        }

        internal static TextureDesc GetOpticalFogTransmittanceDesc(HDCamera hdCamera)
        {
            var colorFormat = GraphicsFormat.R16_SFloat;
            if (LensFlareCommonSRP.IsCloudLayerOpacityNeeded(hdCamera.camera) && Fog.IsMultipleScatteringEnabled(hdCamera, out _))
                colorFormat = GraphicsFormat.R16G16_SFloat;

            return new TextureDesc(Vector2.one, true, true)
            {
                name = "Optical Fog Transmittance",
                format = colorFormat,
                clearBuffer = true,
                clearColor = Color.white,
                enableRandomWrite = true,
            };
        }

        void PrepareVisibleLocalVolumetricFogList(HDCamera hdCamera, CommandBuffer cmd)
        {
            if (!Fog.IsVolumetricFogEnabled(hdCamera))
                return;

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
                m_VisibleLocalVolumetricFogVolumes.Clear();
                m_GlobalVolumeIndices.Clear();

                // Collect all visible finite volume data, and upload it to the GPU.
                var volumes = LocalVolumetricFogManager.manager.PrepareLocalVolumetricFogData(cmd, hdCamera);
                int maxLocalVolumetricFogOnScreen = asset.currentPlatformRenderPipelineSettings.lightLoopSettings.maxLocalVolumetricFogOnScreen;
                var fog = hdCamera.volumeStack.GetComponent<Fog>();

                ulong cameraSceneCullingMask =  HDUtils.GetSceneCullingMaskFromCamera(hdCamera.camera);
                foreach (var volume in volumes)
                {
                    var transform = volume.transform;
                    Vector3 scaleSize = volume.GetScaledSize(transform);
                    Vector3 center = volume.transform.position;

                    // Reject volumes that are completely fade out or outside of the volumetric fog using bounding sphere
                    float boundingSphereRadius = Vector3.Magnitude(scaleSize);
                    float minObbDistance = Vector3.Magnitude(center - camPosition) - hdCamera.camera.nearClipPlane - boundingSphereRadius;
                    if (minObbDistance > volume.parameters.distanceFadeEnd || minObbDistance > fog.depthExtent.value)
                        continue;

#if UNITY_EDITOR
                    if ((volume.gameObject.sceneCullingMask & cameraSceneCullingMask) == 0)
                        continue;
#endif
                    // Handle camera-relative rendering.
                    center -= camOffset;

                    
                    var bounds = GeometryUtils.OBBToAABB(transform.right, transform.up, transform.forward, scaleSize, center);

                    // Frustum cull on the CPU for now. TODO: do it on the GPU.
                    // TODO: account for custom near and far planes of the V-Buffer's frustum.
                    // It's typically much shorter (along the Z axis) than the camera's frustum.
                    // We use AABB instead of OBB because the culling has to match what is done on the C++ side
                    if (GeometryUtility.TestPlanesAABB(hdCamera.frustum.planes, bounds))
                    {
                        if (m_VisibleLocalVolumetricFogVolumes.Count >= maxLocalVolumetricFogOnScreen)
                        {
                            Debug.LogError($"The number of local volumetric fog in the view is above the limit: {m_VisibleLocalVolumetricFogVolumes.Count} instead of {maxLocalVolumetricFogOnScreen}. To fix this, please increase the maximum number of local volumetric fog in the view in the HDRP asset.");
                            break;
                        }

                        // TODO: cache these?
                        var obb = new OrientedBBox(Matrix4x4.TRS(transform.position - camOffset, transform.rotation, scaleSize));
                        m_VisibleVolumeBounds.Add(obb);
                        m_GlobalVolumeIndices.Add(volume.GetGlobalIndex());
                        var visibleData = volume.parameters.ConvertToEngineData();
                        m_VisibleVolumeData.Add(visibleData);

                        m_VisibleLocalVolumetricFogVolumes.Add(volume);
                    }
                }

                // Assign priorities for sorting
                for (int i = 0; i < m_VisibleLocalVolumetricFogVolumes.Count; i++)
                    m_VolumetricFogSortKeys[i] = PackFogVolumeSortKey(m_VisibleLocalVolumetricFogVolumes[i], i);

                // Stable sort to avoid flickering
                CoreUnsafeUtils.MergeSort(m_VolumetricFogSortKeys, m_VisibleLocalVolumetricFogVolumes.Count, ref m_VolumetricFogSortKeysTemp);

                m_VisibleVolumeBoundsBuffer.SetData(m_VisibleVolumeBounds);
                m_VisibleVolumeGlobalIndices.SetData(m_GlobalVolumeIndices);
            }
        }

        unsafe void UpdateShaderVariableslVolumetrics(ref ShaderVariablesVolumetric cb, HDCamera hdCamera, in Vector4 resolution, int maxSliceCount, bool updateVoxelizationFields = false)
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
            cb._NumVisibleLocalVolumetricFog = (uint)m_VisibleLocalVolumetricFogVolumes.Count;
            cb._CornetteShanksConstant = CornetteShanksPhasePartConstant(fog.anisotropy.value);
            cb._VBufferHistoryIsValid = hdCamera.volumetricHistoryIsValid ? 1u : 0u;

            GetHexagonalClosePackedSpheres7(m_xySeq);
            int sampleIndex = frameIndex % 7;
            Vector4 xySeqOffset = new Vector4();
            // TODO: should we somehow reorder offsets in Z based on the offset in XY? S.t. the samples more evenly cover the domain.
            // Currently, we assume that they are completely uncorrelated, but maybe we should correlate them somehow.
            xySeqOffset.Set(m_xySeq[sampleIndex].x, m_xySeq[sampleIndex].y, m_zSeq[sampleIndex], frameIndex);
            cb._VBufferSampleOffset = xySeqOffset;

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

            cb._MaxSliceCount = (uint)maxSliceCount;
            cb._MaxVolumetricFogDistance = fog.depthExtent.value;
            cb._VolumeCount = (uint)m_VisibleLocalVolumetricFogVolumes.Count;

            if (updateVoxelizationFields)
            {
                bool obliqueMatrix = GeometryUtils.IsProjectionMatrixOblique(hdCamera.camera.projectionMatrix);
                if (obliqueMatrix)
                {
                    // Convert the non oblique projection matrix to its  GPU version
                    var gpuProjNonOblique = GL.GetGPUProjectionMatrix(hdCamera.nonObliqueProjMatrix, true);
                    // Build the non oblique view projection matrix
                    var vpNonOblique = gpuProjNonOblique * hdCamera.mainViewConstants.viewMatrix;
                    cb._CameraInverseViewProjection_NO = vpNonOblique.inverse;
                }

                cb._IsObliqueProjectionMatrix = obliqueMatrix ? 1u : 0u;
                cb._CameraRight = hdCamera.camera.transform.right;
            }
        }

        class HeightFogVoxelizationPassData
        {
            public ComputeShader voxelizationCS;
            public int voxelizationKernel;

            public Vector4 resolution;
            public int viewCount;

            public ShaderVariablesVolumetric volumetricCB;

            public TextureHandle densityBuffer;
            public GraphicsBuffer volumetricAmbientProbeBuffer;

            // Underwater fog
            public bool water;
            public BufferHandle waterLine;
            public BufferHandle waterCameraHeight;
            public TextureHandle waterStencil;
        }

        class VolumetricFogVoxelizationPassData
        {
            public List<LocalVolumetricFog> volumetricFogs;
            public int maxSliceCount;
            public HDCamera hdCamera;

            public Vector3Int viewportSize;
            public TextureHandle densityBuffer;
            public RendererListHandle vfxRendererList;
            public RendererListHandle vfxDebugRendererList;
            public ShaderVariablesVolumetric volumetricCB;
            public TextureHandle fogOverdrawOutput;
            public bool fogOverdrawDebugEnabled;

            // Regular fogs
            public ComputeShader volumetricMaterialCS;
            public GraphicsBuffer globalIndirectBuffer;
            public GraphicsBuffer globalIndirectionBuffer;
            public GraphicsBuffer materialDataBuffer;
            public GraphicsBuffer visibleVolumeGlobalIndices;
            public int computeRenderingParametersKernel;
            public ComputeBuffer visibleVolumeBoundsBuffer;
        }

        unsafe TextureHandle ClearAndHeightFogVoxelizationPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, in TransparentPrepassOutput transparentPrepass)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                var fog = hdCamera.volumeStack.GetComponent<Fog>();
                ComputeVolumetricFogSliceCountAndScreenFraction(fog, out var maxSliceCount, out _);

                TextureHandle densityBuffer;
                int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                var currIdx = (frameIndex + 0) & 1;
                var currParams = hdCamera.vBufferParams[currIdx];

                using (var builder = renderGraph.AddRenderPass<HeightFogVoxelizationPassData>("Clear and Height Fog Voxelization", out var passData))
                {
                    passData.viewCount = hdCamera.viewCount;

                    passData.voxelizationCS = m_VolumeVoxelizationCS;
                    passData.voxelizationKernel = 0;

                    var cvp = currParams.viewportSize;

                    passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);

                    UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, passData.resolution, maxSliceCount, true);
                    passData.volumetricCB = m_ShaderVariablesVolumetricCB;

                    passData.densityBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                    { slices = s_CurrentVolumetricBufferSize.z, format = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferDensity" }));

                    passData.volumetricAmbientProbeBuffer = m_SkyManager.GetVolumetricAmbientProbeBuffer(hdCamera);

                    passData.water = transparentPrepass.waterGBuffer.valid && transparentPrepass.underWaterSurface != null;
                    if (passData.water)
                    {
                        passData.waterLine = builder.ReadBuffer(transparentPrepass.waterLine);
                        passData.waterCameraHeight = builder.ReadBuffer(transparentPrepass.waterGBuffer.cameraHeight);
                        passData.waterStencil = builder.ReadTexture(depthBuffer);
                    }

                    CoreUtils.SetKeyword(passData.voxelizationCS, "SUPPORT_WATER_ABSORPTION", passData.water);
                    builder.EnableAsyncCompute(hdCamera.frameSettings.VolumeVoxelizationRunsAsync() && !passData.water);

                    builder.SetRenderFunc(
                        (HeightFogVoxelizationPassData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VBufferDensity, data.densityBuffer);
                            ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._VolumeAmbientProbeBuffer, data.volumetricAmbientProbeBuffer);

                            // Underwater fog
                            if (data.water)
                            {
                                ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._WaterLineBuffer, data.waterLine);
                                ctx.cmd.SetComputeBufferParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._WaterCameraHeightBuffer, data.waterCameraHeight);
                                ctx.cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._RefractiveDepthBuffer, data.waterStencil, 0, RenderTextureSubElement.Depth);
                                ctx.cmd.SetComputeTextureParam(data.voxelizationCS, data.voxelizationKernel, HDShaderIDs._StencilTexture, data.waterStencil, 0, RenderTextureSubElement.Stencil);
                            }

                            ConstantBuffer.Push(ctx.cmd, data.volumetricCB, data.voxelizationCS, HDShaderIDs._ShaderVariablesVolumetric);

                            // The shader defines GROUP_SIZE_1D = 8.
                            ctx.cmd.DispatchCompute(data.voxelizationCS, data.voxelizationKernel, ((int)data.resolution.x + 7) / 8, ((int)data.resolution.y + 7) / 8, data.viewCount);
                        });

                    densityBuffer = passData.densityBuffer;
                }

                return densityBuffer;
            }

            return TextureHandle.nullHandle;
        }

        unsafe TextureHandle FogVolumeAndVFXVoxelizationPass(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle densityBuffer,
            ComputeBuffer visibleVolumeBoundsBuffer,
            CullingResults cullingResults)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                if (!SystemInfo.supportsRenderTargetArrayIndexFromVertexShader)
                {
                    Debug.LogError("Hardware not supported for Volumetric Materials");
                    return densityBuffer;
                }

                int frameIndex = (int)VolumetricFrameIndex(hdCamera);
                var currIdx = (frameIndex + 0) & 1;
                var currParams = hdCamera.vBufferParams[currIdx];

                var fog = hdCamera.volumeStack.GetComponent<Fog>();
                ComputeVolumetricFogSliceCountAndScreenFraction(fog, out var maxSliceCount, out _);

                TextureHandle debugOverdrawTexture = default;
                bool fogOverdrawDebugEnabled = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() && m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.LocalVolumetricFogOverdraw;
                if (fogOverdrawDebugEnabled)
                    debugOverdrawTexture = renderGraph.CreateTexture(
                        new TextureDesc(currParams.viewportSize.x, currParams.viewportSize.y, true, true)
                        {
                            name = "Volumetric Fog Overdraw", format = GetColorBufferFormat(),
                            clearBuffer = true, clearColor = Color.black
                        });

                using (var builder = renderGraph.AddRenderPass<VolumetricFogVoxelizationPassData>("Fog Volume And VFX Voxelization", out var passData))
                {
                    builder.AllowPassCulling(true);

                    var vfxFogVolumeRendererListDesc = new RendererListDesc(s_VolumetricFogPassNames, cullingResults, hdCamera.camera)
                    {
                        rendererConfiguration = PerObjectData.None,
                        renderQueueRange = HDRenderQueue.k_RenderQueue_All,
                        sortingCriteria = SortingCriteria.RendererPriority,
                        excludeObjectMotionVectors = false
                    };

                    passData.vfxRendererList = builder.UseRendererList(renderGraph.CreateRendererList(vfxFogVolumeRendererListDesc));
                    passData.densityBuffer = builder.WriteTexture(densityBuffer);
                    passData.viewportSize = currParams.viewportSize;
                    var cvp = currParams.viewportSize;
                    var res = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);;
                    UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, res, maxSliceCount, true);
                    passData.volumetricCB = m_ShaderVariablesVolumetricCB;
                    passData.fogOverdrawDebugEnabled = fogOverdrawDebugEnabled;
                    if (fogOverdrawDebugEnabled)
                        passData.fogOverdrawOutput = debugOverdrawTexture = builder.UseColorBuffer(debugOverdrawTexture, 0);

                    if (fogOverdrawDebugEnabled)
                    {
                        var vfxDebugFogRenderListDesc = new RendererListDesc(HDShaderPassNames.s_VolumetricFogVFXOverdrawDebugName, cullingResults, hdCamera.camera)
                        {
                            rendererConfiguration = PerObjectData.None,
                            renderQueueRange = HDRenderQueue.k_RenderQueue_All,
                            sortingCriteria = SortingCriteria.RendererPriority | SortingCriteria.RenderQueue,
                            excludeObjectMotionVectors = false
                        };

                        passData.vfxDebugRendererList = builder.UseRendererList(renderGraph.CreateRendererList(vfxDebugFogRenderListDesc));
                    }
                    passData.volumetricMaterialCS = runtimeShaders.volumetricMaterialCS;
                    passData.computeRenderingParametersKernel = passData.volumetricMaterialCS.FindKernel("ComputeVolumetricMaterialRenderingParameters");
                    passData.visibleVolumeBoundsBuffer = visibleVolumeBoundsBuffer;
                    passData.globalIndirectBuffer = LocalVolumetricFogManager.manager.globalIndirectBuffer;
                    passData.globalIndirectionBuffer = LocalVolumetricFogManager.manager.globalIndirectionBuffer;
                    passData.volumetricFogs = m_VisibleLocalVolumetricFogVolumes;
                    passData.materialDataBuffer = LocalVolumetricFogManager.manager.volumetricMaterialDataBuffer;
                    passData.maxSliceCount = maxSliceCount;
                    passData.hdCamera = hdCamera;
                    passData.visibleVolumeGlobalIndices = m_VisibleVolumeGlobalIndices;

                    builder.SetRenderFunc(
                        (VolumetricFogVoxelizationPassData data, RenderGraphContext ctx) =>
                        {
                            // Prepare draw indirect command for the draw
                            int volumeCount = data.volumetricFogs.Count;

                            // Compute the indirect arguments to render volumetric materials
                            ctx.cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, HDShaderIDs._VolumeBounds, data.visibleVolumeBoundsBuffer);
                            ctx.cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, HDShaderIDs._VolumetricGlobalIndirectArgsBuffer, data.globalIndirectBuffer);
                            ctx.cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, HDShaderIDs._VolumetricGlobalIndirectionBuffer, data.globalIndirectionBuffer);
                            ctx.cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, HDShaderIDs._VolumetricVisibleGlobalIndicesBuffer, data.visibleVolumeGlobalIndices);
                            ctx.cmd.SetComputeBufferParam(data.volumetricMaterialCS, data.computeRenderingParametersKernel, HDShaderIDs._VolumetricMaterialData, data.materialDataBuffer);
                            ctx.cmd.SetComputeIntParam(data.volumetricMaterialCS, HDShaderIDs._VolumeCount, volumeCount);
                            ctx.cmd.SetComputeIntParam(data.volumetricMaterialCS, HDShaderIDs._MaxSliceCount, data.maxSliceCount);
                            ctx.cmd.SetComputeIntParam(data.volumetricMaterialCS, HDShaderIDs._VolumetricViewCount, data.hdCamera.viewCount);
                            ConstantBuffer.PushGlobal(ctx.cmd, data.volumetricCB, HDShaderIDs._ShaderVariablesVolumetric);

                            int dispatchXCount = Mathf.Max(1, Mathf.CeilToInt((float)(volumeCount * data.hdCamera.viewCount) / 32.0f));
                            ctx.cmd.DispatchCompute(data.volumetricMaterialCS, data.computeRenderingParametersKernel, dispatchXCount, 1, 1);

                            ctx.cmd.SetGlobalBuffer(HDShaderIDs._VolumetricGlobalIndirectionBuffer, data.globalIndirectionBuffer);

                            CoreUtils.SetRenderTarget(ctx.cmd, data.densityBuffer);
                            ctx.cmd.SetViewport(new Rect(0, 0, data.viewportSize.x, data.viewportSize.y));
                            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, data.vfxRendererList);

                            if (data.fogOverdrawDebugEnabled)
                            {
                                CoreUtils.SetRenderTarget(ctx.cmd, data.fogOverdrawOutput);
                                CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, data.vfxDebugRendererList);
                            }
                        });
                }

                if (fogOverdrawDebugEnabled)
                    PushFullScreenDebugTexture(renderGraph, debugOverdrawTexture, FullScreenDebugMode.LocalVolumetricFogOverdraw);

                return densityBuffer;
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
            public BufferHandle bigTileVolumetricLightListBuffer;
            public GraphicsBuffer volumetricAmbientProbeBuffer;

            // Underwater
            public bool water;
            public BufferHandle waterLine;
            public BufferHandle waterCameraHeight;
            public TextureHandle waterStencil;
            public RenderTargetIdentifier causticsBuffer;
        }

        TextureHandle VolumetricLightingPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle densityBuffer,
            TextureHandle maxZBuffer, in TransparentPrepassOutput transparentPrepass, TextureHandle depthBuffer, BufferHandle bigTileVolumetricLightListBuffer, ShadowResult shadowResult)
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

                    passData.water = transparentPrepass.waterGBuffer.valid && transparentPrepass.underWaterSurface != null && transparentPrepass.underWaterSurface.caustics;

                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "LIGHTLOOP_DISABLE_TILE_AND_CLUSTER", !passData.tiledLighting);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_REPROJECTION", passData.enableReprojection);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "ENABLE_ANISOTROPY", enableAnisotropy);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "VL_PRESET_OPTIMAL", optimal);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "SUPPORT_LOCAL_LIGHTS", !fog.directionalLightsOnly.value);
                    CoreUtils.SetKeyword(passData.volumetricLightingCS, "SUPPORT_WATER_ABSORPTION", passData.water);

                    passData.volumetricLightingKernel = passData.volumetricLightingCS.FindKernel("VolumetricLighting");

                    passData.volumetricFilteringKernel = passData.volumetricLightingFilteringCS.FindKernel("FilterVolumetricLighting");

                    var cvp = currParams.viewportSize;

                    passData.resolution = new Vector4(cvp.x, cvp.y, 1.0f / cvp.x, 1.0f / cvp.y);
                    passData.viewCount = hdCamera.viewCount;
                    passData.filterVolume = ((int)fog.denoisingMode.value & (int)FogDenoisingMode.Gaussian) != 0;
                    passData.sliceCount = (int)(cvp.z);
                    passData.filteringNeedsExtraBuffer = !(SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.LoadStore));

                    ComputeVolumetricFogSliceCountAndScreenFraction(fog, out var maxSliceCount, out _);
                    UpdateShaderVariableslVolumetrics(ref m_ShaderVariablesVolumetricCB, hdCamera, passData.resolution, maxSliceCount);
                    passData.volumetricCB = m_ShaderVariablesVolumetricCB;
                    passData.lightListCB = m_ShaderVariablesLightListCB;

                    if (passData.tiledLighting)
                        passData.bigTileVolumetricLightListBuffer = builder.ReadBuffer(bigTileVolumetricLightListBuffer);
                    passData.densityBuffer = builder.ReadTexture(densityBuffer);
                    passData.depthTexture = builder.ReadTexture(depthTexture);
                    passData.maxZBuffer = builder.ReadTexture(maxZBuffer);
                    passData.lightingBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                    { slices = s_CurrentVolumetricBufferSize.z, format = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferLighting" }));

                    if (passData.filterVolume && passData.filteringNeedsExtraBuffer)
                    {
                        passData.filteringOutputBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(s_CurrentVolumetricBufferSize.x, s_CurrentVolumetricBufferSize.y, false, false)
                        { slices = s_CurrentVolumetricBufferSize.z, format = GraphicsFormat.R16G16B16A16_SFloat, dimension = TextureDimension.Tex3D, enableRandomWrite = true, name = "VBufferLightingFiltered" }));

                        CoreUtils.SetKeyword(passData.volumetricLightingFilteringCS, "NEED_SEPARATE_OUTPUT", passData.filteringNeedsExtraBuffer);
                    }

                    if (passData.enableReprojection)
                    {
                        passData.feedbackBuffer = builder.WriteTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[currIdx]));
                        passData.historyBuffer = builder.ReadTexture(renderGraph.ImportTexture(hdCamera.volumetricHistoryBuffers[prevIdx]));
                    }

                    passData.volumetricAmbientProbeBuffer = m_SkyManager.GetVolumetricAmbientProbeBuffer(hdCamera);

                    // Water stuff
                    if (passData.water)
                    {
                        passData.waterLine = builder.ReadBuffer(transparentPrepass.waterLine);
                        passData.waterCameraHeight = builder.ReadBuffer(transparentPrepass.waterGBuffer.cameraHeight);
                        passData.waterStencil = builder.ReadTexture(depthBuffer);
                        if (transparentPrepass.underWaterSurface.caustics)
                            passData.causticsBuffer = waterSystem.GetUnderWaterSurfaceCaustics();
                    }

                    HDShadowManager.ReadShadowResult(shadowResult, builder);

                    builder.SetRenderFunc(
                        (VolumetricLightingPassData data, RenderGraphContext ctx) =>
                        {
                            if (data.tiledLighting)
                                ctx.cmd.SetComputeBufferParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs.g_vBigTileLightList, data.bigTileVolumetricLightListBuffer);

                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._MaxZMaskTexture, data.maxZBuffer);  // Read

                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._CameraDepthTexture, data.depthTexture);  // Read
                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferDensity, data.densityBuffer);  // Read
                            ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VBufferLighting, data.lightingBuffer); // Write
                            ctx.cmd.SetComputeBufferParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._VolumeAmbientProbeBuffer, data.volumetricAmbientProbeBuffer);

                            // Underwater
                            if (data.water)
                            {
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._WaterCausticsDataBuffer, data.causticsBuffer);
                                ctx.cmd.SetComputeBufferParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._WaterLineBuffer, data.waterLine);
                                ctx.cmd.SetComputeBufferParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._WaterCameraHeightBuffer, data.waterCameraHeight);
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._RefractiveDepthBuffer, data.waterStencil, 0, RenderTextureSubElement.Depth);
                                ctx.cmd.SetComputeTextureParam(data.volumetricLightingCS, data.volumetricLightingKernel, HDShaderIDs._StencilTexture, data.waterStencil, 0, RenderTextureSubElement.Stencil);
                            }

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

        void PrepareAndPushVolumetricCBufferForVFXUpdate(CommandBuffer cmd, HDCamera hdCamera)
        {
            if (Fog.IsVolumetricFogEnabled(hdCamera))
            {
                var fog = hdCamera.volumeStack.GetComponent<Fog>();

                // VFX Update pass only need the max slice count right now
                ComputeVolumetricFogSliceCountAndScreenFraction(fog, out var maxSliceCount, out _);
                m_ShaderVariablesVolumetricCB._MaxSliceCount = (uint)maxSliceCount;

                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesVolumetricCB, HDShaderIDs._ShaderVariablesVolumetric);
            }
        }
    }
} // namespace UnityEngine.Rendering.HighDefinition
