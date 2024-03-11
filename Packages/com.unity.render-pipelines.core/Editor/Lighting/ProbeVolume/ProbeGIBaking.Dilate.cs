using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        static ComputeShader dilationShader;
        static int dilationKernel = -1;
        static internal Dictionary<(int, int), float> s_CustomDilationThresh = new Dictionary<(int, int), float>();

        static void InitDilationShaders()
        {
            if (dilationShader == null)
            {
                dilationShader = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.unity.render-pipelines.core/Editor/Lighting/ProbeVolume/ProbeVolumeCellDilation.compute");
                dilationKernel = dilationShader.FindKernel("DilateCell");
            }
        }

        [GenerateHLSL(needAccessors = false)]
        struct DilatedProbe
        {
            public Vector3 L0;

            public Vector3 L1_0;
            public Vector3 L1_1;
            public Vector3 L1_2;

            public Vector3 L2_0;
            public Vector3 L2_1;
            public Vector3 L2_2;
            public Vector3 L2_3;
            public Vector3 L2_4;

            void ToSphericalHarmonicsL2(ref SphericalHarmonicsL2 sh)
            {
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 0, L0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 1, L1_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 2, L1_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 3, L1_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 4, L2_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 5, L2_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 6, L2_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 7, L2_3);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 8, L2_4);
            }

            void FromSphericalHarmonicsL2(ref SphericalHarmonicsL2 sh)
            {
                L0 = new Vector3(sh[0, 0], sh[1, 0], sh[2, 0]);
                L1_0 = new Vector3(sh[0, 1], sh[1, 1], sh[2, 1]);
                L1_1 = new Vector3(sh[0, 2], sh[1, 2], sh[2, 2]);
                L1_2 = new Vector3(sh[0, 3], sh[1, 3], sh[2, 3]);
                L2_0 = new Vector3(sh[0, 4], sh[1, 4], sh[2, 4]);
                L2_1 = new Vector3(sh[0, 5], sh[1, 5], sh[2, 5]);
                L2_2 = new Vector3(sh[0, 6], sh[1, 6], sh[2, 6]);
                L2_3 = new Vector3(sh[0, 7], sh[1, 7], sh[2, 7]);
                L2_4 = new Vector3(sh[0, 8], sh[1, 8], sh[2, 8]);
            }

            internal void FromSphericalHarmonicsShaderConstants(ProbeReferenceVolume.Cell cell, int probeIdx)
            {
                var sh = new SphericalHarmonicsL2();

                GetProbeAndChunkIndex(probeIdx, out var chunkIndex, out var index);

                var cellChunkData = GetCellChunkData(cell.data, chunkIndex);

                ReadFromShaderCoeffsL0L1(ref sh, cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData, index * 4);
                ReadFromShaderCoeffsL2(ref sh, cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, index * 4);

                FromSphericalHarmonicsL2(ref sh);
            }

            internal void ToSphericalHarmonicsShaderConstants(ProbeReferenceVolume.Cell cell, int probeIdx)
            {
                var sh = new SphericalHarmonicsL2();
                ToSphericalHarmonicsL2(ref sh);

                GetProbeAndChunkIndex(probeIdx, out var chunkIndex, out var index);

                var cellChunkData = GetCellChunkData(cell.data, chunkIndex);

                WriteToShaderCoeffsL0L1(sh, cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData, index * 4);
                WriteToShaderCoeffsL2(sh, cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, index * 4);
            }
        }

        struct DataForDilation
        {
            public ComputeBuffer positionBuffer { get; }
            public ComputeBuffer outputProbes { get; }
            public ComputeBuffer needDilatingBuffer { get; }

            DilatedProbe[] dilatedProbes;

            ProbeReferenceVolume.Cell cell;

            public DataForDilation(ProbeReferenceVolume.Cell cell, float defaultThreshold)
            {
                this.cell = cell;
                var cellData = cell.data;
                var cellDesc = cell.desc;

                int probeCount = cellData.probePositions.Length;

                positionBuffer = new ComputeBuffer(probeCount, System.Runtime.InteropServices.Marshal.SizeOf<Vector3>());
                outputProbes = new ComputeBuffer(probeCount, System.Runtime.InteropServices.Marshal.SizeOf<DilatedProbe>());
                needDilatingBuffer = new ComputeBuffer(probeCount, sizeof(int));

                // Init with pre-dilated SH so we don't need to re-fill from sampled data from texture (that might be less precise).
                dilatedProbes = new DilatedProbe[probeCount];
                int[] needDilating = new int[probeCount];

                for (int i = 0; i < probeCount; ++i)
                {
                    dilatedProbes[i].FromSphericalHarmonicsShaderConstants(cell, i);
                    needDilating[i] = s_CustomDilationThresh.ContainsKey((cellDesc.index, i)) ?
                        (cellData.validity[i] > s_CustomDilationThresh[(cellDesc.index, i)] ? 1 : 0) : (cellData.validity[i] > defaultThreshold ? 1 : 0);
                }

                outputProbes.SetData(dilatedProbes);
                positionBuffer.SetData(cellData.probePositions);
                needDilatingBuffer.SetData(needDilating);
            }

            public void ExtractDilatedProbes()
            {
                outputProbes.GetData(dilatedProbes);

                int probeCount = cell.data.probePositions.Length;
                for (int i = 0; i < probeCount; ++i)
                {
                    dilatedProbes[i].ToSphericalHarmonicsShaderConstants(cell, i);
                }
            }

            public void Dispose()
            {
                positionBuffer.Dispose();
                outputProbes.Dispose();
                needDilatingBuffer.Dispose();
            }
        }

        static readonly int _ProbePositionsBuffer = Shader.PropertyToID("_ProbePositionsBuffer");
        static readonly int _NeedDilating = Shader.PropertyToID("_NeedDilating");
        static readonly int _DilationParameters = Shader.PropertyToID("_DilationParameters");
        static readonly int _DilationParameters2 = Shader.PropertyToID("_DilationParameters2");
        static readonly int _OutputProbes = Shader.PropertyToID("_OutputProbes");
        static readonly int _APVResIndex = Shader.PropertyToID("_APVResIndex");
        static readonly int _APVResCellIndices = Shader.PropertyToID("_APVResCellIndices");
        static readonly int _APVResL0_L1Rx = Shader.PropertyToID("_APVResL0_L1Rx");
        static readonly int _APVResL1G_L1Ry = Shader.PropertyToID("_APVResL1G_L1Ry");
        static readonly int _APVResL1B_L1Rz = Shader.PropertyToID("_APVResL1B_L1Rz");
        static readonly int _APVResL2_0 = Shader.PropertyToID("_APVResL2_0");
        static readonly int _APVResL2_1 = Shader.PropertyToID("_APVResL2_1");
        static readonly int _APVResL2_2 = Shader.PropertyToID("_APVResL2_2");
        static readonly int _APVResL2_3 = Shader.PropertyToID("_APVResL2_3");

        static void PerformDilation(ProbeReferenceVolume.Cell cell, ProbeDilationSettings settings)
        {
            InitDilationShaders();

            DataForDilation data = new DataForDilation(cell, settings.dilationValidityThreshold);

            var cmd = CommandBufferPool.Get("Cell Dilation");

            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _ProbePositionsBuffer, data.positionBuffer);
            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _OutputProbes, data.outputProbes);
            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _NeedDilating, data.needDilatingBuffer);

            // There's an upper limit on the number of bricks supported inside a single cell
            int probeCount = Mathf.Min(cell.data.probePositions.Length, ushort.MaxValue * ProbeBrickPool.kBrickProbeCountTotal);

            cmd.SetComputeVectorParam(dilationShader, _DilationParameters, new Vector4(probeCount, settings.dilationValidityThreshold, settings.dilationDistance, ProbeReferenceVolume.instance.MinBrickSize()));
            cmd.SetComputeVectorParam(dilationShader, _DilationParameters2, new Vector4(settings.squaredDistWeighting ? 1 : 0, 0, 0, 0));

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


            int groupCount = (probeCount + 63) / 64;
            cmd.DispatchCompute(dilationShader, dilationKernel, groupCount, 1, 1);

            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);

            data.ExtractDilatedProbes();
            data.Dispose();
        }
    }
}
