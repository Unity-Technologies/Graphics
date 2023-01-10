using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// This struct contains some static helper functions that can be used when you want to use the
    /// </summary>
    public struct CommandBufferHelpers
    {
        /// <summary>
        /// Get a RasterCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the new RasterCommandBuffer should record it's commands do.</param>
        /// <returns>A RasterCommandBuffer that will record its commands to the given buffer.</returns>
        public static RasterCommandBuffer GetRasterCommandBuffer(CommandBuffer baseBuffer)
        {
            return new RasterCommandBuffer(baseBuffer, null, false);
        }

        /// <summary>
        /// Get a ComputeCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the new RasterCommandBuffer should record it's commands do.</param>
        /// <returns>A ComputeCommandBuffer that will record its commands to the given buffer.</returns>
        public static ComputeCommandBuffer GetComputeCommandBuffer(CommandBuffer baseBuffer)
        {
            return new ComputeCommandBuffer(baseBuffer, null, false);
        }
    }
}
