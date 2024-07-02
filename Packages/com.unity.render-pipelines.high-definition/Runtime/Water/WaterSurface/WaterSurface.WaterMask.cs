using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : MonoBehaviour
    {
        /// <summary>
        /// True if this surface supports simulation mask decals.
        /// </summary>
        public bool simulationMask = false;

        /// <summary>
        /// Specifies the resolution of the mask texture used to represent the mask area.
        /// </summary>
        [Tooltip("Specifies the resolution of the mask texture used to represent the mask area.")]
        public WaterDecalRegionResolution maskRes = WaterDecalRegionResolution.Resolution512;

        /// <summary>
        /// Sets the texture used to attenuate or suppress the swell, agitation and ripples water frequencies.
        /// </summary>
        public Texture waterMask = null;

        /// <summary>
        /// Sets the remapped range of the water mask.
        /// </summary>
        [Tooltip("Sets the remapped range of the water mask.")]
        public Vector2 waterMaskRemap = new Vector2(0.0f, 1.0f);

        /// <summary>
        /// Sets the extent of the water mask in meters.
        /// </summary>
        [Tooltip("Sets the extent of the water mask in meters.")]
        public Vector2 waterMaskExtent = new Vector2(100.0f, 100.0f);

        /// <summary>
        /// Sets the offset of the water mask in meters.
        /// </summary>
        [Tooltip("Sets the offset of the water mask in meters.")]
        public Vector2 waterMaskOffset = new Vector2(0.0f, 0.0f);

        // GPU data
        internal RTHandle maskBuffer = null;

        // Native buffer that the CPU simulation reads from
        internal AsyncTextureSynchronizer<uint> waterMaskSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);

        void FillWaterMaskData(ref WaterSimSearchData wsd)
        {
            var system = HDRenderPipeline.currentPipeline.waterSystem;

            var mask = GetSimulationMaskBuffer(system, true);
            if (mask != null && waterMaskSynchronizer.TryGetBuffer(out var maskBuffer) && maskBuffer.Length > 0 && waterMaskSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeMask = true;
                wsd.maskBuffer = maskBuffer;
                wsd.maskWrapModeU = mask.wrapModeU;
                wsd.maskWrapModeV = mask.wrapModeV;
                wsd.maskResolution = waterMaskSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeMask = false;
                wsd.maskBuffer = system.m_DefaultWaterMask;
            }

            wsd.maskScale = float2(1.0f / waterMaskExtent.x, 1.0f / waterMaskExtent.y);
            wsd.maskOffset = float2(waterMaskOffset.x, waterMaskOffset.y);
            wsd.maskRemap = float2(waterMaskRemap.x, waterMaskRemap.y - waterMaskRemap.x);
        }

        internal void CheckMaskResources()
        {
            if (!HDRenderPipeline.currentPipeline.waterSystem.m_EnableDecalWorkflow)
                return;

            if (simulationMask || supportSimulationFoamMask)
            {
                int resolution = (int)maskRes;
                if (maskBuffer != null && maskBuffer.rt.width != resolution)
                    ReleaseWaterMaskResources();

                if (maskBuffer == null)
                    maskBuffer = RTHandles.Alloc(resolution, resolution, 1, dimension: TextureDimension.Tex2D, colorFormat: GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite: true, wrapMode: TextureWrapMode.Clamp, name: "Water Mask");
            }
            else if (maskBuffer != null)
                ReleaseWaterMaskResources();
        }

        void ReleaseWaterMaskResources()
        {
            RTHandles.Release(maskBuffer);
            maskBuffer = null;

            waterMaskSynchronizer.ReleaseATSResources();
        }

        /// <summary>
        /// Function that returns the simulation mask buffer for the water surface.
        /// If the mask decals are disabled in the global settings or the feature is disabled by the water surface, the function returns null.
        /// </summary>
        /// <seealso cref="WaterSurface.GetDecalRegion"/>
        /// <returns>A texture that holds the simulation mask for each band in the RGB channels and the simulation foam mask in the alpha channel.</returns>
        public Texture GetSimulationMaskBuffer()
        {
            return maskBuffer;
        }

        internal Texture GetSimulationMaskBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            if (system.m_EnableDecalWorkflow)
                return frameSetting && system.m_ActiveMask && simulationMask ? maskBuffer : defaultValue;
            else
                return waterMask != null ? waterMask : defaultValue;
        }

        internal Texture GetSimulationFoamMaskBuffer(WaterSystem system, bool frameSetting, Texture defaultValue = null)
        {
            if (system.m_EnableDecalWorkflow)
                return frameSetting && system.m_ActiveMask && supportSimulationFoamMask ? maskBuffer : defaultValue;
            else
                return simulationFoamMask != null ? simulationFoamMask : defaultValue;
        }
    }
}
