using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Chunk = UnityEngine.Experimental.Rendering.ProbeBrickPool.BrickChunkAlloc;

namespace UnityEngine.Experimental.Rendering
{
    partial class ProbeGIBaking
    {
        static ComputeShader dilationShader;
        static int dilationKernel = -1;

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

            internal SphericalHarmonicsL2 ToSphericalHarmonicsL2()
            {
                SphericalHarmonicsL2 sh = new SphericalHarmonicsL2();
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 0, L0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 1, L1_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 2, L1_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 3, L1_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 4, L2_0);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 5, L2_1);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 6, L2_2);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 7, L2_3);
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 8, L2_4);
                return sh;
            }

            internal void FromSphericalHarmonicsL2(SphericalHarmonicsL2 sh)
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

            internal void FromSphericalHarmonicsShaderConstants(ProbeVolumeSHBands srcBands, NativeArray<Vector4> shData, int probeIdx)
            {
                var stride = srcBands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? 7 : 3;
                var idx0 = probeIdx * stride;

                L0 = shData[idx0];
                L1_0 = new Vector3(shData[idx0 + 0].w, shData[idx0 + 1].x, shData[idx0 + 2].x);
                L1_1 = new Vector3(shData[idx0 + 1].w, shData[idx0 + 1].y, shData[idx0 + 2].y);
                L1_2 = new Vector3(shData[idx0 + 2].w, shData[idx0 + 1].z, shData[idx0 + 2].z);

                if (srcBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    L2_0 = new Vector3(shData[idx0 + 3].x, shData[idx0 + 4].x, shData[idx0 + 5].x);
                    L2_1 = new Vector3(shData[idx0 + 3].y, shData[idx0 + 4].y, shData[idx0 + 5].y);
                    L2_2 = new Vector3(shData[idx0 + 3].z, shData[idx0 + 4].z, shData[idx0 + 5].z);
                    L2_3 = new Vector3(shData[idx0 + 3].w, shData[idx0 + 4].w, shData[idx0 + 5].x);
                    L2_4 = shData[idx0 + 6];
                }
                else
                {
                    L2_0 = L2_1 = L2_2 = L2_3 = L2_4 = Vector3.zero;
                }
            }

            internal void ToSphericalHarmonicsShaderConstants(ProbeVolumeSHBands srcBands, NativeArray<Vector4> shData, int probeIdx)
            {
                var stride = srcBands == ProbeVolumeSHBands.SphericalHarmonicsL2 ? 7 : 3;
                var idx0 = probeIdx * stride;

                shData[idx0 + 0] = new Vector4(L0.x, L0.y, L0.z, L1_0.x);
                shData[idx0 + 1] = new Vector4(L1_0.y, L1_1.y, L1_2.z, L1_0.y);
                shData[idx0 + 2] = new Vector4(L1_0.z, L1_1.y, L1_2.z, L1_0.z);

                if (srcBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
                {
                    shData[idx0 + 3] = new Vector4(L2_0.x, L2_1.x, L2_2.x, L2_3.x);
                    shData[idx0 + 4] = new Vector4(L2_0.y, L2_1.y, L2_2.y, L2_3.y);
                    shData[idx0 + 5] = new Vector4(L2_0.z, L2_1.z, L2_2.z, L2_3.z);
                    shData[idx0 + 6] = new Vector4(L2_4.x, L2_4.y, L2_4.z, 1f);
                }
            }
        }

        struct DataForDilation
        {
            public ComputeBuffer validityBuffer { get; }
            public ComputeBuffer positionBuffer { get; }
            public ComputeBuffer outputProbes { get; }

            DilatedProbe[] dilatedProbes;

            ProbeReferenceVolume.Cell cell;

            public DataForDilation(ProbeReferenceVolume.Cell cell)
            {
                this.cell = cell;

                int probeCount = cell.probePositions.Length;

                validityBuffer = new ComputeBuffer(probeCount, sizeof(float));
                positionBuffer = new ComputeBuffer(probeCount, System.Runtime.InteropServices.Marshal.SizeOf<Vector3>());
                outputProbes = new ComputeBuffer(probeCount, System.Runtime.InteropServices.Marshal.SizeOf<DilatedProbe>());

                // Init with pre-dilated SH so we don't need to re-fill from sampled data from texture (that might be less precise).
                dilatedProbes = new DilatedProbe[probeCount];
                for (int i = 0; i < probeCount; ++i)
                {
                    dilatedProbes[i].FromSphericalHarmonicsShaderConstants(cell.shBands, cell.shData, i);
                }

                outputProbes.SetData(dilatedProbes);
                validityBuffer.SetData(cell.validity);
                positionBuffer.SetData(cell.probePositions);
            }

            public void ExtractDilatedProbes()
            {
                outputProbes.GetData(dilatedProbes);

                int probeCount = cell.probePositions.Length;
                for (int i = 0; i < probeCount; ++i)
                {
                    dilatedProbes[i].ToSphericalHarmonicsShaderConstants(cell.shBands, cell.shData, i);
                }
            }

            public void Dispose()
            {
                validityBuffer.Dispose();
                positionBuffer.Dispose();
                outputProbes.Dispose();
            }
        }

        static readonly int _ValidityBuffer = Shader.PropertyToID("_ValidityBuffer");
        static readonly int _ProbePositionsBuffer = Shader.PropertyToID("_ProbePositionsBuffer");
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

            DataForDilation data = new DataForDilation(cell);

            var cmd = CommandBufferPool.Get("Cell Dilation");

            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _ValidityBuffer, data.validityBuffer);
            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _ProbePositionsBuffer, data.positionBuffer);
            cmd.SetComputeBufferParam(dilationShader, dilationKernel, _OutputProbes, data.outputProbes);

            int probeCount = cell.probePositions.Length;

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
