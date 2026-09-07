namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal class HardwareRayTracingBackend : IRayTracingBackend
    {
        public HardwareRayTracingBackend(RayTracingResources resources)
        {
            m_Resources = resources;
        }

        public IRayTracingShader CreateRayTracingShader(Object shader, string kernelName, GraphicsBuffer dispatchBuffer)
        {
            Utils.CheckSupport(SystemInfo.supportsRayTracingShaders,
                "This GPU supports hardware ray tracing but cannot dispatch ray tracing shaders. " +
                "Check RayTracingContext.GetCapabilities(RayTracingBackend.Hardware) for CapabilityMask.RayTracingShaders before creating the shader.");

            Debug.Assert(shader is RayTracingShader);
            return new HardwareRayTracingShader((RayTracingShader)shader, kernelName, dispatchBuffer);
        }

        public IRayTracingAccelStruct CreateAccelerationStructure(AccelerationStructureOptions options, ReferenceCounter counter)
        {
            return new HardwareRayTracingAccelStruct(options, counter);
        }   

        public ulong GetRequiredTraceScratchBufferSizeInBytes(uint width, uint height, uint depth)
        {
            return 0;
        }

        readonly RayTracingResources m_Resources;
    }
}
