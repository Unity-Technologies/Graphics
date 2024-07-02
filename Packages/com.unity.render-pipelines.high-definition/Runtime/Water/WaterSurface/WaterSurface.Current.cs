using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : MonoBehaviour
    {
        #region Large Current
        /// <summary>
        /// Specifies if the surface supports large current decals.
        /// </summary>
        public bool supportLargeCurrent = false;

        /// <summary>
        /// Specifies the resolution of the texture used to represent the large current area.
        /// </summary>
        [Tooltip("Specifies the resolution of the texture used to represent the large current area.")]
        public WaterDecalRegionResolution largeCurrentRes = WaterDecalRegionResolution.Resolution512;

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
        [Range(0, 1)]
        public float largeCurrentMapInfluence = 1.0f;
        #endregion

        #region Ripples Current
        /// <summary>
        /// Specifies if the surface supports ripples current decals.
        /// </summary>
        public bool supportRipplesCurrent = false;

        /// <summary>
        /// Specifies the resolution of the texture used to represent the ripples current area.
        /// </summary>
        [Tooltip("Specifies the resolution of the texture used to represent the ripples current area.")]
        public WaterDecalRegionResolution ripplesCurrentRes = WaterDecalRegionResolution.Resolution512;

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
        [Range(0, 1)]
        public float ripplesCurrentMapInfluence = 1.0f;
        #endregion

        // GPU data
        internal RTHandle largeCurrentBuffer = null;
        internal RTHandle ripplesCurrentBuffer = null;

        // Native buffers that the CPU simulation reads from
        internal AsyncTextureSynchronizer<uint> largeCurrentMapSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);
        internal AsyncTextureSynchronizer<uint> ripplesCurrentMapSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);

        void FillCurrentMapData(ref WaterSimSearchData wsd)
        {
            var system = HDRenderPipeline.currentPipeline.waterSystem;

            // Common data
            wsd.sectorData = system.m_SectorData;

            // Swell / Agitation
            var largeMap = GetLargeCurrentBuffer(system, true);
            if (largeMap != null && largeCurrentMapSynchronizer.TryGetBuffer(out var currentBuffer) && currentBuffer.Length > 0 && largeCurrentMapSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeGroup0CurrentMap = true;
                wsd.group0CurrentMap = currentBuffer;
                wsd.group0CurrentMapWrapModeU = largeMap.wrapModeU;
                wsd.group0CurrentMapWrapModeV = largeMap.wrapModeV;
                wsd.group0CurrentMapResolution = largeCurrentMapSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeGroup0CurrentMap = false;
                wsd.group0CurrentMap = system.m_DefaultCurrentMap;
                wsd.group0CurrentMapResolution = int2(1, 1);
            }

            wsd.group0CurrentRegionScale = float2(1.0f / largeCurrentRegionExtent.x, 1.0f / largeCurrentRegionExtent.y);
            wsd.group0CurrentRegionOffset = float2(largeCurrentRegionOffset.x, largeCurrentRegionOffset.y);
            wsd.group0CurrentMapInfluence = system.m_EnableDecalWorkflow ? 1.0f : largeCurrentMapInfluence;

            // Ripples
            var ripplesMap = GetRipplesCurrentBuffer(system, true);
            if (ripplesMap != null && ripplesCurrentMapSynchronizer.TryGetBuffer(out currentBuffer) && currentBuffer.Length > 0 && ripplesCurrentMapSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeGroup1CurrentMap = true;
                wsd.group1CurrentMap = currentBuffer;
                wsd.group1CurrentMapWrapModeU = ripplesMap.wrapModeU;
                wsd.group1CurrentMapWrapModeV = ripplesMap.wrapModeV;
                wsd.group1CurrentMapResolution = ripplesCurrentMapSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeGroup1CurrentMap = false;
                wsd.group1CurrentMap = system.m_DefaultCurrentMap;
                wsd.group1CurrentMapResolution = int2(1, 1);
            }

            wsd.group1CurrentRegionScale = float2(1.0f / ripplesCurrentRegionExtent.x, 1.0f / ripplesCurrentRegionExtent.y);
            wsd.group1CurrentRegionOffset = float2(ripplesCurrentRegionOffset.x, ripplesCurrentRegionOffset.y);
            wsd.group1CurrentMapInfluence = system.m_EnableDecalWorkflow ? 1.0f : ripplesCurrentMapInfluence;
        }

        internal void CheckCurrentResources()
        {
            if (!HDRenderPipeline.currentPipeline.waterSystem.m_EnableDecalWorkflow)
                return;

            if (supportLargeCurrent)
            {
                int resolution = (int)largeCurrentRes;
                if (largeCurrentBuffer != null && largeCurrentBuffer.rt.width != resolution)
                    ReleaseLargeCurrentResources();

                if (largeCurrentBuffer == null)
                    largeCurrentBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Large Current");
            }
            else if (largeCurrentBuffer != null)
                ReleaseLargeCurrentResources();

            if (supportRipplesCurrent)
            {
                int resolution = (int)ripplesCurrentRes;
                if (ripplesCurrentBuffer != null && ripplesCurrentBuffer.rt.width != resolution)
                    ReleaseRipplesCurrentResources();

                if (ripplesCurrentBuffer == null)
                    ripplesCurrentBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Ripples Current");
            }
            else if (ripplesCurrentBuffer != null)
                ReleaseRipplesCurrentResources();
        }

        void ReleaseLargeCurrentResources()
        {
            RTHandles.Release(largeCurrentBuffer);
            largeCurrentBuffer = null;

            largeCurrentMapSynchronizer.ReleaseATSResources();
        }

        void ReleaseRipplesCurrentResources()
        {
            RTHandles.Release(ripplesCurrentBuffer);
            ripplesCurrentBuffer = null;

            ripplesCurrentMapSynchronizer.ReleaseATSResources();
        }

        void ReleaseCurrentMapResources()
        {
            ReleaseLargeCurrentResources();
            ReleaseRipplesCurrentResources();
        }

        internal Texture GetLargeCurrentBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            if (surfaceType == WaterSurfaceType.Pool)
                return defaultValue;

            if (system.m_EnableDecalWorkflow)
                return frameSetting && system.m_ActiveLargeCurrent && supportLargeCurrent ? largeCurrentBuffer : defaultValue;
            else
                return largeCurrentMap != null ? largeCurrentMap : defaultValue;
        }

        // Helper for Ocean and Rivers (not Pools, as it's trivial in this case)
        internal bool UsesRipplesCurrent() => ripples && ripplesMotionMode == WaterPropertyOverrideMode.Custom && supportRipplesCurrent;

        internal Texture GetRipplesCurrentBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            if (surfaceType != WaterSurfaceType.Pool)
            {
                if (!ripples)
                    return defaultValue;
                if (ripplesMotionMode == WaterPropertyOverrideMode.Inherit)
                    return GetLargeCurrentBuffer(system, frameSetting, defaultValue);
            }

            if (system.m_EnableDecalWorkflow)
                return frameSetting && system.m_ActiveRipplesCurrent && supportRipplesCurrent ? ripplesCurrentBuffer : defaultValue;
            else
                return ripplesCurrentMap != null ? ripplesCurrentMap : defaultValue;
        }
    }
}
