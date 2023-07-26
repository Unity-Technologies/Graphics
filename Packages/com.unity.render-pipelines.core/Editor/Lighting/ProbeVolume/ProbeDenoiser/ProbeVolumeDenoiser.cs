using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;
using static UnityEngine.Rendering.ProbeReferenceVolume;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;
using RawSphericalHaramonicsL2 = UnityEngine.Rendering.ProbeGIBaking.RawSphericalHarmonicsL2;

namespace UnityEngine.Rendering
{
    class ProbeVolumeDenoiser
    {
        private static Mutex m_mutex = new Mutex();

        private static ComputeShader m_shaderHandle;
        private static int m_populateKernelID;

        private ProbeReferenceVolume.Cell m_cell;

        private RawSphericalHaramonicsL2[] m_outputCoeffsHostBuffer;
        private int m_numOutputProbes;
        private int m_numCachedProbes;
        private Vector3Int m_cacheDims;
        private float m_probeDelta;
        private Vector3 m_cacheLowerBound;
        private Vector3 m_cacheUpperBound;

        private ComputeBuffer m_probePositionDeviceBuffer { get; }
        private ComputeBuffer m_cachedCoeffsDeviceBuffer { get; }
        private ComputeBuffer m_outputCoeffsDeviceBuffer { get; }

        static readonly int _ProbePositionsBuffer = Shader.PropertyToID("_ProbePositionsBuffer");
        static readonly int _CachedCoeffs = Shader.PropertyToID("_CachedCoeffs");
        static readonly int _OutputCoeffs = Shader.PropertyToID("_OutputCoeffs");
        static readonly int _CacheLowerBound = Shader.PropertyToID("_CacheLowerBound");
        static readonly int _CacheDims = Shader.PropertyToID("_CacheDims");
        static readonly int _ProbeDelta = Shader.PropertyToID("_ProbeDelta");
        static readonly int _NumCachedProbes = Shader.PropertyToID("_NumCachedProbes");
        static readonly int _NumOutputProbes = Shader.PropertyToID("_NumOutputProbes");

        static readonly int _APVResIndex = Shader.PropertyToID("_APVResIndex");
        static readonly int _APVResCellIndices = Shader.PropertyToID("_APVResCellIndices");
        static readonly int _APVResL0_L1Rx = Shader.PropertyToID("_APVResL0_L1Rx");
        static readonly int _APVResL1G_L1Ry = Shader.PropertyToID("_APVResL1G_L1Ry");
        static readonly int _APVResL1B_L1Rz = Shader.PropertyToID("_APVResL1B_L1Rz");
        static readonly int _APVResL2_0 = Shader.PropertyToID("_APVResL2_0");
        static readonly int _APVResL2_1 = Shader.PropertyToID("_APVResL2_1");
        static readonly int _APVResL2_2 = Shader.PropertyToID("_APVResL2_2");
        static readonly int _APVResL2_3 = Shader.PropertyToID("_APVResL2_3");

