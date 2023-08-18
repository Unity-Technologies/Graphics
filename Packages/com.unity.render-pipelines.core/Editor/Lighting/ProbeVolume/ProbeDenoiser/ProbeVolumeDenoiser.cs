using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.ProbeReferenceVolume;
using static UnityEngine.Rendering.ProbeVolumeDenoiserSettings;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using RawSphericalHaramonicsL2 = UnityEngine.Rendering.ProbeGIBaking.RawSphericalHarmonicsL2;

namespace UnityEngine.Rendering
{
    public class ProbeVolumeDenoiserException : Exception
    {
        public ProbeVolumeDenoiserException() { }
        public ProbeVolumeDenoiserException(string message) { what = message; }
        public ProbeVolumeDenoiserException(string message, Exception inner) : base(message, inner) { }
        public string what { get; }
    }

    class ProbeVolumeDenoiser : IDisposable
    {
        private static Mutex m_mutex = new Mutex();
        private static ComputeShader m_shaderHandle;

        private static int m_populateKernelID;
        private static int m_nullFilterKernelID;
        private static int m_staticFilterKernelID;

        private ProbeReferenceVolume.Cell m_cell;
        private ProbeVolumeDenoiserSettings m_settings;
        private int m_debugFlags;

        private int[] m_remappedProbeIndices;
        private RawSphericalHaramonicsL2[] m_outputCoeffsHostBuffer;
        private Vector3[] m_probePositionsHostBuffer;
        private float[] m_probeValiditiesHostBuffer;

        private int m_numOutputProbes;
        private int m_numCachedProbes;
        private Vector3Int m_cacheDims;
        private float m_probeDelta;

        private Vector3 m_outputLowerBound;
        private Vector3 m_outputUpperBound;
        private Vector3 m_cacheLowerBound;
        private Vector3 m_cacheUpperBound;

        private ComputeBuffer m_probePositionDeviceBuffer { get; }
        private ComputeBuffer m_cachedCoeffsDeviceBuffer { get; }
        private ComputeBuffer m_cachedValiditiesDeviceBuffer { get; }
        private ComputeBuffer m_outputCoeffsDeviceBuffer { get; }

        static readonly int _ProbePositionsBuffer = Shader.PropertyToID("_ProbePositionsBuffer");
        static readonly int _CachedCoeffs = Shader.PropertyToID("_CachedCoeffs");
        static readonly int _CachedValidities = Shader.PropertyToID("_CachedValidities");
        static readonly int _OutputCoeffs = Shader.PropertyToID("_OutputCoeffs");
        static readonly int _CacheLowerBound = Shader.PropertyToID("_CacheLowerBound");
        static readonly int _CacheDims = Shader.PropertyToID("_CacheDims");
        static readonly int _ProbeDelta = Shader.PropertyToID("_ProbeDelta");
        static readonly int _NumCachedProbes = Shader.PropertyToID("_NumCachedProbes");
        static readonly int _NumOutputProbes = Shader.PropertyToID("_NumOutputProbes");
        static readonly int _N = Shader.PropertyToID("_N");
        static readonly int _M = Shader.PropertyToID("_M");
        static readonly int _DebugFlags = Shader.PropertyToID("_DebugFlags");
        static readonly int _FineTuneParams = Shader.PropertyToID("_FineTuneParams");

        static readonly int _APVResIndex = Shader.PropertyToID("_APVResIndex");
        static readonly int _APVResCellIndices = Shader.PropertyToID("_APVResCellIndices");
        static readonly int _APVResL0_L1Rx = Shader.PropertyToID("_APVResL0_L1Rx");
        static readonly int _APVResL1G_L1Ry = Shader.PropertyToID("_APVResL1G_L1Ry");
        static readonly int _APVResL1B_L1Rz = Shader.PropertyToID("_APVResL1B_L1Rz");
        static readonly int _APVResL2_0 = Shader.PropertyToID("_APVResL2_0");
        static readonly int _APVResL2_1 = Shader.PropertyToID("_APVResL2_1");
        static readonly int _APVResL2_2 = Shader.PropertyToID("_APVResL2_2");
        static readonly int _APVResL2_3 = Shader.PropertyToID("_APVResL2_3");
        static readonly int _APVResValidity = Shader.PropertyToID("_APVResValidity");

