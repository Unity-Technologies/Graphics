using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : MonoBehaviour
    {
        #region Large Current
        /// <summary>
        ///
        /// </summary>
        public float largeCurrentSpeedValue = 0.0f;

        /// <summary>
        /// </summary>
        public Texture largeCurrentMap = null;

        /// <summary>
        /// </summary>
        public Vector2 largeCurrentRegionExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// </summary>
        public Vector2 largeCurrentRegionOffset = new Vector2(0.0f, 0.0f);

        /// <summary>
        ///
        /// </summary>
        public float largeCurrentMapInfluence = 1.0f;
        #endregion

        #region Ripples Current
        /// <summary>
        ///
        /// </summary>
        public float ripplesCurrentSpeedValue = 0.0f;

        /// <summary>
        /// </summary>
        public Texture ripplesCurrentMap = null;

        /// <summary>
        /// </summary>
        public Vector2 ripplesCurrentRegionExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// </summary>
        public Vector2 ripplesCurrentRegionOffset = new Vector2(0.0f, 0.0f);

        /// <summary>
        ///
        /// </summary>
        public float ripplesCurrentMapInfluence = 1.0f;
        #endregion

        // Native buffers that the CPU simulation reads from
        internal AsyncTextureSynchronizer<uint> ripplesCurrentMapSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);
        internal AsyncTextureSynchronizer<uint> largeCurrentMapSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);

        void FillCurrentMapData(ref WaterSimSearchData wsd)
        {
            // Common data
            wsd.sectorData = HDRenderPipeline.currentPipeline.m_SectorData;

            // Swell / Agitation
            if (largeCurrentMap != null && largeCurrentMapSynchronizer.TryGetBuffer(out var currentBuffer) && currentBuffer.Length > 0 && largeCurrentMapSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeGroup0CurrentMap = true;
                wsd.group0CurrentMap = currentBuffer;
                wsd.group0CurrentMapWrapModeU = largeCurrentMap.wrapModeU;
                wsd.group0CurrentMapWrapModeV = largeCurrentMap.wrapModeV;
                wsd.group0CurrentMapResolution = largeCurrentMapSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeGroup0CurrentMap = false;
                wsd.group0CurrentMap = HDRenderPipeline.currentPipeline.m_DefaultCurrentMap;
                wsd.group0CurrentMapResolution = int2(1, 1);
            }

            wsd.group0CurrentRegionScale = float2(1.0f / largeCurrentRegionExtent.x, 1.0f / largeCurrentRegionExtent.y);
            wsd.group0CurrentRegionOffset = float2(largeCurrentRegionOffset.x, largeCurrentRegionOffset.y);
            wsd.group0CurrentMapInfluence = largeCurrentMapInfluence;

            // Ripples
            if (ripplesCurrentMap != null && ripplesCurrentMapSynchronizer.TryGetBuffer(out currentBuffer) && currentBuffer.Length > 0 && ripplesCurrentMapSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeGroup1CurrentMap = true;
                wsd.group1CurrentMap = currentBuffer;
                wsd.group1CurrentMapWrapModeU = ripplesCurrentMap.wrapModeU;
                wsd.group1CurrentMapWrapModeV = ripplesCurrentMap.wrapModeV;
                wsd.group1CurrentMapResolution = ripplesCurrentMapSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeGroup1CurrentMap = false;
                wsd.group1CurrentMap = HDRenderPipeline.currentPipeline.m_DefaultCurrentMap;
                wsd.group1CurrentMapResolution = int2(1, 1);
            }

            wsd.group1CurrentRegionScale = float2(1.0f / ripplesCurrentRegionExtent.x, 1.0f / ripplesCurrentRegionExtent.y);
            wsd.group1CurrentRegionOffset = float2(ripplesCurrentRegionOffset.x, ripplesCurrentRegionOffset.y);
            wsd.group1CurrentMapInfluence = ripplesCurrentMapInfluence;
        }

        void ReleaseCurrentMapResources()
        {
            ripplesCurrentMapSynchronizer.ReleaseATSResources();
            largeCurrentMapSynchronizer.ReleaseATSResources();
        }
    }
}
