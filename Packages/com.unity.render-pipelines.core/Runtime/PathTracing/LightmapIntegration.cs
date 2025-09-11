using Unity.Mathematics;
using UnityEngine.PathTracing.Core;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Integration
{
    internal enum AntiAliasingType { Stochastic, SuperSampling };

    internal static class LightmapIntegratorShaderIDs
    {
        public static readonly int LightmapInOut = Shader.PropertyToID("g_LightmapInOut");
        public static readonly int DirectionalInOut = Shader.PropertyToID("g_DirectionalInOut");
        public static readonly int AdaptiveInOut = Shader.PropertyToID("g_AdaptiveInOut");
        public static readonly int AdaptiveSampling = Shader.PropertyToID("g_AdaptiveSampling");
        public static readonly int AdaptiveStopSamples = Shader.PropertyToID("g_AdaptiveStopSamples");
        public static readonly int AdaptiveCheckIfFullyConverged = Shader.PropertyToID("g_AdaptiveCheckIfFullyConverged");
        public static readonly int AdaptiveThreshold = Shader.PropertyToID("g_AdaptiveThreshold");
        public static readonly int AccumulateDirectional = Shader.PropertyToID("g_AccumulateDirectional");
        public static readonly int SampleCountInOut = Shader.PropertyToID("g_SampleCountInOut");
        public static readonly int SampleCountIn = Shader.PropertyToID("g_SampleCountIn");
        public static readonly int ShaderLocalToWorld = Shader.PropertyToID("g_ShaderLocalToWorld");
        public static readonly int ShaderLocalToWorldNormals = Shader.PropertyToID("g_ShaderLocalToWorldNormals");
        public static readonly int InstanceGeometryIndex = Shader.PropertyToID("g_InstanceGeometryIndex");
        public static readonly int GISampleCount = Shader.PropertyToID("g_GISampleCount");
        public static readonly int AOMaxDistance = Shader.PropertyToID("g_AOMaxDistance");
        public static readonly int InputSampleCountInW = Shader.PropertyToID("g_InputSampleCountInW");
        public static readonly int Normals = Shader.PropertyToID("g_Normals");
        public static readonly int TextureWidth = Shader.PropertyToID("g_TextureWidth");
        public static readonly int TextureHeight = Shader.PropertyToID("g_TextureHeight");
        public static readonly int ReceiveShadows = Shader.PropertyToID("g_ReceiveShadows");
        public static readonly int PushOff = Shader.PropertyToID("g_PushOff");
        public static readonly int IndirectDispatchDimensions = Shader.PropertyToID("g_IndirectDispatchDimensions");
        public static readonly int IndirectDispatchoriginalDimensions = Shader.PropertyToID("g_IndirectDispatchOriginalDimensions");
        public static readonly int InputBufferSelector = Shader.PropertyToID("g_InputBufferSelector");
        public static readonly int InputBufferLength = Shader.PropertyToID("g_InputBufferLength");
        public static readonly int InputBuffer0 = Shader.PropertyToID("g_InputBuffer0");
        public static readonly int InputBuffer1 = Shader.PropertyToID("g_InputBuffer1");
        public static readonly int SelectionOutput = Shader.PropertyToID("g_SelectionOutput");
        public static readonly int GBuffer = Shader.PropertyToID("g_GBuffer");
        public static readonly int SampleOffset = Shader.PropertyToID("g_SampleOffset");
        public static readonly int StochasticAntialiasing = Shader.PropertyToID("g_StochasticAntialiasing");
        public static readonly int SuperSampleWidth = Shader.PropertyToID("g_SuperSampleWidth");
        public static readonly int Float3Buffer = Shader.PropertyToID("g_Float3Buffer");
        public static readonly int DestinationTexture = Shader.PropertyToID("g_DestinationTexture");
        public static readonly int SourceTexture = Shader.PropertyToID("g_SourceTexture");
        public static readonly int SourceX = Shader.PropertyToID("g_SourceX");
        public static readonly int SourceY = Shader.PropertyToID("g_SourceY");
        public static readonly int SourceWidth = Shader.PropertyToID("g_SourceWidth");
        public static readonly int SourceHeight = Shader.PropertyToID("g_SourceHeight");
        public static readonly int DestinationX = Shader.PropertyToID("g_DestinationX");
        public static readonly int DestinationY = Shader.PropertyToID("g_DestinationY");
        public static readonly int TextureInOut = Shader.PropertyToID("g_TextureInOut");
        public static readonly int ExpandedOutput = Shader.PropertyToID("g_ExpandedOutput");
        public static readonly int ExpandedDirectional = Shader.PropertyToID("g_ExpandedDirectional");
        public static readonly int ExpandedSampleCountInW = Shader.PropertyToID("g_ExpandedSampleCountInW");
        public static readonly int ExpandedTexelSampleWidth = Shader.PropertyToID("g_ExpandedTexelSampleWidth");
        public static readonly int MaxLocalSampleCount = Shader.PropertyToID("g_MaxLocalSampleCount");
        public static readonly int SourceBuffer = Shader.PropertyToID("g_SourceBuffer");
        public static readonly int SourceLength = Shader.PropertyToID("g_SourceLength");
        public static readonly int SourceStride = Shader.PropertyToID("g_SourceStride");
        public static readonly int GBufferLength = Shader.PropertyToID("g_GBufferLength");
        public static readonly int CompactedGBuffer = Shader.PropertyToID("g_CompactedGBuffer");
        public static readonly int InstanceWidth = Shader.PropertyToID("g_InstanceWidth");
        public static readonly int ChunkOffsetX = Shader.PropertyToID("g_ChunkOffsetX");
        public static readonly int ChunkOffsetY = Shader.PropertyToID("g_ChunkOffsetY");
        public static readonly int LightmapSamplesExpanded = Shader.PropertyToID("g_LightmapSamplesExpanded");
    }

    internal class LightmapOccupancyIntegrator
    {
        private ComputeShader _occupancyShader;
        private int _occupancyKernel;

        public void Prepare(ComputeShader occupancyShader)
        {
            _occupancyShader = occupancyShader;
            Debug.Assert(_occupancyShader != null);
            _occupancyKernel = _occupancyShader.FindKernel("BlitOccupancy");
        }

        public void Accumulate(
            CommandBuffer cmd,
            Vector2Int instanceTexelSize,
            Vector2Int instanceTexelOffset,
            UVFallbackBuffer uvFallbackBuffer,
            RenderTexture output)
        {
            uvFallbackBuffer.Bind(cmd, _occupancyShader, _occupancyKernel, instanceTexelOffset);
            cmd.SetComputeTextureParam(_occupancyShader, _occupancyKernel, LightmapIntegratorShaderIDs.LightmapInOut, output);
            _occupancyShader.GetKernelThreadGroupSizes(_occupancyKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_occupancyShader, _occupancyKernel, GraphicsHelpers.DivUp(instanceTexelSize.x, x), GraphicsHelpers.DivUp(instanceTexelSize.y, y), 1);
        }
    }

    internal class LightmapDirectIntegrator : System.IDisposable
    {
        private IRayTracingShader _accumulationShader;
        private ComputeShader _normalizationShader;
        private int _normalizationKernel;
        private int _directionalNormalizationKernel;
        private SamplingResources _samplingResources;
        private RTHandle _emptyTexture;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public LightmapDirectIntegrator()
        {
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader normalizationShader, ComputeShader expansionHelpers, SamplingResources samplingResources, RTHandle emptyExposureTexture)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _normalizationShader = normalizationShader;
            Debug.Assert(_normalizationShader != null);
            _normalizationKernel = _normalizationShader.FindKernel("NormalizeRadiance");
            _directionalNormalizationKernel = _normalizationShader.FindKernel("NormalizeDirection");

            _samplingResources = samplingResources;
            _emptyTexture = emptyExposureTexture;

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            uint sampleCountToTakePerTexel,
            uint currentSampleCountPerTexel,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            Vector2Int instanceTexelSize,
            uint2 chunkOffset,
            World world,
            GraphicsBuffer traceScratchBuffer,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer expandedOutput,
            GraphicsBuffer expandedDirectional,
            GraphicsBuffer compactedTexelIndices,
            GraphicsBuffer compactedGbufferLength,
            bool receiveShadows,
            float pushOff,
            uint lightEvaluationsPerBounce,
            bool newChunkStarted)
        {
            bool doDirectional = expandedDirectional != null;
            int instanceWidth = instanceTexelSize.x;
            int instanceHeight = instanceTexelSize.y;
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(gBuffer.count == expandedOutput.count);
            Debug.Assert(sampleCountToTakePerTexel <= expandedSampleWidth);
            Debug.Assert(sampleCountToTakePerTexel > 0);

            // path tracing inputs
            bool preExpose = false;
            float environmentIntensityMultiplier = 1.0f;
            Util.BindPathTracingInputs(cmd, _accumulationShader, false, lightEvaluationsPerBounce, preExpose, 0, environmentIntensityMultiplier, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyTexture);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            var requiredSizeInBytes = _accumulationShader.GetTraceScratchBufferRequiredSizeInBytes((uint)expandedOutput.count, 1, 1);
            if (requiredSizeInBytes > 0)
            {
                var actualScratchBufferSize = (ulong)(traceScratchBuffer.count * traceScratchBuffer.stride);
                Debug.Assert(traceScratchBuffer.stride == sizeof(uint));
                Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
            }

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ReceiveShadows, receiveShadows ? 1 : 0);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.PushOff, pushOff);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, instanceWidth);

            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.AccumulateDirectional, doDirectional ? 1 : 0);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, compactedTexelIndices);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, expandedOutput);
            if (doDirectional)
                _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedDirectional, expandedDirectional);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            if (newChunkStarted)
            {
                // Its time to repopulate the indirect dispatch buffers (as a new instance has started). Use the compacted size for this.
                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            }

            // accumulate (expanded)
            {
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, (int)currentSampleCountPerTexel);
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, (int)sampleCountToTakePerTexel);
                cmd.BeginSample("Accumulation (Expanded)");
                _accumulationShader.Dispatch(cmd, traceScratchBuffer, _accumulationDispatchBuffer);
                cmd.EndSample("Accumulation (Expanded)");
            }
        }

        public void Normalize(CommandBuffer cmd, RenderTexture lightmapInOut)
        {
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.LightmapInOut, lightmapInOut);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, lightmapInOut.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, lightmapInOut.height);
            _normalizationShader.GetKernelThreadGroupSizes(_normalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _normalizationKernel, GraphicsHelpers.DivUp(lightmapInOut.width, x), GraphicsHelpers.DivUp(lightmapInOut.height, y), 1);
        }

        public void NormalizeDirectional(CommandBuffer cmd, RenderTexture directionalInOut, RenderTexture sampleCountInW, RenderTexture normals)
        {
            Debug.Assert(directionalInOut.width == sampleCountInW.width && directionalInOut.height == sampleCountInW.height);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.DirectionalInOut, directionalInOut);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.InputSampleCountInW, sampleCountInW);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.Normals, normals);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, directionalInOut.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, directionalInOut.height);
            _normalizationShader.GetKernelThreadGroupSizes(_directionalNormalizationKernel, out uint x, out uint y, out uint _);
            cmd.DispatchCompute(_normalizationShader, _directionalNormalizationKernel, GraphicsHelpers.DivUp(directionalInOut.width, x), GraphicsHelpers.DivUp(directionalInOut.height, y), 1);
        }
    }

    internal class LightmapIndirectIntegrator : System.IDisposable
    {
        private IRayTracingShader _accumulationShader;
        private ComputeShader _normalizationShader;
        private int _normalizationKernel;
        private int _directionalNormalizationKernel;
        private SamplingResources _samplingResources;
        private RTHandle _emptyTexture;
        private bool _countNEERayAsPathSegment;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public LightmapIndirectIntegrator(bool countNEERayAsPathSegment)
        {
            _countNEERayAsPathSegment = countNEERayAsPathSegment;
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader normalizationShader, ComputeShader expansionHelpers, SamplingResources samplingResources, RTHandle emptyExposureTexture)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _normalizationShader = normalizationShader;
            Debug.Assert(_normalizationShader != null);
            _normalizationKernel = _normalizationShader.FindKernel("NormalizeRadiance");
            _directionalNormalizationKernel = _normalizationShader.FindKernel("NormalizeDirection");

            _samplingResources = samplingResources;
            _emptyTexture = emptyExposureTexture;

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            uint sampleCountToTakePerTexel,
            uint currentSampleCountPerTexel,
            uint bounceCount,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            Vector2Int instanceTexelSize,
            uint2 chunkOffset,
            World world,
            GraphicsBuffer traceScratchBuffer,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer expandedOutput,
            GraphicsBuffer expandedDirectional,
            GraphicsBuffer compactedTexelIndices,
            GraphicsBuffer compactedGbufferLength,
            float pushOff,
            uint lightEvaluationsPerBounce,
            bool newChunkStarted)
        {
            bool doDirectional = expandedDirectional != null;
            int instanceWidth = instanceTexelSize.x;
            int instanceHeight = instanceTexelSize.y;
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(gBuffer.count == expandedOutput.count);
            Debug.Assert(sampleCountToTakePerTexel <= expandedSampleWidth);
            Debug.Assert(sampleCountToTakePerTexel > 0);

            // path tracing inputs
            bool preExpose = false;
            float environmentIntensityMultiplier = 1.0f;
            Util.BindPathTracingInputs(cmd, _accumulationShader, _countNEERayAsPathSegment, lightEvaluationsPerBounce, preExpose, (int)bounceCount, environmentIntensityMultiplier, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyTexture);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            var requiredSizeInBytes = _accumulationShader.GetTraceScratchBufferRequiredSizeInBytes((uint)expandedOutput.count, 1, 1);
            if (requiredSizeInBytes > 0)
            {
                var actualScratchBufferSize = (ulong)(traceScratchBuffer.count * traceScratchBuffer.stride);
                Debug.Assert(traceScratchBuffer.stride == sizeof(uint));
                Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
            }

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.PushOff, pushOff);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, instanceWidth);

            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.AccumulateDirectional, doDirectional ? 1 : 0);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, compactedTexelIndices);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, expandedOutput);
            if (doDirectional)
                _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedDirectional, expandedDirectional);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            if (newChunkStarted)
            {
                // Its time to repopulate the indirect dispatch buffers (as a new instance has started). Use the compacted size for this.
                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            }

            // accumulate (expanded)
            {
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, (int)currentSampleCountPerTexel);
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, (int)sampleCountToTakePerTexel);
                cmd.BeginSample("Accumulation (Expanded)");
                _accumulationShader.Dispatch(cmd, traceScratchBuffer, _accumulationDispatchBuffer);
                cmd.EndSample("Accumulation (Expanded)");
            }
        }

        public void Normalize(CommandBuffer cmd, RenderTexture lightmapInOut)
        {
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.LightmapInOut, lightmapInOut);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, lightmapInOut.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, lightmapInOut.height);
            _normalizationShader.GetKernelThreadGroupSizes(_normalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _normalizationKernel, GraphicsHelpers.DivUp(lightmapInOut.width, x), GraphicsHelpers.DivUp(lightmapInOut.height, y), 1);
        }

        public void NormalizeDirectional(CommandBuffer cmd, RenderTexture directionalInOut, RenderTexture sampleCountInW, RenderTexture normals)
        {
            Debug.Assert(directionalInOut.width == sampleCountInW.width && directionalInOut.height == sampleCountInW.height);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.DirectionalInOut, directionalInOut);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.InputSampleCountInW, sampleCountInW);
            cmd.SetComputeTextureParam(_normalizationShader, _directionalNormalizationKernel, LightmapIntegratorShaderIDs.Normals, normals);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, directionalInOut.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, directionalInOut.height);
            _normalizationShader.GetKernelThreadGroupSizes(_directionalNormalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _directionalNormalizationKernel, GraphicsHelpers.DivUp(directionalInOut.width, x), GraphicsHelpers.DivUp(directionalInOut.height, y), 1);
        }
    }

    internal class LightmapAOIntegrator : System.IDisposable
    {
        private IRayTracingShader _accumulationShader;
        private ComputeShader _normalizationShader;
        private int _normalizationKernel;
        private SamplingResources _samplingResources;
        private RTHandle _emptyTexture;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader normalizationShader, ComputeShader expansionHelpers, SamplingResources samplingResources, RTHandle emptyExposureTexture)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _normalizationShader = normalizationShader;
            Debug.Assert(_normalizationShader != null);
            _normalizationKernel = _normalizationShader.FindKernel("NormalizeAO");

            _samplingResources = samplingResources;
            _emptyTexture = emptyExposureTexture;

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            uint sampleCountToTakePerTexel,
            uint currentSampleCountPerTexel,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            Vector2Int instanceTexelSize,
            uint2 chunkOffset,
            World world,
            GraphicsBuffer traceScratchBuffer,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer expandedOutput,
            GraphicsBuffer compactedTexelIndices,
            GraphicsBuffer compactedGbufferLength,
            float pushOff,
            float aoMaxDistance,
            bool newChunkStarted)
        {
            int instanceWidth = instanceTexelSize.x;
            int instanceHeight = instanceTexelSize.y;
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(gBuffer.count == expandedOutput.count);
            Debug.Assert(sampleCountToTakePerTexel <= expandedSampleWidth);
            Debug.Assert(sampleCountToTakePerTexel > 0);

            // path tracing inputs
            uint lightEvaluationsPerBounce = 1;
            bool preExpose = false;
            float environmentIntensityMultipler = 1.0f;
            Util.BindPathTracingInputs(cmd, _accumulationShader, false, lightEvaluationsPerBounce, preExpose, 0, environmentIntensityMultipler, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyTexture);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            var requiredSizeInBytes = _accumulationShader.GetTraceScratchBufferRequiredSizeInBytes((uint)expandedOutput.count, 1, 1);
            if (requiredSizeInBytes > 0)
            {
                var actualScratchBufferSize = (ulong)(traceScratchBuffer.count * traceScratchBuffer.stride);
                Debug.Assert(traceScratchBuffer.stride == sizeof(uint));
                Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
            }

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.PushOff, pushOff);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.AOMaxDistance, aoMaxDistance);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, instanceWidth);

            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, compactedTexelIndices);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, expandedOutput);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            if (newChunkStarted)
            {
                // Its time to repopulate the indirect dispatch buffers (as a new instance has started). Use the compacted size for this.
                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            }

            // accumulate (expanded)
            {
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, (int)currentSampleCountPerTexel);
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, (int)sampleCountToTakePerTexel);
                cmd.BeginSample("Accumulation (Expanded)");
                _accumulationShader.Dispatch(cmd, traceScratchBuffer, _accumulationDispatchBuffer);
                cmd.EndSample("Accumulation (Expanded)");
            }
        }

        public void Normalize(CommandBuffer cmd, RenderTexture lightmap)
        {
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.LightmapInOut, lightmap);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, lightmap.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, lightmap.height);
            _normalizationShader.GetKernelThreadGroupSizes(_normalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _normalizationKernel, GraphicsHelpers.DivUp(lightmap.width, x), GraphicsHelpers.DivUp(lightmap.height, y), 1);
        }
    }

    internal class LightmapValidityIntegrator : System.IDisposable
    {
        IRayTracingShader _accumulationShader;
        ComputeShader _normalizationShader;
        int _normalizationKernel;
        SamplingResources _samplingResources;
        RTHandle _emptyTexture;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader normalizationShader, ComputeShader expansionHelpers, SamplingResources samplingResources, RTHandle emptyExposureTexture)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _normalizationShader = normalizationShader;
            Debug.Assert(_normalizationShader != null);
            _normalizationKernel = _normalizationShader.FindKernel("NormalizeValidity");

            _samplingResources = samplingResources;
            _emptyTexture = emptyExposureTexture;

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            uint sampleCountToTakePerTexel,
            uint currentSampleCountPerTexel,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            Vector2Int instanceTexelSize,
            uint2 chunkOffset,
            World world,
            GraphicsBuffer traceScratchBuffer,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer expandedOutput,
            GraphicsBuffer compactedTexelIndices,
            GraphicsBuffer compactedGbufferLength,
            float pushOff,
            bool newChunkStarted)
        {
            int instanceWidth = instanceTexelSize.x;
            int instanceHeight = instanceTexelSize.y;
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(gBuffer.count == expandedOutput.count);
            Debug.Assert(sampleCountToTakePerTexel <= expandedSampleWidth);
            Debug.Assert(sampleCountToTakePerTexel > 0);

            // path tracing inputs
            uint lightEvaluationsPerBounce = 1;
            bool preExpose = false;
            float environmentIntensityMultiplier = 1.0f;
            Util.BindPathTracingInputs(cmd, _accumulationShader, false, lightEvaluationsPerBounce, preExpose, 0, environmentIntensityMultiplier, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyTexture);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            var requiredSizeInBytes = _accumulationShader.GetTraceScratchBufferRequiredSizeInBytes((uint)expandedOutput.count, 1, 1);
            if (requiredSizeInBytes > 0)
            {
                var actualScratchBufferSize = (ulong)(traceScratchBuffer.count * traceScratchBuffer.stride);
                Debug.Assert(traceScratchBuffer.stride == sizeof(uint));
                Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
            }

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.PushOff, pushOff);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, instanceWidth);

            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, compactedTexelIndices);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, expandedOutput);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            if (newChunkStarted)
            {
                // Its time to repopulate the indirect dispatch buffers (as a new instance has started). Use the compacted size for this.
                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            }

            // accumulate (expanded)
            {
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, (int)currentSampleCountPerTexel);
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, (int)sampleCountToTakePerTexel);
                cmd.BeginSample("Accumulation (Expanded)");
                _accumulationShader.Dispatch(cmd, traceScratchBuffer, _accumulationDispatchBuffer);
                cmd.EndSample("Accumulation (Expanded)");
            }
        }

        public void Normalize(CommandBuffer cmd, RenderTexture lightmapInOut)
        {
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.LightmapInOut, lightmapInOut);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, lightmapInOut.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, lightmapInOut.height);
            _normalizationShader.GetKernelThreadGroupSizes(_normalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _normalizationKernel, GraphicsHelpers.DivUp(lightmapInOut.width, x), GraphicsHelpers.DivUp(lightmapInOut.height, y), 1);
        }
    }

    internal class LightmapShadowMaskIntegrator : System.IDisposable
    {
        IRayTracingShader _accumulationShader;
        ComputeShader _normalizationShader;
        int _normalizationKernel;
        SamplingResources _samplingResources;
        RTHandle _emptyTexture;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader normalizationShader, ComputeShader expansionHelpers, SamplingResources samplingResources, RTHandle emptyExposureTexture)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _normalizationShader = normalizationShader;
            Debug.Assert(_normalizationShader != null);
            _normalizationKernel = _normalizationShader.FindKernel("NormalizeShadowMask");

            _samplingResources = samplingResources;
            _emptyTexture = emptyExposureTexture;

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            uint sampleCountToTakePerTexel,
            uint currentSampleCountPerTexel,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            Vector2Int instanceTexelSize,
            uint2 chunkOffset,
            World world,
            GraphicsBuffer traceScratchBuffer,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer expandedOutput,
            GraphicsBuffer expandedSampleCountInW,
            GraphicsBuffer compactedTexelIndices,
            GraphicsBuffer compactedGbufferLength,
            bool receiveShadows,
            float pushOff,
            uint lightEvaluationsPerBounce,
            bool newChunkStarted)
        {
            int instanceWidth = instanceTexelSize.x;
            int instanceHeight = instanceTexelSize.y;
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(gBuffer.count == expandedOutput.count);
            Debug.Assert(sampleCountToTakePerTexel <= expandedSampleWidth);
            Debug.Assert(sampleCountToTakePerTexel > 0);

            // path tracing inputs
            bool preExpose = false;
            float environmentIntensityMultiplier = 1.0f;
            Util.BindPathTracingInputs(cmd, _accumulationShader, false, lightEvaluationsPerBounce, preExpose, 0, environmentIntensityMultiplier, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyTexture);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            var requiredSizeInBytes = _accumulationShader.GetTraceScratchBufferRequiredSizeInBytes((uint)expandedOutput.count, 1, 1);
            if (requiredSizeInBytes > 0)
            {
                var actualScratchBufferSize = (ulong)(traceScratchBuffer.count * traceScratchBuffer.stride);
                Debug.Assert(traceScratchBuffer.stride == sizeof(uint));
                Debug.Assert(requiredSizeInBytes <= actualScratchBufferSize);
            }

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ReceiveShadows, receiveShadows ? 1 : 0);
            _accumulationShader.SetFloatParam(cmd, LightmapIntegratorShaderIDs.PushOff, pushOff);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceWidth, instanceWidth);

            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.CompactedGBuffer, compactedTexelIndices);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedOutput, expandedOutput);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.ExpandedSampleCountInW, expandedSampleCountInW);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ExpandedTexelSampleWidth, (int)expandedSampleWidth);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetX, (int)chunkOffset.x);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.ChunkOffsetY, (int)chunkOffset.y);

            if (newChunkStarted)
            {
                // Its time to repopulate the indirect dispatch buffers (as a new instance has started). Use the compacted size for this.
                ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            }

            // accumulate (expanded)
            {
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.SampleOffset, (int)currentSampleCountPerTexel);
                _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.MaxLocalSampleCount, (int)sampleCountToTakePerTexel);
                cmd.BeginSample("Accumulation (Expanded)");
                _accumulationShader.Dispatch(cmd, traceScratchBuffer, _accumulationDispatchBuffer);
                cmd.EndSample("Accumulation (Expanded)");
            }
        }

        public void Normalize(CommandBuffer cmd, RenderTexture lightmap, RenderTexture sampleCountInW)
        {
            Debug.Assert(lightmap.width == sampleCountInW.width && lightmap.height == sampleCountInW.height);
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.LightmapInOut, lightmap);
            cmd.SetComputeTextureParam(_normalizationShader, _normalizationKernel, LightmapIntegratorShaderIDs.InputSampleCountInW, sampleCountInW);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureWidth, lightmap.width);
            cmd.SetComputeIntParam(_normalizationShader, LightmapIntegratorShaderIDs.TextureHeight, lightmap.height);
            _normalizationShader.GetKernelThreadGroupSizes(_normalizationKernel, out uint x, out uint y, out _);
            cmd.DispatchCompute(_normalizationShader, _normalizationKernel, GraphicsHelpers.DivUp(lightmap.width, x), GraphicsHelpers.DivUp(lightmap.height, y), 1);
        }
    }

    internal class GBufferDebug : System.IDisposable
    {
        private IRayTracingShader _accumulationShader;
        private GraphicsBuffer _accumulationDispatchBuffer;
        private ComputeShader _expansionHelpers;
        private int _populateAccumulationDispatchKernel;

        public void Dispose()
        {
            _accumulationDispatchBuffer?.Dispose();
        }

        public GBufferDebug()
        {
        }

        public void Prepare(IRayTracingShader accumulationShader, ComputeShader expansionHelpers)
        {
            _accumulationShader = accumulationShader;
            Debug.Assert(_accumulationShader != null);

            _expansionHelpers = expansionHelpers;
            _populateAccumulationDispatchKernel = _expansionHelpers.FindKernel("PopulateAccumulationDispatch");
            _accumulationDispatchBuffer = RayTracingHelper.CreateDispatchIndirectBuffer();
        }

        public void Accumulate(
            CommandBuffer cmd,
            Matrix4x4 shaderLocalToWorld,
            Matrix4x4 shaderLocalToWorldNormals,
            int instanceGeometryIndex,
            World world,
            GraphicsBuffer gBuffer,
            uint expandedSampleWidth,
            GraphicsBuffer lightmapSamplesExpanded,
            GraphicsBuffer compactedGbufferLength)
        {
            Debug.Assert(math.ispow2(expandedSampleWidth));
            Debug.Assert(lightmapSamplesExpanded.count <= gBuffer.count);
            Debug.Assert(lightmapSamplesExpanded.count % expandedSampleWidth == 0);
            Util.BindWorld(cmd, _accumulationShader, world, 1024);

            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorld, shaderLocalToWorld);
            _accumulationShader.SetMatrixParam(cmd, LightmapIntegratorShaderIDs.ShaderLocalToWorldNormals, shaderLocalToWorldNormals);
            _accumulationShader.SetIntParam(cmd, LightmapIntegratorShaderIDs.InstanceGeometryIndex, instanceGeometryIndex);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.GBuffer, gBuffer);
            _accumulationShader.SetBufferParam(cmd, LightmapIntegratorShaderIDs.LightmapSamplesExpanded, lightmapSamplesExpanded);

            // Its time to repopulate the indirect dispatch buffers. Use the compacted size for this.
            ExpansionHelpers.PopulateAccumulationIndirectDispatch(cmd, _accumulationShader, _expansionHelpers, _populateAccumulationDispatchKernel, expandedSampleWidth, compactedGbufferLength, _accumulationDispatchBuffer);
            cmd.BeginSample("GBuffer Debug");
            _accumulationShader.Dispatch(cmd, null, _accumulationDispatchBuffer);
            cmd.EndSample("GBuffer Debug");
        }
    }
}
