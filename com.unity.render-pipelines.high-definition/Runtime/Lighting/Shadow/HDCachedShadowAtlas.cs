using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDCachedShadowAtlas : HDShadowAtlas
    {
        static private int s_InitialCapacity = 256;

        // Constants.
        private const int m_MinSlotSize = 64;
        private const int m_MaxShadowsPerLight = 6;


        private int m_NextLightID = 0;
        private bool m_CanTryPlacement = false;

        struct CachedShadowRecord
        {
            internal int shadowIndex;
            internal int viewportSize;                               // We assume only square shadows maps.
            internal Vector4 offsetInAtlas;                          // When is registered xy is the offset in the texture atlas, in UVs, the zw is the entry offset in the C# representation.
        }

        // We need an extra struct to track differences in the light transform
        // since we don't have such a callback (a-la invalidate) for those.
        struct CachedTransform
        {
            internal Vector3 position;
            internal Vector3 angles; // Only for area and spot
        }

        private int m_AtlasResolutionInSlots;       // Atlas Resolution / m_MinSlotSize

        private bool m_NeedOptimalPacking = true;   // Whenever this is set to true, the pending lights are sorted before insertion. 

        private List<bool> m_AtlasSlots;            // One entry per slot (of size m_MinSlotSize) true if occupied, false if free.

        // Note: Some of these could be simple lists, but since we might need to search by index some of them and we want to avoid GC alloc, a dictionary is easier.
        // This also mean slightly worse performance, however hopefully the number of cached shadow lights is not huge at any tie.
        private Dictionary<int, CachedShadowRecord> m_PlacedShadows;
        private Dictionary<int, CachedShadowRecord> m_ShadowsPendingRendering;
        private Dictionary<int, HDAdditionalLightData> m_RegisteredLightDataPendingPlacement;
        private Dictionary<int, CachedShadowRecord> m_RecordsPendingPlacement;          // Note: this is different from m_RegisteredLightDataPendingPlacement because it contains records that were allocated in the system
                                                                                        // but they lost their spot (e.g. post defrag). They don't have a light associated anymore if not by index, so we keep a separate collection.

        private Dictionary<int, CachedTransform> m_TransformCaches;
        private List<CachedShadowRecord> m_TempListForPlacement;


        private ShadowMapType m_ShadowType;

        // ------------------------------------------------------------------------------------------
        //                                      Init Functions
        // ------------------------------------------------------------------------------------------
        public HDCachedShadowAtlas(ShadowMapType type)
        {
            m_PlacedShadows = new Dictionary<int, CachedShadowRecord>(s_InitialCapacity);
            m_ShadowsPendingRendering = new Dictionary<int, CachedShadowRecord>(s_InitialCapacity);
            m_TempListForPlacement = new List<CachedShadowRecord>(s_InitialCapacity);

            m_RegisteredLightDataPendingPlacement = new Dictionary<int, HDAdditionalLightData>(s_InitialCapacity);
            m_RecordsPendingPlacement = new Dictionary<int, CachedShadowRecord>(s_InitialCapacity);

            m_TransformCaches = new Dictionary<int, CachedTransform>(s_InitialCapacity / 2);

            m_ShadowType = type;
        }

        public override void InitAtlas(HDShadowAtlasInitParameters atlasInitParams)
        {
            base.InitAtlas(atlasInitParams);
            m_IsACacheForShadows = true;

            m_AtlasResolutionInSlots = HDUtils.DivRoundUp(width, m_MinSlotSize);
            m_AtlasSlots = new List<bool>(m_AtlasResolutionInSlots * m_AtlasResolutionInSlots);
            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots.Add(false);
            }

            // Note: If changing the characteristics of the atlas via HDRP asset, the lights OnEnable will not be called again so we are missing them, however we can explicitly
            // put them back up for placement. If this is the first Init of the atlas, the lines below do nothing.
            DefragmentAtlasAndReRender(atlasInitParams.initParams);
            m_CanTryPlacement = true;
            m_NeedOptimalPacking = true;
        }
        // ------------------------------------------------------------------------------------------

        // ------------------------------------------------------------------------------------------
        //          Functions for mixed cached shadows that need to live in cached atlas
        // ------------------------------------------------------------------------------------------

        public void AddBlitRequestsForUpdatedShadows(HDDynamicShadowAtlas dynamicAtlas)
        {
            foreach (var request in m_ShadowRequests)
            {
                if(request.shouldRenderCachedComponent) // meaning it has been updated this time frame
                {
                    dynamicAtlas.AddRequestToPendingBlitFromCache(request);
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        //          Functions to access and deal with the C# representation of the atlas 
        // ------------------------------------------------------------------------------------------
        private bool IsEntryEmpty(int x, int y)
        {
            return (m_AtlasSlots[y * m_AtlasResolutionInSlots + x] == false);
        }
        private bool IsEntryFull(int x, int y)
        {
            return (m_AtlasSlots[y * m_AtlasResolutionInSlots + x]);
        }

        // Always fill slots in a square shape, for example : if x = 1 and y = 2, if numEntries = 2 it will fill {(1,2),(2,2),(1,3),(2,3)}
        private void FillEntries(int x, int y, int numEntries)
        {
            MarkEntries(x, y, numEntries, true);
        }

        private void MarkEntries(int x, int y, int numEntries, bool value)
        {
            for (int j = y; j < y + numEntries; ++j)
            {
                for (int i = x; i < x + numEntries; ++i)
                {
                    m_AtlasSlots[j * m_AtlasResolutionInSlots + i] = value;
                }
            }
        }

        // Checks if we have a square slot available starting from (x,y) and of size numEntries.
        private bool CheckSlotAvailability(int x, int y, int numEntries)
        {
            for (int j = y; j < y + numEntries; ++j)
            {
                for (int i = x; i < x + numEntries; ++i)
                {
                    if (i >= m_AtlasResolutionInSlots || j >= m_AtlasResolutionInSlots || IsEntryFull(i, j))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal bool FindSlotInAtlas(int resolution, out int x, out int y)
        {
            int numEntries = HDUtils.DivRoundUp(resolution, m_MinSlotSize);

            for (int j = 0; j < m_AtlasResolutionInSlots; ++j)
            {
                for (int i = 0; i < m_AtlasResolutionInSlots; ++i)
                {
                    if (CheckSlotAvailability(i, j, numEntries))
                    {
                        x = i;
                        y = j;
                        return true;
                    }
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        internal bool GetSlotInAtlas(int resolution, out int x, out int y)
        {
            if (FindSlotInAtlas(resolution, out x, out y))
            {
                int numEntries = HDUtils.DivRoundUp(resolution, m_MinSlotSize);
                FillEntries(x, y, numEntries);
                return true;
            }

            return false;
        }
        // ---------------------------------------------------------------------------------------       

        // ------------------------------------------------------------------------------------------
        //                           Entry and exit points to the atlas 
        // ------------------------------------------------------------------------------------------

        internal int GetNextLightIdentifier()
        {
            int outputId = m_NextLightID;
            m_NextLightID += m_MaxShadowsPerLight; // We give unique identifiers to each 
            return outputId;
        }

        internal void RegisterLight(HDAdditionalLightData lightData)
        {
            // If we are trying to register something that we have already placed, we do nothing
            if (lightData.lightIdxForCachedShadows >= 0 && m_PlacedShadows.ContainsKey(lightData.lightIdxForCachedShadows))
                return;

            // We register only if not already pending placement and if enabled. 
            if (!m_RegisteredLightDataPendingPlacement.ContainsKey(lightData.lightIdxForCachedShadows) && lightData.isActiveAndEnabled)
            {
#if UNITY_2020_2_OR_NEWER
                lightData.legacyLight.useViewFrustumForShadowCasterCull = false;
#endif
                lightData.lightIdxForCachedShadows = GetNextLightIdentifier();
                RegisterTransformCacheSlot(lightData);
                m_RegisteredLightDataPendingPlacement.Add(lightData.lightIdxForCachedShadows, lightData);
                m_CanTryPlacement = true;
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            m_RegisteredLightDataPendingPlacement.Remove(lightData.lightIdxForCachedShadows);
            RemoveTransformFromCache(lightData);

            int numberOfShadows = (lightData.type == HDLightType.Point) ? 6 : 1;

            int lightIdx = lightData.lightIdxForCachedShadows;
            lightData.lightIdxForCachedShadows = -1;

            for (int i = 0; i < numberOfShadows; ++i)
            {
                bool valueFound = false;
                int shadowIdx = lightIdx + i;

                m_RecordsPendingPlacement.Remove(shadowIdx);

                valueFound = m_PlacedShadows.TryGetValue(shadowIdx, out CachedShadowRecord recordToRemove);

                if (valueFound)
                {
#if UNITY_2020_2_OR_NEWER
                    lightData.legacyLight.useViewFrustumForShadowCasterCull = true;
#endif
                    m_PlacedShadows.Remove(shadowIdx);
                    m_ShadowsPendingRendering.Remove(shadowIdx);

                    MarkEntries((int)recordToRemove.offsetInAtlas.z, (int)recordToRemove.offsetInAtlas.w, HDUtils.DivRoundUp(recordToRemove.viewportSize, m_MinSlotSize), false);
                    m_CanTryPlacement = true;
                }
            }
        }

        internal void RegisterTransformCacheSlot(HDAdditionalLightData lightData)
        {
            if (lightData.lightIdxForCachedShadows >= 0 && lightData.updateUponLightMovement && !m_TransformCaches.ContainsKey(lightData.lightIdxForCachedShadows))
            {
                CachedTransform transform;
                transform.position = lightData.transform.position;
                transform.angles = lightData.transform.eulerAngles;
                m_TransformCaches.Add(lightData.lightIdxForCachedShadows, transform);
            }
        }

        internal void RemoveTransformFromCache(HDAdditionalLightData lightData)
        {
            m_TransformCaches.Remove(lightData.lightIdxForCachedShadows);
        }


        // ------------------------------------------------------------------------------------------


        // ------------------------------------------------------------------------------------------
        //                           Atlassing on the actual textures 
        // ------------------------------------------------------------------------------------------


        void InsertionSort(ref List<CachedShadowRecord> list, int startIndex, int lastIndex)
        {
            int i = startIndex;

            while (i < lastIndex)
            {
                var curr = list[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.viewportSize > list[j].viewportSize)))
                {
                    list[j + 1] = list[j];
                    j--;
                }

                list[j + 1] = curr;
                i++;
            }
        }

        private void AddLightListToRecordList(Dictionary<int, HDAdditionalLightData> lightList, HDShadowInitParameters initParams, ref List<CachedShadowRecord> recordList)
        {
            foreach (var currentLightData in lightList.Values)
            {
                int resolution = 0;

                resolution = currentLightData.GetResolutionFromSettings(m_ShadowType, initParams);

                HDLightType lightType = currentLightData.type;
                int numberOfShadows = (lightType == HDLightType.Point) ? 6 : 1;

                for (int i = 0; i < numberOfShadows; ++i)
                {
                    CachedShadowRecord record;
                    record.shadowIndex = currentLightData.lightIdxForCachedShadows + i;
                    record.viewportSize = resolution;
                    record.offsetInAtlas = new Vector4(-1, -1, -1, -1); // Will be set later.

                    recordList.Add(record);
                }
            }
        }

        private bool PlaceMultipleShadows(int startIdx, int numberOfShadows)
        {
            int firstShadowIdx = m_TempListForPlacement[startIdx].shadowIndex;

            Vector2Int[] placements = new Vector2Int[m_MaxShadowsPerLight];
            int successfullyPlaced = 0;

            for (int j = 0; j < numberOfShadows; ++j)
            {
                var record = m_TempListForPlacement[startIdx + j];

                Debug.Assert(firstShadowIdx + j == record.shadowIndex);

                int x, y;
                if (GetSlotInAtlas(record.viewportSize, out x, out y))
                {
                    successfullyPlaced++;
                    placements[j] = new Vector2Int(x, y);
                }
                else
                {
                    break;
                }
            }

            // If they all fit, we actually placed them, otherwise we mark the slot that we temp filled as free and go on.
            if(successfullyPlaced == numberOfShadows)   // Success.
            {
                for (int j = 0; j < numberOfShadows; ++j)
                {
                    var record = m_TempListForPlacement[startIdx + j];
                    
                    record.offsetInAtlas = new Vector4(placements[j].x * m_MinSlotSize, placements[j].y * m_MinSlotSize, placements[j].x, placements[j].y);

                    m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                    m_PlacedShadows.Add(record.shadowIndex, record);
                }

                return true;
            }
            else if(successfullyPlaced > 0)   // Couldn't place them all, but we placed something, so we revert those placements.
            {
                int numEntries = HDUtils.DivRoundUp(m_TempListForPlacement[startIdx].viewportSize, m_MinSlotSize);
                for (int j=0; j <successfullyPlaced; ++j)
                {
                    MarkEntries(placements[j].x, placements[j].y, numEntries, false);
                }
            }

            return false;
        }

        private void PerformPlacement()
        {
            for (int i = 0; i < m_TempListForPlacement.Count;)
            {
                int x, y;
                var record = m_TempListForPlacement[i];

                // Since each light gets its index += m_MaxShadowsPerLight, if we have a non %6 == 0, it means it is a shadow from a light with mulitple shadows
                bool isFirstOfASeries = (record.shadowIndex % m_MaxShadowsPerLight == 0) && ((i + 1) < m_TempListForPlacement.Count) && (m_TempListForPlacement[i + 1].shadowIndex % m_MaxShadowsPerLight != 0);

                // NOTE: We assume that if we have a series of shadows, we have six of them! If it is not the case anymore this code should be updated
                // (likely the record should contain how many shadows are associated).
                if (isFirstOfASeries)
                {
                    if (PlaceMultipleShadows(i, m_MaxShadowsPerLight))
                    {
                        m_RegisteredLightDataPendingPlacement.Remove(record.shadowIndex);   // We placed all the shadows of the light, hence we can remove the light from pending placement.
                        for (int subIdx = 0; subIdx < m_MaxShadowsPerLight; ++subIdx)
                        {
                            m_RecordsPendingPlacement.Remove(record.shadowIndex + subIdx);
                        }
                    }

                    i += m_MaxShadowsPerLight;  // We will not need to process depending shadows.
                }
                else // We have only one shadow to place.
                {
                    bool fit = GetSlotInAtlas(record.viewportSize, out x, out y);
                    if (fit)
                    {
                        // Convert offset to atlas offset.
                        record.offsetInAtlas = new Vector4(x * m_MinSlotSize, y * m_MinSlotSize, x, y);

                        m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                        m_PlacedShadows.Add(record.shadowIndex, record);
                        m_RegisteredLightDataPendingPlacement.Remove(record.shadowIndex);
                        m_RecordsPendingPlacement.Remove(record.shadowIndex);
                    }

                    i++;
                }
            }
        }

        // This is the external api to say: do the placement if needed.
        // Also, we assign the resolutions here since we didn't know about HDShadowInitParameters during OnEnable of the light.
        internal void AssignOffsetsInAtlas(HDShadowInitParameters initParameters)
        {
            if (m_RegisteredLightDataPendingPlacement.Count > 0 && m_CanTryPlacement)
            {
                m_TempListForPlacement.Clear();

                m_TempListForPlacement.AddRange(m_RecordsPendingPlacement.Values);
                AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParameters, ref m_TempListForPlacement);

                if (m_NeedOptimalPacking)
                {
                    InsertionSort(ref m_TempListForPlacement, 0, m_TempListForPlacement.Count);
                    m_NeedOptimalPacking = false;
                }

                PerformPlacement();

                m_CanTryPlacement = false; // It is pointless we try the placement every frame if no modifications to the amount of light registered happened.
            }
        }

        internal void DefragmentAtlasAndReRender(HDShadowInitParameters initParams)
        {
            m_TempListForPlacement.Clear();

            m_TempListForPlacement.AddRange(m_PlacedShadows.Values);
            m_TempListForPlacement.AddRange(m_RecordsPendingPlacement.Values);
            AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParams, ref m_TempListForPlacement);

            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots[i] = false;
            }

            // Clear the other state lists.
            m_PlacedShadows.Clear();
            m_ShadowsPendingRendering.Clear();
            m_RecordsPendingPlacement.Clear(); // We'll reset what records are pending.

            // Sort in order to obtain a more optimal packing. 
            InsertionSort(ref m_TempListForPlacement, 0, m_TempListForPlacement.Count);

            PerformPlacement();

            // This is fairly inefficient, but simple and this function should be called very rarely.
            // We need to add to pending the records that were placed but were not in m_RegisteredLightDataPendingPlacement
            // but they don't have a place yet.
            foreach (var record in m_TempListForPlacement)
            {
                if (!m_PlacedShadows.ContainsKey(record.shadowIndex)) // If we couldn't place it
                {
                    int parentLightIdx = record.shadowIndex - (record.shadowIndex % m_MaxShadowsPerLight);
                    if (!m_RegisteredLightDataPendingPlacement.ContainsKey(parentLightIdx)) // Did not come originally from m_RegisteredLightDataPendingPlacement
                    {
                        if (!m_RecordsPendingPlacement.ContainsKey(record.shadowIndex))
                            m_RecordsPendingPlacement.Add(record.shadowIndex, record);
                    }
                }
            }

            m_CanTryPlacement = false;
        }

        // ------------------------------------------------------------------------------------------


        // ------------------------------------------------------------------------------------------
        //                           Functions to query and change state of a shadow
        // ------------------------------------------------------------------------------------------
        internal bool LightIsPendingPlacement(HDAdditionalLightData lightData)
        {
            return (m_RegisteredLightDataPendingPlacement.ContainsKey(lightData.lightIdxForCachedShadows) ||
                    m_RecordsPendingPlacement.ContainsKey(lightData.lightIdxForCachedShadows));
        }

        internal bool ShadowIsPendingRendering(int shadowIdx)
        {
            return m_ShadowsPendingRendering.ContainsKey(shadowIdx);
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData lightData)
        {
            if (!lightData.isActiveAndEnabled) return;

            int lightIdx = lightData.lightIdxForCachedShadows;
            Debug.Assert(lightIdx >= 0);

            if (!m_PlacedShadows.ContainsKey(lightIdx))
            {
                if (m_RegisteredLightDataPendingPlacement.ContainsKey(lightIdx))
                    return;

                RegisterLight(lightData);
            }
            else
            {
                int numberOfShadows = (lightData.type == HDLightType.Point) ? 6 : 1;
                for (int i = 0; i < numberOfShadows; ++i)
                {
                    int shadowIdx = lightIdx + i;
                    ScheduleShadowUpdate(shadowIdx);
                }
            }
        }

        internal void ScheduleShadowUpdate(int shadowIdx)
        {
            // It needs to be placed already, otherwise needs to go through the registering cycle first.
            CachedShadowRecord shadowRecord;
            bool recordFound = m_PlacedShadows.TryGetValue(shadowIdx, out shadowRecord);
            Debug.Assert(recordFound);
            // Return to avoid error when assert is skipped.
            if (!recordFound) return;

            // It already schedule for update we do nothing;
            if (m_ShadowsPendingRendering.ContainsKey(shadowIdx))
                return;

            // Put the record up for rendering
            m_ShadowsPendingRendering.Add(shadowIdx, shadowRecord);
        }

        internal void MarkAsRendered(int shadowIdx)
        {
            if (m_ShadowsPendingRendering.ContainsKey(shadowIdx))
            {
                m_ShadowsPendingRendering.Remove(shadowIdx);
            }
        }

        // Used to update the resolution request processed by the light loop
        internal void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx)
        {
            CachedShadowRecord record;
            bool valueFound = m_PlacedShadows.TryGetValue(shadowIdx, out record);

            if (!valueFound)
            {
                Debug.LogWarning("Trying to render a cached shadow map that doesn't have a slot in the atlas yet.");
            }

            request.cachedAtlasViewport = new Rect(record.offsetInAtlas.x, record.offsetInAtlas.y, record.viewportSize, record.viewportSize);
            request.resolution = new Vector2(record.viewportSize, record.viewportSize);
        }

        internal bool NeedRenderingDueToTransformChange(HDAdditionalLightData lightData, HDLightType lightType)
        {
            bool needUpdate = false;

            if (m_TransformCaches.TryGetValue(lightData.lightIdxForCachedShadows, out CachedTransform cachedTransform))
            {
                float positionThreshold = lightData.cachedShadowTranslationUpdateThreshold;
                Vector3 positionDiffVec = cachedTransform.position - lightData.transform.position;
                float positionDiff = Vector3.Dot(positionDiffVec, positionDiffVec);
                if (positionDiff > positionThreshold * positionThreshold)
                {
                    needUpdate = true;
                }
                if(lightType != HDLightType.Point)
                {
                    float angleDiffThreshold = lightData.cachedShadowAngleUpdateThreshold;
                    Vector3 angleDiff = cachedTransform.angles - lightData.transform.eulerAngles;
                    // Any angle difference 
                    if (Mathf.Abs(angleDiff.x) > angleDiffThreshold || Mathf.Abs(angleDiff.y) > angleDiffThreshold || Mathf.Abs(angleDiff.z) > angleDiffThreshold)
                    {
                        needUpdate = true;
                    }
                }

                if (needUpdate)
                {
                    // Update the record (CachedTransform is a struct, so we remove old one and replace with a new one)
                    m_TransformCaches.Remove(lightData.lightIdxForCachedShadows);
                    cachedTransform.position = lightData.transform.position;
                    cachedTransform.angles = lightData.transform.eulerAngles;
                    m_TransformCaches.Add(lightData.lightIdxForCachedShadows, cachedTransform);
                }
            }

            return needUpdate;
        }

        // ------------------------------------------------------------------------------------------
    }
}


