namespace UnityEngine.Rendering.Tests
{
    [CreateAssetMenu(menuName = "SurfaceCache/Test Shaders", fileName = "TestShaders")]
    internal sealed class TestShaderAsset : ScriptableObject
    {
        public ComputeShader scrolling;
        public ComputeShader eviction;
        public ComputeShader patchAllocation;
        public ComputeShader spatialFiltering;
        public ComputeShader temporalFiltering;
        public ComputeShader defrag;

        // Typed as Object; harness picks one based on the active RayTracingBackend.
        public Object punctualLightSamplingComputeShader;
        public Object punctualLightSamplingRayTracingShader;
        public Object estimationComputeShader;
        public Object estimationRayTracingShader;
    }
}
