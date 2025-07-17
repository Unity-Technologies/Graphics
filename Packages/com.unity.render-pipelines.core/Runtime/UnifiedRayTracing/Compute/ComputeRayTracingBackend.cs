namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal class ComputeRayTracingBackend : IRayTracingBackend
    {
        public ComputeRayTracingBackend(RayTracingResources resources)
        {
            m_Resources = resources;
        }

        public IRayTracingShader CreateRayTracingShader(Object shader, string kernelName, GraphicsBuffer dispatchBuffer)
        {
            Debug.Assert(shader is ComputeShader);
            return new ComputeRayTracingShader((ComputeShader)shader, kernelName, dispatchBuffer);
        }

        public IRayTracingAccelStruct CreateAccelerationStructure(AccelerationStructureOptions options, ReferenceCounter counter)
        {
            return new ComputeRayTracingAccelStruct(options, m_Resources, counter);
        }

        public ulong GetRequiredTraceScratchBufferSizeInBytes(uint width, uint height, uint depth)
        {
            uint rayCount = width * height * depth;
            return (RadeonRays.RadeonRaysAPI.GetTraceMemoryRequirements(rayCount) * RayTracingContext.GetScratchBufferStrideInBytes());
        }

        readonly RayTracingResources m_Resources;
    }
}
