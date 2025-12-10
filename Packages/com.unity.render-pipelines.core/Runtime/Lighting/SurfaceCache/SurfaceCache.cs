#if SURFACE_CACHE

using System;
using Unity.Mathematics;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.Rendering
{
    internal class SurfaceCacheRingConfig : IDisposable
    {
        private GraphicsBuffer _buffer; // Stores (count, start, end) twice to allow double buffering.
        private uint _flipflop = 0;

        public uint OffsetA => GetOffsetA(_flipflop);
        public uint OffsetB => GetOffsetB(_flipflop);
        public uint FlipFlop => _flipflop;
        public GraphicsBuffer Buffer => _buffer;

        public SurfaceCacheRingConfig()
        {
            const int ringBufferElementCount = 3 * 2;
            _buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, ringBufferElementCount, sizeof(uint));
            _buffer.SetData(new uint[ringBufferElementCount], 0, 0, ringBufferElementCount);
        }

        public void Flip()
        {
            _flipflop = Flip(_flipflop);
        }

        static public uint Flip(uint flip)
        {
            return flip ^ 1;
        }

        static public uint GetOffsetA(uint flipflop)
        {
            return flipflop * 3;
        }

        static public uint GetOffsetB(uint flipflop)
        {
            return  (flipflop ^ 1) * 3;
        }

        public void Dispose()
        {
            _buffer.Dispose();
        }
    }

    internal class SurfaceCachePatchList : IDisposable
    {
        private readonly uint _capacity;

        private GraphicsBuffer _geometries;
        private GraphicsBuffer _cellIndices;
        private GraphicsBuffer _counterSets;
        private GraphicsBuffer[] _irradiances;
        private GraphicsBuffer _statistics;
        private GraphicsBuffer[] _restirRealizations;
        private GraphicsBuffer _risAccumulatedLuminances;

        public uint Capacity => _capacity;
        public GraphicsBuffer Geometries => _geometries;
        public GraphicsBuffer CellIndices => _cellIndices;
        public GraphicsBuffer CounterSets => _counterSets;
        public GraphicsBuffer[] Irradiances => _irradiances;
        public GraphicsBuffer Statistics => _statistics;
        public GraphicsBuffer[] RestirRealizations => _restirRealizations;
        public GraphicsBuffer RisAccumulatedLuminances => _risAccumulatedLuminances;

        public SurfaceCachePatchList(uint capacity, SurfaceCacheEstimationMethod estimationMethod)
        {
            _capacity = capacity;
            int capacityInt = (int)capacity;
            const int irradianceStride = sizeof(float) * 12;

            _geometries = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, sizeof(float) * 3 * 2);
            _cellIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, sizeof(uint));
            _counterSets = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, sizeof(uint));

            _irradiances = new GraphicsBuffer[3];
            _irradiances[0] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, irradianceStride);
            _irradiances[1] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, irradianceStride);
            _irradiances[2] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, irradianceStride);
            _statistics = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, sizeof(float) * 3 * 2);

            if (estimationMethod == SurfaceCacheEstimationMethod.Restir)
            {
                _restirRealizations = new GraphicsBuffer[2];
                uint sampleLength = sizeof(float) * 3 * 4 + sizeof(uint);
                uint realizationLength = sampleLength + sizeof(float) * 2;
                _restirRealizations[0] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, (int)realizationLength);
                _restirRealizations[1] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, (int)realizationLength);
            }
            else if (estimationMethod == SurfaceCacheEstimationMethod.Ris)
            {
                uint luminanceLength = sizeof(float) * 9;
                _risAccumulatedLuminances = new GraphicsBuffer(GraphicsBuffer.Target.Structured, capacityInt, (int)luminanceLength);
            }
        }

        public void Dispose()
        {
            foreach (var b in _irradiances)
            {
                b.Dispose();
            }
            _counterSets.Dispose();
            _geometries.Dispose();
            _cellIndices.Dispose();
            _statistics.Dispose();
            _risAccumulatedLuminances?.Dispose();

            if (_restirRealizations != null)
            {
                foreach (var b in _restirRealizations)
                    b.Dispose();
            }
        }
    }

    internal class SurfaceCacheVolume : IDisposable
    {
        public const int InvalidOffset = Int32.MaxValue;
        public const uint InvalidPatchIndex = UInt32.MaxValue; // Must match HLSL side.

        public readonly uint SpatialResolution;
        public readonly uint CascadeCount;
        public readonly float VoxelMinSize;
        public readonly int3[] CascadeOffsets;
        public readonly GraphicsBuffer CascadeOffsetBuffer;
        public readonly GraphicsBuffer CellAllocationMarks;
        public readonly GraphicsBuffer CellPatchIndices;

        public Vector3 TargetPos = Vector3.zero;

        internal SurfaceCacheVolume(uint spatialResolution, uint cascadeCount, float size)
        {
            const uint angularResolution = 4; // Must match HLSL side.
            uint cellCount = spatialResolution * spatialResolution * spatialResolution * angularResolution * angularResolution * cascadeCount;

            SpatialResolution = spatialResolution;
            CascadeCount = cascadeCount;
            VoxelMinSize = size / (spatialResolution * (float)(1u << (int)(cascadeCount - 1u)));
            CascadeOffsets = new int3[cascadeCount];
            CascadeOffsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)cascadeCount, sizeof(int) * 3);
            CellAllocationMarks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(cellCount), sizeof(uint));
            CellPatchIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(cellCount), sizeof(uint));

            {
                var initBuffer = new uint[cellCount];
                CellAllocationMarks.SetData(initBuffer);
                for (int i = 0; i < cellCount; ++i)
                    initBuffer[i] = InvalidPatchIndex;
                CellPatchIndices.SetData(initBuffer);
            }

            for (int i = 0; i < cascadeCount; ++i)
            {
                CascadeOffsets[i] = new int3(InvalidOffset, InvalidOffset, InvalidOffset);
            }
        }

        public void Dispose()
        {
            CascadeOffsetBuffer.Dispose();
            CellAllocationMarks.Dispose();
            CellPatchIndices.Dispose();
        }
    }

    internal enum SurfaceCacheEstimationMethod
    {
        Uniform,
        Restir,
        Ris
    }

    internal struct SurfaceCacheVolumeParameterSet
    {
        public float Size;
        public uint Resolution;
        public uint CascadeCount;
    }

    internal struct SurfaceCacheEstimationParameterSet
    {
        public SurfaceCacheEstimationMethod Method;
        public bool MultiBounce;
        public uint RestirEstimationConfidenceCap;
        public uint RestirEstimationSpatialSampleCount;
        public float RestirEstimationSpatialFilterSize;
        public uint RestirEstimationValidationFrameInterval;
        public uint UniformEstimationSampleCount;
        public uint RisEstimationCandidateCount;
        public float RisEstimationTargetFunctionUpdateWeight;
    }

    internal struct SurfaceCachePatchFilteringParameterSet
    {
        public float TemporalSmoothing;
        public bool SpatialFilterEnabled;
        public uint SpatialFilterSampleCount;
        public float SpatialFilterRadius;
        public bool TemporalPostFilterEnabled;
    }

    internal class SurfaceCacheResourceSet
    {
        internal ComputeShader ScrollingShader;
        internal int ScrollingKernel;
        internal uint3 ScrollingKernelGroupSize;

        internal ComputeShader EvictionShader;
        internal int EvictionKernel;
        internal uint3 EvictionKernelGroupSize;

        internal ComputeShader DefragShader;
        internal int DefragKernel;
        internal uint3 DefragKernelGroupSize;
        internal LocalKeyword DefragKeyword;

        internal IRayTracingShader PunctualLightSamplingShader;
        internal IRayTracingShader UniformEstimationShader;
        internal IRayTracingShader RestirCandidateTemporalShader;
        internal IRayTracingShader RisEstimationShader;

        internal ComputeShader RestirSpatialShader;
        internal int RestirSpatialKernel;
        internal uint3 RestirSpatialKernelGroupSize;

        internal ComputeShader RestirEstimationShader;
        internal int RestirEstimationKernel;
        internal uint3 RestirEstimationKernelGroupSize;

        internal ComputeShader SpatialFilteringShader;
        internal int SpatialFilteringKernel;
        internal uint3 SpatialFilteringKernelGroupSize;

        internal ComputeShader TemporalFilteringShader;
        internal int TemporalFilteringKernel;
        internal uint3 TemporalFilteringKernelGroupSize;

        internal readonly uint SubGroupSize;

        internal SurfaceCacheResourceSet(uint subGroupSize)
        {
            SubGroupSize = subGroupSize;
        }

        internal bool LoadFromRenderPipelineResources(RayTracingContext rtContext)
        {
            var rpResources = GraphicsSettings.GetRenderPipelineSettings<Rendering.SurfaceCacheRenderPipelineResourceSet>();
            if (rpResources == null)
                return false;

            ScrollingShader = rpResources.scrollingShader;
            ScrollingKernel = ScrollingShader.FindKernel("Scroll");
            ScrollingShader.GetKernelThreadGroupSizes(ScrollingKernel, out ScrollingKernelGroupSize.x, out ScrollingKernelGroupSize.y, out ScrollingKernelGroupSize.z);

            EvictionShader = rpResources.evictionShader;
            EvictionKernel = EvictionShader.FindKernel("Evict");
            EvictionShader.GetKernelThreadGroupSizes(EvictionKernel, out EvictionKernelGroupSize.x, out EvictionKernelGroupSize.y, out EvictionKernelGroupSize.z);

            RestirSpatialShader = rpResources.restirSpatialShader;
            RestirSpatialKernel = RestirSpatialShader.FindKernel("ResampleSpatially");
            RestirSpatialShader.GetKernelThreadGroupSizes(RestirSpatialKernel, out RestirSpatialKernelGroupSize.x, out RestirSpatialKernelGroupSize.y, out RestirSpatialKernelGroupSize.z);

            RestirEstimationShader = rpResources.restirEstimationShader;
            RestirEstimationKernel = RestirEstimationShader.FindKernel("Estimate");
            RestirEstimationShader.GetKernelThreadGroupSizes(RestirEstimationKernel, out RestirEstimationKernelGroupSize.x, out RestirEstimationKernelGroupSize.y, out RestirEstimationKernelGroupSize.z);

            SpatialFilteringShader = rpResources.spatialFilteringShader;
            SpatialFilteringKernel = SpatialFilteringShader.FindKernel("FilterSpatially");
            SpatialFilteringShader.GetKernelThreadGroupSizes(SpatialFilteringKernel, out SpatialFilteringKernelGroupSize.x, out SpatialFilteringKernelGroupSize.y, out SpatialFilteringKernelGroupSize.z);

            TemporalFilteringShader = rpResources.temporalFilteringShader;
            TemporalFilteringKernel = TemporalFilteringShader.FindKernel("FilterTemporally");
            TemporalFilteringShader.GetKernelThreadGroupSizes(TemporalFilteringKernel, out TemporalFilteringKernelGroupSize.x, out TemporalFilteringKernelGroupSize.y, out TemporalFilteringKernelGroupSize.z);

            Debug.Assert(SubGroupSize == 8 || SubGroupSize == 16 || SubGroupSize == 32 || SubGroupSize == 48 || SubGroupSize == 64);
            DefragShader = rpResources.defragShader;
            var defragKeyword = "SUB_GROUP_SIZE_" + SubGroupSize;
            DefragShader.EnableKeyword(defragKeyword);
            DefragKernel = DefragShader.FindKernel("Defrag");
            DefragShader.GetKernelThreadGroupSizes(DefragKernel, out DefragKernelGroupSize.x, out DefragKernelGroupSize.y, out DefragKernelGroupSize.z);
            DefragKeyword = new LocalKeyword(DefragShader, defragKeyword);
            DefragShader.DisableKeyword(defragKeyword);

            Object punctualLightSamplingUnifiedObj;
            Object uniformEstimationUnifiedObj;
            Object restirCandidateTemporalUnifiedObj;
            Object risEstimationUnifiedObj;
            if (rtContext.BackendType == RayTracingBackend.Compute)
            {
                punctualLightSamplingUnifiedObj = rpResources.punctualLightSamplingComputeShader;
                uniformEstimationUnifiedObj = rpResources.uniformEstimationComputeShader;
                restirCandidateTemporalUnifiedObj = rpResources.restirCandidateTemporalComputeShader;
                risEstimationUnifiedObj = rpResources.risEstimationComputeShader;
            }
            else
            {
                punctualLightSamplingUnifiedObj = rpResources.punctualLightSamplingRayTracingShader;
                uniformEstimationUnifiedObj = rpResources.uniformEstimationRayTracingShader;
                restirCandidateTemporalUnifiedObj = rpResources.restirCandidateTemporalRayTracingShader;
                risEstimationUnifiedObj = rpResources.risEstimationRayTracingShader;
            }

            PunctualLightSamplingShader = rtContext.CreateRayTracingShader(punctualLightSamplingUnifiedObj);
            UniformEstimationShader = rtContext.CreateRayTracingShader(uniformEstimationUnifiedObj);
            RestirCandidateTemporalShader = rtContext.CreateRayTracingShader(restirCandidateTemporalUnifiedObj);
            RisEstimationShader = rtContext.CreateRayTracingShader(risEstimationUnifiedObj);

            return true;
        }
    }

    internal class SurfaceCache : IDisposable
    {
        public const uint CascadeMax = 8;
        private readonly GraphicsBuffer _punctualLightSamples;
        private readonly SurfaceCachePatchList _patches;
        private readonly SurfaceCacheVolume _volume;
        private readonly SurfaceCacheRingConfig _ringConfig;
        private readonly SurfaceCacheResourceSet _resources;
        private GraphicsBuffer _traceScratch;

        private SurfaceCacheEstimationParameterSet _estimationParams;
        private SurfaceCachePatchFilteringParameterSet _patchFilteringParams;

        private float _shortHysteresis;
        readonly private uint _defragCount;
        readonly private float _albedoBoost = 1.0f;

        public GraphicsBuffer PunctualLightSamples => _punctualLightSamples;
        public SurfaceCachePatchList Patches => _patches;
        public SurfaceCacheVolume Volume => _volume;
        public SurfaceCacheRingConfig RingConfig => _ringConfig;

        private class ScrollingPassData
        {
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal GraphicsBuffer CellAllocationMarks;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer PatchCellIndices;
            internal GraphicsBuffer NewCascadeOffsetsDevice;
            internal int3[] NewCascadeOffsetsHost;
            internal int3[] OldCascadeOffsetsHost;
            internal uint VolumeSpatialResolution;
            internal uint CascadeCount;
        }

        private class EvictionPassData
        {
            internal uint PatchCapacity;
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer PatchCellIndices;
            internal GraphicsBuffer CellAllocationMarks;
            internal GraphicsBuffer CellPatchIndices;
            internal uint RingConfigOffset;
            internal uint FrameIdx;
        }

        private class DefragPassData
        {
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal LocalKeyword Keyword;
            internal uint PatchCapacity;
            internal uint3 ThreadGroupSize;
            internal uint IterationOffset;
            internal uint IterationCount;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchCellIndices;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer PatchIrradiances0;
            internal GraphicsBuffer PatchIrradiances1;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer CellPatchIndices;
            internal uint RingConfigStartFlipflop;
            internal uint EvenIterationPatchOffset;
            internal uint OddIterationPatchOffset;
        }

        private class UniformEstimationPassData
        {
            internal uint PatchCapacity;
            internal IRayTracingShader PunctualLightSamplingShader;
            internal IRayTracingShader EstimationShader;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchIrradiances;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer CascadeOffsets;
            internal GraphicsBuffer PunctualLightSamples;
            internal uint PunctualLightSampleCount;
            internal SurfaceCacheWorld World;
            internal float AlbedoBoost;
            internal uint FrameIdx;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal float ShortHysteresis;
            internal uint RingConfigOffset;
            internal uint SampleCount;
            internal uint VolumeSpatialResolution;
            internal float VolumeVoxelMinSize;
            internal Vector3 VolumeTargetPos;
            internal GraphicsBuffer TraceScratchBuffer;
        }

        private class RestirCandidateTemporalPassData
        {
            internal uint PatchCapacity;
            internal IRayTracingShader Shader;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchIrradiances;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchRealizations;
            internal GraphicsBuffer CascadeOffsets;
            internal GraphicsBuffer CellPatchIndices;
            internal Vector3 VolumeTargetPos;
            internal SurfaceCacheWorld World;
            internal float AlbedoBoost;
            internal uint FrameIdx;
            internal uint RingConfigOffset;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal uint ConfidenceCap;
            internal uint VolumeSpatialResolution;
            internal float VolumeVoxelMinSize;
            internal uint ValidationFrameInterval;
            internal GraphicsBuffer TraceScratchBuffer;
        }

        private class RestirSpatialPassData
        {
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal uint PatchCapacity;
            internal uint FrameIdx;
            internal uint CascadeCount;
            internal uint RingConfigOffset;
            internal uint SampleCount;
            internal float FilterSize;
            internal uint VolumeSpatialResolution;
            internal Vector3 VolumeTargetPos;
            internal float VolumeVoxelMinSize;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer CascadeOffsets;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer InputPatchRealizations;
            internal GraphicsBuffer OutputPatchRealizations;
        }

        private class RestirEstimationPassData
        {
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal uint PatchCapacity;
            internal ComputeShader Shader;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchIrradiances;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer PatchRealizations;
            internal uint RingConfigOffset;
            internal float ShortHysteresis;
        }

        private class RisEstimationPassData
        {
            internal uint PatchCapacity;
            internal IRayTracingShader Shader;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchIrradiances;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer CascadeOffsets;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer PatchAccumulatedLuminances;
            internal SurfaceCacheWorld World;
            internal float AlbedoBoost;
            internal uint FrameIdx;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal uint CandidateCount;
            internal uint RingConfigOffset;
            internal float ShortHysteresis;
            internal uint VolumeSpatialResolution;
            internal Vector3 VolumeTargetPos;
            internal float TargetFunctionUpdateWeight;
            internal float VolumeVoxelMinSize;
            internal GraphicsBuffer TraceScratchBuffer;
        }

        private class SpatialFilterPassData
        {
            internal GraphicsBuffer InputPatchIrradiances;
            internal GraphicsBuffer OutputPatchIrradiances;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer CascadeOffsets;
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal uint PatchCapacity;
            internal uint FrameIdx;
            internal uint CascadeCount;
            internal uint VolumeSpatialResolution;
            internal float VolumeVoxelMinSize;
            internal uint SampleCount;
            internal float Radius;
            internal uint RingConfigOffset;
            internal Vector3 VolumeTargetPos;
        }

        private class TemporalFilterPassData
        {
            internal ComputeShader Shader;
            internal int KernelIndex;
            internal uint3 ThreadGroupSize;
            internal GraphicsBuffer InputPatchIrradiances;
            internal GraphicsBuffer OutputPatchIrradiances;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchCounterSets;
            internal uint PatchCapacity;
            internal uint RingConfigOffset;
            internal float ShortHysteresis;
        }

        internal static class ShaderIDs
        {
            public static readonly int _CellAllocationMarks = Shader.PropertyToID("_CellAllocationMarks");
            public static readonly int _CellPatchIndices = Shader.PropertyToID("_CellPatchIndices");
            public static readonly int _MaterialEntries = Shader.PropertyToID("_MaterialEntries");
            public static readonly int _AlbedoTextures = Shader.PropertyToID("_AlbedoTextures");
            public static readonly int _AlbedoBoost = Shader.PropertyToID("_AlbedoBoost");
            public static readonly int _DirectionalLightDirection = Shader.PropertyToID("_DirectionalLightDirection");
            public static readonly int _DirectionalLightIntensity = Shader.PropertyToID("_DirectionalLightIntensity");
            public static readonly int _MaterialAtlasTexelSize = Shader.PropertyToID("_MaterialAtlasTexelSize");
            public static readonly int _TransmissionTextures = Shader.PropertyToID("_TransmissionTextures");
            public static readonly int _EmissionTextures = Shader.PropertyToID("_EmissionTextures");
            public static readonly int _VolumeTargetPos = Shader.PropertyToID("_VolumeTargetPos");
            public static readonly int _EnvironmentCubemap = Shader.PropertyToID("_EnvironmentCubemap");
            public static readonly int _NewCascadeOffsets = Shader.PropertyToID("_NewCascadeOffsets");
            public static readonly int _OldCascadeOffsets = Shader.PropertyToID("_OldCascadeOffsets");
            public static readonly int _VolumeSpatialResolution = Shader.PropertyToID("_VolumeSpatialResolution");
            public static readonly int _CascadeCount = Shader.PropertyToID("_CascadeCount");
            public static readonly int _ConfidenceCap = Shader.PropertyToID("_ConfidenceCap");
            public static readonly int _CandidateCount = Shader.PropertyToID("_CandidateCount");
            public static readonly int _TargetFunctionUpdateWeight = Shader.PropertyToID("_TargetFunctionUpdateWeight");
            public static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int _FilterSize = Shader.PropertyToID("_FilterSize");
            public static readonly int _MultiBounce = Shader.PropertyToID("_MultiBounce");
            public static readonly int _ValidationFrameInterval = Shader.PropertyToID("_ValidationFrameInterval");
            public static readonly int _VolumeVoxelMinSize = Shader.PropertyToID("_VolumeVoxelMinSize");
            public static readonly int _PunctualLightSampleCount = Shader.PropertyToID("_PunctualLightSampleCount");
            public static readonly int _ShortHysteresis = Shader.PropertyToID("_ShortHysteresis");
            public static readonly int _PatchCellIndices = Shader.PropertyToID("_PatchCellIndices");
            public static readonly int _RingConfigBuffer = Shader.PropertyToID("_RingConfigBuffer");
            public static readonly int _SpotLightPosition = Shader.PropertyToID("_SpotLightPosition");
            public static readonly int _SpotLightDirection = Shader.PropertyToID("_SpotLightDirection");
            public static readonly int _SpotLightCosAngle = Shader.PropertyToID("_SpotLightCosAngle");
            public static readonly int _Radius = Shader.PropertyToID("_Radius");
            public static readonly int _InputPatchIrradiances = Shader.PropertyToID("_InputPatchIrradiances");
            public static readonly int _OutputPatchIrradiances = Shader.PropertyToID("_OutputPatchIrradiances");
            public static readonly int _PatchIrradiances = Shader.PropertyToID("_PatchIrradiances");
            public static readonly int _FrameIdx = Shader.PropertyToID("_FrameIdx");
            public static readonly int _PatchCounterSets = Shader.PropertyToID("_PatchCounterSets");
            public static readonly int _CascadeOffsets = Shader.PropertyToID("_CascadeOffsets");
            public static readonly int _PatchIrradiances0 = Shader.PropertyToID("_PatchIrradiances0");
            public static readonly int _PatchIrradiances1 = Shader.PropertyToID("_PatchIrradiances1");
            public static readonly int _PatchStatistics = Shader.PropertyToID("_PatchStatistics");
            public static readonly int _RingConfigReadOffset = Shader.PropertyToID("_RingConfigReadOffset");
            public static readonly int _RingConfigWriteOffset = Shader.PropertyToID("_RingConfigWriteOffset");
            public static readonly int _PatchOffset = Shader.PropertyToID("_PatchOffset");
            public static readonly int _PatchGeometries = Shader.PropertyToID("_PatchGeometries");
            public static readonly int _PunctualLightSamples = Shader.PropertyToID("_PunctualLightSamples");
            public static readonly int _Samples = Shader.PropertyToID("_Samples");
            public static readonly int _PatchRealizations = Shader.PropertyToID("_PatchRealizations");
            public static readonly int _PatchAccumulatedLuminances = Shader.PropertyToID("_PatchAccumulatedLuminances");
            public static readonly int _InputPatchRealizations = Shader.PropertyToID("_InputPatchRealizations");
            public static readonly int _OutputPatchRealizations = Shader.PropertyToID("_OutputPatchRealizations");

            public static readonly int _RingConfigOffset = Shader.PropertyToID("_RingConfigOffset");
        }

        public SurfaceCache(
            SurfaceCacheResourceSet resources,
            uint defragCount,
            SurfaceCacheVolumeParameterSet volParams,
            SurfaceCacheEstimationParameterSet estimationParams,
            SurfaceCachePatchFilteringParameterSet patchFilteringParams)
        {
            Debug.Assert(volParams.CascadeCount != 0);
            Debug.Assert(volParams.CascadeCount <= CascadeMax);
            Debug.Assert(0.0f <= patchFilteringParams.TemporalSmoothing);
            Debug.Assert(patchFilteringParams.TemporalSmoothing <= 1.0f);

            const uint punctualLightSampleCount = 128;
            const uint patchCapacity = 65536; // Must match HLSL side constant.
            Debug.Assert((UInt64)4294967296 % (UInt64)patchCapacity == 0, "Patch Capacity must be a divisor of 2^32."); // This property is required by the HLSL side ring buffer allocation logic.

            _resources = resources;
            _volume = new SurfaceCacheVolume(volParams.Resolution, volParams.CascadeCount, volParams.Size);
            _ringConfig = new SurfaceCacheRingConfig();
            _patches = new SurfaceCachePatchList(patchCapacity, estimationParams.Method);
            _punctualLightSamples = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)punctualLightSampleCount, sizeof(float) * 16);

            _estimationParams = estimationParams;
            _patchFilteringParams = patchFilteringParams;

            Debug.Assert(0.0f <= patchFilteringParams.TemporalSmoothing && patchFilteringParams.TemporalSmoothing <= 1.0f);
            _shortHysteresis = Mathf.Lerp(0.75f, 0.95f, patchFilteringParams.TemporalSmoothing);

            _defragCount = defragCount;
        }

        public void RecordPreparation(RenderGraph renderGraph, uint frameIdx)
        {
            RecordScrolling(renderGraph);
            RecordEviction(renderGraph, frameIdx);
            RecordDefragmentation(renderGraph, frameIdx);
        }

        internal uint RecordPatchUpdate(RenderGraph renderGraph, uint frameIdx, SurfaceCacheWorld world)
        {
            RecordEstimation(renderGraph, frameIdx, world);
            return RecordFiltering(renderGraph, frameIdx);
        }

        private void RecordDefragmentation(RenderGraph renderGraph, uint frameIdx)
        {
            using (var builder = renderGraph.AddComputePass("Surface Cache Defrag", out DefragPassData passData))
            {
                passData.IterationOffset = frameIdx * _defragCount;
                passData.IterationCount = _defragCount;
                passData.Shader = _resources.DefragShader;
                passData.Keyword = _resources.DefragKeyword;
                passData.KernelIndex = _resources.DefragKernel;
                passData.PatchCapacity = Patches.Capacity;
                passData.RingConfigStartFlipflop = RingConfig.FlipFlop;
                passData.ThreadGroupSize = _resources.DefragKernelGroupSize;
                passData.RingConfigBuffer = RingConfig.Buffer;
                passData.PatchCellIndices = Patches.CellIndices;
                passData.PatchCounterSets = Patches.CounterSets;
                passData.PatchIrradiances0 = Patches.Irradiances[0];
                passData.PatchIrradiances1 = Patches.Irradiances[2];
                passData.PatchGeometries = Patches.Geometries;
                passData.PatchStatistics = Patches.Statistics;
                passData.CellPatchIndices = Volume.CellPatchIndices;
                passData.EvenIterationPatchOffset = 0;
                passData.OddIterationPatchOffset = _resources.SubGroupSize / 2;

                builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                builder.SetRenderFunc((DefragPassData data, ComputeGraphContext cgContext) => Defrag(data, cgContext));

                if (_defragCount % 2 == 1)
                    RingConfig.Flip();
            }
        }

        private void RecordEviction(RenderGraph renderGraph, uint frameIdx)
        {
            using (var builder = renderGraph.AddComputePass("Surface Cache Eviction", out EvictionPassData passData))
            {
                passData.Shader = _resources.EvictionShader;
                passData.KernelIndex = _resources.EvictionKernel;
                passData.ThreadGroupSize = _resources.EvictionKernelGroupSize;
                passData.RingConfigBuffer = RingConfig.Buffer;
                passData.RingConfigOffset = RingConfig.OffsetA;
                passData.PatchCounterSets = Patches.CounterSets;
                passData.PatchCellIndices = Patches.CellIndices;
                passData.CellAllocationMarks = Volume.CellAllocationMarks;
                passData.CellPatchIndices = Volume.CellPatchIndices;
                passData.PatchCapacity = Patches.Capacity;
                passData.FrameIdx = frameIdx;

                builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                builder.SetRenderFunc((EvictionPassData data, ComputeGraphContext cgContext) => Evict(data, cgContext));
            }
        }

        private uint RecordFiltering(RenderGraph renderGraph, uint frameIdx)
        {
            uint outputIrradianceBufferIdx = 0;
            if (_patchFilteringParams.SpatialFilterEnabled)
            {
                outputIrradianceBufferIdx = 1;
                using (var builder = renderGraph.AddComputePass("Surface Cache Spatial Filter", out SpatialFilterPassData passData))
                {
                    passData.InputPatchIrradiances = Patches.Irradiances[0];
                    passData.OutputPatchIrradiances = Patches.Irradiances[1];
                    passData.PatchGeometries = Patches.Geometries;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.Shader = _resources.SpatialFilteringShader;
                    passData.KernelIndex = _resources.SpatialFilteringKernel;
                    passData.ThreadGroupSize = _resources.SpatialFilteringKernelGroupSize;
                    passData.PatchCapacity = Patches.Capacity;
                    passData.FrameIdx = frameIdx;
                    passData.CascadeCount = Volume.CascadeCount;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.VolumeVoxelMinSize = Volume.VoxelMinSize;
                    passData.SampleCount = _patchFilteringParams.SpatialFilterSampleCount;
                    passData.Radius = _patchFilteringParams.SpatialFilterRadius;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.CascadeOffsets = Volume.CascadeOffsetBuffer;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.VolumeTargetPos = Volume.TargetPos;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((SpatialFilterPassData data, ComputeGraphContext cgContext) => FilterSpatially(data, cgContext));
                }
            }

            if (_patchFilteringParams.TemporalPostFilterEnabled)
            {
                using (var builder = renderGraph.AddComputePass("Surface Cache Temporal Filter", out TemporalFilterPassData passData))
                {
                    passData.Shader = _resources.TemporalFilteringShader;
                    passData.KernelIndex = _resources.TemporalFilteringKernel;
                    passData.ThreadGroupSize = _resources.TemporalFilteringKernelGroupSize;
                    passData.InputPatchIrradiances = Patches.Irradiances[outputIrradianceBufferIdx];
                    passData.OutputPatchIrradiances = Patches.Irradiances[2];
                    passData.PatchStatistics = Patches.Statistics;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchCounterSets = Patches.CounterSets;
                    passData.PatchCapacity = Patches.Capacity;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((TemporalFilterPassData data, ComputeGraphContext cgContext) => FilterTemporally(data, cgContext));
                }
                outputIrradianceBufferIdx = 2;
            }

            return outputIrradianceBufferIdx;
        }

        private void RecordEstimation(RenderGraph renderGraph, uint frameIdx, SurfaceCacheWorld world)
        {
            if (_estimationParams.Method == SurfaceCacheEstimationMethod.Uniform)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache Uniform Estimation", out UniformEstimationPassData passData))
                {
                    passData.PatchCapacity = Patches.Capacity;
                    passData.PunctualLightSamplingShader = _resources.PunctualLightSamplingShader;
                    passData.EstimationShader = _resources.UniformEstimationShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = Patches.Irradiances[0];
                    passData.PatchGeometries = Patches.Geometries;
                    passData.PatchStatistics = Patches.Statistics;
                    passData.PatchCounterSets = Patches.CounterSets;
                    passData.PunctualLightSamples = PunctualLightSamples;
                    passData.PunctualLightSampleCount = (uint)PunctualLightSamples.count;
                    passData.World = world;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.VolumeTargetPos = Volume.TargetPos;
                    passData.FrameIdx = frameIdx;
                    passData.AlbedoBoost = _albedoBoost;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.CascadeOffsets = Volume.CascadeOffsetBuffer;
                    passData.CascadeCount = Volume.CascadeCount;
                    passData.MultiBounce = _estimationParams.MultiBounce;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.SampleCount = _estimationParams.UniformEstimationSampleCount;
                    passData.VolumeVoxelMinSize = Volume.VoxelMinSize;

                    RayTracingHelper.ResizeScratchBufferForTrace(passData.EstimationShader, passData.PatchCapacity, 1, 1, ref _traceScratch);
                    RayTracingHelper.ResizeScratchBufferForTrace(passData.PunctualLightSamplingShader, passData.PunctualLightSampleCount, 1, 1, ref _traceScratch);
                    passData.TraceScratchBuffer = _traceScratch;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((UniformEstimationPassData data, UnsafeGraphContext cgContext) => UniformEstimate(data, cgContext));
                }
            }
            else if (_estimationParams.Method == SurfaceCacheEstimationMethod.Restir)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache Restir Candidate + Temporal", out RestirCandidateTemporalPassData passData))
                {
                    passData.PatchCapacity = Patches.Capacity;
                    passData.Shader = _resources.RestirCandidateTemporalShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = Patches.Irradiances[0];
                    passData.PatchGeometries = Patches.Geometries;
                    passData.PatchRealizations = Patches.RestirRealizations[0];
                    passData.CascadeOffsets = Volume.CascadeOffsetBuffer;
                    passData.World = world;
                    passData.AlbedoBoost = _albedoBoost;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.VolumeTargetPos = Volume.TargetPos;
                    passData.FrameIdx = frameIdx;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.CascadeCount = Volume.CascadeCount;
                    passData.MultiBounce = _estimationParams.MultiBounce;
                    passData.ConfidenceCap = _estimationParams.RestirEstimationConfidenceCap;
                    passData.VolumeVoxelMinSize = Volume.VoxelMinSize;
                    passData.ValidationFrameInterval = _estimationParams.RestirEstimationValidationFrameInterval;

                    RayTracingHelper.ResizeScratchBufferForTrace(passData.Shader, passData.PatchCapacity, 1, 1, ref _traceScratch);
                    passData.TraceScratchBuffer = _traceScratch;

                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc((RestirCandidateTemporalPassData data, UnsafeGraphContext cgContext) => RestirGenerateCandidateAndResampleTemporally(data, cgContext));
                }

                using (var builder = renderGraph.AddComputePass("Surface Cache Restir Spatial", out RestirSpatialPassData passData))
                {
                    passData.Shader = _resources.RestirSpatialShader;
                    passData.KernelIndex = _resources.RestirSpatialKernel;
                    passData.ThreadGroupSize = _resources.RestirSpatialKernelGroupSize;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.PatchGeometries = Patches.Geometries;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.CascadeOffsets = Volume.CascadeOffsetBuffer;
                    passData.InputPatchRealizations = Patches.RestirRealizations[0];
                    passData.OutputPatchRealizations = Patches.RestirRealizations[1];
                    passData.PatchCapacity = Patches.Capacity;
                    passData.FrameIdx = frameIdx;
                    passData.VolumeVoxelMinSize = Volume.VoxelMinSize;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.CascadeCount = Volume.CascadeCount;
                    passData.SampleCount = _estimationParams.RestirEstimationSpatialSampleCount;
                    passData.FilterSize = _estimationParams.RestirEstimationSpatialFilterSize;
                    passData.VolumeTargetPos = Volume.TargetPos;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((RestirSpatialPassData data, ComputeGraphContext cgContext) => RestirResampleSpatially(data, cgContext));
                }

                using (var builder = renderGraph.AddComputePass("Surface Cache Restir Estimation", out RestirEstimationPassData passData))
                {
                    passData.KernelIndex = _resources.RestirEstimationKernel;
                    passData.ThreadGroupSize = _resources.RestirEstimationKernelGroupSize;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchGeometries = Patches.Geometries;
                    passData.PatchRealizations = Patches.RestirRealizations[1];
                    passData.PatchCounterSets = Patches.CounterSets;
                    passData.PatchIrradiances = Patches.Irradiances[0];
                    passData.PatchStatistics = Patches.Statistics;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.Shader = _resources.RestirEstimationShader;
                    passData.PatchCapacity = Patches.Capacity;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((RestirEstimationPassData data, ComputeGraphContext cgContext) => RestirEstimate(data, cgContext));
                }
            }
            else if (_estimationParams.Method == SurfaceCacheEstimationMethod.Ris)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache RIS Estimation", out RisEstimationPassData passData))
                {
                    passData.PatchCapacity = Patches.Capacity;
                    passData.Shader = _resources.RisEstimationShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = Patches.Irradiances[0];
                    passData.PatchStatistics = Patches.Statistics;
                    passData.PatchGeometries = Patches.Geometries;
                    passData.PatchCounterSets = Patches.CounterSets;
                    passData.CascadeOffsets = Volume.CascadeOffsetBuffer;
                    passData.World = world;
                    passData.AlbedoBoost = _albedoBoost;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.FrameIdx = frameIdx;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.CascadeCount = Volume.CascadeCount;
                    passData.MultiBounce = _estimationParams.MultiBounce;
                    passData.CandidateCount = _estimationParams.RisEstimationCandidateCount;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.VolumeTargetPos = Volume.TargetPos;
                    passData.TargetFunctionUpdateWeight = _estimationParams.RisEstimationTargetFunctionUpdateWeight;
                    passData.VolumeVoxelMinSize = Volume.VoxelMinSize;
                    passData.PatchAccumulatedLuminances = Patches.RisAccumulatedLuminances;

                    RayTracingHelper.ResizeScratchBufferForTrace(passData.Shader, passData.PatchCapacity, 1, 1, ref _traceScratch);
                    passData.TraceScratchBuffer = _traceScratch;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((RisEstimationPassData data, UnsafeGraphContext cgContext) => RisEstimate(data, cgContext));
                }
            }
            else
            {
                Debug.Assert(false, "Unexpected estimation method.");
            }
        }

        private void RecordScrolling(RenderGraph renderGraph)
        {
            bool cascadesChanged = false;
            Span<int3> oldCascadeOffsets = stackalloc int3[(int)Volume.CascadeCount];

            for (int cascadeIdx = 0; cascadeIdx < Volume.CascadeCount; ++cascadeIdx)
            {
                var camPosVolumeSpace = _volume.TargetPos / (Volume.VoxelMinSize * (1 << cascadeIdx));
                var newOffset = new int3(
                    (int)Math.Round(camPosVolumeSpace.x, MidpointRounding.AwayFromZero),
                    (int)Math.Round(camPosVolumeSpace.y, MidpointRounding.AwayFromZero),
                    (int)Math.Round(camPosVolumeSpace.z, MidpointRounding.AwayFromZero));
                var oldOffset = Volume.CascadeOffsets[cascadeIdx];
                oldCascadeOffsets[cascadeIdx] = oldOffset;
                cascadesChanged = cascadesChanged || math.any(newOffset != oldOffset);
                Volume.CascadeOffsets[cascadeIdx] = newOffset;
            }

            if (cascadesChanged)
            {
                using (var builder = renderGraph.AddComputePass("Surface Cache Scrolling", out ScrollingPassData passData))
                {
                    passData.Shader = _resources.ScrollingShader;
                    passData.KernelIndex = _resources.ScrollingKernel;
                    passData.ThreadGroupSize = _resources.ScrollingKernelGroupSize;
                    passData.CellAllocationMarks = Volume.CellAllocationMarks;
                    passData.CellPatchIndices = Volume.CellPatchIndices;
                    passData.PatchCellIndices = Patches.CellIndices;
                    passData.VolumeSpatialResolution = Volume.SpatialResolution;
                    passData.NewCascadeOffsetsDevice = Volume.CascadeOffsetBuffer;
                    passData.NewCascadeOffsetsHost = Volume.CascadeOffsets;
                    passData.OldCascadeOffsetsHost = oldCascadeOffsets.ToArray();
                    passData.CascadeCount = Volume.CascadeCount;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((ScrollingPassData data, ComputeGraphContext cgContext) => Scroll(data, cgContext));
                }
            }
        }

        static void UniformEstimate(UniformEstimationPassData data, UnsafeGraphContext graphCtx)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);
            var nullableSpotLight = data.World.GetSpotLight();

            if (nullableSpotLight.HasValue)
            {
                var spotLight = nullableSpotLight.Value;
                var shader = data.PunctualLightSamplingShader;
                data.World.GetAccelerationStructure().Bind(cmd, "_RayTracingAccelerationStructure", shader);
                shader.SetVectorParam(cmd, ShaderIDs._SpotLightPosition, spotLight.Position);
                shader.SetVectorParam(cmd, ShaderIDs._SpotLightDirection, spotLight.Direction);
                shader.SetFloatParam(cmd, ShaderIDs._SpotLightCosAngle, Mathf.Cos(spotLight.Angle / 360.0f * 2.0f * Mathf.PI * 0.5f));
                shader.SetFloatParam(cmd, ShaderIDs._FrameIdx, data.FrameIdx);
                shader.SetBufferParam(cmd, ShaderIDs._Samples, data.PunctualLightSamples);
                shader.SetBufferParam(cmd, ShaderIDs._MaterialEntries, data.World.GetMaterialListBuffer());
                shader.SetTextureParam(cmd, ShaderIDs._AlbedoTextures, data.World.GetMaterialAlbedoTextures());
                shader.SetTextureParam(cmd, ShaderIDs._EmissionTextures, data.World.GetMaterialEmissionTextures());
                shader.SetTextureParam(cmd, ShaderIDs._TransmissionTextures, data.World.GetMaterialTransmissionTextures());
                shader.SetFloatParam(cmd, ShaderIDs._AlbedoBoost, data.AlbedoBoost);
                shader.SetFloatParam(cmd, ShaderIDs._MaterialAtlasTexelSize, GetMaterialAtlasTexelSize(data.World.GetMaterialAlbedoTextures()));
                shader.Dispatch(cmd, data.TraceScratchBuffer, data.PunctualLightSampleCount, 1, 1);
            }

            {
                var shader = data.EstimationShader;
                shader.SetBufferParam(cmd, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
                shader.SetBufferParam(cmd, ShaderIDs._PunctualLightSamples, data.PunctualLightSamples);
                shader.SetBufferParam(cmd, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
                shader.SetBufferParam(cmd, ShaderIDs._PatchGeometries, data.PatchGeometries);
                shader.SetBufferParam(cmd, ShaderIDs._PatchStatistics, data.PatchStatistics);
                shader.SetBufferParam(cmd, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
                shader.SetBufferParam(cmd, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
                shader.SetIntParam(cmd, ShaderIDs._FrameIdx, (int)data.FrameIdx);
                shader.SetIntParam(cmd, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
                shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
                shader.SetIntParam(cmd, ShaderIDs._SampleCount, (int)data.SampleCount);
                shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
                shader.SetFloatParam(cmd, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
                shader.SetFloatParam(cmd, ShaderIDs._PunctualLightSampleCount, data.PunctualLightSampleCount);
                shader.SetFloatParam(cmd, ShaderIDs._ShortHysteresis, data.ShortHysteresis);
                shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
                shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                shader.SetVectorParam(cmd, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);
                shader.SetTextureParam(cmd, ShaderIDs._EnvironmentCubemap, data.World.GetEnvironmentTexture());
                shader.SetBufferParam(cmd, ShaderIDs._MaterialEntries, data.World.GetMaterialListBuffer());
                shader.SetTextureParam(cmd, ShaderIDs._AlbedoTextures, data.World.GetMaterialAlbedoTextures());
                shader.SetTextureParam(cmd, ShaderIDs._EmissionTextures, data.World.GetMaterialEmissionTextures());
                shader.SetTextureParam(cmd, ShaderIDs._TransmissionTextures, data.World.GetMaterialTransmissionTextures());
                shader.SetFloatParam(cmd, ShaderIDs._AlbedoBoost, data.AlbedoBoost);
                shader.SetFloatParam(cmd, ShaderIDs._MaterialAtlasTexelSize, GetMaterialAtlasTexelSize(data.World.GetMaterialAlbedoTextures()));
                shader.SetIntParam(cmd, Shader.PropertyToID("_HasSpotLight"), nullableSpotLight.HasValue ? 1 : 0);
                shader.SetVectorParam(cmd, Shader.PropertyToID("_SpotLightIntensity"), nullableSpotLight.HasValue ? nullableSpotLight.Value.Intensity : Vector3.zero);

                var (dirLightDirection, dirLightIntensity) = GetDirectionalLightUniforms(data.World.GetDirectionalLight());
                shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightDirection, dirLightDirection);
                shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightIntensity, dirLightIntensity);

                data.World.GetAccelerationStructure().Bind(cmd, "_RayTracingAccelerationStructure", shader);

                shader.Dispatch(cmd, data.TraceScratchBuffer, data.PatchCapacity, 1, 1);
            }
        }

        static (Vector3, Vector3) GetDirectionalLightUniforms(SurfaceCacheWorld.DirectionalLight? dirLight)
        {
            if (dirLight.HasValue)
                return (dirLight.Value.Direction, dirLight.Value.Intensity);
            else
                return (Vector3.zero, Vector3.zero);
        }

        static void RestirGenerateCandidateAndResampleTemporally(RestirCandidateTemporalPassData data, UnsafeGraphContext graphCtx)
        {
            var shader = data.Shader;
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);

            shader.SetBufferParam(cmd, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            shader.SetBufferParam(cmd, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
            shader.SetBufferParam(cmd, ShaderIDs._PatchGeometries, data.PatchGeometries);
            shader.SetBufferParam(cmd, ShaderIDs._PatchRealizations, data.PatchRealizations);
            shader.SetBufferParam(cmd, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
            shader.SetIntParam(cmd, ShaderIDs._FrameIdx, (int)data.FrameIdx);
            shader.SetIntParam(cmd, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
            shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            shader.SetFloatParam(cmd, ShaderIDs._ConfidenceCap, data.ConfidenceCap);
            shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
            shader.SetIntParam(cmd, ShaderIDs._ValidationFrameInterval, (int)data.ValidationFrameInterval);
            shader.SetFloatParam(cmd, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
            shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            shader.SetVectorParam(cmd, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);
            shader.SetTextureParam(cmd, ShaderIDs._EnvironmentCubemap, data.World.GetEnvironmentTexture());
            shader.SetBufferParam(cmd, ShaderIDs._MaterialEntries, data.World.GetMaterialListBuffer());
            shader.SetTextureParam(cmd, ShaderIDs._AlbedoTextures, data.World.GetMaterialAlbedoTextures());
            shader.SetTextureParam(cmd, ShaderIDs._EmissionTextures, data.World.GetMaterialEmissionTextures());
            shader.SetTextureParam(cmd, ShaderIDs._TransmissionTextures, data.World.GetMaterialTransmissionTextures());
            shader.SetFloatParam(cmd, ShaderIDs._AlbedoBoost, data.AlbedoBoost);
            shader.SetFloatParam(cmd, ShaderIDs._MaterialAtlasTexelSize, GetMaterialAtlasTexelSize(data.World.GetMaterialAlbedoTextures()));

            var (dirLightIntensity, dirLightDirection) = GetDirectionalLightUniforms(data.World.GetDirectionalLight());
            shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightIntensity, dirLightIntensity);
            shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightDirection, dirLightDirection);

            data.World.GetAccelerationStructure().Bind(cmd, "_RayTracingAccelerationStructure", data.Shader);

            shader.Dispatch(cmd, data.TraceScratchBuffer, data.PatchCapacity, 1, 1);
        }

        static void RestirResampleSpatially(RestirSpatialPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._InputPatchRealizations, data.InputPatchRealizations);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._OutputPatchRealizations, data.OutputPatchRealizations);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
            cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIdx);
            cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
            cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
            cmd.SetComputeIntParam(shader, ShaderIDs._FilterSize, (int)data.FilterSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

            uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
            cmd.DispatchCompute(data.Shader, data.KernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
        }

        static void RestirEstimate(RestirEstimationPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchRealizations, data.PatchRealizations);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeFloatParam(shader, ShaderIDs._ShortHysteresis, data.ShortHysteresis);

            uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
            cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
        }

        static void RisEstimate(RisEstimationPassData data, UnsafeGraphContext graphCtx)
        {
            var shader = data.Shader;
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);

            shader.SetBufferParam(cmd, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            shader.SetBufferParam(cmd, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
            shader.SetBufferParam(cmd, ShaderIDs._PatchStatistics, data.PatchStatistics);
            shader.SetBufferParam(cmd, ShaderIDs._PatchGeometries, data.PatchGeometries);
            shader.SetBufferParam(cmd, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
            shader.SetBufferParam(cmd, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
            shader.SetIntParam(cmd, ShaderIDs._FrameIdx, (int)data.FrameIdx);
            shader.SetIntParam(cmd, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
            shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            shader.SetIntParam(cmd, ShaderIDs._CandidateCount, (int)data.CandidateCount);
            shader.SetFloatParam(cmd, ShaderIDs._TargetFunctionUpdateWeight, data.TargetFunctionUpdateWeight);
            shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
            shader.SetFloatParam(cmd, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
            shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            shader.SetBufferParam(cmd, ShaderIDs._PatchAccumulatedLuminances, data.PatchAccumulatedLuminances);
            shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            shader.SetFloatParam(cmd, ShaderIDs._ShortHysteresis, data.ShortHysteresis);
            shader.SetVectorParam(cmd, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);
            shader.SetTextureParam(cmd, ShaderIDs._EnvironmentCubemap, data.World.GetEnvironmentTexture());
            shader.SetBufferParam(cmd, ShaderIDs._MaterialEntries, data.World.GetMaterialListBuffer());
            shader.SetTextureParam(cmd, ShaderIDs._AlbedoTextures, data.World.GetMaterialAlbedoTextures());
            shader.SetTextureParam(cmd, ShaderIDs._EmissionTextures, data.World.GetMaterialEmissionTextures());
            shader.SetTextureParam(cmd, ShaderIDs._TransmissionTextures, data.World.GetMaterialTransmissionTextures());
            shader.SetFloatParam(cmd, ShaderIDs._AlbedoBoost, data.AlbedoBoost);
            shader.SetFloatParam(cmd, ShaderIDs._MaterialAtlasTexelSize, GetMaterialAtlasTexelSize(data.World.GetMaterialAlbedoTextures()));

            var (dirLightIntensity, dirLightDirection) = GetDirectionalLightUniforms(data.World.GetDirectionalLight());
            shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightIntensity, dirLightIntensity);
            shader.SetVectorParam(cmd, ShaderIDs._DirectionalLightDirection, dirLightDirection);

            data.World.GetAccelerationStructure().Bind(cmd, "_RayTracingAccelerationStructure", data.Shader);

            shader.Dispatch(cmd, data.TraceScratchBuffer, data.PatchCapacity, 1, 1);
        }

        static void Defrag(DefragPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;

            cmd.EnableKeyword(shader, data.Keyword);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances0, data.PatchIrradiances0);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances1, data.PatchIrradiances1);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);

            uint iterationEnd = data.IterationOffset + data.IterationCount;
            uint flipflop = data.RingConfigStartFlipflop;
            for (uint iterationIndex = data.IterationOffset; iterationIndex < iterationEnd; ++iterationIndex)
            {
                uint readOffset = SurfaceCacheRingConfig.GetOffsetA(flipflop);
                uint writeOffset = SurfaceCacheRingConfig.GetOffsetB(flipflop);
                uint patchOffset = iterationIndex % 2 == 0 ? data.EvenIterationPatchOffset : data.OddIterationPatchOffset;

                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigReadOffset, (int)readOffset);
                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigWriteOffset, (int)writeOffset);
                cmd.SetComputeIntParam(shader, ShaderIDs._PatchOffset, (int)patchOffset);

                uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, (int)groupCount.z);

                flipflop = SurfaceCacheRingConfig.Flip(flipflop);
            }

            cmd.DisableKeyword(shader, data.Keyword);
        }

        static void FilterSpatially(SpatialFilterPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._InputPatchIrradiances, data.InputPatchIrradiances);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._OutputPatchIrradiances, data.OutputPatchIrradiances);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
            cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)data.FrameIdx);
            cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            cmd.SetComputeIntParam(shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
            cmd.SetComputeFloatParam(shader, ShaderIDs._VolumeVoxelMinSize, data.VolumeVoxelMinSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
            cmd.SetComputeFloatParam(shader, ShaderIDs._Radius, data.Radius);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeVectorParam(shader, ShaderIDs._VolumeTargetPos, data.VolumeTargetPos);

            uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
            cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
        }

        static void FilterTemporally(TemporalFilterPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._InputPatchIrradiances, data.InputPatchIrradiances);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._OutputPatchIrradiances, data.OutputPatchIrradiances);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeFloatParam(shader, ShaderIDs._ShortHysteresis, data.ShortHysteresis);

            uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
            cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
        }

        public void Dispose()
        {
            _volume.Dispose();
            _ringConfig.Dispose();
            _patches.Dispose();
            _punctualLightSamples.Dispose();
            _traceScratch?.Dispose();
        }

        private static uint3 DivUp(uint3 x, uint3 y) => (x + y - 1) / y;

        static void Scroll(ScrollingPassData data, ComputeGraphContext cgContext)
        {
            Debug.Assert(data.NewCascadeOffsetsHost.Length == data.OldCascadeOffsetsHost.Length);
            uint cascadeCount = (uint)data.NewCascadeOffsetsHost.Length;

            var cmd = cgContext.cmd;
            cmd.SetBufferData(data.NewCascadeOffsetsDevice, data.NewCascadeOffsetsHost);
            cmd.SetComputeBufferParam(data.Shader, data.KernelIndex, ShaderIDs._CellAllocationMarks, data.CellAllocationMarks);
            cmd.SetComputeBufferParam(data.Shader, data.KernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            cmd.SetComputeBufferParam(data.Shader, data.KernelIndex, ShaderIDs._NewCascadeOffsets, data.NewCascadeOffsetsDevice);
            cmd.SetComputeBufferParam(data.Shader, data.KernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
            cmd.SetComputeIntParam(data.Shader, ShaderIDs._VolumeSpatialResolution, (int)data.VolumeSpatialResolution);
            cmd.SetComputeIntParam(data.Shader, ShaderIDs._CascadeCount, (int)data.CascadeCount);

            {
                var oldCascadeOffsetsInts = new int[data.OldCascadeOffsetsHost.Length * 4];
                for (uint cascadeIdx = 0; cascadeIdx < cascadeCount; ++cascadeIdx)
                {
                    oldCascadeOffsetsInts[cascadeIdx * 4] = data.OldCascadeOffsetsHost[cascadeIdx][0];
                    oldCascadeOffsetsInts[cascadeIdx * 4 + 1] = data.OldCascadeOffsetsHost[cascadeIdx][1];
                    oldCascadeOffsetsInts[cascadeIdx * 4 + 2] = data.OldCascadeOffsetsHost[cascadeIdx][2];
                    oldCascadeOffsetsInts[cascadeIdx * 4 + 3] = 0;
                }
                cmd.SetComputeIntParams(data.Shader, ShaderIDs._OldCascadeOffsets, oldCascadeOffsetsInts);
            }

            uint3 groupCount = DivUp(new uint3(data.VolumeSpatialResolution, data.VolumeSpatialResolution, data.VolumeSpatialResolution * data.CascadeCount), data.ThreadGroupSize);
            cmd.DispatchCompute(data.Shader, data.KernelIndex, (int)groupCount.x, (int)groupCount.y, (int)groupCount.z);
        }

        static void Evict(EvictionPassData passData, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = passData.Shader;
            var kernelIndex = passData.KernelIndex;
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, passData.RingConfigBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, passData.PatchCounterSets);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, passData.PatchCellIndices);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellAllocationMarks, passData.CellAllocationMarks);
            cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, passData.CellPatchIndices);
            cmd.SetComputeIntParam(shader, ShaderIDs._FrameIdx, (int)passData.FrameIdx);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)passData.RingConfigOffset);

            uint3 groupCount = DivUp(new uint3(passData.PatchCapacity, 1, 1), passData.ThreadGroupSize);
            cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, 1);
        }

        static float GetMaterialAtlasTexelSize(RenderTexture albedoTextures)
        {
            Debug.Assert(albedoTextures.width == albedoTextures.height, "Atlas textures are assumed to be square.");
            return 1.0f / albedoTextures.width;
        }
    }
}

#endif