        public ProbeVolumeDenoiser(ProbeReferenceVolume.Cell cell)
        {
            m_cell = cell;

            m_mutex.WaitOne();
            if (m_shaderHandle == null)
            {
                //m_shaderHandle = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeVolumeDenoiser.compute");
                //m_populateKernelID = m_shaderHandle.FindKernel("PopulateCache");

                Debug.Log("Denoising shaders loaded!");

            }
            m_mutex.ReleaseMutex();

            /*var volume = ProbeReferenceVolume.instance;
            var cellData = m_cell.data;
            m_numOutputProbes = cellData.probePositions.Length;

            Debug.Log(string.Format("Denoising {0}", m_cell.desc.index));

            m_cacheLowerBound = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            m_cacheUpperBound = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            for (int probeIdx = 0; probeIdx < m_numOutputProbes; ++probeIdx)
            {
                m_cacheUpperBound = Vector3.Max(m_cacheUpperBound, cellData.probePositions[probeIdx]);
                m_cacheLowerBound = Vector3.Min(m_cacheLowerBound, cellData.probePositions[probeIdx]);
            }

            m_probeDelta = volume.MinDistanceBetweenProbes();
            Vector3 cellDims = (m_cacheUpperBound - m_cacheLowerBound) / m_probeDelta;

            const int kKernelSize = 5;
            m_cacheDims = new Vector3Int(Mathf.RoundToInt(cellDims.x) + 2 * kKernelSize + 1,
                                         Mathf.RoundToInt(cellDims.y) + 2 * kKernelSize + 1,
                                         Mathf.RoundToInt(cellDims.z) + 2 * kKernelSize + 1);

            m_numCachedProbes = m_cacheDims.x * m_cacheDims.y * m_cacheDims.z;

            Debug.Log(string.Format("{0} -> {1} -> {2}: {3}", m_cacheLowerBound, m_cacheUpperBound, m_cacheDims, m_numCachedProbes));

            // Create the compute buffers
            m_probePositionDeviceBuffer = new ComputeBuffer(m_numOutputProbes, System.Runtime.InteropServices.Marshal.SizeOf<Vector3>());
            m_cachedCoeffsDeviceBuffer = new ComputeBuffer(m_numCachedProbes, System.Runtime.InteropServices.Marshal.SizeOf<RawSphericalHaramonicsL2>());
            m_outputCoeffsDeviceBuffer = new ComputeBuffer(m_numOutputProbes, System.Runtime.InteropServices.Marshal.SizeOf<RawSphericalHaramonicsL2>());

            // Upload the probe position data to the device
            m_probePositionDeviceBuffer.SetData(cellData.probePositions);*/

            Debug.Log("Created device buffers");
        }

        ~ProbeVolumeDenoiser()
        {
            // Clean up device objects
            m_probePositionDeviceBuffer.Dispose();
            m_outputCoeffsDeviceBuffer.Dispose();

            Debug.Log("Cleaned up!");
        }

        private void PrepareProbeVolume(CommandBuffer cmd)
        {
            var refVolume = ProbeReferenceVolume.instance;
            ProbeReferenceVolume.RuntimeResources rr = refVolume.GetRuntimeResources();

            bool validResources = rr.index != null && rr.L0_L1rx != null && rr.L1_G_ry != null && rr.L1_B_rz != null;

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

            cmd.SetComputeBufferParam(m_shaderHandle, m_populateKernelID, _ProbePositionsBuffer, m_probePositionDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_populateKernelID, _OutputCoeffs, m_outputCoeffsDeviceBuffer);
            cmd.SetComputeBufferParam(m_shaderHandle, m_populateKernelID, _CachedCoeffs, m_cachedCoeffsDeviceBuffer);

            cmd.SetComputeVectorParam(m_shaderHandle, _CacheLowerBound, new Vector4(m_cacheLowerBound.x, m_cacheLowerBound.y, m_cacheLowerBound.z));
            cmd.SetComputeVectorParam(m_shaderHandle, _CacheDims, new Vector4(m_cacheDims.x, m_cacheDims.y, m_cacheDims.z));
            cmd.SetComputeFloatParam(m_shaderHandle, _ProbeDelta, m_probeDelta);
            cmd.SetComputeIntParam(m_shaderHandle, _NumCachedProbes, m_numCachedProbes);
            cmd.SetComputeIntParam(m_shaderHandle, _NumOutputProbes, m_numOutputProbes);

            PrepareProbeVolume(cmd);

            // Read back the data from the device
            m_outputCoeffsDeviceBuffer.GetData(m_outputCoeffsHostBuffer);

            // 
            /*for (int probeIdx = 0; probeIdx < m_numOutputProbes; ++probeIdx)
            {
                m_outputProbesHostBuffer[probeIdx].PackToShaderCoefficients(m_cell, probeIdx);
            }*/
        }
    }
}
