#if VOLUMETRIC_FOG

using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;
using SharedIDs = UnityEngine.Rendering.Universal.VolumetricFogRendererFeature.ShaderIDs;

namespace UnityEngine.Rendering.Universal
{
    internal class VolumetricLightingPass : ScriptableRenderPass, IDisposable
    {
        static class ShaderIDs
        {
            public static readonly int _FogAnisotropy = Shader.PropertyToID("_FogAnisotropy");
            public static readonly int _CornetteShanksConstant = Shader.PropertyToID("_CornetteShanksConstant");
            public static readonly int _HalfVoxelArcLength = Shader.PropertyToID("_HalfVoxelArcLength");
            public static readonly int _MainLightColorVolumetric = Shader.PropertyToID("_MainLightColorVolumetric");
            public static readonly int _MainLightDirection = Shader.PropertyToID("_MainLightDirection");
            public static readonly int _MaxZMaskTexture = Shader.PropertyToID("_MaxZMaskTexture");
            public static readonly int _VBufferUnitDepthTexelSpacing = Shader.PropertyToID("_VBufferUnitDepthTexelSpacing");
            public static readonly int _VBufferVoxelSize = Shader.PropertyToID("_VBufferVoxelSize");
            public static readonly int _VolumetricLightingExtinctionCutoff = Shader.PropertyToID("_VolumetricLightingExtinctionCutoff");
            public static readonly int _FogAmbientProbe = Shader.PropertyToID("_FogAmbientProbe");

            // Reprojection
            public static readonly int _VBufferHistory = Shader.PropertyToID("_VBufferHistory");
            public static readonly int _VBufferFeedback = Shader.PropertyToID("_VBufferFeedback");
            public static readonly int _VBufferSampleOffset = Shader.PropertyToID("_VBufferSampleOffset");
            public static readonly int _VBufferHistoryIsValid = Shader.PropertyToID("_VBufferHistoryIsValid");
            public static readonly int _VBufferHistoryViewportScale = Shader.PropertyToID("_VBufferHistoryViewportScale");
            public static readonly int _VBufferHistoryViewportLimit = Shader.PropertyToID("_VBufferHistoryViewportLimit");
            public static readonly int _VBufferPrevDistanceEncodingParams = Shader.PropertyToID("_VBufferPrevDistanceEncodingParams");
            public static readonly int _VBufferPrevDistanceDecodingParams = Shader.PropertyToID("_VBufferPrevDistanceDecodingParams");
            public static readonly int _PrevCamPosWS = Shader.PropertyToID("_PrevCamPosWS");
            public static readonly int _VBufferPrevViewProjMatrix = Shader.PropertyToID("_VBufferPrevViewProjMatrix");

            public static readonly int _AdditionalLightsCount = Shader.PropertyToID("_AdditionalLightsCount");
            public static readonly int AdditionalLights = Shader.PropertyToID("AdditionalLights");
            public static readonly int urp_ZBinBuffer = Shader.PropertyToID("urp_ZBinBuffer");
            public static readonly int urp_TileBuffer = Shader.PropertyToID("urp_TileBuffer");
            public static readonly int urp_ZBins = Shader.PropertyToID("urp_ZBins");
            public static readonly int urp_Tiles = Shader.PropertyToID("urp_Tiles");
            public static readonly int _FPParams0 = Shader.PropertyToID("_FPParams0");
            public static readonly int _FPParams1 = Shader.PropertyToID("_FPParams1");
            public static readonly int _FPParams2 = Shader.PropertyToID("_FPParams2");
            public static readonly int _WorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
            public static readonly int unity_MatrixV = Shader.PropertyToID("unity_MatrixV");
            public static readonly int unity_OrthoParams = Shader.PropertyToID("unity_OrthoParams");

