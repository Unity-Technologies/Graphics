namespace UnityEngine.Rendering.Tests
{
    [CreateAssetMenu(menuName = "SurfaceCache/Test Shaders", fileName = "TestShaders")]
    internal sealed class TestResourceAsset : ScriptableObject
    {
        public ComputeShader scrolling;
        public ComputeShader eviction;
        public ComputeShader patchAllocation;
        public ComputeShader spatialFiltering;
        public ComputeShader temporalFiltering;
        public ComputeShader defrag;

        public ComputeShader punctualLightSamplingComputeShader;
        public RayTracingShader punctualLightSamplingRayTracingShader;
        public ComputeShader estimationComputeShader;
        public RayTracingShader estimationRayTracingShader;

        public ComputeShader geometryPoolKernels;
        public ComputeShader copyBuffer;
        public ComputeShader copyPositions;
        public ComputeShader bitHistogram;
        public ComputeShader blockReducePart;
        public ComputeShader blockScan;
        public ComputeShader buildHlbvh;
        public ComputeShader restructureBvh;
        public ComputeShader scatter;

        public ComputeShader blitCubemap;
        public ComputeShader blitGrayScaleCookie;
        public ComputeShader setAlphaChannelShader;
        public ComputeShader environmentImportanceSamplingBuild;
        public Mesh skyBoxMesh;
        public Mesh sixFaceSkyBoxMesh;
        public ComputeShader buildLightGridShader;
        public Shader solidColorShader;
    }
}
