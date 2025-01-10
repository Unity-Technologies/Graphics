using System;
using System.Runtime.CompilerServices;
using UnityEngine.VFX;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// This struct contains some static helper functions that can be used when you want to convert between Commandbuffer and RasterCommandBuffer/ComputeCommandBuffer/UnsafeCommandBuffer
    /// </summary>
    public struct CommandBufferHelpers
    {
        internal static RasterCommandBuffer rasterCmd = new RasterCommandBuffer(null, null, false);
        internal static ComputeCommandBuffer computeCmd = new ComputeCommandBuffer(null, null, false);
        internal static UnsafeCommandBuffer unsafeCmd = new UnsafeCommandBuffer(null, null, false);

        /// <summary>
        /// Get a RasterCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the RasterCommandBuffer should record it's commands to.</param>
        /// <returns>A RasterCommandBuffer that will record its commands to the given buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComputeCommandBuffer GetComputeCommandBuffer(CommandBuffer baseBuffer)
        {
            computeCmd.m_WrappedCommandBuffer = baseBuffer;
            return computeCmd;
        }

        /// <summary>
        /// Get an UnsafeCommandBuffer given an standard CommandBuffer.
        /// </summary>
        /// <param name="baseBuffer">The CommandBuffer the UnsafeCommandBuffer should record its commands to.</param>
        /// <returns>A UnsafeCommandBuffer that will record its commands to the given buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UnsafeCommandBuffer GetUnsafeCommandBuffer(CommandBuffer baseBuffer)
        {
            unsafeCmd.m_WrappedCommandBuffer = baseBuffer;
            return unsafeCmd;
        }

        /// <summary>
        /// Get the actual unity engine CommandBuffer backing a UnsafeCommandBuffer. This strips the last remnants of render graph safety from the UnsafeCommandBuffer
        /// you are fully on your own now to ensure any and all render graph safety. Please carefully consider if you really need this.
        /// </summary>
        /// <param name="baseBuffer">The UnsafeCommandBuffer you want to get the engine commandbuffer from.</param>
        /// <returns>A CommandBuffer that will record its commands to the given buffer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CommandBuffer GetNativeCommandBuffer(UnsafeCommandBuffer baseBuffer)
        {
            return baseBuffer.m_WrappedCommandBuffer;
        }

        /// <summary>
        /// Wrapper for VFXManager.ProcessCameraCommand that works with UnsafeCommandBuffer.
        /// </summary>
        /// <param name="cam">The Camera to process the VFX commands for.</param>
        /// <param name="cmd">The CommandBuffer to push commands to (can be null).</param>
        /// <param name="camXRSettings">The XR settings that the Visual Effect Graph uses to process the Camera commands.</param>
        /// <param name="results">The culling results to use.</param>
        public static void VFXManager_ProcessCameraCommand(Camera cam, UnsafeCommandBuffer cmd,
            VFXCameraXRSettings camXRSettings, CullingResults results)
        {
            VFXManager.ProcessCameraCommand(cam, cmd.m_WrappedCommandBuffer, camXRSettings, results);
        }
    }
}