            // Light cookies
            public static readonly int _MainLightCookieTexture = Shader.PropertyToID("_MainLightCookieTexture");
            public static readonly int _AdditionalLightsCookieAtlasTexture = Shader.PropertyToID("_AdditionalLightsCookieAtlasTexture");
            public static readonly int _MainLightWorldToLight = Shader.PropertyToID("_MainLightWorldToLight");
            public static readonly int _MainLightCookieTextureFormat = Shader.PropertyToID("_MainLightCookieTextureFormat");
            public static readonly int _AdditionalLightsCookieAtlasTextureFormat = Shader.PropertyToID("_AdditionalLightsCookieAtlasTextureFormat");
            public static readonly int LightCookies = Shader.PropertyToID("LightCookies");
        }

        const float kLightCookieFormatNone = -1f;
        const int kZeroLightCBVec4sPerLight = 5;

        ComputeShader m_Shader;
        int m_Kernel;
        RTHandle m_MainLightCookieRTHandle;
        Texture m_MainLightCookieSrc;
        // Zero-filled stand-ins
        GraphicsBuffer m_ZeroLightCB;
        int m_ZeroLightCBMaxLights;

        static readonly System.Collections.Generic.Dictionary<EntityId, int> s_FrameCounters = new();
        // 7 hexagonal-close-packed XY offsets (within (-0.5, 0.5)^2, rotated 15° to maximise XY coverage) and 7 Z
        // offsets paired so adjacent frames roughly cover the full slice extent.
        static readonly Vector2[] s_XySeq = ComputeHexagonalClosePackedSpheres7();
        static readonly float[] s_ZSeq = { 7f / 14f, 3f / 14f, 11f / 14f, 5f / 14f, 9f / 14f, 1f / 14f, 13f / 14f };

        class VolumetricLightingPassData
        {
            // Shader
            public ComputeShader cs;
            public int kernel;
            public bool enableReprojection;

            // Keywords
            public LocalKeyword disableTexture2DXArrayKeyword;
            public LocalKeyword enableReprojectionKeyword;
            public LocalKeyword supportLocalLightsKeyword;
            public bool depthIsArray;
            public bool applyLocalLights;

            // Textures
            public TextureHandle depthTexture;
            public TextureHandle maxZMaskTexture;
            public TextureHandle vbufferDensity;
            public TextureHandle vbuffer;

            // Reprojection textures (valid when enableReprojection)
            public TextureHandle historyBuffer;
            public TextureHandle feedbackBuffer;

            // Camera
            public Vector4 cameraPositionWS;
            public float cameraNearPlane;
            public Vector4 zBufferParams;
            public Matrix4x4 coordToViewDirWS;
            public Matrix4x4 viewMatrix;
            public Vector4 orthoParams;

            // VBuffer geometry
            public int vbufferW, vbufferH;
            public int sliceCount;
            public Vector4 vbufferViewportSize;
            public float voxelSize;
            public float unitDepthTexelSpacing;
            public Vector4 decodingParams;
            public Vector4 encodingParams;
            public float halfVoxelArcLength;

            // Fog
            public float fogAnisotropy;
            public float cornetteShanksConstant;
            public float extinctionCutoff;
            public Vector4 ambientProbe;

            // Additional lights
            public GraphicsBuffer additionalLightsCB;
            public int additionalLightsCBSizeBytes;
            public int additionalLightsCount;
            public NativeArray<VisibleLight> visibleLights;
            public int mainLightIndex;
            public GraphicsBuffer zBinsBuffer;
            public GraphicsBuffer tileMasksBuffer;
            public Vector4 fpParams0, fpParams1, fpParams2;

            // Light cookies
            public LightCookieManager cookieManager;
            public bool sampleCookies;
            public TextureHandle mainCookieTexture;
            public Matrix4x4 mainCookieWorldToLight;
            public float mainCookieFormat;
            public TextureHandle additionalCookieAtlas;
            public float additionalCookieFormat;

            // Zero-filled stand-in
            public GraphicsBuffer zeroLightCB;
            public int zeroLightCBSizeBytes;
            public int zeroCookieCBSizeBytes;

