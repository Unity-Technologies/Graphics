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

    internal class SurfaceCacheGrid : IDisposable
    {
        public const int InvalidOffset = Int32.MaxValue;
        public const uint InvalidPatchIndex = UInt32.MaxValue; // Must match HLSL side.

        public readonly uint GridSize;
        public readonly uint CascadeCount;
        public readonly float VoxelMinSize;
        public Vector3 TargetPos = Vector3.zero;

        public int3[] CascadeOffsets;

        public GraphicsBuffer CascadeOffsetBuffer;

        public GraphicsBuffer CellAllocationMarks;
        public GraphicsBuffer CellPatchIndices;

        internal SurfaceCacheGrid(uint gridSize, uint cascadeCount, float voxelMinSize)
        {
            GridSize = gridSize;
            CascadeCount = cascadeCount;
            VoxelMinSize = voxelMinSize;
            CascadeOffsets = new int3[cascadeCount];
            for (int i = 0; i < cascadeCount; ++i)
            {
                CascadeOffsets[i] = new int3(InvalidOffset, InvalidOffset, InvalidOffset);
            }
            CascadeOffsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)cascadeCount, sizeof(int) * 3);

            const uint angularResolution = 4; // Must match HLSL side.
            uint cellCount = gridSize * gridSize * gridSize * angularResolution * angularResolution * cascadeCount;
            var initBuffer = new uint[cellCount];

            CellAllocationMarks = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(cellCount), sizeof(uint));
            CellAllocationMarks.SetData(initBuffer);
            CellPatchIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)(cellCount), sizeof(uint));
            for (int i = 0; i < cellCount; ++i)
                initBuffer[i] = InvalidPatchIndex;
            CellPatchIndices.SetData(initBuffer);
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

        internal bool LoadFromRenderPipeResources(RayTracingContext rtContext)
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
            DefragShader.EnableKeyword("SUB_GROUP_SIZE_" + SubGroupSize);
            DefragKernel = DefragShader.FindKernel("Defrag");
            DefragShader.GetKernelThreadGroupSizes(DefragKernel, out DefragKernelGroupSize.x, out DefragKernelGroupSize.y, out DefragKernelGroupSize.z);

            Object uniformEstimationUnifiedObj;
            Object restirCandidateTemporalUnifiedObj;
            Object risEstimationObj;
            if (rtContext.BackendType == RayTracingBackend.Compute)
            {
                uniformEstimationUnifiedObj = rpResources.uniformEstimationComputeShader;
                restirCandidateTemporalUnifiedObj = rpResources.restirCandidateTemporalComputeShader;
                risEstimationObj = rpResources.risEstimationComputeShader;
            }
            else
            {
                uniformEstimationUnifiedObj = rpResources.uniformEstimationRayTracingShader;
                restirCandidateTemporalUnifiedObj = rpResources.restirCandidateTemporalRayTracingShader;
                risEstimationObj = rpResources.risEstimationRayTracingShader;
            }

            UniformEstimationShader = rtContext.CreateRayTracingShader(uniformEstimationUnifiedObj);
            RestirCandidateTemporalShader = rtContext.CreateRayTracingShader(restirCandidateTemporalUnifiedObj);
            RisEstimationShader = rtContext.CreateRayTracingShader(risEstimationObj);

            return true;
        }
    }

    internal class SurfaceCache : IDisposable
    {
        public const uint CascadeMax = 8;
        private readonly SurfaceCachePatchList _patchList;
        private readonly SurfaceCacheGrid _grid;
        private readonly SurfaceCacheRingConfig _ringConfig;
        private readonly SurfaceCacheResourceSet _resources;
        private GraphicsBuffer _traceScratch;

        // Light transport settings.
        private readonly SurfaceCacheEstimationMethod _estimationMethod;
        private readonly bool _multiBounce;
        private readonly uint _restirEstimationConfidenceCap;
        private readonly uint _restirEstimationSpatialSampleCount;
        private readonly float _restirEstimationSpatialFilterSize;
        private readonly uint _restirEstimationValidationFrameInterval;
        private readonly uint _uniformEstimationSampleCount;
        private readonly uint _risEstimationCandidateCount;
        private readonly float _risEstimationTargetFunctionUpdateWeight;

        // Patch Filtering
        readonly private float _shortHysteresis;
        readonly private bool _spatialFilterEnabled;
        readonly private uint _spatialFilterSampleCount;
        readonly private float _spatialFilterRadius;
        readonly private bool _temporalPostFilterEnabled;

        // Patch Maintenance
        readonly private uint _defragCount = 2;

        public SurfaceCachePatchList PatchList => _patchList;
        public SurfaceCacheGrid Grid => _grid;
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
            internal uint GridSize;
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
            internal IRayTracingShader Shader;
            internal GraphicsBuffer RingConfigBuffer;
            internal GraphicsBuffer PatchIrradiances;
            internal GraphicsBuffer PatchGeometries;
            internal GraphicsBuffer PatchStatistics;
            internal GraphicsBuffer PatchCounterSets;
            internal GraphicsBuffer CellPatchIndices;
            internal GraphicsBuffer CascadeOffsets;
            internal PathTracing.Core.World World;
            internal uint FrameIdx;
            internal uint GridSize;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal float ShortHysteresis;
            internal uint RingConfigOffset;
            internal uint SampleCount;
            internal float VoxelMinSize;
            internal Vector3 GridTargetPos;
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
            internal Vector3 GridTargetPos;
            internal PathTracing.Core.World World;
            internal uint FrameIdx;
            internal uint GridSize;
            internal uint RingConfigOffset;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal uint ConfidenceCap;
            internal float VoxelMinSize;
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
            internal uint GridSize;
            internal uint CascadeCount;
            internal uint RingConfigOffset;
            internal float VoxelMinSize;
            internal uint SampleCount;
            internal float FilterSize;
            internal Vector3 GridTargetPos;
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
            internal PathTracing.Core.World World;
            internal uint FrameIdx;
            internal uint GridSize;
            internal uint CascadeCount;
            internal bool MultiBounce;
            internal uint CandidateCount;
            internal uint RingConfigOffset;
            internal float ShortHysteresis;
            internal Vector3 GridTargetPos;
            internal float TargetFunctionUpdateWeight;
            internal float VoxelMinSize;
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
            internal uint GridSize;
            internal float VoxelMinSize;
            internal uint SampleCount;
            internal float Radius;
            internal uint RingConfigOffset;
            internal Vector3 GridTargetPos;
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
            public static readonly int _GridTargetPos = Shader.PropertyToID("_GridTargetPos");
            public static readonly int _NewCascadeOffsets = Shader.PropertyToID("_NewCascadeOffsets");
            public static readonly int _OldCascadeOffsets = Shader.PropertyToID("_OldCascadeOffsets");
            public static readonly int _GridSize = Shader.PropertyToID("_GridSize");
            public static readonly int _CascadeCount = Shader.PropertyToID("_CascadeCount");
            public static readonly int _ConfidenceCap = Shader.PropertyToID("_ConfidenceCap");
            public static readonly int _CandidateCount = Shader.PropertyToID("_CandidateCount");
            public static readonly int _TargetFunctionUpdateWeight = Shader.PropertyToID("_TargetFunctionUpdateWeight");
            public static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
            public static readonly int _FilterSize = Shader.PropertyToID("_FilterSize");
            public static readonly int _MultiBounce = Shader.PropertyToID("_MultiBounce");
            public static readonly int _ValidationFrameInterval = Shader.PropertyToID("_ValidationFrameInterval");
            public static readonly int _VoxelMinSize = Shader.PropertyToID("_VoxelMinSize");
            public static readonly int _ShortHysteresis = Shader.PropertyToID("_ShortHysteresis");
            public static readonly int _PatchCellIndices = Shader.PropertyToID("_PatchCellIndices");
            public static readonly int _RingConfigBuffer = Shader.PropertyToID("_RingConfigBuffer");
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
            public static readonly int _PatchRealizations = Shader.PropertyToID("_PatchRealizations");
            public static readonly int _PatchAccumulatedLuminances = Shader.PropertyToID("_PatchAccumulatedLuminances");
            public static readonly int _InputPatchRealizations = Shader.PropertyToID("_InputPatchRealizations");
            public static readonly int _OutputPatchRealizations = Shader.PropertyToID("_OutputPatchRealizations");

            public static readonly int _RingConfigOffset = Shader.PropertyToID("_RingConfigOffset");
        }

        public SurfaceCache(
            SurfaceCacheResourceSet resources,
            uint gridSize, float voxelMinSize, uint cascadeCount, SurfaceCacheEstimationMethod estimationMethod,
            bool multiBounce,
            uint restirEstimationConfidenceCap,
            uint restirEstimationSpatialSampleCount,
            float restirEstimationSpatialFilterSize,
            uint restirEstimationValidationFrameInterval,
            uint uniformEstimationSampleCount,
            uint risEstimationCandidateCount,
            float risEstimationTargetFunctionUpdateWeight,
            float temporalSmoothing,
            bool spatialFilterEnabled,
            uint spatialFilterSampleCount,
            float spatialFilterRadius,
            bool temporalPostFilterEnabled)
        {
            Debug.Assert(cascadeCount != 0);
            Debug.Assert(cascadeCount <= CascadeMax);
            Debug.Assert(0.0f <= temporalSmoothing);
            Debug.Assert(temporalSmoothing <= 1.0f);

            uint patchCapacity = 65536; // Must match HLSL side constant.
            Debug.Assert((UInt64)4294967296 % (UInt64)patchCapacity == 0, "Patch Capacity must be a divisor of 2^32."); // This property is required by the HLSL side ring buffer allocation logic.

            _resources = resources;
            _grid = new SurfaceCacheGrid(gridSize, cascadeCount, voxelMinSize);
            _ringConfig = new SurfaceCacheRingConfig();
            _patchList = new SurfaceCachePatchList(patchCapacity, estimationMethod);

            _estimationMethod = estimationMethod;
            _multiBounce = multiBounce;
            _restirEstimationConfidenceCap = restirEstimationConfidenceCap;
            _restirEstimationSpatialSampleCount = restirEstimationSpatialSampleCount;
            _restirEstimationSpatialFilterSize = restirEstimationSpatialFilterSize;
            _restirEstimationValidationFrameInterval = restirEstimationValidationFrameInterval;
            _uniformEstimationSampleCount = uniformEstimationSampleCount;
            _risEstimationCandidateCount = risEstimationCandidateCount;
            _risEstimationTargetFunctionUpdateWeight = risEstimationTargetFunctionUpdateWeight;

            Debug.Assert(0.0f <= temporalSmoothing && temporalSmoothing <= 1.0f);
            float shortHysteresis = Mathf.Lerp(0.75f, 0.95f, temporalSmoothing);
            _shortHysteresis = shortHysteresis;
            _spatialFilterEnabled = spatialFilterEnabled;
            _spatialFilterSampleCount = spatialFilterSampleCount;
            _spatialFilterRadius = spatialFilterRadius;
            _temporalPostFilterEnabled = temporalPostFilterEnabled;

            _defragCount = 2;
        }

        public void RecordPreparation(RenderGraph renderGraph, uint frameIdx)
        {
            RecordScrolling(renderGraph);
            RecordEviction(renderGraph, frameIdx);
            RecordDefragmentation(renderGraph, frameIdx);
        }

        internal uint RecordPatchUpdate(RenderGraph renderGraph, uint frameIdx, PathTracing.Core.World world)
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
                passData.KernelIndex = _resources.DefragKernel;
                passData.PatchCapacity = PatchList.Capacity;
                passData.RingConfigStartFlipflop = RingConfig.FlipFlop;
                passData.ThreadGroupSize = _resources.DefragKernelGroupSize;
                passData.RingConfigBuffer = RingConfig.Buffer;
                passData.PatchCellIndices = PatchList.CellIndices;
                passData.PatchCounterSets = PatchList.CounterSets;
                passData.PatchIrradiances0 = PatchList.Irradiances[0];
                passData.PatchIrradiances1 = PatchList.Irradiances[2];
                passData.PatchGeometries = PatchList.Geometries;
                passData.PatchStatistics = PatchList.Statistics;
                passData.CellPatchIndices = Grid.CellPatchIndices;
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
                passData.PatchCounterSets = PatchList.CounterSets;
                passData.PatchCellIndices = PatchList.CellIndices;
                passData.CellAllocationMarks = Grid.CellAllocationMarks;
                passData.CellPatchIndices = Grid.CellPatchIndices;
                passData.PatchCapacity = PatchList.Capacity;
                passData.FrameIdx = frameIdx;

                builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                builder.SetRenderFunc((EvictionPassData data, ComputeGraphContext cgContext) => Evict(data, cgContext));
            }
        }

        private uint RecordFiltering(RenderGraph renderGraph, uint frameIdx)
        {
            uint outputIrradianceBufferIdx = 0;
            if (_spatialFilterEnabled)
            {
                outputIrradianceBufferIdx = 1;
                using (var builder = renderGraph.AddComputePass("Surface Cache Spatial Filter", out SpatialFilterPassData passData))
                {
                    passData.InputPatchIrradiances = PatchList.Irradiances[0];
                    passData.OutputPatchIrradiances = PatchList.Irradiances[1];
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.Shader = _resources.SpatialFilteringShader;
                    passData.KernelIndex = _resources.SpatialFilteringKernel;
                    passData.ThreadGroupSize = _resources.SpatialFilteringKernelGroupSize;
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.FrameIdx = frameIdx;
                    passData.CascadeCount = Grid.CascadeCount;
                    passData.GridSize = Grid.GridSize;
                    passData.VoxelMinSize = Grid.VoxelMinSize;
                    passData.SampleCount = _spatialFilterSampleCount;
                    passData.Radius = _spatialFilterRadius;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.CascadeOffsets = Grid.CascadeOffsetBuffer;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.GridTargetPos = Grid.TargetPos;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((SpatialFilterPassData data, ComputeGraphContext cgContext) => FilterSpatially(data, cgContext));
                }
            }

            if (_temporalPostFilterEnabled)
            {
                using (var builder = renderGraph.AddComputePass("Surface Cache Temporal Filter", out TemporalFilterPassData passData))
                {
                    passData.Shader = _resources.TemporalFilteringShader;
                    passData.KernelIndex = _resources.TemporalFilteringKernel;
                    passData.ThreadGroupSize = _resources.TemporalFilteringKernelGroupSize;
                    passData.InputPatchIrradiances = PatchList.Irradiances[outputIrradianceBufferIdx];
                    passData.OutputPatchIrradiances = PatchList.Irradiances[2];
                    passData.PatchStatistics = PatchList.Statistics;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchCounterSets = PatchList.CounterSets;
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((TemporalFilterPassData data, ComputeGraphContext cgContext) => FilterTemporally(data, cgContext));
                }
                outputIrradianceBufferIdx = 2;
            }

            return outputIrradianceBufferIdx;
        }

        private void RecordEstimation(RenderGraph renderGraph, uint frameIdx, PathTracing.Core.World world)
        {
            if (_estimationMethod == SurfaceCacheEstimationMethod.Uniform)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache Uniform Estimation", out UniformEstimationPassData passData))
                {
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.Shader = _resources.UniformEstimationShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = PatchList.Irradiances[0];
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.PatchStatistics = PatchList.Statistics;
                    passData.PatchCounterSets = PatchList.CounterSets;
                    passData.World = world;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.GridTargetPos = Grid.TargetPos;
                    passData.FrameIdx = frameIdx;
                    passData.GridSize = Grid.GridSize;
                    passData.CascadeOffsets = Grid.CascadeOffsetBuffer;
                    passData.CascadeCount = Grid.CascadeCount;
                    passData.MultiBounce = _multiBounce;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.SampleCount = _uniformEstimationSampleCount;
                    passData.VoxelMinSize = Grid.VoxelMinSize;

                    RayTracingHelper.ResizeScratchBufferForTrace(passData.Shader, passData.PatchCapacity, 1, 1, ref _traceScratch);
                    passData.TraceScratchBuffer = _traceScratch;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((UniformEstimationPassData data, UnsafeGraphContext cgContext) => UniformEstimate(data, cgContext));
                }
            }
            else if (_estimationMethod == SurfaceCacheEstimationMethod.Restir)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache Restir Candidate + Temporal", out RestirCandidateTemporalPassData passData))
                {
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.Shader = _resources.RestirCandidateTemporalShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = PatchList.Irradiances[0];
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.PatchRealizations = PatchList.RestirRealizations[0];
                    passData.CascadeOffsets = Grid.CascadeOffsetBuffer;
                    passData.World = world;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.GridTargetPos = Grid.TargetPos;
                    passData.FrameIdx = frameIdx;
                    passData.GridSize = Grid.GridSize;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.CascadeCount = Grid.CascadeCount;
                    passData.MultiBounce = _multiBounce;
                    passData.ConfidenceCap = _restirEstimationConfidenceCap;
                    passData.VoxelMinSize = Grid.VoxelMinSize;
                    passData.ValidationFrameInterval = _restirEstimationValidationFrameInterval;

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
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.CascadeOffsets = Grid.CascadeOffsetBuffer;
                    passData.InputPatchRealizations = PatchList.RestirRealizations[0];
                    passData.OutputPatchRealizations = PatchList.RestirRealizations[1];
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.FrameIdx = frameIdx;
                    passData.VoxelMinSize = Grid.VoxelMinSize;
                    passData.GridSize = Grid.GridSize;
                    passData.CascadeCount = Grid.CascadeCount;
                    passData.SampleCount = _restirEstimationSpatialSampleCount;
                    passData.FilterSize = _restirEstimationSpatialFilterSize;
                    passData.GridTargetPos = Grid.TargetPos;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((RestirSpatialPassData data, ComputeGraphContext cgContext) => RestirResampleSpatially(data, cgContext));
                }

                using (var builder = renderGraph.AddComputePass("Surface Cache Restir Estimation", out RestirEstimationPassData passData))
                {
                    passData.KernelIndex = _resources.RestirEstimationKernel;
                    passData.ThreadGroupSize = _resources.RestirEstimationKernelGroupSize;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.PatchRealizations = PatchList.RestirRealizations[1];
                    passData.PatchCounterSets = PatchList.CounterSets;
                    passData.PatchIrradiances = PatchList.Irradiances[0];
                    passData.PatchStatistics = PatchList.Statistics;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.Shader = _resources.RestirEstimationShader;
                    passData.PatchCapacity = PatchList.Capacity;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((RestirEstimationPassData data, ComputeGraphContext cgContext) => RestirEstimate(data, cgContext));
                }
            }
            else if (_estimationMethod == SurfaceCacheEstimationMethod.Ris)
            {
                using (var builder = renderGraph.AddUnsafePass("Surface Cache RIS Estimation", out RisEstimationPassData passData))
                {
                    passData.PatchCapacity = PatchList.Capacity;
                    passData.Shader = _resources.RisEstimationShader;
                    passData.RingConfigBuffer = RingConfig.Buffer;
                    passData.PatchIrradiances = PatchList.Irradiances[0];
                    passData.PatchStatistics = PatchList.Statistics;
                    passData.PatchGeometries = PatchList.Geometries;
                    passData.PatchCounterSets = PatchList.CounterSets;
                    passData.CascadeOffsets = Grid.CascadeOffsetBuffer;
                    passData.World = world;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.FrameIdx = frameIdx;
                    passData.GridSize = Grid.GridSize;
                    passData.CascadeCount = Grid.CascadeCount;
                    passData.MultiBounce = _multiBounce;
                    passData.CandidateCount = _risEstimationCandidateCount;
                    passData.RingConfigOffset = RingConfig.OffsetA;
                    passData.ShortHysteresis = _shortHysteresis;
                    passData.GridTargetPos = Grid.TargetPos;
                    passData.TargetFunctionUpdateWeight = _risEstimationTargetFunctionUpdateWeight;
                    passData.VoxelMinSize = Grid.VoxelMinSize;
                    passData.PatchAccumulatedLuminances = PatchList.RisAccumulatedLuminances;

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
            Span<int3> oldCascadeOffsets = stackalloc int3[(int)Grid.CascadeCount];

            for (int cascadeIdx = 0; cascadeIdx < Grid.CascadeCount; ++cascadeIdx)
            {
                var camPosGridSpace = _grid.TargetPos / (Grid.VoxelMinSize * (1 << cascadeIdx));
                var newOffset = new int3(
                    (int)Math.Round(camPosGridSpace.x, MidpointRounding.AwayFromZero),
                    (int)Math.Round(camPosGridSpace.y, MidpointRounding.AwayFromZero),
                    (int)Math.Round(camPosGridSpace.z, MidpointRounding.AwayFromZero));
                var oldOffset = Grid.CascadeOffsets[cascadeIdx];
                oldCascadeOffsets[cascadeIdx] = oldOffset;
                cascadesChanged = cascadesChanged || math.any(newOffset != oldOffset);
                Grid.CascadeOffsets[cascadeIdx] = newOffset;
            }

            if (cascadesChanged)
            {
                using (var builder = renderGraph.AddComputePass("Surface Cache Scrolling", out ScrollingPassData passData))
                {
                    passData.Shader = _resources.ScrollingShader;
                    passData.KernelIndex = _resources.ScrollingKernel;
                    passData.ThreadGroupSize = _resources.ScrollingKernelGroupSize;
                    passData.CellAllocationMarks = Grid.CellAllocationMarks;
                    passData.CellPatchIndices = Grid.CellPatchIndices;
                    passData.PatchCellIndices = PatchList.CellIndices;
                    passData.GridSize = Grid.GridSize;
                    passData.NewCascadeOffsetsDevice = Grid.CascadeOffsetBuffer;
                    passData.NewCascadeOffsetsHost = Grid.CascadeOffsets;
                    passData.OldCascadeOffsetsHost = oldCascadeOffsets.ToArray();
                    passData.CascadeCount = Grid.CascadeCount;

                    builder.AllowGlobalStateModification(true); // Set to ensure ordering.
                    builder.SetRenderFunc((ScrollingPassData data, ComputeGraphContext cgContext) => Scroll(data, cgContext));
                }
            }
        }

        static void UniformEstimate(UniformEstimationPassData data, UnsafeGraphContext graphCtx)
        {
            var shader = data.Shader;
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphCtx.cmd);

            shader.SetBufferParam(cmd, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
            shader.SetBufferParam(cmd, ShaderIDs._PatchIrradiances, data.PatchIrradiances);
            shader.SetBufferParam(cmd, ShaderIDs._PatchGeometries, data.PatchGeometries);
            shader.SetBufferParam(cmd, ShaderIDs._PatchStatistics, data.PatchStatistics);
            shader.SetBufferParam(cmd, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
            shader.SetBufferParam(cmd, ShaderIDs._CascadeOffsets, data.CascadeOffsets);
            shader.SetIntParam(cmd, ShaderIDs._FrameIdx, (int)data.FrameIdx);
            shader.SetIntParam(cmd, ShaderIDs._GridSize, (int)data.GridSize);
            shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            shader.SetIntParam(cmd, ShaderIDs._SampleCount, (int)data.SampleCount);
            shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
            shader.SetFloatParam(cmd, ShaderIDs._VoxelMinSize, data.VoxelMinSize);
            shader.SetFloatParam(cmd, ShaderIDs._ShortHysteresis, data.ShortHysteresis);
            shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            shader.SetVectorParam(cmd, ShaderIDs._GridTargetPos, data.GridTargetPos);

            PathTracing.Core.Util.BindWorld(cmd, data.Shader, data.World, 32);
            shader.SetFloatParam(cmd, UnityEngine.PathTracing.Core.Util.ShaderProperties.AlbedoBoost, 1.0f);

            shader.Dispatch(cmd, data.TraceScratchBuffer, data.PatchCapacity, 1, 1);
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
            shader.SetIntParam(cmd, ShaderIDs._GridSize, (int)data.GridSize);
            shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            shader.SetFloatParam(cmd, ShaderIDs._ConfidenceCap, (float)data.ConfidenceCap);
            shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
            shader.SetIntParam(cmd, ShaderIDs._ValidationFrameInterval, (int)data.ValidationFrameInterval);
            shader.SetFloatParam(cmd, ShaderIDs._VoxelMinSize, data.VoxelMinSize);
            shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            shader.SetVectorParam(cmd, ShaderIDs._GridTargetPos, data.GridTargetPos);

            PathTracing.Core.Util.BindWorld(cmd, data.Shader, data.World, 32);
            shader.SetFloatParam(cmd, PathTracing.Core.Util.ShaderProperties.AlbedoBoost, 1.0f);

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
            cmd.SetComputeFloatParam(shader, ShaderIDs._VoxelMinSize, data.VoxelMinSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._GridSize, (int)data.GridSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
            cmd.SetComputeIntParam(shader, ShaderIDs._FilterSize, (int)data.FilterSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeVectorParam(shader, ShaderIDs._GridTargetPos, data.GridTargetPos);

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
            shader.SetIntParam(cmd, ShaderIDs._GridSize, (int)data.GridSize);
            shader.SetIntParam(cmd, ShaderIDs._CascadeCount, (int)data.CascadeCount);
            shader.SetIntParam(cmd, ShaderIDs._CandidateCount, (int)data.CandidateCount);
            shader.SetFloatParam(cmd, ShaderIDs._TargetFunctionUpdateWeight, data.TargetFunctionUpdateWeight);
            shader.SetIntParam(cmd, ShaderIDs._MultiBounce, data.MultiBounce ? 1 : 0);
            shader.SetFloatParam(cmd, ShaderIDs._VoxelMinSize, data.VoxelMinSize);
            shader.SetBufferParam(cmd, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
            shader.SetBufferParam(cmd, ShaderIDs._PatchAccumulatedLuminances, data.PatchAccumulatedLuminances);
            shader.SetIntParam(cmd, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            shader.SetFloatParam(cmd, ShaderIDs._ShortHysteresis, data.ShortHysteresis);
            shader.SetVectorParam(cmd, ShaderIDs._GridTargetPos, data.GridTargetPos);

            PathTracing.Core.Util.BindWorld(cmd, data.Shader, data.World, 32);
            shader.SetFloatParam(cmd, PathTracing.Core.Util.ShaderProperties.AlbedoBoost, 1.0f);

            shader.Dispatch(cmd, data.TraceScratchBuffer, data.PatchCapacity, 1, 1);
        }

        static void Defrag(DefragPassData data, ComputeGraphContext cgContext)
        {
            var cmd = cgContext.cmd;
            var shader = data.Shader;
            var kernelIndex = data.KernelIndex;

            uint iterationEnd = data.IterationOffset + data.IterationCount;
            uint flipflop = data.RingConfigStartFlipflop;
            for (uint iterationIndex = data.IterationOffset; iterationIndex < iterationEnd; ++iterationIndex)
            {
                uint readOffset = SurfaceCacheRingConfig.GetOffsetA(flipflop);
                uint writeOffset = SurfaceCacheRingConfig.GetOffsetB(flipflop);
                uint patchOffset = iterationIndex % 2 == 0 ? data.EvenIterationPatchOffset : data.OddIterationPatchOffset;

                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._RingConfigBuffer, data.RingConfigBuffer);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCellIndices, data.PatchCellIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._CellPatchIndices, data.CellPatchIndices);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchCounterSets, data.PatchCounterSets);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances0, data.PatchIrradiances0);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchIrradiances1, data.PatchIrradiances1);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchGeometries, data.PatchGeometries);
                cmd.SetComputeBufferParam(shader, kernelIndex, ShaderIDs._PatchStatistics, data.PatchStatistics);
                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigReadOffset, (int)readOffset);
                cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigWriteOffset, (int)writeOffset);
                cmd.SetComputeIntParam(shader, ShaderIDs._PatchOffset, (int)patchOffset);

                uint3 groupCount = DivUp(new uint3(data.PatchCapacity, 1, 1), data.ThreadGroupSize);
                cmd.DispatchCompute(shader, kernelIndex, (int)groupCount.x, (int)groupCount.y, (int)groupCount.z);

                flipflop = SurfaceCacheRingConfig.Flip(flipflop);
            }
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
            cmd.SetComputeIntParam(shader, ShaderIDs._GridSize, (int)data.GridSize);
            cmd.SetComputeFloatParam(shader, ShaderIDs._VoxelMinSize, data.VoxelMinSize);
            cmd.SetComputeIntParam(shader, ShaderIDs._SampleCount, (int)data.SampleCount);
            cmd.SetComputeFloatParam(shader, ShaderIDs._Radius, data.Radius);
            cmd.SetComputeIntParam(shader, ShaderIDs._RingConfigOffset, (int)data.RingConfigOffset);
            cmd.SetComputeVectorParam(shader, ShaderIDs._GridTargetPos, data.GridTargetPos);

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
            _grid.Dispose();
            _ringConfig.Dispose();
            _patchList.Dispose();
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
            cmd.SetComputeIntParam(data.Shader, ShaderIDs._GridSize, (int)data.GridSize);
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

            uint3 groupCount = DivUp(new uint3(data.GridSize, data.GridSize, data.GridSize * data.CascadeCount), data.ThreadGroupSize);
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
    }
}

#endif
