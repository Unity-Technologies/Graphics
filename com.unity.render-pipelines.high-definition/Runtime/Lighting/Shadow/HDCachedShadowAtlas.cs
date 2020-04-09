using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO_FCC: Big todos list:
    // - Render graph :( 

    // TODO: IMPORTANT!! EXCLUDE CASCADE SHADOW MAPS FROM MOST OF THIS? OR NOT? 

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

        private int m_MaxAtlasResolution;
        private int m_AtlasResolutionInSlots;       // Atlas Resolution / m_MinSlotSize

        private bool m_NeedOptimalPacking = true;

        private List<bool> m_AtlasSlots;

        // Note: Some of these could be simple lists, but since we might need to search by index some of them and we want to avoid GC alloc, a dictionary is easier.
        // This also mean slightly worse performance, however hopefully the number of cached shadow lights is not huge at any tie.
        private Dictionary<int, CachedShadowRecord> m_PlacedShadows;
        private Dictionary<int, CachedShadowRecord> m_ShadowsPendingRendering;
        private Dictionary<int, HDAdditionalLightData> m_RegisteredLightDataPendingPlacement;
        private List<(string, int)> DBG_NAMES_LIGHT;
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

            DBG_NAMES_LIGHT = new List<(string, int)>();

            m_ShadowType = type;
        }

        public override void InitAtlas(RenderPipelineResources renderPipelineResources, int width, int height, int atlasShaderID, int atlasSizeShaderID, Material clearMaterial, int maxShadowRequests, BlurAlgorithm blurAlgorithm = BlurAlgorithm.None, FilterMode filterMode = FilterMode.Bilinear, DepthBits depthBufferBits = DepthBits.Depth16, RenderTextureFormat format = RenderTextureFormat.Shadowmap, string name = "", int momentAtlasShaderID = 0)
        {
            base.InitAtlas(renderPipelineResources, width, height, atlasShaderID, atlasSizeShaderID, clearMaterial, maxShadowRequests, blurAlgorithm, filterMode, depthBufferBits, format, name, momentAtlasShaderID);

            m_MaxAtlasResolution = width;
            m_AtlasResolutionInSlots = HDUtils.DivRoundUp(m_MaxAtlasResolution, m_MinSlotSize);
            m_AtlasSlots = new List<bool>(m_AtlasResolutionInSlots * m_AtlasResolutionInSlots);
            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots.Add(false);
            }

        }
        // ------------------------------------------------------------------------------------------



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
                lightData.legacyLight.useInfiniteFrustumForShadowCasterCull = true;
                lightData.lightIdxForCachedShadows = GetNextLightIdentifier();

                DBG_NAMES_LIGHT.Add((lightData.name, lightData.lightIdxForCachedShadows));

                m_RegisteredLightDataPendingPlacement.Add(lightData.lightIdxForCachedShadows, lightData);
                m_CanTryPlacement = true;
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            DBG_NAMES_LIGHT.RemoveAll(x => x.Item1 == lightData.name);

            // todo is it here the right place?
            m_RegisteredLightDataPendingPlacement.Remove(lightData.lightIdxForCachedShadows);

            int numberOfShadows = (lightData.type == HDLightType.Point) ? 6 : 1;

            int lightIdx = lightData.lightIdxForCachedShadows;

            for (int i = 0; i < numberOfShadows; ++i)
            {
                CachedShadowRecord recordToRemove;
                bool valueFound = false;
                int shadowIdx = lightIdx + i;

                valueFound = m_PlacedShadows.TryGetValue(shadowIdx, out recordToRemove);

                if (valueFound)
                {
                    lightData.legacyLight.useInfiniteFrustumForShadowCasterCull = false;
                    m_PlacedShadows.Remove(shadowIdx);
                    m_ShadowsPendingRendering.Remove(shadowIdx);

                    MarkEntries((int)recordToRemove.offsetInAtlas.z, (int)recordToRemove.offsetInAtlas.w, HDUtils.DivRoundUp(recordToRemove.viewportSize, m_MinSlotSize), false);
                    m_CanTryPlacement = true;
                }
            }
        }

        // ------------------------------------------------------------------------------------------



        // ------------------------------------------------------------------------------------------
        //                           Atlassing on the actual textures 
        // ------------------------------------------------------------------------------------------

        internal bool LightIsPendingPlacement(HDAdditionalLightData lightData)
        {
            return m_RegisteredLightDataPendingPlacement.ContainsKey(lightData.lightIdxForCachedShadows);
        }

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
                        m_RegisteredLightDataPendingPlacement.Remove(record.shadowIndex);   // We placed all the shadows of the light, hence we can remove the light from pending placement.

                    i += m_MaxShadowsPerLight;  // We will not need to process depending shadows.
                }
                else if(!isFirstOfASeries) // We have only one shadow to place.
                {
                    bool fit = GetSlotInAtlas(record.viewportSize, out x, out y);
                    if (fit)
                    {
                        // Convert offset to atlas offset.
                        record.offsetInAtlas = new Vector4(x * m_MinSlotSize, y * m_MinSlotSize, x, y);

                        m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                        m_PlacedShadows.Add(record.shadowIndex, record);
                        m_RegisteredLightDataPendingPlacement.Remove(record.shadowIndex);
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

                AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParameters, ref m_TempListForPlacement);

                // TODO: If we went for resizable atlas, here we should resize already, before even trying to fit in.
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
            m_TempListForPlacement.AddRange(m_ShadowsPendingRendering.Values); // This should really be empty at this point, but in case is not invalidate that.
            AddLightListToRecordList(m_RegisteredLightDataPendingPlacement, initParams, ref m_TempListForPlacement);

            // Clear the other state lists.
            m_PlacedShadows.Clear();
            m_ShadowsPendingRendering.Clear();

            // Sort in order to obtain a more optimal packing. 
            InsertionSort(ref m_TempListForPlacement, 0, m_TempListForPlacement.Count);

            PerformPlacement();

            m_CanTryPlacement = false;
        }

        internal void UpdateResolutionRequest(ref HDShadowResolutionRequest request, int shadowIdx)
        {
            CachedShadowRecord record;
            bool valueFound = m_PlacedShadows.TryGetValue(shadowIdx, out record);

            if(!valueFound)
            {
                Debug.LogWarning("Trying to render a cached shadow map that doesn't have a slot in the atlas yet.");
            }

            request.atlasViewport = new Rect(record.offsetInAtlas.x, record.offsetInAtlas.y, record.viewportSize, record.viewportSize);
            request.resolution = new Vector2(record.viewportSize, record.viewportSize);
        }

        // ------------------------------------------------------------------------------------------


        // ------------------------------------------------------------------------------------------
        //                           Update/Rendering related functions 
        // ------------------------------------------------------------------------------------------

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
            Debug.Assert(m_PlacedShadows.TryGetValue(shadowIdx, out shadowRecord));

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

        // ------------------------------------------------------------------------------------------
    }
}


