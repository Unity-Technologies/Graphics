using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map atlas for additional shadow-casting Lights.
    /// </summary>
    public partial class AdditionalLightsShadowCasterPass : ScriptableRenderPass
    {
        // Internal
        internal RTHandle m_AdditionalLightsShadowmapHandle;

        // Private
        private int renderTargetWidth;
        private int renderTargetHeight;
        private bool m_CreateEmptyShadowmap;
        private bool m_EmptyShadowmapNeedsClear;
        private bool m_IssuedMessageAboutShadowSlicesTooMany;
        private bool m_IssuedMessageAboutShadowMapsRescale;
        private bool m_IssuedMessageAboutShadowMapsTooBig;
        private bool m_IssuedMessageAboutRemovedShadowSlices;
        private static bool m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall;
        private static bool m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall;
        private readonly bool m_UseStructuredBuffer;
        private float m_MaxShadowDistanceSq;
        private float m_CascadeBorder;
        private PassData m_PassData;
        private RTHandle m_EmptyAdditionalLightShadowmapTexture;
        private bool[] m_VisibleLightIndexToIsCastingShadows;                          // maps a "global" visible light index (index to lightData.visibleLights) to a shadow casting state (Is the light casting shadows or not?)
        private short[] m_VisibleLightIndexToAdditionalLightIndex;                     // maps a "global" visible light index (index to lightData.visibleLights) to an "additional light index" (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...), or -1 if it is not an additional light (i.e if it is the main light)
        private short[] m_AdditionalLightIndexToVisibleLightIndex;                     // maps additional light index (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...) to its "global" visible light index (index to lightData.visibleLights)
        private Vector4[] m_AdditionalLightIndexToShadowParams;                        // per-additional-light shadow info passed to the lighting shader (x: shadowStrength, y: softShadows, z: light type, w: perLightFirstShadowSliceIndex)
        private Matrix4x4[] m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix;     // per-shadow-slice info passed to the lighting shader
        private ShadowSliceData[] m_AdditionalLightsShadowSlices;
        private readonly List<byte> m_GlobalShadowSliceIndexToPerLightShadowSliceIndex = new(); // For each shadow slice, store its "per-light shadow slice index" in the punctual light that casts it (can be up to 5 for point lights)
        private readonly List<short> m_ShadowSliceToAdditionalLightIndex = new();               // For each shadow slice, store the "additional light indices" of the punctual light that casts it
        private readonly Dictionary<int, ulong> m_ShadowRequestsHashes = new();                 // used to keep track of changes in the shadow requests and shadow atlas configuration (per camera)
        private readonly ProfilingSampler m_ProfilingSetupSampler = new ("Setup Additional Shadows");
        private RenderTextureDescriptor m_AdditionalLightShadowDescriptor;

        // Constants and Statics
        private const int k_ShadowmapBufferBits = 16;
        private const int k_EmptyShadowMapDimensions = 1;
        // Magic numbers used to identify light type when rendering shadow receiver.
        // Keep in sync with AdditionalLightRealtimeShadow code in com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl
        private const float k_LightTypeIdentifierInShadowParams_Spot = 0;
        private const float k_LightTypeIdentifierInShadowParams_Point = 1;
        private const string k_AdditionalLightShadowMapTextureName = "_AdditionalLightsShadowmapTexture";
        private const string k_EmptyAdditionalLightShadowMapTextureName = "_EmptyAdditionalLightShadowmapTexture";
        // x is used in RenderAdditionalShadowMapAtlas to skip shadow map rendering for non-shadow-casting lights.
        // w is perLightFirstShadowSliceIndex, used in Lighting shader to find if Additional light casts shadows.
        private static readonly Vector4 c_DefaultShadowParams = new (0, 0, 0, -1);
        private static Vector4 s_EmptyAdditionalShadowFadeParams;
        private static Vector4[] s_EmptyAdditionalLightIndexToShadowParams;

        // Classes
        private static class AdditionalShadowsConstantBuffer
        {
            public static readonly int _AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightsWorldToShadow");
            public static readonly int _AdditionalShadowParams = Shader.PropertyToID("_AdditionalShadowParams");
            public static readonly int _AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            public static readonly int _AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            public static readonly int _AdditionalShadowFadeParams = Shader.PropertyToID("_AdditionalShadowFadeParams");
            public static readonly int _AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");
            public static readonly int _AdditionalLightsShadowmapID = Shader.PropertyToID(k_AdditionalLightShadowMapTextureName);
            public static readonly int _AdditionalLightsWorldToShadow_SSBO = Shader.PropertyToID("_AdditionalLightsWorldToShadow_SSBO");
            public static readonly int _AdditionalShadowParams_SSBO = Shader.PropertyToID("_AdditionalShadowParams_SSBO");
        }

        private class PassData
        {
            internal int shadowmapID;
            internal bool emptyShadowmap;
            internal bool useStructuredBuffer;
            internal bool stripShadowsOffVariants;
            internal Matrix4x4 viewMatrix;
            internal Vector2Int allocatedShadowAtlasSize;
            internal TextureHandle shadowmapTexture;
            internal UniversalLightData lightData;
            internal UniversalShadowData shadowData;
            internal AdditionalLightsShadowCasterPass pass;
            internal readonly RendererList[] shadowRendererLists = new RendererList[ShaderOptions.k_MaxVisibleLightCountDesktop];
            internal readonly RendererListHandle[] shadowRendererListsHdl = new RendererListHandle[ShaderOptions.k_MaxVisibleLightCountDesktop];
        }

        /// <summary>
        /// Creates a new <c>AdditionalLightsShadowCasterPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public AdditionalLightsShadowCasterPass(RenderPassEvent evt)
        {
            profilingSampler = new ProfilingSampler("Draw Additional Lights Shadowmap");
            renderPassEvent = evt;

            m_PassData = new PassData();
            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;

            // Pre-allocated a fixed size. CommandBuffer.SetGlobal* does allow this data to grow.
            int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
            const int maxMainLights = 1;
            int maxVisibleLights = maxVisibleAdditionalLights + maxMainLights;
            int maxAdditionalLightShadowParams = m_UseStructuredBuffer ? maxVisibleLights : Math.Min(maxVisibleLights, maxVisibleAdditionalLights);

            // These array sizes should be as big as ScriptableCullingParameters.maximumVisibleLights (that is defined during ScriptableRenderer.SetupCullingParameters).
            // We initialize these array sizes with the number of visible lights allowed by the UniversalRenderer.
            // The number of visible lights can become much higher when using the Deferred rendering path, we resize the arrays during Setup() if required.
            m_AdditionalLightIndexToVisibleLightIndex = new short[maxAdditionalLightShadowParams];
            m_VisibleLightIndexToAdditionalLightIndex = new short[maxVisibleLights];
            m_VisibleLightIndexToIsCastingShadows = new bool[maxVisibleLights];
            m_AdditionalLightIndexToShadowParams = new Vector4[maxAdditionalLightShadowParams];

            s_EmptyAdditionalLightIndexToShadowParams = new Vector4[maxAdditionalLightShadowParams];
            for (int i = 0; i < s_EmptyAdditionalLightIndexToShadowParams.Length; i++)
                s_EmptyAdditionalLightIndexToShadowParams[i] = c_DefaultShadowParams;

            // Uniform buffers are faster on some platforms, but they have stricter size limitations
            if (!m_UseStructuredBuffer)
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[maxVisibleAdditionalLights];

            m_EmptyShadowmapNeedsClear = true;
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            m_AdditionalLightsShadowmapHandle?.Release();
            m_EmptyAdditionalLightShadowmapTexture?.Release();
        }

        // Returns the guard angle that must be added to a frustum angle covering a projection map of resolution sliceResolutionInTexels,
        // in order to also cover a guard band of size guardBandSizeInTexels around the projection map.
        // Formula illustrated in https://i.ibb.co/wpW5Mnf/Calc-Guard-Angle.png
        internal static float CalcGuardAngle(float frustumAngleInDegrees, float guardBandSizeInTexels, float sliceResolutionInTexels)
        {
            float frustumAngle = frustumAngleInDegrees * Mathf.Deg2Rad;
            float halfFrustumAngle = frustumAngle / 2;
            float tanHalfFrustumAngle = Mathf.Tan(halfFrustumAngle);

            float halfSliceResolution = sliceResolutionInTexels / 2;
            float halfGuardBand = guardBandSizeInTexels / 2;
            float factorBetweenAngleTangents = 1 + halfGuardBand / halfSliceResolution;

            float tanHalfGuardAnglePlusHalfFrustumAngle = tanHalfFrustumAngle * factorBetweenAngleTangents;

            float halfGuardAnglePlusHalfFrustumAngle = Mathf.Atan(tanHalfGuardAnglePlusHalfFrustumAngle);
            float halfGuardAngleInRadian = halfGuardAnglePlusHalfFrustumAngle - halfFrustumAngle;

            float guardAngleInRadian = 2 * halfGuardAngleInRadian;
            float guardAngleInDegree = guardAngleInRadian * Mathf.Rad2Deg;

            return guardAngleInDegree;
        }

        // Returns the guard angle that must be added to a point light shadow face frustum angle
        // in order to avoid shadows missing at the boundaries between cube faces.
        internal static float GetPointLightShadowFrustumFovBiasInDegrees(int shadowSliceResolution, bool shadowFiltering)
        {
            // Commented-out code below uses the theoretical formula to compute the required guard angle based on the number of additional
            // texels that the projection should cover. It is close to HDRP's HDShadowUtils.CalcGuardAnglePerspective method.
            // However, due to precision issues or other filtering performed at lighting for example, this formula also still requires a fudge factor.
            // Since we only handle a fixed number of resolutions, we use empirical values instead.
#if false
            float fudgeFactor = 1.5f;
            return fudgeFactor * CalcGuardAngle(90, shadowFiltering ? 5 : 1, shadowSliceResolution);
#endif

            float fovBias = 4.00f;

            // Empirical value found to remove gaps between point light shadow faces in test scenes.
            // We can see that the guard angle is roughly proportional to the inverse of resolution https://docs.google.com/spreadsheets/d/1QrIZJn18LxVKq2-K1XS4EFRZcZdZOJTTKKhDN8Z1b_s
            if (shadowSliceResolution <= ShadowUtils.kMinimumPunctualLightHardShadowResolution)
            {
                if (!m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall)
                {
                    Debug.LogWarning("Too many additional punctual lights shadows, increase shadow atlas size or remove some shadowed lights");
                    m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall = true; // Only output this once per shadow requests configuration
                }
            }
            else if (shadowSliceResolution <= 16)
                fovBias = 43.0f;
            else if (shadowSliceResolution <= 32)
                fovBias = 18.55f;
            else if (shadowSliceResolution <= 64)
                fovBias = 8.63f;
            else if (shadowSliceResolution <= 128)
                fovBias = 4.13f;
            else if (shadowSliceResolution <= 256)
                fovBias = 2.03f;
            else if (shadowSliceResolution <= 512)
                fovBias = 1.00f;
            else if (shadowSliceResolution <= 1024)
                fovBias = 0.50f;
            else if (shadowSliceResolution <= 2048)
                fovBias = 0.25f;

            if (shadowFiltering)
            {
                if (shadowSliceResolution <= ShadowUtils.kMinimumPunctualLightSoftShadowResolution)
                {
                    if (!m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall)
                    {
                        Debug.LogWarning("Too many additional punctual lights shadows to use Soft Shadows. Increase shadow atlas size, remove some shadowed lights or use Hard Shadows.");
                        // With such small resolutions no fovBias can give good visual results
                        m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall = true; // Only output this once per shadow requests configuration
                    }
                }
                else if (shadowSliceResolution <= 32)
                    fovBias += 9.35f;
                else if (shadowSliceResolution <= 64)
                    fovBias += 4.07f;
                else if (shadowSliceResolution <= 128)
                    fovBias += 1.77f;
                else if (shadowSliceResolution <= 256)
                    fovBias += 0.85f;
                else if (shadowSliceResolution <= 512)
                    fovBias += 0.39f;
                else if (shadowSliceResolution <= 1024)
                    fovBias += 0.17f;
                else if (shadowSliceResolution <= 2048)
                    fovBias += 0.074f;

                // These values were verified to work on untethered devices for which m_SupportsBoxFilterForShadows is true.
                // TODO: Investigate finer-tuned values for those platforms. Soft shadows are implemented differently for them.
            }

            return fovBias;
        }

        private ulong ResolutionLog2ForHash(int resolution)
        {
            switch (resolution)
            {
                case 4096: return 12;
                case 2048: return 11;
                case 1024: return 10;
                case 0512: return 09;
            }
            return 08;
        }

        private ulong ComputeShadowRequestHash(UniversalLightData lightData, UniversalShadowData shadowData)
        {
            ulong numberOfShadowedPointLights = 0;
            ulong numberOfSoftShadowedLights = 0;
            ulong numberOfShadowsWithResolution0128 = 0;
            ulong numberOfShadowsWithResolution0256 = 0;
            ulong numberOfShadowsWithResolution0512 = 0;
            ulong numberOfShadowsWithResolution1024 = 0;
            ulong numberOfShadowsWithResolution2048 = 0;
            ulong numberOfShadowsWithResolution4096 = 0;

            var visibleLights = lightData.visibleLights;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                ref VisibleLight vl = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                Light light = vl.light;

                if (!ShadowUtils.IsValidShadowCastingLight(lightData, visibleLightIndex, vl.lightType, light.shadows, light.shadowStrength))
                    continue;

                switch (vl.lightType)
                {
                    case LightType.Spot: ++numberOfSoftShadowedLights; break;
                    case LightType.Point: ++numberOfShadowedPointLights; break;
                }

                int resolution = shadowData.resolution[visibleLightIndex];
                switch (resolution)
                {
                    case 0128: ++numberOfShadowsWithResolution0128; break;
                    case 0256: ++numberOfShadowsWithResolution0256; break;
                    case 0512: ++numberOfShadowsWithResolution0512; break;
                    case 1024: ++numberOfShadowsWithResolution1024; break;
                    case 2048: ++numberOfShadowsWithResolution2048; break;
                    case 4096: ++numberOfShadowsWithResolution4096; break;
                }
            }
            ulong shadowRequestsHash = ResolutionLog2ForHash(shadowData.additionalLightsShadowmapWidth) - 8; // bits [00~02]
            shadowRequestsHash |= numberOfShadowedPointLights << 03;        // bits [03~10]
            shadowRequestsHash |= numberOfSoftShadowedLights << 11;         // bits [11~18]
            shadowRequestsHash |= numberOfShadowsWithResolution0128 << 19;  // bits [19~26]
            shadowRequestsHash |= numberOfShadowsWithResolution0256 << 27;  // bits [27~34]
            shadowRequestsHash |= numberOfShadowsWithResolution0512 << 35;  // bits [35~42]
            shadowRequestsHash |= numberOfShadowsWithResolution1024 << 43;  // bits [43~49]
            shadowRequestsHash |= numberOfShadowsWithResolution2048 << 50;  // bits [50~56]
            shadowRequestsHash |= numberOfShadowsWithResolution4096 << 57;  // bits [57~63]
            return shadowRequestsHash;
        }

        private float GetLightTypeIdentifierForShadowParams(LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Spot:
                    return k_LightTypeIdentifierInShadowParams_Spot;
                case LightType.Point:
                    return k_LightTypeIdentifierInShadowParams_Point;
                default:
                    return -1;
            }
        }

        private bool UsesBakedShadows(Light light)
        {
            return light.bakingOutput.lightmapBakeType != LightmapBakeType.Realtime;
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData"></param>
        /// <returns></returns>
        public bool Setup(ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            return Setup(universalRenderingData, cameraData, lightData, shadowData);
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData">Data containing rendering settings.</param>
        /// <param name="cameraData">Data containing camera settings.</param>
        /// <param name="lightData">Data containing light settings.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <returns>Returns true if additional shadows are enabled otherwise false.</returns>
        public bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            using var profScope = new ProfilingScope(m_ProfilingSetupSampler);

            if (!shadowData.additionalLightShadowsEnabled)
                return false;

            if (!shadowData.supportsAdditionalLightShadows)
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants, lightData, shadowData);

            Clear();

            renderTargetWidth = shadowData.additionalLightsShadowmapWidth;
            renderTargetHeight = shadowData.additionalLightsShadowmapHeight;

            var visibleLights = lightData.visibleLights;
            ref AdditionalLightsShadowAtlasLayout atlasLayout = ref shadowData.shadowAtlasLayout;

            // Check changes in the shadow requests and shadow atlas configuration - compute shadow request/configuration hash
            // Should only be done in the editor or development builds as computing the hash has a cost that's clearly visible when profiling.
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!cameraData.isPreviewCamera)
            {
                ulong newShadowRequestHash = ComputeShadowRequestHash(lightData, shadowData);
                m_ShadowRequestsHashes.TryGetValue(cameraData.camera.GetHashCode(), out ulong oldShadowRequestHash);
                if (oldShadowRequestHash != newShadowRequestHash)
                {
                    m_ShadowRequestsHashes[cameraData.camera.GetHashCode()] = newShadowRequestHash;

                    // config changed ; reset error message flags as we might need to issue those messages again
                    m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall = false;
                    m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall = false;
                    m_IssuedMessageAboutShadowMapsRescale = false;
                    m_IssuedMessageAboutShadowMapsTooBig = false;
                    m_IssuedMessageAboutShadowSlicesTooMany = false;
                    m_IssuedMessageAboutRemovedShadowSlices = false;
                }
            }
            #endif

            if (m_VisibleLightIndexToAdditionalLightIndex.Length < visibleLights.Length)
            {
                // Array "visibleLights" is returned by ScriptableRenderContext.Cull()
                // The maximum number of "visibleLights" that ScriptableRenderContext.Cull() should return, is defined by parameter ScriptableCullingParameters.maximumVisibleLights
                // Universal RP sets this "ScriptableCullingParameters.maximumVisibleLights" value during ScriptableRenderer.SetupCullingParameters.
                // When using Deferred rendering, it is possible to specify a very high number of visible lights.
                m_VisibleLightIndexToAdditionalLightIndex = new short[visibleLights.Length];
                m_VisibleLightIndexToIsCastingShadows = new bool[visibleLights.Length];
            }

            int maxAdditionalLightShadowParams = m_UseStructuredBuffer ? visibleLights.Length : Math.Min(visibleLights.Length, UniversalRenderPipeline.maxVisibleAdditionalLights);
            if (m_AdditionalLightIndexToVisibleLightIndex.Length < maxAdditionalLightShadowParams)
            {
                m_AdditionalLightIndexToVisibleLightIndex = new short[maxAdditionalLightShadowParams];
                m_AdditionalLightIndexToShadowParams = new Vector4[maxAdditionalLightShadowParams];
            }

            int totalShadowSlicesCount = atlasLayout.GetTotalShadowSlicesCount();
            int totalShadowResolutionRequestCount = atlasLayout.GetTotalShadowResolutionRequestCount();
            int shadowSlicesScaleFactor = atlasLayout.GetShadowSlicesScaleFactor();
            bool hasTooManyShadowMaps = atlasLayout.HasTooManyShadowMaps();
            int atlasSize = atlasLayout.GetAtlasSize();

            if (totalShadowSlicesCount < totalShadowResolutionRequestCount)
            {
                if (!m_IssuedMessageAboutRemovedShadowSlices)
                {
                    Debug.LogWarning($"Too many additional punctual lights shadows to look good, URP removed {totalShadowResolutionRequestCount - totalShadowSlicesCount } shadow maps to make the others fit in the shadow atlas. To avoid this, increase shadow atlas size, remove some shadowed lights, replace soft shadows by hard shadows ; or replace point lights by spot lights");
                    m_IssuedMessageAboutRemovedShadowSlices = true;  // Only output this once per shadow requests configuration
                }
            }

            if (!m_IssuedMessageAboutShadowMapsTooBig && hasTooManyShadowMaps)
            {
                Debug.LogWarning($"Too many additional punctual lights shadows. URP tried reducing shadow resolutions by {shadowSlicesScaleFactor} but it was still too much. Increase shadow atlas size, decrease big shadow resolutions, or reduce the number of shadow maps active in the same frame (currently was {totalShadowSlicesCount}).");
                m_IssuedMessageAboutShadowMapsTooBig = true; // Only output this once per shadow requests configuration
            }

            if (!m_IssuedMessageAboutShadowMapsRescale && shadowSlicesScaleFactor > 1)
            {
                Debug.Log($"Reduced additional punctual light shadows resolution by {shadowSlicesScaleFactor} to make {totalShadowSlicesCount} shadow maps fit in the {atlasSize}x{atlasSize} shadow atlas. To avoid this, increase shadow atlas size, decrease big shadow resolutions, or reduce the number of shadow maps active in the same frame");
                m_IssuedMessageAboutShadowMapsRescale = true; // Only output this once per shadow requests configuration
            }

            if (m_AdditionalLightsShadowSlices == null || m_AdditionalLightsShadowSlices.Length < totalShadowSlicesCount)
                m_AdditionalLightsShadowSlices = new ShadowSliceData[totalShadowSlicesCount];

            if (m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix == null ||
                (m_UseStructuredBuffer && (m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length < totalShadowSlicesCount)))   // m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix can be resized when using SSBO to pass shadow data (no size limitation)
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[totalShadowSlicesCount];

            // initialize _AdditionalShadowParams
            for (int i = 0; i < maxAdditionalLightShadowParams; ++i)
                m_AdditionalLightIndexToShadowParams[i] = c_DefaultShadowParams;

            for (int i = 0; i < m_VisibleLightIndexToAdditionalLightIndex.Length; ++i)
            {
                m_VisibleLightIndexToAdditionalLightIndex[i] = -1;
                m_VisibleLightIndexToIsCastingShadows[i] = false;
            }

            short additionalLightCount = 0;
            short validShadowCastingLightsCount = 0;
            bool supportsSoftShadows = shadowData.supportsSoftShadows;
            bool isDeferred = ((UniversalRenderer)cameraData.renderer).renderingModeActual == RenderingMode.Deferred;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                // Skip main directional light as it is not packed into the shadow atlas
                if (visibleLightIndex == lightData.mainLightIndex)
                    continue;

                short lightIndexToUse = isDeferred ? validShadowCastingLightsCount : additionalLightCount++;

                // We need to always set these indices, even if the light is not shadow casting or doesn't fit in the shadow slices (UUM-46577)
                m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = lightIndexToUse;

                // If we've reached the maximum amount of lights to support, we move on to the next light.
                if (lightIndexToUse >= m_AdditionalLightIndexToVisibleLightIndex.Length)
                    continue;

                m_AdditionalLightIndexToVisibleLightIndex[lightIndexToUse] = (short) visibleLightIndex;
                if (m_ShadowSliceToAdditionalLightIndex.Count >= totalShadowSlicesCount)
                    continue;

                ref VisibleLight visibleLight = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                Light light = visibleLight.light;
                if (light == null)
                    break;

                LightType lightType = visibleLight.lightType;
                bool usesBakedShadows = UsesBakedShadows(light);
                float lightTypeIdentifierForShadowParams = GetLightTypeIdentifierForShadowParams(lightType);
                int perLightShadowSlicesCount = ShadowUtils.GetPunctualLightShadowSlicesCount(lightType);
                bool isValidShadowCastingLight = ShadowUtils.IsValidShadowCastingLight(lightData, visibleLightIndex, visibleLight.lightType, light.shadows, light.shadowStrength);
                if (isValidShadowCastingLight && (m_ShadowSliceToAdditionalLightIndex.Count + perLightShadowSlicesCount) > totalShadowSlicesCount)
                {
                    if (!m_IssuedMessageAboutShadowSlicesTooMany)
                    {
                        // This case can especially happen in Deferred, where there can be a high number of visibleLights
                        Debug.Log($"There are too many shadowed additional punctual lights active at the same time, URP will not render all the shadows. To ensure all shadows are rendered, reduce the number of shadowed additional lights in the scene ; make sure they are not active at the same time ; or replace point lights by spot lights (spot lights use less shadow maps than point lights).");
                        m_IssuedMessageAboutShadowSlicesTooMany = true; // Only output this once
                    }
                    break;
                }

                float softShadows = ShadowUtils.SoftShadowQualityToShaderProperty(light, (supportsSoftShadows && light.shadows == LightShadows.Soft));
                int perLightFirstShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices
                bool shouldAddLight = false;
                for (byte perLightShadowSlice = 0; perLightShadowSlice < perLightShadowSlicesCount; ++perLightShadowSlice)
                {
                    int globalShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices

                    // We need to iterate the lights even though additional lights are disabled because
                    // cullResults.GetShadowCasterBounds() does the fence sync for the shadow culling jobs.
                    bool lightRangeContainsShadowCasters = renderingData.cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds _);
                    if (!shadowData.supportsAdditionalLightShadows || !isValidShadowCastingLight || !lightRangeContainsShadowCasters)
                    {
                        // Even though there are not real-time shadows, the lights might be using shadowmasks,
                        // which is why we need to update the shadow parameters, for example so shadow strength can be used.
                        // This check is to make sure we only update this for spot and point lights.
                        if (usesBakedShadows && lightTypeIdentifierForShadowParams > -1)
                        {
                            m_AdditionalLightIndexToShadowParams[lightIndexToUse] = new Vector4(light.shadowStrength, softShadows, lightTypeIdentifierForShadowParams, lightIndexToUse);
                            m_VisibleLightIndexToIsCastingShadows[visibleLightIndex] = usesBakedShadows;
                        }
                        continue;
                    }

                    if (!atlasLayout.HasSpaceForLight(visibleLightIndex))
                    {
                        // We could not find place in the shadow atlas for shadow maps of this light.
                        // Skip it.
                    }
                    else if (lightType == LightType.Spot)
                    {
                        ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(visibleLightIndex);
                        ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.slices.UnsafeElementAt(0);

                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix = sliceData.viewMatrix;
                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix = sliceData.projectionMatrix;
                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData = sliceData.splitData;

                        if (shadowCullingInfos.IsSliceValid(0))
                        {
                            m_ShadowSliceToAdditionalLightIndex.Add(lightIndexToUse);
                            m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Add(perLightShadowSlice);
                            m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = sliceData.shadowTransform;
                            m_AdditionalLightIndexToShadowParams[lightIndexToUse] = new Vector4(light.shadowStrength, softShadows, lightTypeIdentifierForShadowParams, perLightFirstShadowSliceIndex);
                            shouldAddLight = true;
                        }
                    }
                    else if (lightType == LightType.Point)
                    {
                        ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(visibleLightIndex);
                        ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.slices.UnsafeElementAt(perLightShadowSlice);

                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix = sliceData.viewMatrix;
                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix = sliceData.projectionMatrix;
                        m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData = sliceData.splitData;

                        if (shadowCullingInfos.IsSliceValid(perLightShadowSlice))
                        {
                            m_ShadowSliceToAdditionalLightIndex.Add(lightIndexToUse);
                            m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Add(perLightShadowSlice);
                            m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = sliceData.shadowTransform;
                            m_AdditionalLightIndexToShadowParams[lightIndexToUse] = new Vector4(light.shadowStrength, softShadows, lightTypeIdentifierForShadowParams, perLightFirstShadowSliceIndex);
                            shouldAddLight = true;
                        }
                    }
                }

                if (shouldAddLight)
                {
                    m_VisibleLightIndexToIsCastingShadows[visibleLightIndex] = true;
                    m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = lightIndexToUse;
                    m_AdditionalLightIndexToVisibleLightIndex[lightIndexToUse] = (short) visibleLightIndex;
                    validShadowCastingLightsCount++;
                }
                else
                {
                    m_VisibleLightIndexToIsCastingShadows[visibleLightIndex] = usesBakedShadows;
                    m_AdditionalLightIndexToShadowParams[lightIndexToUse] = new Vector4(light.shadowStrength, softShadows, lightTypeIdentifierForShadowParams, c_DefaultShadowParams.w);
                }
            }

            // Lights that need to be rendered in the shadow map atlas
            if (validShadowCastingLightsCount == 0)
                return SetupForEmptyRendering(cameraData.renderer.stripShadowsOffVariants, lightData, shadowData);

            int shadowCastingLightsBufferCount = m_ShadowSliceToAdditionalLightIndex.Count;

            // Trim shadow atlas dimensions if possible (to avoid allocating texture space that will not be used)
            int atlasMaxX = 0;
            int atlasMaxY = 0;
            for (int sortedShadowResolutionRequestIndex = 0; sortedShadowResolutionRequestIndex < totalShadowSlicesCount; ++sortedShadowResolutionRequestIndex)
            {
                var shadowResolutionRequest = atlasLayout.GetSortedShadowResolutionRequest(sortedShadowResolutionRequestIndex);
                atlasMaxX = Mathf.Max(atlasMaxX, shadowResolutionRequest.offsetX + shadowResolutionRequest.allocatedResolution);
                atlasMaxY = Mathf.Max(atlasMaxY, shadowResolutionRequest.offsetY + shadowResolutionRequest.allocatedResolution);
            }
            // ...but make sure we still use power-of-two dimensions (might perform better on some hardware)

            renderTargetWidth = Mathf.NextPowerOfTwo(atlasMaxX);
            renderTargetHeight = Mathf.NextPowerOfTwo(atlasMaxY);

            float oneOverAtlasWidth = 1.0f / renderTargetWidth;
            float oneOverAtlasHeight = 1.0f / renderTargetHeight;

            for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowCastingLightsBufferCount; ++globalShadowSliceIndex)
            {
                int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                // We can skip the slice if strength is zero.
                if (Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f) || Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                    continue;

                int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];
                int perLightSliceIndex = m_GlobalShadowSliceIndexToPerLightShadowSliceIndex[globalShadowSliceIndex];
                var shadowResolutionRequest = atlasLayout.GetSliceShadowResolutionRequest(visibleLightIndex, perLightSliceIndex);
                int sliceResolution = shadowResolutionRequest.allocatedResolution;

                Matrix4x4 sliceTransform = Matrix4x4.identity;
                sliceTransform.m00 = sliceResolution * oneOverAtlasWidth;
                sliceTransform.m11 = sliceResolution * oneOverAtlasHeight;

                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetX = shadowResolutionRequest.offsetX;
                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetY = shadowResolutionRequest.offsetY;
                m_AdditionalLightsShadowSlices[globalShadowSliceIndex].resolution = sliceResolution;

                sliceTransform.m03 = m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetX * oneOverAtlasWidth;
                sliceTransform.m13 = m_AdditionalLightsShadowSlices[globalShadowSliceIndex].offsetY * oneOverAtlasHeight;

                // We bake scale and bias to each shadow map in the atlas in the matrix.
                // saves some instructions in shader.
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = sliceTransform * m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex];
            }

            UpdateTextureDescriptorIfNeeded();

            m_MaxShadowDistanceSq = cameraData.maxShadowDistance * cameraData.maxShadowDistance;
            m_CascadeBorder = shadowData.mainLightShadowCascadeBorder;
            m_CreateEmptyShadowmap = false;
            useNativeRenderPass = true;

            return true;
        }

        private void UpdateTextureDescriptorIfNeeded()
        {
            if (   m_AdditionalLightShadowDescriptor.width != renderTargetWidth
                || m_AdditionalLightShadowDescriptor.height != renderTargetHeight
                || m_AdditionalLightShadowDescriptor.depthBufferBits != k_ShadowmapBufferBits
                || m_AdditionalLightShadowDescriptor.colorFormat != RenderTextureFormat.Shadowmap)
            {
                m_AdditionalLightShadowDescriptor = new RenderTextureDescriptor(renderTargetWidth, renderTargetHeight, RenderTextureFormat.Shadowmap, k_ShadowmapBufferBits);
            }
        }

        bool SetupForEmptyRendering(bool stripShadowsOffVariants, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            if (!stripShadowsOffVariants)
                return false;

            shadowData.isKeywordAdditionalLightShadowsEnabled = true;
            m_CreateEmptyShadowmap = true;
            useNativeRenderPass = false;

            // Even though there are not real-time shadows, the lights might be using shadowmasks,
            // which is why we need to update the shadow parameters, for example so shadow strength can be used.

            // _AdditionalShadowFadeParams
            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSq, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
            s_EmptyAdditionalShadowFadeParams = new Vector4(shadowFadeScale, shadowFadeBias, 0, 0);

            var visibleLights = lightData.visibleLights;
            if (m_VisibleLightIndexToAdditionalLightIndex.Length < visibleLights.Length)
            {
                m_VisibleLightIndexToAdditionalLightIndex = new short[visibleLights.Length];
                m_VisibleLightIndexToIsCastingShadows = new bool[visibleLights.Length];
                s_EmptyAdditionalLightIndexToShadowParams = new Vector4[visibleLights.Length];
            }

            // Initialize _AdditionalShadowParams
            short additionalLightCount = 0;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                // Skip main directional light as it is not packed into the shadow atlas
                if (visibleLightIndex == lightData.mainLightIndex)
                    continue;

                ref VisibleLight visibleLight = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                Light light = visibleLight.light;
                if (light == null)
                    continue;

                // Only want to update this for spot and point lights
                float lightType = GetLightTypeIdentifierForShadowParams(light.type);
                if (lightType < 0f)
                    continue;

                short lightIndexToUse = additionalLightCount++;
                LightShadows shadows = light.shadows;
                bool lightHasShadows = shadows != LightShadows.None;
                if (lightHasShadows)
                {
                    bool lightHasSoftShadows = shadows != LightShadows.Soft;
                    bool supportsSoftShadows = shadowData.supportsSoftShadows;
                    float softShadows = ShadowUtils.SoftShadowQualityToShaderProperty(light, (supportsSoftShadows && lightHasSoftShadows));
                    s_EmptyAdditionalLightIndexToShadowParams[lightIndexToUse] = new Vector4(light.shadowStrength, softShadows, lightType, lightIndexToUse);
                }
                else
                {
                    s_EmptyAdditionalLightIndexToShadowParams[lightIndexToUse] = c_DefaultShadowParams;
                }

                m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = lightIndexToUse;
                m_VisibleLightIndexToIsCastingShadows[visibleLightIndex] = UsesBakedShadows(light);
            }

            return true;
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618

            if (m_CreateEmptyShadowmap)
            {
                // Required for scene view camera(URP renderer not initialized)
                if (ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_EmptyAdditionalLightShadowmapTexture, k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions, k_ShadowmapBufferBits, name: k_EmptyAdditionalLightShadowMapTextureName))
                    m_EmptyShadowmapNeedsClear = true;

                if (!m_EmptyShadowmapNeedsClear)
                {
                    return;
                }

                ConfigureTarget(m_EmptyAdditionalLightShadowmapTexture);
                m_EmptyShadowmapNeedsClear = false;
            }
            else
            {
                ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_AdditionalLightsShadowmapHandle, renderTargetWidth, renderTargetHeight, k_ShadowmapBufferBits, name: k_AdditionalLightShadowMapTextureName);
                ConfigureTarget(m_AdditionalLightsShadowmapHandle);
            }

            ConfigureClear(ClearFlag.All, Color.black);

            #pragma warning restore CS0618
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            RasterCommandBuffer rasterCommandBuffer = CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);
            if (m_CreateEmptyShadowmap)
            {
                rasterCommandBuffer.EnableKeyword(ShaderGlobalKeywords.AdditionalLightShadows);
                SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
                universalRenderingData.commandBuffer.SetGlobalTexture(AdditionalShadowsConstantBuffer._AdditionalLightsShadowmapID, m_EmptyAdditionalLightShadowmapTexture);
                return;
            }

            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            if (!shadowData.supportsAdditionalLightShadows)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            InitPassData(ref m_PassData, cameraData, lightData, shadowData);
            m_PassData.allocatedShadowAtlasSize = m_AdditionalLightsShadowmapHandle.referenceSize;
            InitRendererLists(ref universalRenderingData.cullResults, ref m_PassData, context, default(RenderGraph), false);
            RenderAdditionalShadowmapAtlas(rasterCommandBuffer, ref m_PassData, false);
            universalRenderingData.commandBuffer.SetGlobalTexture(AdditionalShadowsConstantBuffer._AdditionalLightsShadowmapID, m_AdditionalLightsShadowmapHandle.nameID);
        }

        /// <summary>
        /// Gets the additional light index from the global visible light index, which is used to index arrays _AdditionalLightsPosition, _AdditionalShadowParams, etc.
        /// </summary>
        /// <param name="visibleLightIndex">The index of the visible light.</param>
        /// <returns>The additional light index.</returns>
        public int GetShadowLightIndexFromLightIndex(int visibleLightIndex)
        {
            if (visibleLightIndex < 0 || visibleLightIndex >= m_VisibleLightIndexToAdditionalLightIndex.Length || !m_VisibleLightIndexToIsCastingShadows[visibleLightIndex])
                return -1;

            return m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex];
        }

        private void Clear()
        {
            m_ShadowSliceToAdditionalLightIndex.Clear();
            m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Clear();
        }

        internal static void SetShadowParamsForEmptyShadowmap(RasterCommandBuffer rasterCommandBuffer)
        {
            rasterCommandBuffer.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowFadeParams, s_EmptyAdditionalShadowFadeParams);

            if (RenderingUtils.useStructuredBuffer)
            {
                ComputeBuffer shadowParamsBuffer = ShaderData.instance.GetAdditionalLightShadowParamsStructuredBuffer(s_EmptyAdditionalLightIndexToShadowParams.Length);
                shadowParamsBuffer.SetData(s_EmptyAdditionalLightIndexToShadowParams);
                rasterCommandBuffer.SetGlobalBuffer(AdditionalShadowsConstantBuffer._AdditionalShadowParams_SSBO, shadowParamsBuffer);
            }
            else
            {
                rasterCommandBuffer.SetGlobalVectorArray(AdditionalShadowsConstantBuffer._AdditionalShadowParams, s_EmptyAdditionalLightIndexToShadowParams);
            }
        }

        private void RenderAdditionalShadowmapAtlas(RasterCommandBuffer cmd, ref PassData data, bool useRenderGraph)
        {
            NativeArray<VisibleLight> visibleLights = data.lightData.visibleLights;

            bool additionalLightHasSoftShadows = false;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.AdditionalLightsShadow)))
            {
                // For non-RG, need set the worldToCamera Matrix as that is not set for passes executed before normal rendering,
                // otherwise shadows will behave incorrectly when Scene and Game windows are open at the same time (UUM-63267).
                if (!useRenderGraph)
                    ShadowUtils.SetWorldToCameraAndCameraToWorldMatrices(cmd, data.viewMatrix);

                bool anyShadowSliceRenderer = false;
                int shadowSlicesCount = m_ShadowSliceToAdditionalLightIndex.Count;
                if (shadowSlicesCount > 0)
                    cmd.SetKeyword(ShaderGlobalKeywords.CastingPunctualLightShadow, true);

                Vector4 lastShadowBias = new Vector4(-10f, -10f, -10f, -10f);
                for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowSlicesCount; ++globalShadowSliceIndex)
                {
                    int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                    // we do the shadow strength check here again here because we might have zero strength for non-shadow-casting lights.
                    // In that case we need the shadow data buffer but we can skip rendering them to shadowmap.
                    if (   ShadowUtils.FastApproximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f)
                        || ShadowUtils.FastApproximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                        continue;

                    int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];
                    ref VisibleLight shadowLight = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                    ShadowSliceData shadowSliceData = m_AdditionalLightsShadowSlices[globalShadowSliceIndex];
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, visibleLightIndex, data.shadowData, shadowSliceData.projectionMatrix, shadowSliceData.resolution);

                    // Update the bias when rendering the first slice or when the bias has changed
                    if (globalShadowSliceIndex == 0 || !ShadowUtils.FastApproximately(shadowBias, lastShadowBias))
                    {
                        ShadowUtils.SetShadowBias(cmd, shadowBias);
                        lastShadowBias = shadowBias;
                    }

                    // Update light position
                    Vector3 lightPosition = shadowLight.localToWorldMatrix.GetColumn(3);
                    ShadowUtils.SetLightPosition(cmd, lightPosition);

                    // Note: _LightDirection is not updated for additional lights.
                    // For Directional lights, _LightDirection is used when applying shadow Normal Bias.
                    // For Spot lights and Point lights _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.

                    RendererList shadowRendererList = useRenderGraph? data.shadowRendererListsHdl[globalShadowSliceIndex] : data.shadowRendererLists[globalShadowSliceIndex];
                    ShadowUtils.RenderShadowSlice(cmd, ref shadowSliceData, ref shadowRendererList, shadowSliceData.projectionMatrix, shadowSliceData.viewMatrix);
                    additionalLightHasSoftShadows |= shadowLight.light.shadows == LightShadows.Soft;
                    anyShadowSliceRenderer = true;
                }

                // We share soft shadow settings for main light and additional lights to save keywords.
                // So we check here if pipeline supports soft shadows and either main light or any additional light has soft shadows
                // to enable the keyword.
                // TODO: In PC and Consoles we can upload shadow data per light and branch on shader. That will be more likely way faster.
                bool mainLightHasSoftShadows = data.shadowData.supportsMainLightShadows &&
                                               data.lightData.mainLightIndex != -1 &&
                                               visibleLights[data.lightData.mainLightIndex].light.shadows == LightShadows.Soft;

                // If the OFF variant has been stripped, the additional light shadows keyword must always be enabled
                bool hasOffVariant = !data.stripShadowsOffVariants;
                data.shadowData.isKeywordAdditionalLightShadowsEnabled = !hasOffVariant || anyShadowSliceRenderer;
                cmd.SetKeyword(ShaderGlobalKeywords.AdditionalLightShadows, data.shadowData.isKeywordAdditionalLightShadowsEnabled);

                bool softShadows = data.shadowData.supportsSoftShadows && (mainLightHasSoftShadows || additionalLightHasSoftShadows);
                data.shadowData.isKeywordSoftShadowsEnabled = softShadows;
                ShadowUtils.SetSoftShadowQualityShaderKeywords(cmd, data.shadowData);

                if (anyShadowSliceRenderer)
                    SetupAdditionalLightsShadowReceiverConstants(cmd, data.allocatedShadowAtlasSize, data.useStructuredBuffer, softShadows);
            }
        }

        // Set constant buffer data that will be used during the lighting/shadowing pass
        private void SetupAdditionalLightsShadowReceiverConstants(RasterCommandBuffer cmd, Vector2Int allocatedShadowAtlasSize, bool useStructuredBuffer, bool softShadows)
        {
            if (useStructuredBuffer)
            {
                // per-light data
                var shadowParamsBuffer = ShaderData.instance.GetAdditionalLightShadowParamsStructuredBuffer(m_AdditionalLightIndexToShadowParams.Length);
                shadowParamsBuffer.SetData(m_AdditionalLightIndexToShadowParams);
                cmd.SetGlobalBuffer(AdditionalShadowsConstantBuffer._AdditionalShadowParams_SSBO, shadowParamsBuffer);

                // per-shadow-slice data
                var shadowSliceMatricesBuffer = ShaderData.instance.GetAdditionalLightShadowSliceMatricesStructuredBuffer(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length);
                shadowSliceMatricesBuffer.SetData(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix);
                cmd.SetGlobalBuffer(AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow_SSBO, shadowSliceMatricesBuffer);
            }
            else
            {
                cmd.SetGlobalVectorArray(AdditionalShadowsConstantBuffer._AdditionalShadowParams, m_AdditionalLightIndexToShadowParams);                         // per-light data
                cmd.SetGlobalMatrixArray(AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow, m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix); // per-shadow-slice data
            }

            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSq, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
            cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowFadeParams, new Vector4(shadowFadeScale, shadowFadeBias, 0, 0));

            if (softShadows)
            {
                Vector2 invShadowAtlasSize = Vector2.one / allocatedShadowAtlasSize;
                Vector2 invHalfShadowAtlasSize = invShadowAtlasSize * 0.5f;

                cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset0,
                        new Vector4(-invHalfShadowAtlasSize.x, -invHalfShadowAtlasSize.y,
                            invHalfShadowAtlasSize.x, -invHalfShadowAtlasSize.y));
                cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowOffset1,
                        new Vector4(-invHalfShadowAtlasSize.x, invHalfShadowAtlasSize.y,
                            invHalfShadowAtlasSize.x, invHalfShadowAtlasSize.y));

                cmd.SetGlobalVector(AdditionalShadowsConstantBuffer._AdditionalShadowmapSize, new Vector4(invShadowAtlasSize.x, invShadowAtlasSize.y,
                    allocatedShadowAtlasSize.x, allocatedShadowAtlasSize.y));
            }
        }

        private void InitPassData(ref PassData passData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            passData.pass = this;

            passData.lightData = lightData;
            passData.shadowData = shadowData;
            passData.viewMatrix = cameraData.GetViewMatrix();
            passData.stripShadowsOffVariants = cameraData.renderer.stripShadowsOffVariants;

            passData.emptyShadowmap = m_CreateEmptyShadowmap;
            passData.useStructuredBuffer = m_UseStructuredBuffer;
        }

        private void InitRendererLists(ref CullingResults cullResults, ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            if (m_CreateEmptyShadowmap)
                return;

            for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < m_ShadowSliceToAdditionalLightIndex.Count; ++globalShadowSliceIndex)
            {
                int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];
                int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];

                ShadowDrawingSettings settings = new (cullResults, visibleLightIndex) {
                    useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers
                };

                if(useRenderGraph)
                    passData.shadowRendererListsHdl[globalShadowSliceIndex] = renderGraph.CreateShadowRendererList(ref settings);
                else
                    passData.shadowRendererLists[globalShadowSliceIndex] = context.CreateShadowRendererList(ref settings);
            }
        }

        internal TextureHandle Render(RenderGraph graph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                InitPassData(ref passData, cameraData, lightData, shadowData);
                InitRendererLists(ref renderingData.cullResults, ref passData, default(ScriptableRenderContext), graph, true);

                TextureHandle shadowTexture;
                if (!m_CreateEmptyShadowmap)
                {
                    for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < m_ShadowSliceToAdditionalLightIndex.Count; ++globalShadowSliceIndex)
                    {
                        builder.UseRendererList(passData.shadowRendererListsHdl[globalShadowSliceIndex]);
                    }

                    shadowTexture = UniversalRenderer.CreateRenderGraphTexture(graph, m_AdditionalLightShadowDescriptor, k_AdditionalLightShadowMapTextureName, true,  ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear);
                    builder.SetRenderAttachmentDepth(shadowTexture, AccessFlags.Write);
                }
                else
                {
                    shadowTexture = graph.defaultResources.defaultShadowTexture;
                }

                TextureDesc descriptor = shadowTexture.GetDescriptor(graph);
                passData.allocatedShadowAtlasSize = new Vector2Int(descriptor.width, descriptor.height);

                // RENDERGRAPH TODO: Need this as shadowmap is only used as Global Texture and not a buffer, so would get culled by RG
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                if (shadowTexture.IsValid())
                    builder.SetGlobalTextureAfterPass(shadowTexture, AdditionalShadowsConstantBuffer._AdditionalLightsShadowmapID);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RasterCommandBuffer rasterCommandBuffer = context.cmd;
                    if (!data.emptyShadowmap)
                    {
                        data.pass.RenderAdditionalShadowmapAtlas(rasterCommandBuffer, ref data, true);
                    }
                    else
                    {
                        rasterCommandBuffer.EnableKeyword(ShaderGlobalKeywords.AdditionalLightShadows);
                        SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
                    }
                });

                return shadowTexture;
            }
        }
    }
}