            // Reprojection constants
            public Vector4 sampleOffset; // xy = HCP jitter, z = slice jitter, w = frame index
            public uint historyIsValid;
            public Vector4 historyViewportScale; // (vbufferW / historyW, vbufferH / historyH, sliceCount / historyD)
            public Vector4 historyViewportLimit; // ((vbufferW - 0.5) / historyW, ...)
            public Vector4 prevEncodingParams;
            public Vector4 prevDecodingParams;
            public Vector4 prevCamPosWS;
            public Matrix4x4 prevViewProj;
        }

        public VolumetricLightingPass()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<VolumetricFogResources>(out var resources);
            m_Shader = resources.volumetricLightingCS;
            if (m_Shader != null)
                m_Kernel = m_Shader.FindKernel("VolumetricLighting");
        }

        public void Dispose()
        {
            m_ZeroLightCB?.Dispose();
            m_ZeroLightCB = null;
            m_MainLightCookieRTHandle?.Release();
            m_MainLightCookieRTHandle = null;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Shader == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();
            var fogData = frameData.GetOrCreate<VolumetricFogFrameData>();

            if (!fogData.vbufferDensity.IsValid())
                return;

            var maxZMaskTexture = fogData.maxZMaskTexture;
            if (!resourceData.cameraDepthTexture.IsValid() || !maxZMaskTexture.IsValid())
                return;

            ref readonly var vBufferParams = ref fogData.vBufferParams;

            Camera camera = cameraData.camera;
            bool depthIsArray = cameraData.cameraTargetDescriptor.dimension == TextureDimension.Tex2DArray;

            EntityId cameraId = camera.GetEntityId();
            s_FrameCounters.TryGetValue(cameraId, out int frameIndex);
            s_FrameCounters[cameraId] = frameIndex + 1;

            // Reprojection setup
            bool cameraSupportsReprojection = camera.cameraType == CameraType.Game
                || (camera.cameraType == CameraType.SceneView && CoreUtils.AreAnimatedMaterialsEnabled(camera));
            bool reprojectionRequested = fogData.enableReprojection && cameraSupportsReprojection;
            VolumetricFogHistory historyForRead = null;
            VolumetricFogHistory historyForWrite = null;
            if (reprojectionRequested && cameraData.historyManager != null)
            {
                cameraData.historyManager.RequestAccess<VolumetricFogHistory>();
                historyForWrite = cameraData.historyManager.GetHistoryForWrite<VolumetricFogHistory>();
                if (historyForWrite != null)
                {
                    bool ok = historyForWrite.Update(vBufferParams.vbufferW, vBufferParams.vbufferH, vBufferParams.sliceCount);
                    if (!ok)
                        historyForWrite = null;
                }
                historyForRead = cameraData.historyManager.GetHistoryForRead<VolumetricFogHistory>();
            }
            bool reprojectionActive = historyForWrite != null;

            bool applyLocalLights = fogData.lightFilter != VolumetricFogLightFilter.DirectionalOnly
                                    && RenderingUtils.usePersistentConstantBuffer
                                    && ((UniversalRenderer)cameraData.renderer).usesClusterLightLoop;

            using (var builder = renderGraph.AddComputePass<VolumetricLightingPassData>("Volumetric Lighting", out var passData))
            {
                passData.cs = m_Shader;
                passData.kernel = m_Kernel;
                passData.enableReprojection = reprojectionActive;

                passData.disableTexture2DXArrayKeyword = new LocalKeyword(m_Shader, ShaderKeywordStrings.DisableTexture2DXArray);
                passData.enableReprojectionKeyword = new LocalKeyword(m_Shader, "ENABLE_REPROJECTION");
                passData.supportLocalLightsKeyword = new LocalKeyword(m_Shader, "SUPPORT_LOCAL_LIGHTS");
                passData.depthIsArray = depthIsArray;
                passData.applyLocalLights = applyLocalLights;

                passData.vbufferW = vBufferParams.vbufferW;
                passData.vbufferH = vBufferParams.vbufferH;
                passData.sliceCount = vBufferParams.sliceCount;
                passData.coordToViewDirWS = vBufferParams.coordToViewDirWS;
                passData.decodingParams = vBufferParams.decodingParams;
                passData.encodingParams = vBufferParams.encodingParams;
                passData.vbufferViewportSize = vBufferParams.viewportSize;
                passData.halfVoxelArcLength = vBufferParams.halfVoxelArcLength;

                passData.visibleLights = lightData.visibleLights;
                passData.mainLightIndex = lightData.mainLightIndex;

                var forwardLights = ((UniversalRenderer)cameraData.renderer).forwardLights;
                passData.zBinsBuffer = forwardLights.zBinsBuffer;
                passData.tileMasksBuffer = forwardLights.tileMasksBuffer;
                {
                    var pixelSize = cameraData.pixelRect.size;
                    passData.fpParams0 = new Vector4(forwardLights.zBinScale, forwardLights.zBinOffset, forwardLights.lightCount, forwardLights.directionalLightCount);
                    passData.fpParams1 = new Vector4(pixelSize.x / forwardLights.actualTileWidth, pixelSize.y / forwardLights.actualTileWidth, forwardLights.tileResolution.x, forwardLights.wordsPerTile);
                    passData.fpParams2 = new Vector4(forwardLights.binCount, forwardLights.tileResolution.x * forwardLights.tileResolution.y, 0, 0);
                }

                passData.additionalLightsCB = forwardLights.additionalLightsConstantBuffer;
                passData.additionalLightsCBSizeBytes = forwardLights.additionalLightsConstantBufferSizeBytes;
                passData.additionalLightsCount = applyLocalLights ? lightData.additionalLightsCount : 0;
                passData.cookieManager = forwardLights.lightCookieManager;
                passData.sampleCookies = fogData.enableLightCookies;

                // Zero stand-in for the AdditionalLights and LightCookies cbuffers, bound when the owning system's
                // buffer is unavailable.
                {
                    int maxLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
                    int additionalLightsVec4 = kZeroLightCBVec4sPerLight * maxLights;
                    int cookieVec4 = maxLights * 6 + CoreUtils.DivRoundUp(maxLights, 32);
                    if (m_ZeroLightCB == null || m_ZeroLightCBMaxLights != maxLights)
                    {
                        m_ZeroLightCB?.Dispose();
                        m_ZeroLightCB = new GraphicsBuffer(GraphicsBuffer.Target.Constant, cookieVec4, 16);
                        m_ZeroLightCB.SetData(new Vector4[cookieVec4]);
                        m_ZeroLightCBMaxLights = maxLights;
                    }
                    passData.zeroLightCB = m_ZeroLightCB;
                    passData.zeroLightCBSizeBytes = additionalLightsVec4 * 16;
                    passData.zeroCookieCBSizeBytes = cookieVec4 * 16;
                }

                // Cookie textures
                {
                    var whiteTexture = renderGraph.defaultResources.whiteTexture;

                    Texture mainCookie = null;
                    passData.mainCookieWorldToLight = Matrix4x4.identity;
                    passData.mainCookieFormat = kLightCookieFormatNone;
                    if (forwardLights.lightCookieManager != null && passData.mainLightIndex >= 0)
                    {
                        var mainLight = passData.visibleLights[passData.mainLightIndex];
                        forwardLights.lightCookieManager.GetMainLightCookieData(ref mainLight,
                            out mainCookie, out passData.mainCookieWorldToLight, out passData.mainCookieFormat);
                    }

                    if (mainCookie != null)
                    {
                        if (m_MainLightCookieRTHandle == null || m_MainLightCookieSrc != mainCookie)
                        {
                            m_MainLightCookieRTHandle?.Release();
                            m_MainLightCookieRTHandle = RTHandles.Alloc(mainCookie);
                            m_MainLightCookieSrc = mainCookie;
                        }
                        passData.mainCookieTexture = renderGraph.ImportTexture(m_MainLightCookieRTHandle);
                    }
                    else
                    {
                        passData.mainCookieTexture = whiteTexture;
                    }

                    var cookieAtlas = forwardLights.lightCookieManager?.AdditionalLightsCookieAtlasTexture;
                    passData.additionalCookieAtlas = cookieAtlas != null ? renderGraph.ImportTexture(cookieAtlas) : whiteTexture;
                    passData.additionalCookieFormat = cookieAtlas != null
                        ? (float)forwardLights.lightCookieManager.GetLightCookieShaderFormat(cookieAtlas.rt.graphicsFormat)
                        : kLightCookieFormatNone;

                    builder.UseTexture(passData.mainCookieTexture, AccessFlags.Read);
                    builder.UseTexture(passData.additionalCookieAtlas, AccessFlags.Read);
                }

                passData.fogAnisotropy = fogData.fogAnisotropy;
                passData.extinctionCutoff = fogData.extinctionCutoff;

                // Bake the ambient probe L0 into a single Vector4 the kernel can read with no math.
                {
                    var probe = RenderSettings.ambientProbe;
                    float scale = Mathf.PI * Mathf.Sqrt(4.0f * Mathf.PI);
                    float r = probe[0, 0] * scale;
                    float g = probe[1, 0] * scale;
                    float b = probe[2, 0] * scale;
                    passData.ambientProbe = new Vector4(r, g, b, 0f);
                }
                // (3 / (8π)) · (1 - g²) / (2 + g²) — direction-independent part of the Cornette-Shanks phase function.
                // Avoids re-multiplying an extra constant per light.
                {
                    float g = fogData.fogAnisotropy;
                    passData.cornetteShanksConstant = (3f / (8f * Mathf.PI)) * (1f - g * g) / (2f + g * g);
                }
                passData.voxelSize = vBufferParams.voxelSize;
                passData.unitDepthTexelSpacing = vBufferParams.unitDepthTexelSpacing;
                passData.cameraNearPlane = camera.nearClipPlane;

                // Per-frame jitter offset
                int sampleIndex = frameIndex % 7;
                passData.sampleOffset = new Vector4(s_XySeq[sampleIndex].x, s_XySeq[sampleIndex].y, s_ZSeq[sampleIndex], frameIndex);

                // History bookkeeping for the reprojection branch.
                if (reprojectionActive)
                {
                    passData.feedbackBuffer = renderGraph.ImportTexture(historyForWrite.GetCurrentTexture());
                    builder.UseTexture(passData.feedbackBuffer, AccessFlags.Write);

                    var historyTex = historyForRead?.GetPreviousTexture() ?? historyForWrite.GetCurrentTexture();
                    passData.historyBuffer = renderGraph.ImportTexture(historyTex);
                    builder.UseTexture(passData.historyBuffer, AccessFlags.Read);

                    // The history needs at least 2 valid frames before we can blend.
                    bool valid = historyForRead != null && historyForRead.validFrames >= 2;
                    passData.historyIsValid = valid ? 1u : 0u;

                    passData.historyViewportScale = new Vector4(1f, 1f, 1f, 0f);
                    passData.historyViewportLimit = new Vector4(
                        1f - 0.5f / vBufferParams.vbufferW,
                        1f - 0.5f / vBufferParams.vbufferH,
                        1f - 0.5f / vBufferParams.sliceCount,
                        0f);

                    // Previous-frame log-depth params. Currently reuses the current-frame params, which is correct only
                    // when depthExtent / sliceDistributionUniformity didn't change since last frame.
                    passData.prevEncodingParams = vBufferParams.encodingParams;
                    passData.prevDecodingParams = vBufferParams.decodingParams;

                    // Pull previous view-projection / camera position from the per-camera motion tracker.
                    Matrix4x4 prevVP = Matrix4x4.identity;
                    Vector3 prevCamPos = camera.transform.position;
                    if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var additional))
                    {
                        var motionData = additional.motionVectorsPersistentData;
                        prevVP = motionData.previousViewProjection;
                        prevCamPos = motionData.previousWorldSpaceCameraPos;
                    }
                    passData.prevViewProj = prevVP;
                    passData.prevCamPosWS = new Vector4(prevCamPos.x, prevCamPos.y, prevCamPos.z, 0f);
                }