        private bool IsValidBBox(Vector3 lower, Vector3 upper)
        {
            return lower.x <= upper.x && lower.y <= upper.y && lower.z <= upper.z;
        }

        public ProbeVolumeDenoiser(ProbeReferenceVolume.Cell cell, ProbeVolumeDenoiserSettings settings)
        {
            m_cell = cell;
            m_settings = settings;

            m_debugFlags = 0;
            if (m_settings.debugMode) { m_debugFlags |= 1; }
            if (m_settings.isolateCell && m_cell.desc.index == m_settings.isolateCellIdx) { m_debugFlags |= 2; }
            if (m_settings.showInvalidProbes) { m_debugFlags |= 4; }

            Diag.Assert(m_settings.kernelSize >= 0 && m_settings.kernelSize <= 5, $"Kernel size {m_settings.kernelSize} is out of bounds [0, 5].");
            Diag.Assert(m_settings.patchSize >= 0 && m_settings.patchSize <= 2, $"Patch size {m_settings.patchSize} is out of bounds [0, 2].");

            m_mutex.WaitOne();
            if (m_shaderHandle == null)
            {
                m_shaderHandle = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeDenoiser/ProbeVolumeDenoiser.compute");

                m_populateKernelID = m_shaderHandle.FindKernel("PopulateCache");
                m_staticFilterKernelID = m_shaderHandle.FindKernel("StaticFilter");
                 
            }
            m_mutex.ReleaseMutex();

            var volume = ProbeReferenceVolume.instance;
            var cellData = m_cell.data;
            var cellDesc = m_cell.desc;

            if (false)
            {
                // Get a list of indices for all probes in this cell
                m_numOutputProbes = cellDesc.probeCount;                
                m_remappedProbeIndices = ProbeGIBaking.GetRemappedProbeIndices(cellDesc, cellData);
            }
            else
            {
                m_numOutputProbes = cellData.probePositions.Length;
                m_remappedProbeIndices = new int[m_numOutputProbes];
                for(int idx = 0; idx < m_numOutputProbes; ++idx)
                {
                    m_remappedProbeIndices[idx] = idx;
                }
            }

            Diag.Assert(m_numOutputProbes > 0, "Call contains no probes");
            Diag.Assert(m_remappedProbeIndices.Length == m_numOutputProbes, "Remap returned a list with zero entries.");

            // Allocate some memory for the input and output data
            m_probePositionsHostBuffer = new Vector3[m_numOutputProbes];
            m_probeValiditiesHostBuffer = new float[m_numOutputProbes];
            m_outputCoeffsHostBuffer = new RawSphericalHaramonicsL2[m_numOutputProbes];

            // Copy the probe data and compute boundaries of the cell
            m_outputLowerBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            m_outputUpperBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int probeIdx = 0; probeIdx < m_numOutputProbes; ++probeIdx)
            {
                int remappedIdx = m_remappedProbeIndices[probeIdx];
                m_probePositionsHostBuffer[probeIdx] = cellData.probePositions[remappedIdx];
                m_probeValiditiesHostBuffer[probeIdx] = cellData.validity[remappedIdx];

                m_outputUpperBound = Vector3.Max(m_outputUpperBound, m_probePositionsHostBuffer[probeIdx]);
                m_outputLowerBound = Vector3.Min(m_outputLowerBound, m_probePositionsHostBuffer[probeIdx]);
            }

            Diag.Assert(IsValidBBox(m_outputLowerBound, m_outputUpperBound), "Invalid bbox");

