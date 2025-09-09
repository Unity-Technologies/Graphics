using System;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine.LightTransport;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Integration
{
    internal class UnityComputeProbeIntegrator : IProbeIntegrator
    {
        private readonly ProbeIntegrator _probeIntegrator;
        private UnityComputeWorld _world;
        private uint _bounceCount;
        private uint _directLightingEvaluationCount;
        private uint _numIndirectLightingEvaluations;
        private uint _basePositionsOffset;

        private static class ShaderProperties
        {
            public static readonly int MappingTable = Shader.PropertyToID("g_MappingTable");
            public static readonly int PerProbeLightIndicesInput = Shader.PropertyToID("g_PerProbeLightIndicesInput");
            public static readonly int PerProbeLightIndicesOutput = Shader.PropertyToID("g_PerProbeLightIndicesOutput");
            public static readonly int PerProbeLightIndicesInputOffset = Shader.PropertyToID("g_PerProbeLightIndicesInputOffset");
            public static readonly int MaxLightsPerProbe = Shader.PropertyToID("g_MaxLightsPerProbe");
            public static readonly int ProbeCount = Shader.PropertyToID("g_ProbeCount");
        }
        private ComputeShader _probeOcclusionLightIndexMappingShader;
        private int _probeOcclusionLightIndexMappingKernel;
        private Rendering.Sampling.SamplingResources _samplingResources;
        private ProbeIntegratorResources _integrationResources;

        public UnityComputeProbeIntegrator(bool countNEERayAsPathSegment, Rendering.Sampling.SamplingResources samplingResources, ProbeIntegratorResources integrationResources, ComputeShader probeOcclusionLightIndexMappingShader)
        {
            _probeIntegrator = new ProbeIntegrator(countNEERayAsPathSegment);
            _samplingResources = samplingResources;
            _probeOcclusionLightIndexMappingShader = probeOcclusionLightIndexMappingShader;
            _probeOcclusionLightIndexMappingKernel = _probeOcclusionLightIndexMappingShader.FindKernel("MapIndices");
            _integrationResources = integrationResources;
        }

        public void Dispose()
        {
            _probeIntegrator.Dispose();
        }

        public IProbeIntegrator.Result IntegrateDirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            UnityComputeDeviceContext unifiedContext = context as UnityComputeDeviceContext;
            Debug.Assert(unifiedContext != null);
            Debug.Assert(sampleCount > 0);

            ProbeIntegrator.GetRadianceScratchBufferSizesInDwords((uint)positionCount, (uint)sampleCount, out uint expansionBufferSize, out uint reductionBufferSize);
            var expansionBuffer = unifiedContext.GetTemporaryBuffer(expansionBufferSize, sizeof(float));
            var reductionBuffer = unifiedContext.GetTemporaryBuffer(reductionBufferSize, sizeof(float));

            uint sampleOffset = 0;
            _probeIntegrator.EstimateDirectRadianceShl2(
                unifiedContext.GetCommandBuffer(),
                _world.PathTracingWorld,
                _basePositionsOffset + (uint)positionOffset,
                (uint)positionCount,
                sampleOffset,
                (uint)sampleCount,
                _directLightingEvaluationCount,
                ignoreEnvironment,
                unifiedContext.GetComputeBuffer(radianceEstimateOut.Id),
                (uint)radianceEstimateOut.Offset,
                unifiedContext.GetComputeBuffer(expansionBuffer),
                unifiedContext.GetComputeBuffer(reductionBuffer));

            return new IProbeIntegrator.Result(IProbeIntegrator.ResultType.Success, string.Empty);
        }

        public IProbeIntegrator.Result IntegrateIndirectRadiance(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            bool ignoreEnvironment, BufferSlice<SphericalHarmonicsL2> radianceEstimateOut)
        {
            var unifiedContext = context as UnityComputeDeviceContext;
            Debug.Assert(unifiedContext != null);
            Debug.Assert(sampleCount > 0);

            ProbeIntegrator.GetRadianceScratchBufferSizesInDwords((uint)positionCount, (uint)sampleCount, out uint expansionBufferSize, out uint reductionBufferSize);
            var expansionBuffer = unifiedContext.GetTemporaryBuffer(expansionBufferSize, sizeof(float));
            var reductionBuffer = unifiedContext.GetTemporaryBuffer(reductionBufferSize, sizeof(float));

            uint sampleOffset = 0;
            _probeIntegrator.EstimateIndirectRadianceShl2(
                unifiedContext.GetCommandBuffer(),
                _world.PathTracingWorld,
                _basePositionsOffset + (uint)positionOffset,
                (uint)positionCount,
                _bounceCount,
                sampleOffset,
                (uint)sampleCount,
                _numIndirectLightingEvaluations,
                ignoreEnvironment,
                unifiedContext.GetComputeBuffer(radianceEstimateOut.Id),
                (uint)radianceEstimateOut.Offset,
                unifiedContext.GetComputeBuffer(expansionBuffer),
                unifiedContext.GetComputeBuffer(reductionBuffer));

            return new IProbeIntegrator.Result(IProbeIntegrator.ResultType.Success, string.Empty);
        }

        public IProbeIntegrator.Result IntegrateValidity(IDeviceContext context, int positionOffset, int positionCount, int sampleCount, BufferSlice<float> validityEstimateOut)
        {
            UnityComputeDeviceContext unifiedContext = context as UnityComputeDeviceContext;
            Debug.Assert(unifiedContext != null);
            Debug.Assert(sampleCount > 0);

            ProbeIntegrator.GetValidityScratchBufferSizesInDwords((uint)positionCount, (uint)sampleCount, out uint expansionBufferSize, out uint reductionBufferSize);
            var expansionBuffer = unifiedContext.GetTemporaryBuffer(expansionBufferSize, sizeof(float));
            var reductionBuffer = unifiedContext.GetTemporaryBuffer(reductionBufferSize, sizeof(float));

            uint sampleOffset = 0;
            _probeIntegrator.EstimateValidity(
                unifiedContext.GetCommandBuffer(),
                _world.PathTracingWorld,
                _basePositionsOffset + (uint)positionOffset,
                (uint)positionCount,
                sampleOffset,
                (uint)sampleCount,
                unifiedContext.GetComputeBuffer(validityEstimateOut.Id),
                (uint)validityEstimateOut.Offset,
                unifiedContext.GetComputeBuffer(expansionBuffer),
                unifiedContext.GetComputeBuffer(reductionBuffer));

            return new IProbeIntegrator.Result(IProbeIntegrator.ResultType.Success, string.Empty);
        }

        public IProbeIntegrator.Result IntegrateOcclusion(IDeviceContext context, int positionOffset, int positionCount, int sampleCount,
            int maxLightsPerProbe, BufferSlice<int> perProbeLightIndices, BufferSlice<float> probeOcclusionEstimateOut)
        {
            UnityComputeDeviceContext unifiedContext = context as UnityComputeDeviceContext;
            Debug.Assert(unifiedContext != null);
            Debug.Assert(maxLightsPerProbe > 0);
            Debug.Assert(sampleCount > 0);

            var cmd = unifiedContext.GetCommandBuffer();

            // The input per-probe light indices refer to elements of the light list used by LightBaker (ie. BakeInput.lightData).
            // This is by necessity, since that is the contract of the IProbeIntegrator interface, and the ordering of lights may be considered 'global'.
            // However, ProbeIntegrator needs per-probe light indices that refer to elements of the light list used by the path tracer
            // (ie. World.LightList) in order to access the light data in shader. We therefore need to convert the input indices.
            int[] lightIndexMapping = new int[_world.LightHandles.Length];
            for (int lightIndex = 0; lightIndex < lightIndexMapping.Length; lightIndex++)
            {
                var lightHandle = _world.LightHandles[lightIndex];
                int worldLightIndex = _world.PathTracingWorld.LightHandleToLightListIndex[lightHandle];
                lightIndexMapping[lightIndex] = worldLightIndex;
            }
            if (lightIndexMapping.Length == 0) // Avoid 0-sized buffer in case of no lights in scene
                lightIndexMapping = new int[] { -1 };
            using NativeArray<int> lightIndexMappingArray = new(lightIndexMapping, Allocator.Temp);
            var lightIndexMappingBuffer = unifiedContext.GetTemporaryBuffer((ulong)lightIndexMapping.Length, sizeof(int));
            unifiedContext.WriteBuffer(lightIndexMappingBuffer.Slice<int>(), lightIndexMappingArray);
            var perProbeLightIndicesWorld = unifiedContext.GetTemporaryBuffer((ulong)(positionCount * maxLightsPerProbe), sizeof(int));
            cmd.SetComputeBufferParam(_probeOcclusionLightIndexMappingShader, _probeOcclusionLightIndexMappingKernel, ShaderProperties.MappingTable, unifiedContext.GetComputeBuffer(lightIndexMappingBuffer));
            cmd.SetComputeBufferParam(_probeOcclusionLightIndexMappingShader, _probeOcclusionLightIndexMappingKernel, ShaderProperties.PerProbeLightIndicesInput, unifiedContext.GetComputeBuffer(perProbeLightIndices.Id));
            cmd.SetComputeBufferParam(_probeOcclusionLightIndexMappingShader, _probeOcclusionLightIndexMappingKernel, ShaderProperties.PerProbeLightIndicesOutput, unifiedContext.GetComputeBuffer(perProbeLightIndicesWorld));
            cmd.SetComputeIntParam(_probeOcclusionLightIndexMappingShader, ShaderProperties.PerProbeLightIndicesInputOffset, (int)perProbeLightIndices.Offset);
            cmd.SetComputeIntParam(_probeOcclusionLightIndexMappingShader, ShaderProperties.MaxLightsPerProbe, maxLightsPerProbe);
            cmd.SetComputeIntParam(_probeOcclusionLightIndexMappingShader, ShaderProperties.ProbeCount, positionCount);
            _probeOcclusionLightIndexMappingShader.GetKernelThreadGroupSizes(_probeOcclusionLightIndexMappingKernel, out uint threadGroupSizeX, out _, out _);
            cmd.DispatchCompute(_probeOcclusionLightIndexMappingShader, _probeOcclusionLightIndexMappingKernel, GraphicsHelpers.DivUp(positionCount, threadGroupSizeX), 1, 1);

            ProbeIntegrator.GetOcclusionScratchBufferSizesInDwords((uint)maxLightsPerProbe, (uint)positionCount, (uint)sampleCount, out uint expansionBufferSize, out uint reductionBufferSize);
            var expansionBuffer = unifiedContext.GetTemporaryBuffer(expansionBufferSize, sizeof(float));
            var reductionBuffer = unifiedContext.GetTemporaryBuffer(reductionBufferSize, sizeof(float));

            uint sampleOffset = 0;
            _probeIntegrator.EstimateLightOcclusion(
                cmd,
                _world.PathTracingWorld,
                _basePositionsOffset + (uint)positionOffset,
                (uint)positionCount,
                sampleOffset,
                (uint)sampleCount,
                (uint)maxLightsPerProbe,
                unifiedContext.GetComputeBuffer(perProbeLightIndicesWorld),
                0u,
                unifiedContext.GetComputeBuffer(probeOcclusionEstimateOut.Id),
                (uint)probeOcclusionEstimateOut.Offset,
                unifiedContext.GetComputeBuffer(expansionBuffer),
                unifiedContext.GetComputeBuffer(reductionBuffer));

            return new IProbeIntegrator.Result(IProbeIntegrator.ResultType.Success, string.Empty);
        }

        public void Prepare(IDeviceContext context, IWorld world, BufferSlice<Vector3> positions, float pushoff, int bounceCount)
        {
            _bounceCount = (uint)bounceCount;
            _directLightingEvaluationCount = 4;
            _numIndirectLightingEvaluations = 1;

            _world = world as UnityComputeWorld;
            Debug.Assert(world != null);

            UnityComputeDeviceContext unifiedContext = context as UnityComputeDeviceContext;
            Debug.Assert(unifiedContext != null);

            _basePositionsOffset = (uint)positions.Offset;
            Debug.Assert(_world != null, nameof(_world) + " != null");

            _probeIntegrator.Prepare(unifiedContext.GetComputeBuffer(positions.Id), _integrationResources, _samplingResources);
        }

        public void SetProgressReporter(BakeProgressState progressState) => _probeIntegrator.SetProgressReporter(progressState);
    }
}