                // Compute _ZBufferParams
                {
                    float nearPlane = camera.nearClipPlane;
                    float far = camera.farClipPlane;
                    float invNear = Mathf.Approximately(nearPlane, 0.0f) ? 0.0f : 1.0f / nearPlane;
                    float invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;
                    float zc0 = 1.0f - far * invNear;
                    float zc1 = far * invNear;
                    var zBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);
                    if (SystemInfo.usesReversedZBuffer)
                    {
                        zBufferParams.y += zBufferParams.x;
                        zBufferParams.x = -zBufferParams.x;
                        zBufferParams.w += zBufferParams.z;
                        zBufferParams.z = -zBufferParams.z;
                    }
                    passData.zBufferParams = zBufferParams;
                }

                passData.cameraPositionWS = camera.transform.position;
                passData.viewMatrix = cameraData.GetViewMatrix(0);
                float orthoHalfH = camera.orthographicSize;
                float orthoHalfW = orthoHalfH * camera.aspect;
                passData.orthoParams = new Vector4(orthoHalfW, orthoHalfH, 0f, camera.orthographic ? 1f : 0f);

                // Input textures
                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(passData.depthTexture, AccessFlags.Read);

                passData.maxZMaskTexture = maxZMaskTexture;
                builder.UseTexture(passData.maxZMaskTexture, AccessFlags.Read);

                if (resourceData.mainShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.mainShadowsTexture, AccessFlags.Read);
                if (resourceData.additionalShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.additionalShadowsTexture, AccessFlags.Read);

                passData.vbufferDensity = fogData.vbufferDensity;
                builder.UseTexture(passData.vbufferDensity, AccessFlags.Read);

                // Output v-buffer
                passData.vbuffer = renderGraph.CreateTexture(new TextureDesc(vBufferParams.vbufferW, vBufferParams.vbufferH)
                {
                    format = GraphicsFormat.R16G16B16A16_SFloat,
                    dimension = TextureDimension.Tex3D,
                    slices = vBufferParams.sliceCount,
                    enableRandomWrite = true,
                    name = "VBuffer Lighting"
                });
                builder.UseTexture(passData.vbuffer, AccessFlags.ReadWrite);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (VolumetricLightingPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    int kernel = data.kernel;

                    cmd.SetKeyword(cs, data.disableTexture2DXArrayKeyword, !data.depthIsArray);
                    cmd.SetKeyword(cs, data.enableReprojectionKeyword, data.enableReprojection);
                    cmd.SetKeyword(cs, data.supportLocalLightsKeyword, data.applyLocalLights);

                    cmd.SetComputeMatrixParam(cs, SharedIDs._VBufferCoordToViewDirWS, data.coordToViewDirWS);
                    cmd.SetComputeVectorParam(cs, SharedIDs._DepthDecodingParams, data.decodingParams);
                    cmd.SetComputeVectorParam(cs, SharedIDs._DepthEncodingParams, data.encodingParams);
                    cmd.SetComputeVectorParam(cs, SharedIDs._VBufferSize, data.vbufferViewportSize);
                    UniversalRenderPipeline.InitializeLightConstants_Common(data.visibleLights, data.mainLightIndex,
                        out var mainLightPos, out var mainLightCol, out _, out _, out _);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._MainLightDirection, new Vector4(mainLightPos.x, mainLightPos.y, mainLightPos.z, 0f));
                    cmd.SetComputeVectorParam(cs, ShaderIDs._MainLightColorVolumetric, new Vector4(mainLightCol.x, mainLightCol.y, mainLightCol.z, 0f));
                    cmd.SetComputeFloatParam(cs, ShaderIDs._FogAnisotropy, data.fogAnisotropy);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._CornetteShanksConstant, data.cornetteShanksConstant);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VolumetricLightingExtinctionCutoff, data.extinctionCutoff);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._FogAmbientProbe, data.ambientProbe);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VBufferVoxelSize, data.voxelSize);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VBufferUnitDepthTexelSpacing, data.unitDepthTexelSpacing);
                    cmd.SetComputeIntParam(cs, SharedIDs._VBufferSliceCount, data.sliceCount);
                    cmd.SetComputeFloatParam(cs, SharedIDs._VBufferRcpSliceCount, 1.0f / data.sliceCount);
                    cmd.SetComputeFloatParam(cs, SharedIDs._CameraNearPlane, data.cameraNearPlane);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._HalfVoxelArcLength, data.halfVoxelArcLength);
                    cmd.SetComputeVectorParam(cs, SharedIDs._CameraPositionWS, data.cameraPositionWS);
                    cmd.SetComputeVectorParam(cs, SharedIDs._ZBufferParams, data.zBufferParams);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._VBufferSampleOffset, data.sampleOffset);

                    cmd.SetComputeTextureParam(cs, kernel, SharedIDs._CameraDepthTexture, data.depthTexture);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._MaxZMaskTexture, data.maxZMaskTexture);
                    cmd.SetComputeTextureParam(cs, kernel, SharedIDs._VBufferDensity, data.vbufferDensity);
                    cmd.SetComputeTextureParam(cs, kernel, SharedIDs._VBuffer, data.vbuffer);

                    // Bind URP's persistent AdditionalLights constant buffer, or the zero stand-in if ForwardLights
                    // didn't provide one.
                    {
                        GraphicsBuffer additionalLightsCB = data.additionalLightsCB ?? data.zeroLightCB;
                        int additionalLightsCBSizeBytes = data.additionalLightsCB != null ? data.additionalLightsCBSizeBytes : data.zeroLightCBSizeBytes;
                        cmd.SetComputeConstantBufferParam(cs, ShaderIDs.AdditionalLights, additionalLightsCB, 0, additionalLightsCBSizeBytes);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._AdditionalLightsCount,
                            new Vector4(data.additionalLightsCount, 0f, 0f, 0f));
                    }

                    if (data.zBinsBuffer != null && data.tileMasksBuffer != null)
                    {
                        if (RenderingUtils.useStructuredBuffer)
                        {
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs.urp_ZBins, data.zBinsBuffer);
                            cmd.SetComputeBufferParam(cs, kernel, ShaderIDs.urp_Tiles, data.tileMasksBuffer);
                        }
                        else
                        {
                            cmd.SetComputeConstantBufferParam(cs, ShaderIDs.urp_ZBinBuffer,
                                data.zBinsBuffer, 0, UniversalRenderPipeline.maxZBinWords * 4);
                            cmd.SetComputeConstantBufferParam(cs, ShaderIDs.urp_TileBuffer,
                                data.tileMasksBuffer, 0, UniversalRenderPipeline.maxTileWords * 4);
                        }

                        cmd.SetComputeVectorParam(cs, ShaderIDs._FPParams0, data.fpParams0);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._FPParams1, data.fpParams1);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._FPParams2, data.fpParams2);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._WorldSpaceCameraPos, data.cameraPositionWS);
                        cmd.SetComputeMatrixParam(cs, ShaderIDs.unity_MatrixV, data.viewMatrix);
                        cmd.SetComputeVectorParam(cs, ShaderIDs.unity_OrthoParams, data.orthoParams);
                    }

                    // Light cookies
                    var cookieManager = data.cookieManager;
                    bool sampleCookies = data.sampleCookies && cookieManager != null && cookieManager.IsKeywordLightCookieEnabled;
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._MainLightCookieTexture, data.mainCookieTexture);
                    cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._AdditionalLightsCookieAtlasTexture, data.additionalCookieAtlas);
                    cmd.SetComputeMatrixParam(cs, ShaderIDs._MainLightWorldToLight, sampleCookies ? data.mainCookieWorldToLight : Matrix4x4.identity);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._MainLightCookieTextureFormat, sampleCookies ? data.mainCookieFormat : kLightCookieFormatNone);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._AdditionalLightsCookieAtlasTextureFormat, sampleCookies ? data.additionalCookieFormat : kLightCookieFormatNone);
                    {
                        GraphicsBuffer lightCookieCB = data.zeroLightCB;
                        int lightCookieCBSizeBytes = data.zeroCookieCBSizeBytes;
                        if (sampleCookies && cookieManager.AdditionalLightsCookieConstantBuffer != null)
                        {
                            lightCookieCB = cookieManager.AdditionalLightsCookieConstantBuffer;
                            lightCookieCBSizeBytes = cookieManager.AdditionalLightsCookieConstantBufferSizeBytes;
                        }
                        cmd.SetComputeConstantBufferParam(cs, ShaderIDs.LightCookies, lightCookieCB, 0, lightCookieCBSizeBytes);
                    }

                    if (data.enableReprojection)
                    {
                        cmd.SetComputeIntParam(cs, ShaderIDs._VBufferHistoryIsValid, (int)data.historyIsValid);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._VBufferHistoryViewportScale, data.historyViewportScale);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._VBufferHistoryViewportLimit, data.historyViewportLimit);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._VBufferPrevDistanceEncodingParams, data.prevEncodingParams);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._VBufferPrevDistanceDecodingParams, data.prevDecodingParams);
                        cmd.SetComputeVectorParam(cs, ShaderIDs._PrevCamPosWS, data.prevCamPosWS);
                        cmd.SetComputeMatrixParam(cs, ShaderIDs._VBufferPrevViewProjMatrix, data.prevViewProj);
                        cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._VBufferHistory, data.historyBuffer);
                        cmd.SetComputeTextureParam(cs, kernel, ShaderIDs._VBufferFeedback, data.feedbackBuffer);
                    }

                    int dispatchX = CoreUtils.DivRoundUp(data.vbufferW, 8);
                    int dispatchY = CoreUtils.DivRoundUp(data.vbufferH, 8);
                    cmd.DispatchCompute(cs, kernel, dispatchX, dispatchY, 1);
                });

                fogData.vbuffer = passData.vbuffer;
                fogData.encodingParams = vBufferParams.encodingParams;
                fogData.decodingParams = vBufferParams.decodingParams;
                fogData.viewportSize = vBufferParams.viewportSize;
            }
        }

        // 7 hexagonally close-packed disc samples within (-0.5, 0.5)^2, rotated 15° to maximise
        // coverage along XY.
        static Vector2[] ComputeHexagonalClosePackedSpheres7()
        {
            const float r = 0.17054068870105443882f;
            float d = 2 * r;
            float s = r * Mathf.Sqrt(3f);
            var coords = new[]
            {
                new Vector2(0, 0),
                new Vector2(-d, 0),
                new Vector2(d, 0),
                new Vector2(-r, -s),
                new Vector2(r, s),
                new Vector2(r, -s),
                new Vector2(-r, s),
            };
            const float cos15 = 0.96592582628906828675f;
            const float sin15 = 0.25881904510252076235f;
            for (int i = 0; i < coords.Length; i++)
            {
                var c = coords[i];
                coords[i] = new Vector2(c.x * cos15 - c.y * sin15, c.x * sin15 + c.y * cos15);
            }
            return coords;
        }
    }
}

#endif