            // Calculate the dimensions of a constant grid with resolution equal to the smallest probe distance.
            // Expand it so that it includes the margin required by the filter kernel
            m_probeDelta = volume.MinDistanceBetweenProbes();
            int marginSize = m_settings.kernelSize + m_settings.patchSize;
            Vector3 cellDims = new Vector3(1, 1, 1) + (m_outputUpperBound - m_outputLowerBound) / m_probeDelta;
            m_cacheDims = new Vector3Int(Mathf.RoundToInt(cellDims.x) + 2 * marginSize,
                                         Mathf.RoundToInt(cellDims.y) + 2 * marginSize,
                                         Mathf.RoundToInt(cellDims.z) + 2 * marginSize);

            // Allocate some memory to receive the 
            m_numCachedProbes = m_cacheDims.x * m_cacheDims.y * m_cacheDims.z;

            // Calculate the bounds of the cache
            Vector3 cacheMargin = new Vector3(m_probeDelta, m_probeDelta, m_probeDelta) * marginSize;
            m_cacheLowerBound = m_outputLowerBound - cacheMargin;
            m_cacheUpperBound = m_outputUpperBound + cacheMargin;

            if ((m_debugFlags & 1) != 0)
            {
                Debug.Log(string.Format("Output bounds: {0} -> {1}", m_outputLowerBound, m_outputUpperBound));
                Debug.Log(string.Format("Cache bounds: {0} -> {1}", m_cacheLowerBound, m_cacheUpperBound));
                Debug.Log(string.Format("Cache dims: {0}", m_cacheDims));
                Debug.Log(string.Format("Num output probes: {0}", m_numOutputProbes));
                Debug.Log(string.Format("Num cached probes: {0}", m_numCachedProbes));
            }

            // Create the compute buffers
            m_probePositionDeviceBuffer = new ComputeBuffer(m_probePositionsHostBuffer.Length, System.Runtime.InteropServices.Marshal.SizeOf<Vector3>());
            m_cachedCoeffsDeviceBuffer = new ComputeBuffer(m_numCachedProbes * 9, System.Runtime.InteropServices.Marshal.SizeOf<Vector3>());
            m_cachedValiditiesDeviceBuffer = new ComputeBuffer(m_numCachedProbes, System.Runtime.InteropServices.Marshal.SizeOf<int>());
            m_outputCoeffsDeviceBuffer = new ComputeBuffer(m_numOutputProbes, System.Runtime.InteropServices.Marshal.SizeOf<RawSphericalHaramonicsL2>());

            // Upload the probe position data to the device
            m_probePositionDeviceBuffer.SetData(m_probePositionsHostBuffer);
        }

        public void Dispose()
        {
            // Clean up device objects
            m_probePositionDeviceBuffer.Dispose();
            m_cachedCoeffsDeviceBuffer.Dispose();
            m_cachedValiditiesDeviceBuffer.Dispose();
            m_outputCoeffsDeviceBuffer.Dispose();
        }

        private void PrepareProbeVolume(CommandBuffer cmd)
        {
            var refVolume = ProbeReferenceVolume.instance;
            ProbeReferenceVolume.RuntimeResources rr = refVolume.GetRuntimeResources();

            bool validResources = rr.index != null && rr.L0_L1rx != null && rr.L1_G_ry != null && rr.L1_B_rz != null;

            Diag.Assert(validResources, "Probe volume resources are invalid.");

            if (validResources)
            {
                cmd.SetGlobalBuffer(_APVResIndex, rr.index);
                cmd.SetGlobalBuffer(_APVResCellIndices, rr.cellIndices);

                cmd.SetGlobalTexture(_APVResL0_L1Rx, rr.L0_L1rx);
                cmd.SetGlobalTexture(_APVResL1G_L1Ry, rr.L1_G_ry);
                cmd.SetGlobalTexture(_APVResL1B_L1Rz, rr.L1_B_rz);

                cmd.SetGlobalTexture(_APVResL2_0, rr.L2_0);
                cmd.SetGlobalTexture(_APVResL2_1, rr.L2_1);
                cmd.SetGlobalTexture(_APVResL2_2, rr.L2_2);
                cmd.SetGlobalTexture(_APVResL2_3, rr.L2_3);

                cmd.SetGlobalTexture(_APVResValidity, rr.Validity);
            }

            ProbeVolumeShadingParameters parameters;
            parameters.normalBias = 0;
            parameters.viewBias = 0;
            parameters.scaleBiasByMinDistanceBetweenProbes = false;
            parameters.samplingNoise = 0;
            parameters.weight = 1f;
            parameters.leakReductionMode = APVLeakReductionMode.None;
            parameters.occlusionWeightContribution = 0.0f;
            parameters.minValidNormalWeight = 0.0f;
            parameters.frameIndexForNoise = 0;
            parameters.reflNormalizationLowerClamp = 0.1f;
            parameters.reflNormalizationUpperClamp = 1.0f;
            ProbeReferenceVolume.instance.UpdateConstantBuffer(cmd, parameters);
        }

