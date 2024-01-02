using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using static Unity.Mathematics.math;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class WaterSurface : MonoBehaviour
    {
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

        // Native buffer that the CPU simulation reads from
        internal AsyncTextureSynchronizer<uint> waterMaskSynchronizer = new AsyncTextureSynchronizer<uint>(GraphicsFormat.R8G8B8A8_UNorm);

        void FillWaterMaskData(ref WaterSimSearchData wsd)
        {
            // Water Mask
            if (waterMask != null && waterMaskSynchronizer.TryGetBuffer(out var maskBuffer) && maskBuffer.Length > 0 && waterMaskSynchronizer.CurrentResolution().x != 0)
            {
                wsd.activeMask = true;
                wsd.maskBuffer = maskBuffer;
                wsd.maskWrapModeU = waterMask.wrapModeU;
                wsd.maskWrapModeV = waterMask.wrapModeV;
                wsd.maskResolution = waterMaskSynchronizer.CurrentResolution();
            }
            else
            {
                wsd.activeMask = false;
                wsd.maskBuffer = HDRenderPipeline.currentPipeline.m_DefaultWaterMask;
            }

            wsd.maskScale = float2(1.0f / waterMaskExtent.x, 1.0f / waterMaskExtent.y);
            wsd.maskOffset = float2(waterMaskOffset.x, waterMaskOffset.y);
            wsd.maskRemap = float2(waterMaskRemap.x, waterMaskRemap.y - waterMaskRemap.x);
        }

        void ReleaseWaterMaskResources()
        {
            waterMaskSynchronizer.ReleaseATSResources();
        }
    }
}
