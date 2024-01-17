using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using Chunk = UnityEngine.Rendering.ProbeBrickPool.BrickChunkAlloc;

namespace UnityEngine.Rendering
{
    partial class ProbeGIBaking
    {
        [GenerateHLSL(needAccessors = false)]
        public struct RawSphericalHarmonicsL2
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

                /*SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 0, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 1, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 2, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 3, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 4, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 5, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 6, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 7, new Vector3(0.0f, 0.0f, 0.0f));
                SphericalHarmonicsL2Utils.SetCoefficient(ref sh, 8, new Vector3(0.0f, 0.0f, 0.0f));*/
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

            internal void UnpackFromShaderCoefficients(ProbeReferenceVolume.Cell cell, int probeIdx)
            {
                var sh = new SphericalHarmonicsL2();

                GetProbeAndChunkIndex(probeIdx, out var chunkIndex, out var index);

                var cellChunkData = GetCellChunkData(cell.data, chunkIndex);

                ReadFromShaderCoeffsL0L1(ref sh, cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData, index * 4);
                ReadFromShaderCoeffsL2(ref sh, cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, index * 4);

                FromSphericalHarmonicsL2(ref sh);
            }

            internal void PackToShaderCoefficients(ProbeReferenceVolume.Cell cell, int probeIdx)
            {
                var sh = new SphericalHarmonicsL2();
                ToSphericalHarmonicsL2(ref sh);

                GetProbeAndChunkIndex(probeIdx, out var chunkIndex, out var index);

                var cellChunkData = GetCellChunkData(cell.data, chunkIndex);

                WriteToShaderCoeffsL0L1(sh, cellChunkData.shL0L1RxData, cellChunkData.shL1GL1RyData, cellChunkData.shL1BL1RzData, index * 4);
                WriteToShaderCoeffsL2(sh, cellChunkData.shL2Data_0, cellChunkData.shL2Data_1, cellChunkData.shL2Data_2, cellChunkData.shL2Data_3, index * 4);
            }
        }
    }
}
