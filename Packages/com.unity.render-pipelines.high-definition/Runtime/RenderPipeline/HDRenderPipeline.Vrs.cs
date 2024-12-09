using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Pipeline specifics for VRS
    /// </summary>
    public partial class HDRenderPipeline
    {
        /// <summary>
        /// Preprocess VRS resources once at initialization
        /// </summary>
        void VrsInitializeResources()
        {
            if (!currentPlatformRenderPipelineSettings.supportVariableRateShading)
                return;

            Vrs.InitializeResources();
        }

        /// <summary>
        /// Dispose of preprocessed VRS resources
        /// </summary>
        void VrsDisposeResources()
        {
            Vrs.DisposeResources();
        }

        /// <summary>
        /// Get or create VRS image.
        /// </summary>
        /// <param name="hdCamera">Camera to get shading rate image from.</param>
        /// <param name="bufferCount">Number of buffer in history.</param>
        /// <returns>The created shading rate image handle or null if not possible.</returns>
        static RTHandle RequestVrsHistory(HDCamera hdCamera, int bufferCount)
        {
            if (bufferCount > 0)
                return RequestVrsRTHandle(hdCamera, bufferCount, (int)HDCameraFrameHistoryType.Vrs);

            return null;
        }

        /// <summary>
        /// Helper function for RequestVrsHistory
        /// </summary>
        static RTHandle RequestVrsRTHandle(HDCamera hdCamera, int bufferCount, int id)
        {
            if (!ShadingRateInfo.supportsPerImageTile)
                return null; // Tile size will be 0x0 and alloc fails.

            return hdCamera.GetCurrentFrameRT(id) ??
                   hdCamera.AllocHistoryFrameRT(id,
                    VrsAllocatorFunction,
                    bufferCount);
        }

        // Static allocator function to avoid allocations.
        static RTHandle VrsAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, new RTHandleAllocInfo(name: string.Format("{0}_VrsHistoryBuffer{1}", viewName, frameIndex))
            {
                slices = 1,
                dimension = TextureDimension.Tex2D,
                format = ShadingRateInfo.graphicsFormat,
                enableRandomWrite = true,
                useDynamicScale = true,
                useMipMap = false,
                autoGenerateMips = false,
                enableShadingRate = true,
            });
        }
    }
}
