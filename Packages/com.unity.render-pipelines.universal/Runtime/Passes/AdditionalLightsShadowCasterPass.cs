using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map atlas for additional shadow-casting Lights.
    /// </summary>
    public partial class AdditionalLightsShadowCasterPass : ScriptableRenderPass
    {
        private static class AdditionalShadowsConstantBuffer
        {
            public static int _AdditionalLightsWorldToShadow;
            public static int _AdditionalShadowParams;
            public static int _AdditionalShadowOffset0;
            public static int _AdditionalShadowOffset1;
            public static int _AdditionalShadowFadeParams;
            public static int _AdditionalShadowmapSize;
        }

        internal struct ShadowResolutionRequest
        {
            public int visibleLightIndex;
            public int perLightShadowSliceIndex;
            public int requestedResolution;
            public bool softShadow;         // otherwise it's hard-shadow (no filtering)
            public bool pointLightShadow;   // otherwise it's spot light shadow (1 shadow slice instead of 6)

            public int offsetX;             // x coordinate of the square area allocated in the atlas for this shadow map
            public int offsetY;             // y coordinate of the square area allocated in the atlas for this shadow map
            public int allocatedResolution; // width of the square area allocated in the atlas for this shadow map

            public ShadowResolutionRequest(int _visibleLightIndex, int _perLightShadowSliceIndex, int _requestedResolution, bool _softShadow, bool _pointLightShadow)
            {
                visibleLightIndex = _visibleLightIndex;
                perLightShadowSliceIndex = _perLightShadowSliceIndex;
                requestedResolution = _requestedResolution;
                softShadow = _softShadow;
                pointLightShadow = _pointLightShadow;

                offsetX = 0;
                offsetY = 0;
                allocatedResolution = 0;
            }
        }

        /// <summary>
        /// x is used in RenderAdditionalShadowMapAtlas to skip shadow map rendering for non-shadow-casting lights.
        /// w is perLightFirstShadowSliceIndex, used in Lighting shader to find if Additional light casts shadows.
        /// </summary>
        readonly static Vector4 c_DefaultShadowParams = new Vector4(0, 0, 0, -1);

        static int m_AdditionalLightsWorldToShadow_SSBO;
        static int m_AdditionalShadowParams_SSBO;
        bool m_UseStructuredBuffer;

        const int k_ShadowmapBufferBits = 16;
        private int m_AdditionalLightsShadowmapID;
        internal RTHandle m_AdditionalLightsShadowmapHandle;


        float m_MaxShadowDistanceSq;
        float m_CascadeBorder;

        ShadowSliceData[] m_AdditionalLightsShadowSlices = null;

        int[] m_VisibleLightIndexToAdditionalLightIndex = null;                         // maps a "global" visible light index (index to renderingData.lightData.visibleLights) to an "additional light index" (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...), or -1 if it is not an additional light (i.e if it is the main light)
        int[] m_AdditionalLightIndexToVisibleLightIndex = null;                         // maps additional light index (index to arrays _AdditionalLightsPosition, _AdditionalShadowParams, ...) to its "global" visible light index (index to renderingData.lightData.visibleLights)
        List<int> m_ShadowSliceToAdditionalLightIndex = new List<int>();                // For each shadow slice, store the "additional light indices" of the punctual light that casts it
        List<int> m_GlobalShadowSliceIndexToPerLightShadowSliceIndex = new List<int>(); // For each shadow slice, store its "per-light shadow slice index" in the punctual light that casts it (can be up to 5 for point lights)

        Vector4[] m_AdditionalLightIndexToShadowParams = null;                          // per-additional-light shadow info passed to the lighting shader (x: shadowStrength, y: softShadows, z: light type, w: perLightFirstShadowSliceIndex)
        Matrix4x4[] m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = null;       // per-shadow-slice info passed to the lighting shader

        List<ShadowResolutionRequest> m_ShadowResolutionRequests = new List<ShadowResolutionRequest>();  // intermediate array used to compute the final resolution of each shadow slice rendered in the frame
        float[] m_VisibleLightIndexToCameraSquareDistance = null;                                        // stores for each shadowed additional light its (squared) distance to camera ; used to sub-sort shadow requests according to how close their casting light is
        ShadowResolutionRequest[] m_SortedShadowResolutionRequests = null;
        int[] m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex = null;                 // for each visible light, store the index of its first shadow slice in m_SortedShadowResolutionRequests (for quicker access)
        List<RectInt> m_UnusedAtlasSquareAreas = new List<RectInt>();                                    // this list tracks space available in the atlas

        bool m_CreateEmptyShadowmap;

        int renderTargetWidth;
        int renderTargetHeight;

        ProfilingSampler m_ProfilingSetupSampler = new ProfilingSampler("Setup Additional Shadows");

        /// <summary>
        /// Creates a new <c>AdditionalLightsShadowCasterPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public AdditionalLightsShadowCasterPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(AdditionalLightsShadowCasterPass));
            renderPassEvent = evt;

            AdditionalShadowsConstantBuffer._AdditionalLightsWorldToShadow = Shader.PropertyToID("_AdditionalLightsWorldToShadow");
            AdditionalShadowsConstantBuffer._AdditionalShadowParams = Shader.PropertyToID("_AdditionalShadowParams");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset0 = Shader.PropertyToID("_AdditionalShadowOffset0");
            AdditionalShadowsConstantBuffer._AdditionalShadowOffset1 = Shader.PropertyToID("_AdditionalShadowOffset1");
            AdditionalShadowsConstantBuffer._AdditionalShadowFadeParams = Shader.PropertyToID("_AdditionalShadowFadeParams");
            AdditionalShadowsConstantBuffer._AdditionalShadowmapSize = Shader.PropertyToID("_AdditionalShadowmapSize");
            m_AdditionalLightsShadowmapID = Shader.PropertyToID("_AdditionalLightsShadowmapTexture");

            m_AdditionalLightsWorldToShadow_SSBO = Shader.PropertyToID("_AdditionalLightsWorldToShadow_SSBO");
            m_AdditionalShadowParams_SSBO = Shader.PropertyToID("_AdditionalShadowParams_SSBO");

            m_UseStructuredBuffer = RenderingUtils.useStructuredBuffer;

            // Preallocated a fixed size. CommandBuffer.SetGlobal* does allow this data to grow.
            int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;
            const int maxMainLights = 1;
            int maxVisibleLights = maxVisibleAdditionalLights + maxMainLights;
            int maxAdditionalLightShadowParams = m_UseStructuredBuffer ? maxVisibleLights : Math.Min(maxVisibleLights, maxVisibleAdditionalLights);

            // These array sizes should be as big as ScriptableCullingParameters.maximumVisibleLights (that is defined during ScriptableRenderer.SetupCullingParameters).
            // We initialize these array sizes with the number of visible lights allowed by the UniversalRenderer.
            // The number of visible lights can become much higher when using the Deferred rendering path, we resize the arrays during Setup() if required.
            m_AdditionalLightIndexToVisibleLightIndex = new int[maxAdditionalLightShadowParams];
            m_VisibleLightIndexToAdditionalLightIndex = new int[maxVisibleLights];
            m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex = new int[maxVisibleLights];
            m_AdditionalLightIndexToShadowParams = new Vector4[maxAdditionalLightShadowParams];
            m_VisibleLightIndexToCameraSquareDistance = new float[maxVisibleLights];

            if (!m_UseStructuredBuffer)
            {
                // Uniform buffers are faster on some platforms, but they have stricter size limitations
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[maxVisibleAdditionalLights];
                m_UnusedAtlasSquareAreas.Capacity = maxVisibleAdditionalLights;
                m_ShadowResolutionRequests.Capacity = maxVisibleAdditionalLights;
            }
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            m_AdditionalLightsShadowmapHandle?.Release();
        }

        private int GetPunctualLightShadowSlicesCount(in LightType lightType)
        {
            switch (lightType)
            {
                case LightType.Spot:
                    return 1;
                case LightType.Point:
                    return 6;
                default:
                    return 0;
            }
        }

        // Magic numbers used to identify light type when rendering shadow receiver.
        // Keep in sync with AdditionalLightRealtimeShadow code in com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl
        private const float LightTypeIdentifierInShadowParams_Spot = 0;
        private const float LightTypeIdentifierInShadowParams_Point = 1;


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

        private const int kMinimumPunctualLightHardShadowResolution = 8;
        private const int kMinimumPunctualLightSoftShadowResolution = 16;
        // Minimal shadow map resolution required to have meaningful shadows visible during lighting
        int MinimalPunctualLightShadowResolution(bool softShadow)
        {
            return softShadow ? kMinimumPunctualLightSoftShadowResolution : kMinimumPunctualLightHardShadowResolution;
        }

        // Returns the guard angle that must be added to a point light shadow face frustum angle
        // in order to avoid shadows missing at the boundaries between cube faces.
        internal static float GetPointLightShadowFrustumFovBiasInDegrees(int shadowSliceResolution, bool shadowFiltering)
        {
            // Commented-out code below uses the theoretical formula to compute the required guard angle based on the number of additional
            // texels that the projection should cover. It is close to HDRP's HDShadowUtils.CalcGuardAnglePerspective method.
            // However, due to precision issues or other filterings performed at lighting for example, this formula also still requires a fudge factor.
            // Since we only handle a fixed number of resolutions, we use empirical values instead.
#if false
            float fudgeFactor = 1.5f;
            return fudgeFactor * CalcGuardAngle(90, shadowFiltering ? 5 : 1, shadowSliceResolution);
#endif


            float fovBias = 4.00f;

            // Empirical value found to remove gaps between point light shadow faces in test scenes.
            // We can see that the guard angle is roughly proportional to the inverse of resolution https://docs.google.com/spreadsheets/d/1QrIZJn18LxVKq2-K1XS4EFRZcZdZOJTTKKhDN8Z1b_s
            if (shadowSliceResolution <= kMinimumPunctualLightHardShadowResolution)
            {
                #if DEVELOPMENT_BUILD
                if (!m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall)
                {
                    Debug.LogWarning("Too many additional punctual lights shadows, increase shadow atlas size or remove some shadowed lights");
                    m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall = true; // Only output this once per shadow requests configuration
                }
                #endif
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

            if (shadowFiltering)
            {
                if (shadowSliceResolution <= kMinimumPunctualLightSoftShadowResolution)
                {
                    #if DEVELOPMENT_BUILD
                    if (!m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall)
                    {
                        Debug.LogWarning("Too many additional punctual lights shadows to use Soft Shadows. Increase shadow atlas size, remove some shadowed lights or use Hard Shadows.");
                        // With such small resolutions no fovBias can give good visual results
                        m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall = true; // Only output this once per shadow requests configuration
                    }
                    #endif
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

                // These values were verified to work on untethered devices for which m_SupportsBoxFilterForShadows is true.
                // TODO: Investigate finer-tuned values for those platforms. Soft shadows are implemented differently for them.
            }

            return fovBias;
        }

        // Adapted from InsertionSort() in com.unity.render-pipelines.high-definition/Runtime/Lighting/Shadow/HDDynamicShadowAtlas.cs
        // Sort array in decreasing requestedResolution order,
        // sub-sorting in "HardShadow > SoftShadow" and then "Spot > Point", i.e place last requests that will be removed in priority to make room for the others, because their resolution is too small to produce good-looking shadows ; or because they take relatively more space in the atlas )
        // sub-sub-sorting in light distance to camera
        // then grouping in increasing visibleIndex (and sub-sorting each group in ShadowSliceIndex order)
        internal void InsertionSort(ShadowResolutionRequest[] array, int startIndex, int lastIndex)
        {
            int i = startIndex + 1;

            while (i < lastIndex)
            {
                var curr = array[i];
                int j = i - 1;

                // Sort in priority order
                while ((j >= 0) && ((curr.requestedResolution > array[j].requestedResolution)
                                    || (curr.requestedResolution == array[j].requestedResolution && !curr.softShadow && array[j].softShadow)
                                    || (curr.requestedResolution == array[j].requestedResolution && curr.softShadow == array[j].softShadow && !curr.pointLightShadow && array[j].pointLightShadow)
                                    || (curr.requestedResolution == array[j].requestedResolution && curr.softShadow == array[j].softShadow && curr.pointLightShadow == array[j].pointLightShadow && m_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] < m_VisibleLightIndexToCameraSquareDistance[array[j].visibleLightIndex])
                                    || (curr.requestedResolution == array[j].requestedResolution && curr.softShadow == array[j].softShadow && curr.pointLightShadow == array[j].pointLightShadow && m_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] == m_VisibleLightIndexToCameraSquareDistance[array[j].visibleLightIndex] && curr.visibleLightIndex < array[j].visibleLightIndex)
                                    || (curr.requestedResolution == array[j].requestedResolution && curr.softShadow == array[j].softShadow && curr.pointLightShadow == array[j].pointLightShadow && m_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] == m_VisibleLightIndexToCameraSquareDistance[array[j].visibleLightIndex] && curr.visibleLightIndex == array[j].visibleLightIndex && curr.perLightShadowSliceIndex < array[j].perLightShadowSliceIndex)))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = curr;
                i++;
            }
        }

        int EstimateScaleFactorNeededToFitAllShadowsInAtlas(in ShadowResolutionRequest[] shadowResolutionRequests, int endIndex, int atlasWidth)
        {
            long totalTexelsInShadowAtlas = atlasWidth * atlasWidth;

            long totalTexelsInShadowRequests = 0;
            for (int shadowRequestIndex = 0; shadowRequestIndex < endIndex; ++shadowRequestIndex)
                totalTexelsInShadowRequests += shadowResolutionRequests[shadowRequestIndex].requestedResolution * shadowResolutionRequests[shadowRequestIndex].requestedResolution;

            int estimatedScaleFactor = 1;
            while (totalTexelsInShadowRequests > totalTexelsInShadowAtlas * estimatedScaleFactor * estimatedScaleFactor)
                estimatedScaleFactor *= 2;

            return estimatedScaleFactor;
        }

        // Assigns to each of the first totalShadowSlicesCount items in m_SortedShadowResolutionRequests a location in the shadow atlas based on requested resolutions.
        // If necessary, scales down shadow maps active in the frame, to make all of them fit in the atlas.
        void AtlasLayout(int atlasSize, int totalShadowSlicesCount, int estimatedScaleFactor)
        {
            bool allShadowSlicesFitInAtlas = false;
            bool tooManyShadows = false;
            int shadowSlicesScaleFactor = estimatedScaleFactor;

            while (!allShadowSlicesFitInAtlas && !tooManyShadows)
            {
                m_UnusedAtlasSquareAreas.Clear();
                m_UnusedAtlasSquareAreas.Add(new RectInt(0, 0, atlasSize, atlasSize));

                allShadowSlicesFitInAtlas = true;

                for (int shadowRequestIndex = 0; shadowRequestIndex < totalShadowSlicesCount; ++shadowRequestIndex)
                {
                    var resolution = m_SortedShadowResolutionRequests[shadowRequestIndex].requestedResolution / shadowSlicesScaleFactor;

                    if (resolution < MinimalPunctualLightShadowResolution(m_SortedShadowResolutionRequests[shadowRequestIndex].softShadow))
                    {
                        tooManyShadows = true;
                        break;
                    }

                    bool foundSpaceInAtlas = false;

                    // Try to find free space in the atlas
                    for (int unusedAtlasSquareAreaIndex = 0; unusedAtlasSquareAreaIndex < m_UnusedAtlasSquareAreas.Count; ++unusedAtlasSquareAreaIndex)
                    {
                        var atlasArea = m_UnusedAtlasSquareAreas[unusedAtlasSquareAreaIndex];
                        var atlasAreaWidth = atlasArea.width;
                        var atlasAreaHeight = atlasArea.height;
                        var atlasAreaX = atlasArea.x;
                        var atlasAreaY = atlasArea.y;
                        if (atlasAreaWidth >= resolution)
                        {
                            // we can use this atlas area for the shadow request
                            m_SortedShadowResolutionRequests[shadowRequestIndex].offsetX = atlasAreaX;
                            m_SortedShadowResolutionRequests[shadowRequestIndex].offsetY = atlasAreaY;
                            m_SortedShadowResolutionRequests[shadowRequestIndex].allocatedResolution = resolution;

                            // this atlas space is not available anymore, so remove it from the list
                            m_UnusedAtlasSquareAreas.RemoveAt(unusedAtlasSquareAreaIndex);

                            // make sure to split space so that the rest of this square area can be used
                            int remainingShadowRequestsCount = totalShadowSlicesCount - shadowRequestIndex - 1; // (no need to add more than that)
                            int newSquareAreasCount = 0;
                            int newSquareAreaWidth = resolution; // we split the area in squares of same size
                            int newSquareAreaHeight = resolution;
                            var newSquareAreaX = atlasAreaX;
                            var newSquareAreaY = atlasAreaY;
                            while (newSquareAreasCount < remainingShadowRequestsCount)
                            {
                                newSquareAreaX += newSquareAreaWidth;
                                if (newSquareAreaX + newSquareAreaWidth > (atlasAreaX + atlasAreaWidth))
                                {
                                    newSquareAreaX = atlasAreaX;
                                    newSquareAreaY += newSquareAreaHeight;
                                    if (newSquareAreaY + newSquareAreaHeight > (atlasAreaY + atlasAreaHeight))
                                        break;
                                }

                                // replace the space we removed previously by new smaller squares (inserting them in this order ensures shadow maps will be packed at the side of the atlas, without gaps)
                                m_UnusedAtlasSquareAreas.Insert(unusedAtlasSquareAreaIndex + newSquareAreasCount, new RectInt(newSquareAreaX, newSquareAreaY, newSquareAreaWidth, newSquareAreaHeight));
                                ++newSquareAreasCount;
                            }

                            foundSpaceInAtlas = true;
                            break;
                        }
                    }

                    if (!foundSpaceInAtlas)
                    {
                        allShadowSlicesFitInAtlas = false;
                        break;
                    }
                }

                if (!allShadowSlicesFitInAtlas && !tooManyShadows)
                    shadowSlicesScaleFactor *= 2;
            }

            #if DEVELOPMENT_BUILD
            if (!m_IssuedMessageAboutShadowMapsTooBig && tooManyShadows)
            {
                Debug.LogWarning($"Too many additional punctual lights shadows. URP tried reducing shadow resolutions by {shadowSlicesScaleFactor} but it was still too much. Increase shadow atlas size, decrease big shadow resolutions, or reduce the number of shadow maps active in the same frame (currently was {totalShadowSlicesCount}).");
                m_IssuedMessageAboutShadowMapsTooBig = true; // Only output this once per shadow requests configuration
            }

            if (!m_IssuedMessageAboutShadowMapsRescale && shadowSlicesScaleFactor > 1)
            {
                Debug.Log($"Reduced additional punctual light shadows resolution by {shadowSlicesScaleFactor} to make {totalShadowSlicesCount} shadow maps fit in the {atlasSize}x{atlasSize} shadow atlas. To avoid this, increase shadow atlas size, decrease big shadow resolutions, or reduce the number of shadow maps active in the same frame");
                m_IssuedMessageAboutShadowMapsRescale = true; // Only output this once per shadow requests configuration
            }
            #endif
        }

        #if DEVELOPMENT_BUILD
        private bool m_IssuedMessageAboutShadowSlicesTooMany = false;
        private bool m_IssuedMessageAboutShadowMapsRescale = false;
        private bool m_IssuedMessageAboutShadowMapsTooBig = false;
        private bool m_IssuedMessageAboutRemovedShadowSlices = false;
        private static bool m_IssuedMessageAboutPointLightHardShadowResolutionTooSmall = false;
        private static bool m_IssuedMessageAboutPointLightSoftShadowResolutionTooSmall = false;
        #endif

        Dictionary<int, ulong> m_ShadowRequestsHashes = new Dictionary<int, ulong>();  // used to keep track of changes in the shadow requests and shadow atlas configuration (per camera)

        ulong ResolutionLog2ForHash(int resolution)
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

        ulong ComputeShadowRequestHash(ref RenderingData renderingData)
        {
            ulong numberOfShadowedPointLights = 0;
            ulong numberOfSoftShadowedLights = 0;
            ulong numberOfShadowsWithResolution0128 = 0;
            ulong numberOfShadowsWithResolution0256 = 0;
            ulong numberOfShadowsWithResolution0512 = 0;
            ulong numberOfShadowsWithResolution1024 = 0;
            ulong numberOfShadowsWithResolution2048 = 0;
            ulong numberOfShadowsWithResolution4096 = 0;

            var visibleLights = renderingData.lightData.visibleLights;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                if (!IsValidShadowCastingLight(ref renderingData.lightData, visibleLightIndex))
                    continue;
                ref VisibleLight vl = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                if (vl.lightType == LightType.Point)
                    ++numberOfShadowedPointLights;
                if (vl.light.shadows == LightShadows.Soft)
                    ++numberOfSoftShadowedLights;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 0128)
                    ++numberOfShadowsWithResolution0128;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 0256)
                    ++numberOfShadowsWithResolution0256;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 0512)
                    ++numberOfShadowsWithResolution0512;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 1024)
                    ++numberOfShadowsWithResolution1024;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 2048)
                    ++numberOfShadowsWithResolution2048;
                if (renderingData.shadowData.resolution[visibleLightIndex] == 4096)
                    ++numberOfShadowsWithResolution4096;
            }
            ulong shadowRequestsHash = ResolutionLog2ForHash(renderingData.shadowData.additionalLightsShadowmapWidth) - 8; // bits [00~02]
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

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData"></param>
        /// <returns></returns>
        public bool Setup(ref RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(null, m_ProfilingSetupSampler);

            if (!renderingData.shadowData.additionalLightShadowsEnabled)
                return false;

            if (!renderingData.shadowData.supportsAdditionalLightShadows)
                return SetupForEmptyRendering(ref renderingData);

            Clear();

            renderTargetWidth = renderingData.shadowData.additionalLightsShadowmapWidth;
            renderTargetHeight = renderingData.shadowData.additionalLightsShadowmapHeight;

            var visibleLights = renderingData.lightData.visibleLights;
            int additionalLightsCount = renderingData.lightData.additionalLightsCount;

            int atlasWidth = renderingData.shadowData.additionalLightsShadowmapWidth;

            int totalShadowResolutionRequestsCount = 0; // Number of shadow slices that we would need for all shadowed additional (punctual) lights in the scene. We might have to ignore some of those requests if they do not fit in the shadow atlas.

            m_ShadowResolutionRequests.Clear();

            #if DEVELOPMENT_BUILD
            // Check changes in the shadow requests and shadow atlas configuration - compute shadow request/configuration hash
            if (!renderingData.cameraData.isPreviewCamera)
            {
                ulong newShadowRequestHash = ComputeShadowRequestHash(ref renderingData);
                ulong oldShadowRequestHash = 0;
                m_ShadowRequestsHashes.TryGetValue(renderingData.cameraData.camera.GetHashCode(), out oldShadowRequestHash);
                if (oldShadowRequestHash != newShadowRequestHash)
                {
                    m_ShadowRequestsHashes[renderingData.cameraData.camera.GetHashCode()] = newShadowRequestHash;

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
                m_VisibleLightIndexToAdditionalLightIndex = new int[visibleLights.Length];
                m_VisibleLightIndexToCameraSquareDistance = new float[visibleLights.Length];
                m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex = new int[visibleLights.Length];
            }

            int maxAdditionalLightShadowParams = m_UseStructuredBuffer ? visibleLights.Length : Math.Min(visibleLights.Length, UniversalRenderPipeline.maxVisibleAdditionalLights);
            if (m_AdditionalLightIndexToVisibleLightIndex.Length < maxAdditionalLightShadowParams)
            {
                m_AdditionalLightIndexToVisibleLightIndex = new int[maxAdditionalLightShadowParams];
                m_AdditionalLightIndexToShadowParams = new Vector4[maxAdditionalLightShadowParams];
            }

            // reset m_VisibleLightIndexClosenessToCamera
            for (int visibleLightIndex = 0; visibleLightIndex < m_VisibleLightIndexToCameraSquareDistance.Length; ++visibleLightIndex)
                m_VisibleLightIndexToCameraSquareDistance[visibleLightIndex] = float.MaxValue;

            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                if (visibleLightIndex == renderingData.lightData.mainLightIndex)
                    // Skip main directional light as it is not packed into the shadow atlas
                    continue;

                if (IsValidShadowCastingLight(ref renderingData.lightData, visibleLightIndex))
                {
                    ref VisibleLight vl = ref visibleLights.UnsafeElementAt(visibleLightIndex);

                    int shadowSlicesCountForThisLight = GetPunctualLightShadowSlicesCount(vl.lightType);
                    totalShadowResolutionRequestsCount += shadowSlicesCountForThisLight;

                    for (int perLightShadowSliceIndex = 0; perLightShadowSliceIndex < shadowSlicesCountForThisLight; ++perLightShadowSliceIndex)
                    {
                        m_ShadowResolutionRequests.Add(new ShadowResolutionRequest(visibleLightIndex, perLightShadowSliceIndex, renderingData.shadowData.resolution[visibleLightIndex],
                            (vl.light.shadows == LightShadows.Soft), (vl.lightType == LightType.Point)));
                    }
                    // mark this light as casting shadows
                    m_VisibleLightIndexToCameraSquareDistance[visibleLightIndex] = (renderingData.cameraData.camera.transform.position - vl.light.transform.position).sqrMagnitude;
                }
            }

            if (m_SortedShadowResolutionRequests == null || m_SortedShadowResolutionRequests.Length < totalShadowResolutionRequestsCount)
                m_SortedShadowResolutionRequests = new ShadowResolutionRequest[totalShadowResolutionRequestsCount];

            for (int shadowRequestIndex = 0; shadowRequestIndex < m_ShadowResolutionRequests.Count; ++shadowRequestIndex)
                m_SortedShadowResolutionRequests[shadowRequestIndex] = m_ShadowResolutionRequests[shadowRequestIndex];
            for (int sortedArrayIndex = totalShadowResolutionRequestsCount; sortedArrayIndex < m_SortedShadowResolutionRequests.Length; ++sortedArrayIndex)
                m_SortedShadowResolutionRequests[sortedArrayIndex].requestedResolution = 0; // reset unused entries
            InsertionSort(m_SortedShadowResolutionRequests, 0, totalShadowResolutionRequestsCount);

            // To avoid visual artifacts when there is not enough place in the atlas, we remove shadow slices that would be allocated a too small resolution.
            // When not using structured buffers, m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length maps to _AdditionalLightsWorldToShadow in Shadows.hlsl
            // In that case we have to limit its size because uniform buffers cannot be higher than 64kb for some platforms.
            int totalShadowSlicesCount = m_UseStructuredBuffer ? totalShadowResolutionRequestsCount : Math.Min(totalShadowResolutionRequestsCount, UniversalRenderPipeline.maxVisibleAdditionalLights);  // Number of shadow slices that we will actually be able to fit in the shadow atlas without causing visual artifacts.

            // Find biggest end index in m_SortedShadowResolutionRequests array, under which all shadow requests can be allocated a big enough shadow atlas slot, to not cause rendering artifacts
            bool allShadowsAfterStartIndexHaveEnoughResolution = false;
            int estimatedScaleFactor = 1;
            while (!allShadowsAfterStartIndexHaveEnoughResolution && totalShadowSlicesCount > 0)
            {
                estimatedScaleFactor = EstimateScaleFactorNeededToFitAllShadowsInAtlas(m_SortedShadowResolutionRequests, totalShadowSlicesCount, atlasWidth);

                // check if resolution of the least priority shadow slice request would be acceptable
                if (m_SortedShadowResolutionRequests[totalShadowSlicesCount - 1].requestedResolution >= estimatedScaleFactor * MinimalPunctualLightShadowResolution(m_SortedShadowResolutionRequests[totalShadowSlicesCount - 1].softShadow))
                    allShadowsAfterStartIndexHaveEnoughResolution = true;
                else // Skip shadow requests for this light ; their resolution is too small to look any good
                    totalShadowSlicesCount -= GetPunctualLightShadowSlicesCount(m_SortedShadowResolutionRequests[totalShadowSlicesCount - 1].pointLightShadow ? LightType.Point : LightType.Spot);
            }

            #if DEVELOPMENT_BUILD
            if (totalShadowSlicesCount < totalShadowResolutionRequestsCount)
            {
                if (!m_IssuedMessageAboutRemovedShadowSlices)
                {
                    Debug.LogWarning($"Too many additional punctual lights shadows to look good, URP removed {totalShadowResolutionRequestsCount - totalShadowSlicesCount } shadow maps to make the others fit in the shadow atlas. To avoid this, increase shadow atlas size, remove some shadowed lights, replace soft shadows by hard shadows ; or replace point lights by spot lights");
                    m_IssuedMessageAboutRemovedShadowSlices = true;  // Only output this once per shadow requests configuration
                }
            }
            #endif

            for (int sortedArrayIndex = totalShadowSlicesCount; sortedArrayIndex < m_SortedShadowResolutionRequests.Length; ++sortedArrayIndex)
                m_SortedShadowResolutionRequests[sortedArrayIndex].requestedResolution = 0; // Reset entries that we cannot fit in the atlas

            // Reset the reverse lookup array
            for (int visibleLightIndex = 0; visibleLightIndex < m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex.Length; ++visibleLightIndex)
                m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex] = -1;
            // Update the reverse lookup array (starting from the end of the array, in order to use index of slice#0 in case a same visibleLight has several shadowSlices)
            for (int sortedArrayIndex = totalShadowSlicesCount - 1; sortedArrayIndex >= 0; --sortedArrayIndex)
                m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[m_SortedShadowResolutionRequests[sortedArrayIndex].visibleLightIndex] = sortedArrayIndex;

            AtlasLayout(atlasWidth, totalShadowSlicesCount, estimatedScaleFactor);


            if (m_AdditionalLightsShadowSlices == null || m_AdditionalLightsShadowSlices.Length < totalShadowSlicesCount)
                m_AdditionalLightsShadowSlices = new ShadowSliceData[totalShadowSlicesCount];

            if (m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix == null ||
                (m_UseStructuredBuffer && (m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length < totalShadowSlicesCount)))   // m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix can be resized when using SSBO to pass shadow data (no size limitation)
                m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix = new Matrix4x4[totalShadowSlicesCount];

            // initialize _AdditionalShadowParams
            for (int i = 0; i < maxAdditionalLightShadowParams; ++i)
                m_AdditionalLightIndexToShadowParams[i] = c_DefaultShadowParams;

            int validShadowCastingLightsCount = 0;
            bool supportsSoftShadows = renderingData.shadowData.supportsSoftShadows;
            int additionalLightCount = 0;
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                ref VisibleLight shadowLight = ref visibleLights.UnsafeElementAt(visibleLightIndex);

                // Skip main directional light as it is not packed into the shadow atlas
                if (visibleLightIndex == renderingData.lightData.mainLightIndex)
                {
                    m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = -1;
                    continue;
                }

                int additionalLightIndex = additionalLightCount++;
                if (additionalLightIndex >= m_AdditionalLightIndexToVisibleLightIndex.Length)
                    continue;

                // We need to always set these indices, even if the light is not shadow casting or doesn't fit in the shadow slices (UUM-46577)
                m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex] = visibleLightIndex;
                m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex] = additionalLightIndex;

                if (m_ShadowSliceToAdditionalLightIndex.Count >= totalShadowSlicesCount || additionalLightIndex >= maxAdditionalLightShadowParams)
                    continue;

                LightType lightType = shadowLight.lightType;
                int perLightShadowSlicesCount = GetPunctualLightShadowSlicesCount(lightType);

                if ((m_ShadowSliceToAdditionalLightIndex.Count + perLightShadowSlicesCount) > totalShadowSlicesCount && IsValidShadowCastingLight(ref renderingData.lightData, visibleLightIndex))
                {
                    #if DEVELOPMENT_BUILD
                    if (!m_IssuedMessageAboutShadowSlicesTooMany)
                    {
                        // This case can especially happen in Deferred, where there can be a high number of visibleLights
                        Debug.Log($"There are too many shadowed additional punctual lights active at the same time, URP will not render all the shadows. To ensure all shadows are rendered, reduce the number of shadowed additional lights in the scene ; make sure they are not active at the same time ; or replace point lights by spot lights (spot lights use less shadow maps than point lights).");
                        m_IssuedMessageAboutShadowSlicesTooMany = true; // Only output this once
                    }
                    #endif
                    break;
                }

                int perLightFirstShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices

                bool isValidShadowCastingLight = false;
                for (int perLightShadowSlice = 0; perLightShadowSlice < perLightShadowSlicesCount; ++perLightShadowSlice)
                {
                    int globalShadowSliceIndex = m_ShadowSliceToAdditionalLightIndex.Count; // shadowSliceIndex within the global array of all additional light shadow slices

                    bool lightRangeContainsShadowCasters = renderingData.cullResults.GetShadowCasterBounds(visibleLightIndex, out var shadowCastersBounds);
                    if (lightRangeContainsShadowCasters)
                    {
                        // We need to iterate the lights even though additional lights are disabled because
                        // cullResults.GetShadowCasterBounds() does the fence sync for the shadow culling jobs.
                        if (!renderingData.shadowData.supportsAdditionalLightShadows)
                            continue;

                        if (IsValidShadowCastingLight(ref renderingData.lightData, visibleLightIndex))
                        {
                            if (m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex] == -1)
                            {
                                // We could not find place in the shadow atlas for shadow maps of this light.
                                // Skip it.
                            }
                            else if (lightType == LightType.Spot)
                            {
                                bool success = ShadowUtils.ExtractSpotLightMatrix(ref renderingData.cullResults,
                                    ref renderingData.shadowData,
                                    visibleLightIndex,
                                    out var shadowTransform,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData);

                                if (success)
                                {
                                    m_ShadowSliceToAdditionalLightIndex.Add(additionalLightIndex);
                                    m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Add(perLightShadowSlice);
                                    var light = shadowLight.light;
                                    float shadowStrength = light.shadowStrength;
                                    float softShadows = ShadowUtils.SoftShadowQualityToShaderProperty(light, (supportsSoftShadows && light.shadows == LightShadows.Soft));
                                    Vector4 shadowParams = new Vector4(shadowStrength, softShadows, LightTypeIdentifierInShadowParams_Spot, perLightFirstShadowSliceIndex);
                                    m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = shadowTransform;
                                    m_AdditionalLightIndexToShadowParams[additionalLightIndex] = shadowParams;
                                    isValidShadowCastingLight = true;
                                }
                            }
                            else if (lightType == LightType.Point)
                            {
                                var sliceResolution = m_SortedShadowResolutionRequests[m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex]].allocatedResolution;
                                float fovBias = GetPointLightShadowFrustumFovBiasInDegrees(sliceResolution, (shadowLight.light.shadows == LightShadows.Soft));
                                // Note: the same fovBias will also be used to compute ShadowUtils.GetShadowBias

                                bool success = ShadowUtils.ExtractPointLightMatrix(ref renderingData.cullResults,
                                    ref renderingData.shadowData,
                                    visibleLightIndex,
                                    (CubemapFace)perLightShadowSlice,
                                    fovBias,
                                    out var shadowTransform,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].viewMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].projectionMatrix,
                                    out m_AdditionalLightsShadowSlices[globalShadowSliceIndex].splitData);

                                if (success)
                                {
                                    m_ShadowSliceToAdditionalLightIndex.Add(additionalLightIndex);
                                    m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Add(perLightShadowSlice);
                                    var light = shadowLight.light;
                                    float shadowStrength = light.shadowStrength;
                                    float softShadows = ShadowUtils.SoftShadowQualityToShaderProperty(light, (supportsSoftShadows && light.shadows == LightShadows.Soft));
                                    Vector4 shadowParams = new Vector4(shadowStrength, softShadows, LightTypeIdentifierInShadowParams_Point, perLightFirstShadowSliceIndex);
                                    m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix[globalShadowSliceIndex] = shadowTransform;
                                    m_AdditionalLightIndexToShadowParams[additionalLightIndex] = shadowParams;
                                    isValidShadowCastingLight = true;
                                }
                            }
                        }
                    }
                }

                if (isValidShadowCastingLight)
                    validShadowCastingLightsCount++;
            }

            // Lights that need to be rendered in the shadow map atlas
            if (validShadowCastingLightsCount == 0)
                return SetupForEmptyRendering(ref renderingData);

            int shadowCastingLightsBufferCount = m_ShadowSliceToAdditionalLightIndex.Count;

            // Trim shadow atlas dimensions if possible (to avoid allocating texture space that will not be used)
            int atlasMaxX = 0;
            int atlasMaxY = 0;
            for (int sortedShadowResolutionRequestIndex = 0; sortedShadowResolutionRequestIndex < totalShadowSlicesCount; ++sortedShadowResolutionRequestIndex)
            {
                var shadowResolutionRequest = m_SortedShadowResolutionRequests[sortedShadowResolutionRequestIndex];
                atlasMaxX = Mathf.Max(atlasMaxX, shadowResolutionRequest.offsetX + shadowResolutionRequest.allocatedResolution);
                atlasMaxY = Mathf.Max(atlasMaxY, shadowResolutionRequest.offsetY + shadowResolutionRequest.allocatedResolution);
            }
            // ...but make sure we still use power-of-two dimensions (might perform better on some hardware)

            renderTargetWidth = Mathf.NextPowerOfTwo(atlasMaxX);
            renderTargetHeight = Mathf.NextPowerOfTwo(atlasMaxY);

            float oneOverAtlasWidth = 1.0f / renderTargetWidth;
            float oneOverAtlasHeight = 1.0f / renderTargetHeight;

            Matrix4x4 sliceTransform;
            for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowCastingLightsBufferCount; ++globalShadowSliceIndex)
            {
                int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                // We can skip the slice if strength is zero.
                if (Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f) || Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                    continue;

                int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];
                int sortedShadowResolutionRequestFirstSliceIndex = m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex];
                int perLightSliceIndex = m_GlobalShadowSliceIndexToPerLightShadowSliceIndex[globalShadowSliceIndex];
                var shadowResolutionRequest = m_SortedShadowResolutionRequests[sortedShadowResolutionRequestFirstSliceIndex + perLightSliceIndex];
                int sliceResolution = shadowResolutionRequest.allocatedResolution;

                sliceTransform = Matrix4x4.identity;
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

            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_AdditionalLightsShadowmapHandle, renderTargetWidth, renderTargetHeight, k_ShadowmapBufferBits, name: "_AdditionalLightsShadowmapTexture");

            m_MaxShadowDistanceSq = renderingData.cameraData.maxShadowDistance * renderingData.cameraData.maxShadowDistance;
            m_CascadeBorder = renderingData.shadowData.mainLightShadowCascadeBorder;
            m_CreateEmptyShadowmap = false;
            useNativeRenderPass = true;

            return true;
        }

        bool SetupForEmptyRendering(ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.renderer.stripShadowsOffVariants)
                return false;

            renderingData.shadowData.isKeywordAdditionalLightShadowsEnabled = true;
            ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_AdditionalLightsShadowmapHandle, 1, 1, k_ShadowmapBufferBits, name: "_AdditionalLightsShadowmapTexture");
            m_CreateEmptyShadowmap = true;
            useNativeRenderPass = false;

            // initialize _AdditionalShadowParams
            for (int i = 0; i < m_AdditionalLightIndexToShadowParams.Length; ++i)
                m_AdditionalLightIndexToShadowParams[i] = c_DefaultShadowParams;

            return true;
        }

        /// <inheritdoc/>
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            ConfigureTarget(m_AdditionalLightsShadowmapHandle);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_CreateEmptyShadowmap)
            {
                SetEmptyAdditionalShadowmapAtlas(ref context, ref renderingData);
                renderingData.commandBuffer.SetGlobalTexture(m_AdditionalLightsShadowmapID, m_AdditionalLightsShadowmapHandle.nameID);

                return;
            }

            if (renderingData.shadowData.supportsAdditionalLightShadows)
            {
                RenderAdditionalShadowmapAtlas(ref context, ref renderingData);
                renderingData.commandBuffer.SetGlobalTexture(m_AdditionalLightsShadowmapID, m_AdditionalLightsShadowmapHandle.nameID);
            }
        }

        // Function called by Deferred Renderer
        /// <summary>
        /// Gets the additional light index from the global visible light index, which is used to index arrays _AdditionalLightsPosition, _AdditionalShadowParams, etc.
        /// </summary>
        /// <param name="visibleLightIndex">The index of the visible light.</param>
        /// <returns>The additional light index.</returns>
        public int GetShadowLightIndexFromLightIndex(int visibleLightIndex)
        {
            if (visibleLightIndex < 0 || visibleLightIndex >= m_VisibleLightIndexToAdditionalLightIndex.Length)
                return -1;

            return m_VisibleLightIndexToAdditionalLightIndex[visibleLightIndex];
        }

        void Clear()
        {
            m_ShadowSliceToAdditionalLightIndex.Clear();
            m_GlobalShadowSliceIndexToPerLightShadowSliceIndex.Clear();
        }

        void SetEmptyAdditionalShadowmapAtlas(ref ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = renderingData.commandBuffer;
            CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, true);
            if (RenderingUtils.useStructuredBuffer)
            {
                var shadowParamsBuffer = ShaderData.instance.GetAdditionalLightShadowParamsStructuredBuffer(m_AdditionalLightIndexToShadowParams.Length);
                shadowParamsBuffer.SetData(m_AdditionalLightIndexToShadowParams);
                cmd.SetGlobalBuffer(m_AdditionalShadowParams_SSBO, shadowParamsBuffer);
            }
            else
            {
                cmd.SetGlobalVectorArray(AdditionalShadowsConstantBuffer._AdditionalShadowParams, m_AdditionalLightIndexToShadowParams);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        void RenderAdditionalShadowmapAtlas(ref ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cullResults = renderingData.cullResults;
            var lightData = renderingData.lightData;

            NativeArray<VisibleLight> visibleLights = lightData.visibleLights;

            bool additionalLightHasSoftShadows = false;

            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.AdditionalLightsShadow)))
            {
                bool anyShadowSliceRenderer = false;
                int shadowSlicesCount = m_ShadowSliceToAdditionalLightIndex.Count;
                for (int globalShadowSliceIndex = 0; globalShadowSliceIndex < shadowSlicesCount; ++globalShadowSliceIndex)
                {
                    int additionalLightIndex = m_ShadowSliceToAdditionalLightIndex[globalShadowSliceIndex];

                    // we do the shadow strength check here again here because we might have zero strength for non-shadow-casting lights.
                    // In that case we need the shadow data buffer but we can skip rendering them to shadowmap.
                    if (Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].x, 0.0f) || Mathf.Approximately(m_AdditionalLightIndexToShadowParams[additionalLightIndex].w, -1.0f))
                        continue;

                    int visibleLightIndex = m_AdditionalLightIndexToVisibleLightIndex[additionalLightIndex];

                    ref VisibleLight shadowLight = ref visibleLights.UnsafeElementAt(visibleLightIndex);

                    ShadowSliceData shadowSliceData = m_AdditionalLightsShadowSlices[globalShadowSliceIndex];

                    var settings = new ShadowDrawingSettings(cullResults, visibleLightIndex, BatchCullingProjectionType.Perspective);
                    settings.useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers;
                    settings.splitData = shadowSliceData.splitData;
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, visibleLightIndex,
                        ref renderingData.shadowData, shadowSliceData.projectionMatrix, shadowSliceData.resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, true);
                    ShadowUtils.RenderShadowSlice(cmd, ref context, ref shadowSliceData, ref settings);
                    additionalLightHasSoftShadows |= shadowLight.light.shadows == LightShadows.Soft;
                    anyShadowSliceRenderer = true;
                }

                // We share soft shadow settings for main light and additional lights to save keywords.
                // So we check here if pipeline supports soft shadows and either main light or any additional light has soft shadows
                // to enable the keyword.
                // TODO: In PC and Consoles we can upload shadow data per light and branch on shader. That will be more likely way faster.
                bool mainLightHasSoftShadows = renderingData.shadowData.supportsMainLightShadows &&
                                               lightData.mainLightIndex != -1 &&
                                               visibleLights[lightData.mainLightIndex].light.shadows == LightShadows.Soft;

                // If the OFF variant has been stripped, the additional light shadows keyword must always be enabled
                bool hasOffVariant = !renderingData.cameraData.renderer.stripShadowsOffVariants;
                renderingData.shadowData.isKeywordAdditionalLightShadowsEnabled = !hasOffVariant || anyShadowSliceRenderer;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.AdditionalLightShadows, renderingData.shadowData.isKeywordAdditionalLightShadowsEnabled);

                bool softShadows = renderingData.shadowData.supportsSoftShadows && (mainLightHasSoftShadows || additionalLightHasSoftShadows);
                renderingData.shadowData.isKeywordSoftShadowsEnabled = softShadows;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, renderingData.shadowData.isKeywordSoftShadowsEnabled);

                if (anyShadowSliceRenderer)
                    SetupAdditionalLightsShadowReceiverConstants(cmd, softShadows);
            }
        }

        // Set constant buffer data that will be used during the lighting/shadowing pass
        void SetupAdditionalLightsShadowReceiverConstants(CommandBuffer cmd, bool softShadows)
        {
            if (m_UseStructuredBuffer)
            {
                // per-light data
                var shadowParamsBuffer = ShaderData.instance.GetAdditionalLightShadowParamsStructuredBuffer(m_AdditionalLightIndexToShadowParams.Length);
                shadowParamsBuffer.SetData(m_AdditionalLightIndexToShadowParams);
                cmd.SetGlobalBuffer(m_AdditionalShadowParams_SSBO, shadowParamsBuffer);

                // per-shadow-slice data
                var shadowSliceMatricesBuffer = ShaderData.instance.GetAdditionalLightShadowSliceMatricesStructuredBuffer(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length);
                shadowSliceMatricesBuffer.SetData(m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix);
                cmd.SetGlobalBuffer(m_AdditionalLightsWorldToShadow_SSBO, shadowSliceMatricesBuffer);
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
                Vector2Int allocatedShadowAtlasSize = m_AdditionalLightsShadowmapHandle.referenceSize;
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

        bool IsValidShadowCastingLight(ref LightData lightData, int i)
        {
            if (i == lightData.mainLightIndex)
                return false;

            ref VisibleLight shadowLight = ref lightData.visibleLights.UnsafeElementAt(i);

            // Directional and light shadows are not supported in the shadow map atlas
            if (shadowLight.lightType == LightType.Directional)
                return false;

            Light light = shadowLight.light;
            return light != null && light.shadows != LightShadows.None && !Mathf.Approximately(light.shadowStrength, 0.0f);
        }

        private class PassData
        {
            internal AdditionalLightsShadowCasterPass pass;
            internal RenderGraph graph;

            internal TextureHandle shadowmapTexture;
            internal RenderingData renderingData;
            internal int shadowmapID;

            internal bool emptyShadowmap;
        }

        internal TextureHandle Render(RenderGraph graph, ref RenderingData renderingData)
        {
            TextureHandle shadowTexture;

            using (var builder = graph.AddRenderPass<PassData>("Additional Lights Shadowmap", out var passData, base.profilingSampler))
            {
                InitPassData(ref passData, ref renderingData, ref graph);

                if (!m_CreateEmptyShadowmap)
                {
                    passData.shadowmapTexture = UniversalRenderer.CreateRenderGraphTexture(graph, m_AdditionalLightsShadowmapHandle.rt.descriptor, "Additional Shadowmap", true,  ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear);
                    builder.UseDepthBuffer(passData.shadowmapTexture, DepthAccess.Write);
                }

                // Need this as shadowmap is only used as Global Texture and not a buffer, so would get culled by RG
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    if (!data.emptyShadowmap)
                        data.pass.RenderAdditionalShadowmapAtlas(ref context.renderContext, ref data.renderingData);
                });

                shadowTexture = passData.shadowmapTexture;
            }

            using (var builder = graph.AddRenderPass<PassData>("Set Additional Shadow Globals", out var passData, base.profilingSampler))
            {
                InitPassData(ref passData, ref renderingData, ref graph);

                passData.shadowmapTexture = shadowTexture;

                if (shadowTexture.IsValid())
                    builder.UseDepthBuffer(shadowTexture, DepthAccess.Read);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    if (data.emptyShadowmap)
                    {
                        data.pass.SetEmptyAdditionalShadowmapAtlas(ref context.renderContext, ref data.renderingData);
                        data.shadowmapTexture = data.graph.defaultResources.defaultShadowTexture;
                    }

                    data.renderingData.commandBuffer.SetGlobalTexture(data.shadowmapID, data.shadowmapTexture);
                });

                return passData.shadowmapTexture;
            }
        }

        void InitPassData(ref PassData passData, ref RenderingData renderingData, ref RenderGraph graph)
        {
            passData.pass = this;
            passData.graph = graph;

            passData.emptyShadowmap = m_CreateEmptyShadowmap;
            passData.shadowmapID = m_AdditionalLightsShadowmapID;
            passData.renderingData = renderingData;
        }
    }
}
