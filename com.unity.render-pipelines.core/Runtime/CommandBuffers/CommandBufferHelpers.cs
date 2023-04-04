using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// This struct contains some static helper functions that can be used when you want to convert between Commandbuffer and RasterCommandBuffer/ComputeCommandBuffer/LowLevelCommandBuffer
    /// </summary>
    public struct CommandBufferHelpers
    {
        static internal RasterCommandBuffer rasterCmd = new RasterCommandBuffer(null, null, false);
        static internal ComputeCommandBuffer computeCmd = new ComputeCommandBuffer(null, null, false);
        static internal LowLevelCommandBuffer lowlevelCmd = new LowLevelCommandBuffer(null, null, false);

        /// <summary>
        /// Get a RasterCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the RasterCommandBuffer should record it's commands to.</param>
        /// <returns>A RasterCommandBuffer that will record its commands to the given buffer.</returns>
        public static RasterCommandBuffer GetRasterCommandBuffer(CommandBuffer baseBuffer)
        {
            rasterCmd.m_WrappedCommandBuffer = baseBuffer;
            return rasterCmd;
        }

        /// <summary>
        /// Get a ComputeCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the RasterCommandBuffer should record it's commands to.</param>
        /// <returns>A ComputeCommandBuffer that will record its commands to the given buffer.</returns>
        public static ComputeCommandBuffer GetComputeCommandBuffer(CommandBuffer baseBuffer)
        {
            computeCmd.m_WrappedCommandBuffer = baseBuffer;
            return computeCmd;
        }

        /// <summary>
        /// Get a LowLevelCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the LowLevelCommandBuffer should record it's commands to.</param>
        /// <returns>A LowLevelCommandBuffer that will record its commands to the given buffer.</returns>
        public static LowLevelCommandBuffer GetLowLevelCommandBuffer(CommandBuffer baseBuffer)
        {
            lowlevelCmd.m_WrappedCommandBuffer = baseBuffer;
            return lowlevelCmd;
        }
    }
}
