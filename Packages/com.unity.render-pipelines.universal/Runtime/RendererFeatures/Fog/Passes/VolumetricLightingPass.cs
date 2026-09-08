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
            public static readonly int _MainLightVolumetricShadowParams = Shader.PropertyToID("_MainLightVolumetricShadowParams");
            public static readonly int _AdditionalLightsVolumetricParams = Shader.PropertyToID("_AdditionalLightsVolumetricParams");
            public static readonly int _MaxZMaskTexture = Shader.PropertyToID("_MaxZMaskTexture");
            public static readonly int _VBufferUnitDepthTexelSpacing = Shader.PropertyToID("_VBufferUnitDepthTexelSpacing");
            public static readonly int _VBufferVoxelSize = Shader.PropertyToID("_VBufferVoxelSize");
            public static readonly int _VolumetricLightingExtinctionCutoff = Shader.PropertyToID("_VolumetricLightingExtinctionCutoff");
            public static readonly int _VolumetricAmbientProbe = Shader.PropertyToID("_VolumetricAmbientProbe");

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
        LocalKeyword m_DisableTexture2DXArrayKeyword;
        LocalKeyword m_EnableReprojectionKeyword;
        LocalKeyword m_SupportLocalLightsKeyword;
        LocalKeyword m_SupportLocalLightsShadowsKeyword;
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
            public LocalKeyword supportLocalLightsShadowsKeyword;
            public bool depthIsArray;
            public bool applyLocalLights;
            public UniversalShadowData shadowData;

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
            public Matrix4x4 coordToViewDirWS;
            public Matrix4x4 viewMatrix;

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
            public readonly Vector4[] ambientProbe = new Vector4[7];

            // Additional lights
            public GraphicsBuffer additionalLightsCB;
            public int additionalLightsCBSizeBytes;
            public int additionalLightsCount;
            public NativeArray<VisibleLight> visibleLights;
            public int mainLightIndex;
            public ForwardLights forwardLights;
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

            // Linear (Built-in compatible) light falloff
            public LocalKeyword lightFalloffLinearKeyword;
            public bool useLinearFalloff;

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
            if (m_Shader == null)
                return;

            m_Kernel = m_Shader.FindKernel("VolumetricLighting");
            m_DisableTexture2DXArrayKeyword = new LocalKeyword(m_Shader, ShaderKeywordStrings.DisableTexture2DXArray);
            m_EnableReprojectionKeyword = new LocalKeyword(m_Shader, "ENABLE_REPROJECTION");
            m_SupportLocalLightsKeyword = new LocalKeyword(m_Shader, "SUPPORT_LOCAL_LIGHTS");
            m_SupportLocalLightsShadowsKeyword = new LocalKeyword(m_Shader, "SUPPORT_LOCAL_LIGHTS_SHADOWS");
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

                passData.disableTexture2DXArrayKeyword = m_DisableTexture2DXArrayKeyword;
                passData.enableReprojectionKeyword = m_EnableReprojectionKeyword;
                passData.supportLocalLightsKeyword = m_SupportLocalLightsKeyword;
                passData.supportLocalLightsShadowsKeyword = m_SupportLocalLightsShadowsKeyword;
                passData.depthIsArray = depthIsArray;
                passData.applyLocalLights = applyLocalLights;
                passData.shadowData = frameData.Get<UniversalShadowData>();

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
                passData.forwardLights = forwardLights;
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
                    passData.additionalCookieFormat = forwardLights.lightCookieManager?.additionalLightsCookieAtlasFormat ?? kLightCookieFormatNone;

                    builder.UseTexture(passData.mainCookieTexture, AccessFlags.Read);
                    builder.UseTexture(passData.additionalCookieAtlas, AccessFlags.Read);
                }

                // Linear (Built-in compatible) light falloff
                {
                    passData.lightFalloffLinearKeyword = new LocalKeyword(m_Shader, ShaderKeywordStrings.LightFalloffLinear);
                    passData.useLinearFalloff = applyLocalLights && UniversalRenderPipeline.IsLinearFalloffEnabled();
                }

                passData.fogAnisotropy = fogData.fogAnisotropy;
                passData.extinctionCutoff = fogData.extinctionCutoff;

                // Convolve the sky ambient probe with the Cornette-Shanks phase
                {
                    var sh = RenderSettings.ambientProbe;

                    // Undo the cosine rescaling
                    // -------------------------
                    // RenderSettings.ambientProbe  stores each coefficient pre-multiplied by
                    // SphericalHarmonicsL2::kNormalizationConstants, which fold the SH basis
                    // constants and the clamped-cosine (irradiance) kernel into the coefficients.
                    // Undo this before convolving with the phase function.
                    float fC0 = 0.2820947918f, fC1 = 0.3257350079f;
                    float fC2 = 0.2731371076f, fC3 = 0.0788478913f, fC4 = 0.1365685538f;
                    for (int c = 0; c < 3; c++)
                    {
                        sh[c, 0] /= fC0;
                        sh[c, 1] /= -fC1; sh[c, 2] /= fC1; sh[c, 3] /= -fC1;
                        sh[c, 4] /= fC2; sh[c, 5] /= -fC2; sh[c, 6] /= fC3; sh[c, 7] /= -fC2; sh[c, 8] /= fC4;
                    }

                    // Cornette-Shanks zonal harmonics
                    // -------------------------------
                    // A zonal harmonic is just an SH function that is rotationally symmetric about
                    // an axis. A phase function only depends on the angle between two directions,
                    // e.g. an incoming light direction and the view direction, i.e. p(cosθ). So it
                    // is rotationally symmetric, and thus a good fit for zonal harmonics.
                    float g = fogData.fogAnisotropy;
                    float g2 = g * g;
                    float zh0 = 0.282095f;
                    float zh1 = 0.293162f * g * (4.0f + g2) / (2.0f + g2);
                    float zh2 = (0.126157f + 1.44179f * g2 + 0.324403f * g2 * g2) / (2.0f + g2);

                    // Convolution of the ambient SH and the phase ZH
                    // ----------------------------------------------
                    // According to Peter-Pike Sloan's "Stupid Spherical Harmonics Tricks" (page 6),
                    // an SH can be convolved with a circularly symmetric function like a ZH using
                    // the formula:
                    // convolved_sh_{m, l} = sqrt(4π/(2l + 1)) * zh_l * sh_{m, l}
                    float p0 = Mathf.Sqrt(4.0f * Mathf.PI) * zh0;        // When l = 0
                    float p1 = Mathf.Sqrt(4.0f * Mathf.PI / 3.0f) * zh1; // When l = 1
                    float p2 = Mathf.Sqrt(4.0f * Mathf.PI / 5.0f) * zh2; // When l = 2
                    // Convolve across R, G, B channels, given by c = 0, c = 1, c = 2 respectively.
                    for (int c = 0; c < 3; c++)
                    {
                        // m = 0 band
                        sh[c, 0] *= p0;
                        // m = 1 band
                        sh[c, 1] *= p1; sh[c, 2] *= p1; sh[c, 3] *= p1;
                        // m = 2 band
                        sh[c, 4] *= p2; sh[c, 5] *= p2; sh[c, 6] *= p2; sh[c, 7] *= p2; sh[c, 8] *= p2;
                    }

                    // Premultiply by the SH basis constants
                    // --------------------------------------
                    // Evaluating an SH means summing coefficient_{l,m} * basisFunction_{l,m}(dir) over
                    // the 9 terms (band l = 0..2, order m = -l..l). Each basis function is a
                    // polynomial in the direction scaled by a fixed normalization constant. To keep
                    // per-pixel evaluation cheap, SampleSH9 drops those constants and evaluates the
                    // bare polynomials, so we have to fold the constants into the coefficients here
                    // instead. The signs come from the canonical SH convention that
                    // RenderSettings.ambientProbe uses; without them SampleSH9 reads back a probe that
                    // is both wrongly scaled and has some directional terms flipped.
                    float k0 = 0.2820947918f, k1 = 0.4886025119f;
                    float k2 = 1.0925484306f, k3 = 0.3153915653f, k4 = 0.5462742153f;
                    for (int c = 0; c < 3; c++)
                    {
                        sh[c, 0] *= k0;
                        sh[c, 1] *= -k1; sh[c, 2] *= k1; sh[c, 3] *= -k1;
                        sh[c, 4] *= k2; sh[c, 5] *= -k2; sh[c, 6] *= k3; sh[c, 7] *= -k2; sh[c, 8] *= k4;
                    }

                    // Pack into { x, y, z, DC } order for SampleSH9
                    // ---------------------------------------------
                    for (int c = 0; c < 3; c++)
                    {
                        passData.ambientProbe[c] = new Vector4(sh[c, 3], sh[c, 1], sh[c, 2], sh[c, 0] - sh[c, 6]);
                        passData.ambientProbe[3 + c] = new Vector4(sh[c, 4], sh[c, 5], sh[c, 6] * 3.0f, sh[c, 7]);
                    }
                    passData.ambientProbe[6] = new Vector4(sh[0, 8], sh[1, 8], sh[2, 8], 1.0f);
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
                    // when cutoffDistance / sliceDistributionUniformity didn't change since last frame.
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


                passData.cameraPositionWS = camera.transform.position;
                passData.viewMatrix = cameraData.GetViewMatrix(0);

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
                // Bind the v-buffer globally for the apply pass and transparent surfaces.
                builder.SetGlobalTextureAfterPass(passData.vbuffer, SharedIDs._VBuffer);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (VolumetricLightingPassData data, ComputeGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var cs = data.cs;
                    int kernel = data.kernel;

                    cmd.SetKeyword(cs, data.disableTexture2DXArrayKeyword, !data.depthIsArray);
                    cmd.SetKeyword(cs, data.enableReprojectionKeyword, data.enableReprojection);
                    bool sampleLocalShadows = data.applyLocalLights && data.shadowData.isKeywordAdditionalLightShadowsEnabled;
                    cmd.SetKeyword(cs, data.supportLocalLightsKeyword, data.applyLocalLights && !sampleLocalShadows);
                    cmd.SetKeyword(cs, data.supportLocalLightsShadowsKeyword, sampleLocalShadows);

                    cmd.SetComputeMatrixParam(cs, SharedIDs._VBufferCoordToViewDirWS, data.coordToViewDirWS);
                    cmd.SetComputeVectorParam(cs, SharedIDs._DepthDecodingParams, data.decodingParams);
                    cmd.SetComputeVectorParam(cs, SharedIDs._DepthEncodingParams, data.encodingParams);
                    cmd.SetComputeVectorParam(cs, SharedIDs._VBufferSize, data.vbufferViewportSize);
                    UniversalRenderPipeline.InitializeLightConstants_Common(data.visibleLights, data.mainLightIndex,
                        out var mainLightPos, out var mainLightCol, out _, out _, out _);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._MainLightDirection, new Vector4(mainLightPos.x, mainLightPos.y, mainLightPos.z, 0f));
                    // The per-light volumetric params are filled by ForwardLights' setup pass, which executes before
                    // this one; they must be read here at render time, not when this pass is recorded.
                    Vector4 mainLightVolumetricParams = data.forwardLights.mainLightVolumetricParams;
                    cmd.SetComputeVectorParam(cs, ShaderIDs._MainLightColorVolumetric,
                        new Vector4(mainLightCol.x, mainLightCol.y, mainLightCol.z, 0f) * mainLightVolumetricParams.x);
                    cmd.SetComputeVectorParam(cs, ShaderIDs._MainLightVolumetricShadowParams,
                        new Vector4(mainLightVolumetricParams.y, 1f - mainLightVolumetricParams.y, 0f, 0f));
                    if (data.applyLocalLights)
                        cmd.SetComputeVectorArrayParam(cs, ShaderIDs._AdditionalLightsVolumetricParams, data.forwardLights.additionalLightsVolumetricParams);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._FogAnisotropy, data.fogAnisotropy);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._CornetteShanksConstant, data.cornetteShanksConstant);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VolumetricLightingExtinctionCutoff, data.extinctionCutoff);
                    cmd.SetComputeVectorArrayParam(cs, ShaderIDs._VolumetricAmbientProbe, data.ambientProbe);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VBufferVoxelSize, data.voxelSize);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._VBufferUnitDepthTexelSpacing, data.unitDepthTexelSpacing);
                    cmd.SetComputeIntParam(cs, SharedIDs._VBufferSliceCount, data.sliceCount);
                    cmd.SetComputeFloatParam(cs, SharedIDs._VBufferRcpSliceCount, 1.0f / data.sliceCount);
                    cmd.SetComputeFloatParam(cs, SharedIDs._CameraNearPlane, data.cameraNearPlane);
                    cmd.SetComputeFloatParam(cs, ShaderIDs._HalfVoxelArcLength, data.halfVoxelArcLength);
                    cmd.SetComputeVectorParam(cs, SharedIDs._CameraPositionWS, data.cameraPositionWS);
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

                    // Linear (Built-in compatible) light falloff
                    cmd.SetKeyword(cs, data.lightFalloffLinearKeyword, data.useLinearFalloff);

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
