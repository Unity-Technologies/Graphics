
namespace UnityEngine.Rendering
{
    internal class DynamicGISkyOcclusionResources : ScriptableObject
    {
        [Reload("Editor/Lighting/ProbeVolume/DynamicGI/DynamicGISkyOcclusion.raytrace")]
        public RayTracingShader hardwareRayTracingShader;
        [Reload("Editor/Lighting/ProbeVolume/DynamicGI/DynamicGISkyOcclusion.compute")]
        public ComputeShader rayTracingShader;
    }
}
