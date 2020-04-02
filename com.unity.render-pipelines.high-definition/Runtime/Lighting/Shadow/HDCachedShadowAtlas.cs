using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    // TODO_FCC: List of optimizations.

        // TODO: IMPORTANT!! EXCLUDE CASCADE SHADOW MAPS FROM MOST OF THIS? OR NOT? 

    class HDCachedShadowAtlas : HDShadowAtlas
    {
        static private int s_InitialCapacity = 512; // TODO_FCC: Determine this better.

        // DBG
        private bool hasPrinted = false;

        // Constants.
        private const int m_MinSlotSize = 64;
        private const int m_MaxShadowsPerLight = 6;

        private int m_NextLightID = 0;

        struct CachedShadowRecord
        {
            internal int shadowIndex;
            internal int viewportSize;                               // We assume only square shadows maps.
            internal Vector4 offsetInAtlas;                          // When is registered xy is the offset in the texture atlas, in UVs, the zw is the entry offset in the C# representation.

            internal Vector4 GetShadowViewport() // In atlas/texture space
            {
                return new Vector4(offsetInAtlas.x, offsetInAtlas.y, viewportSize, viewportSize);
            }
        }

        private int m_MaxAtlasResolution;
        private int m_AtlasResolutionInSlots;       // Atlas Resolution / m_MinSlotSize

        private bool m_NeedOptimalPacking = true;

        private ShadowMapType shadowMapType = ShadowMapType.PunctualAtlas;

        List<bool> m_AtlasSlots;

        // TODO: My guess is that these two can be simple vectors, not sure we need the dictionary at all...
        private Dictionary<int, CachedShadowRecord> m_PlacedShadows;

        private Dictionary<int, CachedShadowRecord> m_ShadowsPendingRendering;


        private List<HDAdditionalLightData> m_RegisteredLightDataPendingPlacement;


        private List<string> DBG_NAMES_LIGHT;


        private List<CachedShadowRecord> m_TempListForPlacement;

        // Have a pending rendering list?
        // A shadow will check here if it is pending a rendering call.

        // ------------------------------------------------------------------------------------------
        //                                      Init Functions
        // ------------------------------------------------------------------------------------------
        public HDCachedShadowAtlas()
        {
            // Assuming it'll be filled with 128 shadow maps
            m_PlacedShadows = new Dictionary<int, CachedShadowRecord>(s_InitialCapacity);
            m_ShadowsPendingRendering = new Dictionary<int, CachedShadowRecord>(s_InitialCapacity);
            // We pre allocate the biggest size possible to make sure we don't allocate often.
            m_TempListForPlacement = new List<CachedShadowRecord>(s_InitialCapacity);

            m_RegisteredLightDataPendingPlacement = new List<HDAdditionalLightData>(s_InitialCapacity);

            DBG_NAMES_LIGHT = new List<string>();
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

        internal bool GetSlotInAtlas(int resolution, out int x, out int y)
        {
            int numEntries = HDUtils.DivRoundUp(resolution, m_MinSlotSize);

            for (int j=0; j < m_AtlasResolutionInSlots; ++j)
            {
                for(int i = 0; i < m_AtlasResolutionInSlots; ++i)
                {
                    if(CheckSlotAvailability(i, j, numEntries))
                    {
                        FillEntries(i, j, numEntries);
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
        // ---------------------------------------------------------------------------------------       


        // ----- TESTING FUNCTIONS DELETE -----
        public void DebugAddSlots()
        {
            if (hasPrinted) return;
             
            int x, y;
            GetSlotInAtlas(512, out x, out y);
            DebugPrintAtlas();
            hasPrinted = true;

            for (int i = 0; i < m_AtlasResolutionInSlots * m_AtlasResolutionInSlots; ++i)
            {
                m_AtlasSlots[i] = false;
            }
        }
        internal void DebugPrintAtlas()
        { 
            for(int y = 0; y < m_AtlasResolutionInSlots; ++y)
            {
                string row = "ROW " + y +"\t";
                for (int x = 0; x < m_AtlasResolutionInSlots; ++x)
                {
                    row += IsEntryEmpty(x, y) ? "O" : "X";
                    row += "\t";
                } 
                Debug.Log(row);
            }
        }
        // -------------------------------------



        // TODO: REALLY IMPORTANT, HOW DO WE ASSIGN IDS, PER LIGHT OR PER SHADOW?

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
            if(!m_RegisteredLightDataPendingPlacement.Contains(lightData) && lightData.enabled)
            {
                lightData.lightIdxForCachedShadows = GetNextLightIdentifier();

               // Debug.Log("Registering " + lightData.lightIdxForCachedShadows);
                DBG_NAMES_LIGHT.Add(lightData.name);

                m_RegisteredLightDataPendingPlacement.Add(lightData);
            }
        }

        internal void EvictLight(HDAdditionalLightData lightData)
        {
            Debug.Assert(shadowMapType != ShadowMapType.CascadedDirectional);

            CachedShadowRecord recordToRemove;
            bool valueFound = m_PlacedShadows.TryGetValue(lightData.lightIdxForCachedShadows, out recordToRemove);

            DBG_NAMES_LIGHT.RemoveAll(x => x == lightData.name);

            // todo is it here the right place?
            m_RegisteredLightDataPendingPlacement.Remove(lightData);

            if (valueFound)
            {
                int numberOfShadows = (shadowMapType == ShadowMapType.PunctualAtlas) ? 6 : 1;

                int lightIdx = lightData.lightIdxForCachedShadows;

                for (int i = 0; i < numberOfShadows; ++i)
                {
                    int shadowIdx = lightIdx + i;

                    valueFound = m_PlacedShadows.TryGetValue(shadowIdx, out recordToRemove);
 
                    if (valueFound)
                    {
                        m_PlacedShadows.Remove(shadowIdx);
                        m_ShadowsPendingRendering.Remove(shadowIdx);

                        MarkEntries((int)recordToRemove.offsetInAtlas.z, (int)recordToRemove.offsetInAtlas.w, HDUtils.DivRoundUp(recordToRemove.viewportSize, m_MinSlotSize), false);
                    }

                }
            }
        }

        // ------------------------------------------------------------------------------------------



        // ------------------------------------------------------------------------------------------
        //                           Atlassing on the actual textures 
        // ------------------------------------------------------------------------------------------

        // TODO: The idea is to either size up or ignore.
        private void DealWithFullAtlas()
        {
        }

        void InsertionSort(CachedShadowRecord[] array, int startIndex, int lastIndex)
        {
            int i = startIndex;

            while (i < lastIndex)
            {
                var curr = array[i];

                int j = i - 1;

                // Sort in descending order.
                while ((j >= 0) && ((curr.viewportSize > array[j].viewportSize)))
                {
                    array[j + 1] = array[j];
                    j--;
                }

                array[j + 1] = curr;
                i++;
            }
        }

        // This is the external api to say: do the placement if needed.
        // Also, we assign the resolutions here since we didn't know about HDShadowInitParameters during OnEnable of the light.
        internal void AssignOffsetsInAtlas(HDShadowInitParameters initParameters)
        {

            m_TempListForPlacement.Clear();

            foreach (var currentLightData in m_RegisteredLightDataPendingPlacement)
            {
               // var resolution = currentLightData.shadowre
                int resolution;
               
                switch (shadowMapType)
                {
                    case ShadowMapType.CascadedDirectional:
                        resolution = Math.Min(currentLightData.shadowResolution.Value(initParameters.shadowResolutionDirectional), initParameters.maxDirectionalShadowMapResolution);
                        break;
                    case ShadowMapType.PunctualAtlas:
                        resolution = Math.Min(currentLightData.shadowResolution.Value(initParameters.shadowResolutionPunctual), initParameters.maxPunctualShadowMapResolution);
                        break;
                    case ShadowMapType.AreaLightAtlas:
                        resolution = Math.Min(currentLightData.shadowResolution.Value(initParameters.shadowResolutionArea), initParameters.maxAreaShadowMapResolution);
                        break;
                    default:
                        resolution = 0;
                        break;
                }

                // TODO_FCC Handle this better of course.
                Debug.Assert(shadowMapType != ShadowMapType.CascadedDirectional);

                int numberOfShadows = (shadowMapType == ShadowMapType.PunctualAtlas) ? 6 : 1;

                for (int i = 0; i<numberOfShadows; ++i)
                {
                    CachedShadowRecord record;
                    record.shadowIndex = currentLightData.lightIdxForCachedShadows + i;
                    record.viewportSize = resolution;
                    record.offsetInAtlas = new Vector4(-1, -1, -1, -1); // Will be set later.

                    m_TempListForPlacement.Add(record);
                }
            }

            // TODO: We don't need it anymore? 
            m_RegisteredLightDataPendingPlacement.Clear();


            // TODO: If we went for resizable atlas, here we should resize already, before even trying to fit in.
            if (m_NeedOptimalPacking)
            {
                InsertionSort(m_TempListForPlacement.ToArray(), 0, m_TempListForPlacement.Count);
                m_NeedOptimalPacking = false;
            }

            for (int i = 0; i < m_TempListForPlacement.Count; ++i)
            {
                int x, y;
                var record = m_TempListForPlacement[i];

                bool fit = GetSlotInAtlas(record.viewportSize, out x, out y);
                if (fit)
                {
                    // Convert offset to atlas offset.
                    record.offsetInAtlas = new Vector4(x * m_MinSlotSize / m_AtlasResolutionInSlots, y * m_MinSlotSize / m_AtlasResolutionInSlots, x, y);

                    m_ShadowsPendingRendering.Add(record.shadowIndex, record);
                    m_PlacedShadows.Add(record.shadowIndex, record);
                }
            }
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
            int lightIdx = lightData.lightIdxForCachedShadows;
            Debug.Assert(lightIdx >= 0);

            if (!m_PlacedShadows.ContainsKey(lightIdx))
            {
                if (m_RegisteredLightDataPendingPlacement.Find(x => x.lightIdxForCachedShadows == lightData.lightIdxForCachedShadows))
                    return;

                RegisterLight(lightData);
            }
            else
            {
                // Number of shadows TODO_FCC: AGAIN, HANDLE DIRECTIONAL!
                int numberOfShadows = (shadowMapType == ShadowMapType.PunctualAtlas) ? 6 : 1;
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

        // ------------------------------------------------------------------------------------------
    }
}