        public void Apply()
        {
            var cmd = CommandBufferPool.Get("Probe Denoising");

            // Params for cache
            cmd.SetComputeBufferParam(m_shaderHandle, m_populateKernelID, _CachedCoeffs, m_cachedCoeffsDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_populateKernelID, _CachedValidities, m_cachedValiditiesDeviceBuffer);

            // Params for static filter kernel
            cmd.SetComputeBufferParam(m_shaderHandle, m_staticFilterKernelID, _ProbePositionsBuffer, m_probePositionDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_staticFilterKernelID, _OutputCoeffs, m_outputCoeffsDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_staticFilterKernelID, _CachedCoeffs, m_cachedCoeffsDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_staticFilterKernelID, _CachedValidities, m_cachedValiditiesDeviceBuffer);

            cmd.SetComputeVectorParam(m_shaderHandle, _CacheLowerBound, new Vector4(m_cacheLowerBound.x, m_cacheLowerBound.y, m_cacheLowerBound.z));
            cmd.SetComputeVectorParam(m_shaderHandle, _CacheDims, new Vector4(m_cacheDims.x, m_cacheDims.y, m_cacheDims.z));
            cmd.SetComputeFloatParam(m_shaderHandle, _ProbeDelta, m_probeDelta);
            cmd.SetComputeIntParam(m_shaderHandle, _NumCachedProbes, m_numCachedProbes);
            cmd.SetComputeIntParam(m_shaderHandle, _NumOutputProbes, m_numOutputProbes);
            cmd.SetComputeIntParam(m_shaderHandle, _N, m_settings.kernelSize);
            cmd.SetComputeIntParam(m_shaderHandle, _M, m_settings.patchSize);
            cmd.SetComputeIntParam(m_shaderHandle, _DebugFlags, m_debugFlags);
            cmd.SetComputeVectorParam(m_shaderHandle, _FineTuneParams, new Vector4(m_settings.samplerBias, 0.0f, 0.0f, 0.0f));

            PrepareProbeVolume(cmd);

            // Populate the cache with values
            const int kDefaultBlockSize = 64;
            int numBlocks = (m_numCachedProbes + kDefaultBlockSize - 1) / kDefaultBlockSize;
            cmd.DispatchCompute(m_shaderHandle, m_populateKernelID, numBlocks, 1, 1);

            // Filter the cached values
            numBlocks = (m_numOutputProbes + kDefaultBlockSize - 1) / kDefaultBlockSize;
            cmd.DispatchCompute(m_shaderHandle, m_staticFilterKernelID, numBlocks, 1, 1);

            // Execute the command queue
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);

            // Read back the data from the device and pack the filtered values as shader coefficients
            m_outputCoeffsDeviceBuffer.GetData(m_outputCoeffsHostBuffer); 
            for (int probeIdx = 0; probeIdx < m_numOutputProbes; ++probeIdx)
            {
                m_outputCoeffsHostBuffer[probeIdx].PackToShaderCoefficients(m_cell, m_remappedProbeIndices[probeIdx]);
            }
        }
    }
}
