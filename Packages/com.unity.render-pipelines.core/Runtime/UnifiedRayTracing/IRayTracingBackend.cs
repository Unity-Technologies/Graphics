using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal interface IRayTracingBackend
    {
        IRayTracingShader CreateRayTracingShader(
            Object shader,
            string kernelName,
            GraphicsBuffer dispatchBuffer);

        IRayTracingAccelStruct CreateAccelerationStructure(
            AccelerationStructureOptions options,
            ReferenceCounter counter);

        ulong GetRequiredTraceScratchBufferSizeInBytes(uint width, uint height, uint depth);
    }
}
