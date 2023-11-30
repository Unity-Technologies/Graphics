using System;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDCachedShadowAtlas : HDShadowAtlas
    {
        static private int s_InitialCapacity = 256;

        // Constants.
        private const int m_MaxShadowsPerLight = 6;


        private int m_NextLightID = 0;
        private bool m_CanTryPlacement = false;

        internal struct CachedShadowRecord
        {
            internal int shadowIndex;
            internal int viewportSize;                               // We assume only square shadows maps.
            internal Vector4 offsetInAtlas;                          // When is registered xy is the offset in the texture atlas, in UVs, the zw is the entry offset in the C# representation.
            internal bool rendersOnPlacement;
        }

        // We need an extra struct to track differences in the light transform
        // since we don't have such a callback (a-la invalidate) for those.
        internal struct CachedTransform
        {
            internal Vector3 position;
            internal Vector3 angles; // Only for area and spot
        }

        enum SlotValue : byte
        {
            Free,
            Occupied,
            TempOccupied        //  Used when checking if it will fit.
        }

        private int m_AtlasResolutionInSlots;       // Atlas Resolution / k_MinSlotSize

        private bool m_NeedOptimalPacking = true;   // Whenever this is set to true, the pending lights are sorted before insertion.

        private List<SlotValue> m_AtlasSlots;            // One entry per slot (of size k_MinSlotSize) true if occupied, false if free.

        // Note: Some of these could be simple lists, but since we might need to search by index some of them and we want to avoid GC alloc, a dictionary is easier.
        // This also mean slightly worse performance, however hopefully the number of cached shadow lights is not huge at any tie.
        private NativeParallelHashMap<int, CachedShadowRecord> m_PlacedShadows;
        private NativeParallelHashMap<int, CachedShadowRecord> m_ShadowsPendingRendering;
        private NativeParallelHashMap<int, int> m_ShadowsWithValidData;                            // Shadows that have been placed and rendered at least once (OnDemand shadows are not rendered unless requested explicitly). It is a dictionary for fast access by shadow index.
        private NativeParallelHashMap<int, HDLightRenderEntity> m_RegisteredLightDataPendingPlacement;
        private NativeParallelHashMap<int, CachedShadowRecord> m_RecordsPendingPlacement;          // Note: this is different from m_RegisteredLightDataPendingPlacement because it contains records that were allocated in the system
                                                                                        // but they lost their spot (e.g. post defrag). They don't have a light associated anymore if not by index, so we keep a separate collection.

        private NativeParallelHashMap<int, CachedTransform> m_TransformCaches;
        private NativeList<CachedShadowRecord> m_TempListForPlacement;
        private NativeList<int> m_TempListForLightDataIndices;


        private ShadowMapType m_ShadowType;

        // ------------------------------------------------------------------------------------------
        //                                      Init Functions
        // ------------------------------------------------------------------------------------------
        public HDCachedShadowAtlas(ShadowMapType type)
        {
            m_ShadowType = type;

            m_PlacedShadows = new NativeParallelHashMap<int, CachedShadowRecord>(s_InitialCapacity, Allocator.Persistent);
            m_ShadowsPendingRendering = new NativeParallelHashMap<int, CachedShadowRecord>(s_InitialCapacity, Allocator.Persistent);
            m_ShadowsWithValidData = new NativeParallelHashMap<int, int>(s_InitialCapacity, Allocator.Persistent);

            m_TempListForPlacement = new NativeList<CachedShadowRecord>(s_InitialCapacity, Allocator.Persistent);
            m_TempListForLightDataIndices = new NativeList<int>(s_InitialCapacity, Allocator.Persistent);

            m_RegisteredLightDataPendingPlacement = new NativeParallelHashMap<int, HDLightRenderEntity>(s_InitialCapacity, Allocator.Persistent);
            m_RecordsPendingPlacement = new NativeParallelHashMap<int, CachedShadowRecord>(s_InitialCapacity, Allocator.Persistent);

            m_TransformCaches = new NativeParallelHashMap<int, CachedTransform>(s_InitialCapacity / 2, Allocator.Persistent);
        }

        public override void InitAtlas(HDShadowAtlasInitParameters atlasInitParams)
        {
            base.InitAtlas(atlasInitParams);

            m_IsACacheForShadows = true;

            m_AtlasResolutionInSlots = HDUtils.DivRoundUp(width, HDCachedShadowManager.k_MinSlotSize);
            m_AtlasSlots = new List<SlotValue>(m_AtlasResolutionInSlots * m_AtlasResolutionInSlots);
            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots.Add(SlotValue.Free);
            }

            // Note: If changing the characteristics of the atlas via HDRP asset, the lights OnEnable will not be called again so we are missing them, however we can explicitly
            // put them back up for placement. If this is the first Init of the atlas, the lines below do nothing.
            DefragmentAtlasAndReRender(atlasInitParams.initParams);
            m_CanTryPlacement = true;
            m_NeedOptimalPacking = true;
        }

        // ------------------------------------------------------------------------------------------
        //          Functions to access and deal with the C# representation of the atlas
        // ------------------------------------------------------------------------------------------
        private bool IsEntryEmpty(int x, int y)
        {
            return (m_AtlasSlots[y * m_AtlasResolutionInSlots + x] == SlotValue.Free);
        }

        private bool IsEntryFull(int x, int y)
        {
            return (m_AtlasSlots[y * m_AtlasResolutionInSlots + x]) != SlotValue.Free;
        }

        private bool IsEntryTempOccupied(int x, int y)
        {
            return (m_AtlasSlots[y * m_AtlasResolutionInSlots + x]) == SlotValue.TempOccupied;
        }

        // Always fill slots in a square shape, for example : if x = 1 and y = 2, if numEntries = 2 it will fill {(1,2),(2,2),(1,3),(2,3)}
        private void FillEntries(int x, int y, int numEntries)
        {
            MarkEntries(x, y, numEntries, SlotValue.Occupied);
        }

        private void MarkEntries(int x, int y, int numEntries, SlotValue value)
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

        internal bool FindSlotInAtlas(int resolution, bool tempFill, out int x, out int y)
        {
            int numEntries = HDUtils.DivRoundUp(resolution, HDCachedShadowManager.k_MinSlotSize);
            for (int j = 0; j < m_AtlasResolutionInSlots; ++j)
            {
                for (int i = 0; i < m_AtlasResolutionInSlots; ++i)
                {
                    if (CheckSlotAvailability(i, j, numEntries))
                    {
                        x = i;
                        y = j;

                        if (tempFill)
                            MarkEntries(x, y, numEntries, SlotValue.TempOccupied);

                        return true;
                    }
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        internal void FreeTempFilled(int x, int y, int resolution)
        {
            int numEntries = HDUtils.DivRoundUp(resolution, HDCachedShadowManager.k_MinSlotSize);
            for (int j = y; j < y + numEntries; ++j)
            {
                for (int i = x; i < x + numEntries; ++i)
                {
                    if (m_AtlasSlots[j * m_AtlasResolutionInSlots + i] == SlotValue.TempOccupied)
                    {
                        m_AtlasSlots[j * m_AtlasResolutionInSlots + i] = SlotValue.Free;
                    }
                }
            }
        }

        internal bool FindSlotInAtlas(int resolution, out int x, out int y)
        {
            return FindSlotInAtlas(resolution, false, out x, out y);
        }

        internal bool GetSlotInAtlas(int resolution, out int x, out int y)
        {
            if (FindSlotInAtlas(resolution, out x, out y))
            {
                int numEntries = HDUtils.DivRoundUp(resolution, HDCachedShadowManager.k_MinSlotSize);
                FillEntries(x, y, numEntries);
                return true;
            }

            return false;
        }

        internal void GetUnmanageDataForShadowRequestJobs(ref HDCachedShadowAtlasDataForShadowRequestUpdateJob dataForShadowRequestUpdateJob)
        {
            dataForShadowRequestUpdateJob.shadowRequests = m_ShadowRequests;
            dataForShadowRequestUpdateJob.shadowsPendingRendering = m_ShadowsPendingRendering;
            dataForShadowRequestUpdateJob.shadowsWithValidData = m_ShadowsWithValidData;
            dataForShadowRequestUpdateJob.registeredLightDataPendingPlacement = m_RegisteredLightDataPendingPlacement;
            dataForShadowRequestUpdateJob.recordsPendingPlacement = m_RecordsPendingPlacement;
            dataForShadowRequestUpdateJob.transformCaches = m_TransformCaches;
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
            if (!lightData.lightEntity.valid)
                return;

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
                m_RegisteredLightDataPendingPlacement.Add(lightData.lightIdxForCachedShadows, lightData.lightEntity);
                m_CanTryPlacement = true;
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData, LightType cachedLightType)
        {
            if (!m_RegisteredLightDataPendingPlacement.IsCreated)
                return;

            m_RegisteredLightDataPendingPlacement.Remove(lightData.lightIdxForCachedShadows);

            RemoveTransformFromCache(lightData);

            int numberOfShadows = (cachedLightType == LightType.Point) ? 6 : 1;

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
                    m_ShadowsWithValidData.Remove(shadowIdx);

                    MarkEntries((int)recordToRemove.offsetInAtlas.z, (int)recordToRemove.offsetInAtlas.w, HDUtils.DivRoundUp(recordToRemove.viewportSize, HDCachedShadowManager.k_MinSlotSize), SlotValue.Free);
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


        unsafe void InsertionSort(NativeList<CachedShadowRecord> list, int startIndex, int lastIndex)
        {
            int i = startIndex;

            ref UnsafeList<CachedShadowRecord> unsafeList = ref *list.GetUnsafeList();

            while (i < lastIndex)
            {
                var curr = unsafeList[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.viewportSize > unsafeList[j].viewportSize)))
                {
                    unsafeList[j + 1] = unsafeList[j];
                    j--;
                }

                unsafeList[j + 1] = curr;
                i++;
            }
        }

        private unsafe void AddLightListToRecordList(NativeParallelHashMap<int, HDLightRenderEntity> lightList, HDShadowInitParameters initParams, ref NativeList<CachedShadowRecord> recordList)
        {
            if (HDLightRenderDatabase.instance.lightCount == 0)
                return;

            DynamicArray<HDAdditionalLightData> additionalLightDatas = HDLightRenderDatabase.instance.hdAdditionalLightData;
            NativeList<int> tempIndexList = m_TempListForLightDataIndices;
            HDLightRenderDatabase.instance.GetDataIndicesFromEntities(lightList, tempIndexList);
            int entityIndicesLength = tempIndexList.Length;
            ref UnsafeList<CachedShadowRecord> recordListUnsafe = ref *recordList.GetUnsafeList();
            ref UnsafeList<int> tempIndexUnsafeList = ref *tempIndexList.GetUnsafeList();

            for (int i = 0; i < entityIndicesLength; i++)
            {
                int dataIndex = tempIndexUnsafeList[i];
                var currentLightData = additionalLightDatas[dataIndex];
                int resolution = 0;

                resolution = currentLightData.GetResolutionFromSettings(m_ShadowType, initParams, cachedResolution: true);

                LightType lightType = currentLightData.legacyLight.type;
                int numberOfShadows = (lightType == LightType.Point) ? 6 : 1;

                for (int j = 0; j < numberOfShadows; ++j)
                {
                    CachedShadowRecord record;
                    record.shadowIndex = currentLightData.lightIdxForCachedShadows + j;
                    record.viewportSize = resolution;
                    record.offsetInAtlas = new Vector4(-1, -1, -1, -1); // Will be set later.
                    // Only situation in which we allow not to render on placement if it is OnDemand and onDemandShadowRenderOnPlacement is false
                    record.rendersOnPlacement = (currentLightData.shadowUpdateMode == ShadowUpdateMode.OnDemand) ? (currentLightData.forceRenderOnPlacement || currentLightData.onDemandShadowRenderOnPlacement) : true;
                    currentLightData.forceRenderOnPlacement = false; // reset the force flag as we scheduled the rendering forcefully already.
                    recordListUnsafe.Add(record);
                }
            }

            tempIndexList.ResizeUninitialized(0);
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
            if (successfullyPlaced == numberOfShadows)   // Success.
            {
                for (int j = 0; j < numberOfShadows; ++j)
                {
                    var record = m_TempListForPlacement[startIdx + j];

                    record.offsetInAtlas = new Vector4(placements[j].x * HDCachedShadowManager.k_MinSlotSize, placements[j].y * HDCachedShadowManager.k_MinSlotSize, placements[j].x, placements[j].y);

                    if (record.rendersOnPlacement)
                    {
                        m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                    }
                    m_PlacedShadows.Add(record.shadowIndex, record);
                }

                return true;
            }
            else if (successfullyPlaced > 0)   // Couldn't place them all, but we placed something, so we revert those placements.
            {
                int numEntries = HDUtils.DivRoundUp(m_TempListForPlacement[startIdx].viewportSize, HDCachedShadowManager.k_MinSlotSize);
                for (int j = 0; j < successfullyPlaced; ++j)
                {
                    MarkEntries(placements[j].x, placements[j].y, numEntries, SlotValue.Free);
                }
            }

            return false;
        }

        private void PerformPlacement()
        {
            for (int i = 0; i < m_TempListForPlacement.Length;)
            {
                int x, y;
                var record = m_TempListForPlacement[i];

                // Since each light gets its index += m_MaxShadowsPerLight, if we have a non %6 == 0, it means it is a shadow from a light with mulitple shadows
                bool isFirstOfASeries = (record.shadowIndex % m_MaxShadowsPerLight == 0) && ((i + 1) < m_TempListForPlacement.Length) && (m_TempListForPlacement[i + 1].shadowIndex % m_MaxShadowsPerLight != 0);

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
                        record.offsetInAtlas = new Vector4(x * HDCachedShadowManager.k_MinSlotSize, y * HDCachedShadowManager.k_MinSlotSize, x, y);

                        if (record.rendersOnPlacement)
                        {
                            m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                        }
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
            if (!m_RegisteredLightDataPendingPlacement.IsEmpty && m_CanTryPlacement)
            {
                m_TempListForPlacement.Clear();

                var recordsPendingPlacementValues = m_RecordsPendingPlacement.GetValueArray(Allocator.Temp);
                m_TempListForPlacement.AddRange(recordsPendingPlacementValues);
                recordsPendingPlacementValues.Dispose();
                AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParameters, ref m_TempListForPlacement);

                if (m_NeedOptimalPacking)
                {
                    InsertionSort(m_TempListForPlacement, 0, m_TempListForPlacement.Length);
                    m_NeedOptimalPacking = false;
                }

                PerformPlacement();

                m_CanTryPlacement = false; // It is pointless we try the placement every frame if no modifications to the amount of light registered happened.
            }
        }

        internal void DefragmentAtlasAndReRender(HDShadowInitParameters initParams)
        {
            if (!m_PlacedShadows.IsCreated)
                return;

            m_TempListForPlacement.Clear();

            var placedShadowsValues = m_PlacedShadows.GetValueArray(Allocator.Temp);
            m_TempListForPlacement.AddRange(placedShadowsValues);
            var recordsPendingPlacementValues = m_RecordsPendingPlacement.GetValueArray(Allocator.Temp);
            m_TempListForPlacement.AddRange(recordsPendingPlacementValues);
            recordsPendingPlacementValues.Dispose();
            placedShadowsValues.Dispose();
            AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParams, ref m_TempListForPlacement);

            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots[i] = SlotValue.Free;
            }

            // Clear the other state lists.
            m_PlacedShadows.Clear();
            m_ShadowsPendingRendering.Clear();
            m_ShadowsWithValidData.Clear();
            m_RecordsPendingPlacement.Clear(); // We'll reset what records are pending.

            // Sort in order to obtain a more optimal packing.
            InsertionSort(m_TempListForPlacement, 0, m_TempListForPlacement.Length);

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
        internal bool LightIsPendingPlacement(int lightIdxForCachedShadows)
        {
            return m_RegisteredLightDataPendingPlacement.ContainsKey(lightIdxForCachedShadows) || m_RecordsPendingPlacement.ContainsKey(lightIdxForCachedShadows);
        }

        internal bool ShadowHasRenderedAtLeastOnce(int shadowIdx)
        {
            if (!m_ShadowsWithValidData.IsCreated)
                return false;

            return m_ShadowsWithValidData.ContainsKey(shadowIdx);
        }

        internal bool FullLightShadowHasRenderedAtLeastOnce(HDAdditionalLightData lightData)
        {
            if (!m_ShadowsWithValidData.IsCreated)
                return false;

            int cachedShadowIdx = lightData.lightIdxForCachedShadows;
            if (lightData.legacyLight.type == LightType.Point)
            {
                bool allRendered = true;
                for (int i = 0; i < 6; ++i)
                {
                    allRendered = allRendered && m_ShadowsWithValidData.ContainsKey(cachedShadowIdx + i);
                }

                return allRendered;
            }
            return m_ShadowsWithValidData.ContainsKey(cachedShadowIdx);
        }

        internal bool LightIsPlaced(HDAdditionalLightData lightData)
        {
            if (!m_ShadowsWithValidData.IsCreated)
                return false;

            int cachedShadowIdx = lightData.lightIdxForCachedShadows;
            return cachedShadowIdx >= 0 && m_PlacedShadows.ContainsKey(cachedShadowIdx);
        }

        internal void ScheduleShadowUpdate(HDAdditionalLightData lightData)
        {
            if (!lightData.isActiveAndEnabled) return;

            int lightIdx = lightData.lightIdxForCachedShadows;

            if (!m_PlacedShadows.ContainsKey(lightIdx))
            {
                if (m_RegisteredLightDataPendingPlacement.ContainsKey(lightIdx))
                    return;

                lightData.forceRenderOnPlacement = true;
                RegisterLight(lightData);
            }
            else
            {
                int numberOfShadows = (lightData.legacyLight.type == LightType.Point) ? 6 : 1;
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

        internal void OverrideShadowResolutionRequestWithCachedData(ref HDShadowResolutionRequest request, int shadowIdx)
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

        internal override void DisposeNativeCollections()
        {
            base.DisposeNativeCollections();

            if (m_PlacedShadows.IsCreated)
            {
                m_PlacedShadows.Dispose();
                m_PlacedShadows = default;
                m_ShadowsPendingRendering.Dispose();
                m_ShadowsPendingRendering = default;
                m_ShadowsWithValidData.Dispose();
                m_ShadowsWithValidData = default;
                m_TempListForPlacement.Dispose();
                m_TempListForPlacement = default;
                m_TempListForLightDataIndices.Dispose();
                m_TempListForLightDataIndices = default;
                m_RegisteredLightDataPendingPlacement.Dispose();
                m_RegisteredLightDataPendingPlacement = default;
                m_RecordsPendingPlacement.Dispose();
                m_RecordsPendingPlacement = default;
                m_TransformCaches.Dispose();
                m_TransformCaches = default;
            }
        }

        // ------------------------------------------------------------------------------------------
    }
}
