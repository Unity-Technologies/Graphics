using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal struct AdditionalLightsShadowAtlasLayout
    {
        internal struct ShadowResolutionRequest
        {
            public ushort visibleLightIndex;
            public ushort perLightShadowSliceIndex;
            public ushort requestedResolution;
            public ushort offsetX;             // x coordinate of the square area allocated in the atlas for this shadow map
            public ushort offsetY;             // y coordinate of the square area allocated in the atlas for this shadow map
            public ushort allocatedResolution; // width of the square area allocated in the atlas for this shadow map

            [Flags]
            private enum SettingsOptions : ushort
            {
                None = 0,
                SoftShadow = (1 << 0),
                PointLightShadow = (1 << 1),
                All = 0xFFFF
            }
            private SettingsOptions m_ShadowProperties;

            public bool softShadow
            {
                get => m_ShadowProperties.HasFlag(SettingsOptions.SoftShadow); // otherwise it's hard-shadow (no filtering)
                set
                {
                    if (value)
                        m_ShadowProperties |= SettingsOptions.SoftShadow;
                    else
                        m_ShadowProperties &= ~SettingsOptions.SoftShadow;
                }
            }

            public bool pointLightShadow
            {
                get => m_ShadowProperties.HasFlag(SettingsOptions.PointLightShadow); // otherwise it's spot light shadow (1 shadow slice instead of 6)
                set
                {
                    if (value)
                        m_ShadowProperties |= SettingsOptions.PointLightShadow;
                    else
                        m_ShadowProperties &= ~SettingsOptions.PointLightShadow;
                }
            }
        }

        // Static fields used to avoid GC allocs of intermediate computations
        static List<RectInt> s_UnusedAtlasSquareAreas; // This list tracks space available in the atlas
        static List<ShadowResolutionRequest> s_ShadowResolutionRequests; // intermediate array used to compute the final resolution of each shadow slice rendered in the frame
        static float[] s_VisibleLightIndexToCameraSquareDistance; // stores for each shadowed additional light its (squared) distance to camera ; used to sub-sort shadow requests according to how close their casting light is
        static Func<ShadowResolutionRequest, ShadowResolutionRequest, int> s_CompareShadowResolutionRequest;
        static ShadowResolutionRequest[] s_SortedShadowResolutionRequests;

        NativeArray<ShadowResolutionRequest> m_SortedShadowResolutionRequests;
        NativeArray<int> m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex; // for each visible light, store the index of its first shadow slice in m_SortedShadowResolutionRequests (for quicker access)
        int m_TotalShadowSlicesCount;
        int m_TotalShadowResolutionRequestCount;
        bool m_TooManyShadowMaps;
        int m_ShadowSlicesScaleFactor;
        int m_AtlasSize;

        public AdditionalLightsShadowAtlasLayout(UniversalLightData lightData, UniversalShadowData shadowData, UniversalCameraData cameraData)
        {
            bool useStructuredBuffer = RenderingUtils.useStructuredBuffer;
            NativeArray<VisibleLight> visibleLights = lightData.visibleLights;
            int numberOfVisibleLights = visibleLights.Length;

            if (s_UnusedAtlasSquareAreas == null)
                s_UnusedAtlasSquareAreas = new List<RectInt>();

            if (s_ShadowResolutionRequests == null)
                s_ShadowResolutionRequests = new List<ShadowResolutionRequest>();

            if (s_VisibleLightIndexToCameraSquareDistance == null || s_VisibleLightIndexToCameraSquareDistance.Length < numberOfVisibleLights)
                s_VisibleLightIndexToCameraSquareDistance = new float[numberOfVisibleLights];

            if (s_CompareShadowResolutionRequest == null)
                s_CompareShadowResolutionRequest = CreateCompareShadowResolutionRequesPredicate();

            if (!useStructuredBuffer)
            {
                int newCapacity = UniversalRenderPipeline.maxVisibleAdditionalLights;

                if (s_UnusedAtlasSquareAreas.Capacity < newCapacity)
                    s_UnusedAtlasSquareAreas.Capacity = newCapacity;

                if (s_ShadowResolutionRequests.Count < numberOfVisibleLights)
                {
                    s_ShadowResolutionRequests.Capacity = numberOfVisibleLights;
                    int diff = numberOfVisibleLights - s_ShadowResolutionRequests.Count + 1;
                    for (int i = 0; i < diff; i++)
                        s_ShadowResolutionRequests.Add(new ShadowResolutionRequest());
                }

            }

            s_UnusedAtlasSquareAreas.Clear();

            ushort totalShadowResolutionRequestsCount = 0; // Number of shadow slices that we would need for all shadowed additional (punctual) lights in the scene. We might have to ignore some of those requests if they do not fit in the shadow atlas.
            for (int visibleLightIndex = 0; visibleLightIndex < visibleLights.Length; ++visibleLightIndex)
            {
                // Skip main directional light as it is not packed into the shadow atlas
                if (visibleLightIndex == lightData.mainLightIndex)
                {
                    s_VisibleLightIndexToCameraSquareDistance[visibleLightIndex] = float.MaxValue;
                    continue;
                }

                ref VisibleLight vl = ref visibleLights.UnsafeElementAt(visibleLightIndex);
                Light light = vl.light;
                LightType lightType = vl.lightType;
                LightShadows lightShadows = light.shadows;
                float shadowStrength = light.shadowStrength;
                if (!ShadowUtils.IsValidShadowCastingLight(lightData, visibleLightIndex, lightType, lightShadows, shadowStrength))
                {
                    s_VisibleLightIndexToCameraSquareDistance[visibleLightIndex] = float.MaxValue;
                    continue;
                }

                bool softShadows = (lightShadows == LightShadows.Soft);
                bool pointLightShadow = (lightType == LightType.Point);
                ushort visibleLightIndexUshort = (ushort)visibleLightIndex;
                ushort requestedResolution = (ushort)shadowData.resolution[visibleLightIndex];
                int shadowSlicesCountForThisLight = ShadowUtils.GetPunctualLightShadowSlicesCount(lightType);
                for (ushort perLightShadowSliceIndex = 0; perLightShadowSliceIndex < shadowSlicesCountForThisLight; ++perLightShadowSliceIndex)
                {
                    if (totalShadowResolutionRequestsCount >= s_ShadowResolutionRequests.Count)
                        s_ShadowResolutionRequests.Add(new ShadowResolutionRequest());

                    ShadowResolutionRequest request = s_ShadowResolutionRequests[totalShadowResolutionRequestsCount];
                    request.visibleLightIndex = visibleLightIndexUshort;
                    request.perLightShadowSliceIndex = perLightShadowSliceIndex;
                    request.requestedResolution = requestedResolution;
                    request.softShadow = softShadows;
                    request.pointLightShadow = pointLightShadow;
                    s_ShadowResolutionRequests[totalShadowResolutionRequestsCount] = request;
                    totalShadowResolutionRequestsCount++;
                }

                // mark this light as casting shadows
                s_VisibleLightIndexToCameraSquareDistance[visibleLightIndex] = (cameraData.worldSpaceCameraPos - light.transform.position).sqrMagnitude;
            }

            if (s_SortedShadowResolutionRequests == null || s_SortedShadowResolutionRequests.Length < totalShadowResolutionRequestsCount)
                s_SortedShadowResolutionRequests = new ShadowResolutionRequest[totalShadowResolutionRequestsCount];

            for (int i = 0; i < totalShadowResolutionRequestsCount; ++i)
                s_SortedShadowResolutionRequests[i] = s_ShadowResolutionRequests[i];

            using (new ProfilingScope(Sorting.s_QuickSortSampler))
            {
                Sorting.QuickSort(s_SortedShadowResolutionRequests, 0, totalShadowResolutionRequestsCount - 1, s_CompareShadowResolutionRequest);
            }

            m_SortedShadowResolutionRequests = new NativeArray<ShadowResolutionRequest>(s_SortedShadowResolutionRequests, Allocator.Temp);

            // To avoid visual artifacts when there is not enough place in the atlas, we remove shadow slices that would be allocated a too small resolution.
            // When not using structured buffers, m_AdditionalLightShadowSliceIndexTo_WorldShadowMatrix.Length maps to _AdditionalLightsWorldToShadow in Shadows.hlsl
            // In that case we have to limit its size because uniform buffers cannot be higher than 64kb for some platforms.
            int totalShadowSlicesCount = useStructuredBuffer ? totalShadowResolutionRequestsCount : Math.Min(totalShadowResolutionRequestsCount, UniversalRenderPipeline.maxVisibleAdditionalLights);  // Number of shadow slices that we will actually be able to fit in the shadow atlas without causing visual artifacts.
            int atlasSize = shadowData.additionalLightsShadowmapWidth;

            // Find biggest end index in m_SortedShadowResolutionRequests array, under which all shadow requests can be allocated a big enough shadow atlas slot, to not cause rendering artifacts
            bool allShadowsAfterStartIndexHaveEnoughResolution = false;
            int estimatedScaleFactor = 1;
            while (!allShadowsAfterStartIndexHaveEnoughResolution && totalShadowSlicesCount > 0)
            {
                ShadowResolutionRequest request = m_SortedShadowResolutionRequests[totalShadowSlicesCount - 1];
                estimatedScaleFactor = EstimateScaleFactorNeededToFitAllShadowsInAtlas(m_SortedShadowResolutionRequests, totalShadowSlicesCount, atlasSize);

                // check if resolution of the least priority shadow slice request would be acceptable
                if (request.requestedResolution >= estimatedScaleFactor * ShadowUtils.MinimalPunctualLightShadowResolution(request.softShadow))
                    allShadowsAfterStartIndexHaveEnoughResolution = true;
                else // Skip shadow requests for this light ; their resolution is too small to look any good
                    totalShadowSlicesCount -= ShadowUtils.GetPunctualLightShadowSlicesCount(request.pointLightShadow ? LightType.Point : LightType.Spot);
            }

            for (int sortedArrayIndex = totalShadowSlicesCount; sortedArrayIndex < m_SortedShadowResolutionRequests.Length; ++sortedArrayIndex)
                m_SortedShadowResolutionRequests[sortedArrayIndex] = default; // Reset entries that we cannot fit in the atlas

            m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex = new NativeArray<int>(visibleLights.Length, Allocator.Temp);

            // Reset the reverse lookup array
            for (int visibleLightIndex = 0; visibleLightIndex < m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex.Length; ++visibleLightIndex)
                m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex] = -1;

            // Update the reverse lookup array (starting from the end of the array, in order to use index of slice#0 in case a same visibleLight has several shadowSlices)
            for (int sortedArrayIndex = totalShadowSlicesCount - 1; sortedArrayIndex >= 0; --sortedArrayIndex)
            {
                int visibleLightIndex = s_SortedShadowResolutionRequests[sortedArrayIndex].visibleLightIndex;
                m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[visibleLightIndex] = sortedArrayIndex;
            }

            // Assigns to each of the first totalShadowSlicesCount items in m_SortedShadowResolutionRequests a location in the shadow atlas based on requested resolutions.
            // If necessary, scales down shadow maps active in the frame, to make all of them fit in the atlas.
            bool allShadowSlicesFitInAtlas = false;
            bool tooManyShadows = false;
            int shadowSlicesScaleFactor = estimatedScaleFactor;

            while (!allShadowSlicesFitInAtlas && !tooManyShadows)
            {
                s_UnusedAtlasSquareAreas.Clear();
                s_UnusedAtlasSquareAreas.Add(new RectInt(0, 0, atlasSize, atlasSize));
                allShadowSlicesFitInAtlas = true;

                for (int shadowRequestIndex = 0; shadowRequestIndex < totalShadowSlicesCount; ++shadowRequestIndex)
                {
                    var resolution = m_SortedShadowResolutionRequests[shadowRequestIndex].requestedResolution / shadowSlicesScaleFactor;

                    if (resolution < ShadowUtils.MinimalPunctualLightShadowResolution(m_SortedShadowResolutionRequests[shadowRequestIndex].softShadow))
                    {
                        tooManyShadows = true;
                        break;
                    }

                    bool foundSpaceInAtlas = false;

                    // Try to find free space in the atlas
                    for (int unusedAtlasSquareAreaIndex = 0; unusedAtlasSquareAreaIndex < s_UnusedAtlasSquareAreas.Count; ++unusedAtlasSquareAreaIndex)
                    {
                        RectInt atlasArea = s_UnusedAtlasSquareAreas[unusedAtlasSquareAreaIndex];
                        int atlasAreaWidth = atlasArea.width;
                        if (atlasAreaWidth < resolution)
                            continue;

                        int atlasAreaHeight = atlasArea.height;
                        int atlasAreaX = atlasArea.x;
                        int atlasAreaY = atlasArea.y;

                        // we can use this atlas area for the shadow request
                        ref ShadowResolutionRequest shadowRequest = ref m_SortedShadowResolutionRequests.UnsafeElementAtMutable(shadowRequestIndex);
                        shadowRequest.offsetX = (ushort)atlasAreaX;
                        shadowRequest.offsetY = (ushort)atlasAreaY;
                        shadowRequest.allocatedResolution = (ushort) resolution;

                        // this atlas space is not available anymore, so remove it from the list
                        s_UnusedAtlasSquareAreas.RemoveAt(unusedAtlasSquareAreaIndex);

                        // make sure to split space so that the rest of this square area can be used
                        int remainingShadowRequestsCount = totalShadowSlicesCount - shadowRequestIndex - 1; // (no need to add more than that)
                        int newSquareAreasCount = 0;
                        int newSquareAreaWidth = resolution; // we split the area in squares of same size
                        int newSquareAreaHeight = resolution;
                        int newSquareAreaX = atlasAreaX;
                        int newSquareAreaY = atlasAreaY;

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
                            s_UnusedAtlasSquareAreas.Insert(unusedAtlasSquareAreaIndex + newSquareAreasCount, new RectInt(newSquareAreaX, newSquareAreaY, newSquareAreaWidth, newSquareAreaHeight));
                            ++newSquareAreasCount;
                        }

                        foundSpaceInAtlas = true;
                        break;
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

            m_TooManyShadowMaps = tooManyShadows;
            m_ShadowSlicesScaleFactor = shadowSlicesScaleFactor;
            m_TotalShadowSlicesCount = totalShadowSlicesCount;
            m_TotalShadowResolutionRequestCount = totalShadowResolutionRequestsCount;
            m_AtlasSize = atlasSize;
        }

        public int GetTotalShadowSlicesCount() => m_TotalShadowSlicesCount;

        public int GetTotalShadowResolutionRequestCount() => m_TotalShadowResolutionRequestCount;

        public bool HasTooManyShadowMaps() => m_TooManyShadowMaps;

        public int GetShadowSlicesScaleFactor() => m_ShadowSlicesScaleFactor;

        public int GetAtlasSize() => m_AtlasSize;

        public bool HasSpaceForLight(int originalVisibleLightIndex)
        {
            return m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[originalVisibleLightIndex] != -1;
        }

        public ShadowResolutionRequest GetSortedShadowResolutionRequest(int sortedShadowResolutionRequestIndex)
        {
            return m_SortedShadowResolutionRequests[sortedShadowResolutionRequestIndex];
        }

        public ShadowResolutionRequest GetSliceShadowResolutionRequest(int originalVisibleLightIndex, int sliceIndex)
        {
            int sortedShadowResolutionRequestIndex = m_VisibleLightIndexToSortedShadowResolutionRequestsFirstSliceIndex[originalVisibleLightIndex];
            return m_SortedShadowResolutionRequests[sortedShadowResolutionRequestIndex + sliceIndex];
        }

        public static void ClearStaticCaches()
        {
            s_UnusedAtlasSquareAreas = null;
            s_ShadowResolutionRequests = null;
            s_VisibleLightIndexToCameraSquareDistance = null;
            s_CompareShadowResolutionRequest = null;
            s_SortedShadowResolutionRequests = null;
        }

        static int EstimateScaleFactorNeededToFitAllShadowsInAtlas(in NativeArray<ShadowResolutionRequest> shadowResolutionRequests, int endIndex, int atlasSize)
        {
            long totalTexelsInShadowAtlas = atlasSize * atlasSize;

            long totalTexelsInShadowRequests = 0;
            for (int shadowRequestIndex = 0; shadowRequestIndex < endIndex; ++shadowRequestIndex)
                totalTexelsInShadowRequests += shadowResolutionRequests[shadowRequestIndex].requestedResolution * shadowResolutionRequests[shadowRequestIndex].requestedResolution;

            int estimatedScaleFactor = 1;
            while (totalTexelsInShadowRequests > totalTexelsInShadowAtlas * estimatedScaleFactor * estimatedScaleFactor)
                estimatedScaleFactor *= 2;

            return estimatedScaleFactor;
        }

        // Sort array in decreasing requestedResolution order,
        // sub-sorting in "HardShadow > SoftShadow",
        //   i.e place last requests that will be removed in priority to make room for the others,
        //   because their resolution is too small to produce good-looking shadows ; or because they take relatively more space in the atlas )
        // sub-sub-sorting in light distance to camera
        // then grouping in increasing visibleIndex (and sub-sorting each group in ShadowSliceIndex order)
        static Func<ShadowResolutionRequest, ShadowResolutionRequest, int> CreateCompareShadowResolutionRequesPredicate()
        {
            return (ShadowResolutionRequest curr, ShadowResolutionRequest other) =>
            {
                return (((curr.requestedResolution > other.requestedResolution)
                         || (curr.requestedResolution == other.requestedResolution && !curr.softShadow && other.softShadow)
                         || (curr.requestedResolution == other.requestedResolution && curr.softShadow == other.softShadow && s_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] < s_VisibleLightIndexToCameraSquareDistance[other.visibleLightIndex])
                         || (curr.requestedResolution == other.requestedResolution && curr.softShadow == other.softShadow && s_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] == s_VisibleLightIndexToCameraSquareDistance[other.visibleLightIndex] && curr.visibleLightIndex < other.visibleLightIndex)
                         || (curr.requestedResolution == other.requestedResolution && curr.softShadow == other.softShadow && s_VisibleLightIndexToCameraSquareDistance[curr.visibleLightIndex] == s_VisibleLightIndexToCameraSquareDistance[other.visibleLightIndex] && curr.visibleLightIndex == other.visibleLightIndex && curr.perLightShadowSliceIndex < other.perLightShadowSliceIndex)))
                    ? -1 : 1;
            };
        }
    }
}
