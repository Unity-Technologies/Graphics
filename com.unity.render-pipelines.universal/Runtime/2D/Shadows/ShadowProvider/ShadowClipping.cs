using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    using Path = UnsafeList<IntPoint>;
    using Paths = UnsafeList<UnsafeList<IntPoint>>;

    public struct ShadowClipping
    {
        const float k_FloatMultipler = 65536;

        ClipperOffset m_ClipOffset;
        Paths m_Solution;


        public void Initialize()
        {
            m_ClipOffset = new ClipperOffset();
            m_Solution = new Paths(1, Allocator.Temp, NativeArrayOptions.ClearMemory);
        }

        public void Clear()
        {
            m_ClipOffset.Clear();
        }

        public void ContractPath(float offset)
        {
            m_ClipOffset.ArcTolerance = 200.0f / 4.0f; // low detail
            m_ClipOffset.Execute(ref m_Solution, k_FloatMultipler * offset);
        }

        public int GetOutputPaths()
        {
            return m_Solution.Length;
        }

        public int GetOutputPathLength(int pathIndex)
        {
            return m_Solution[pathIndex].Length;
        }

        public void GetOutputPath(int pathIndex, ref NativeArray<Vector3> outPath, int startIndex = 0)
        {
            Path clippedPath = m_Solution[pathIndex];

            int length = (int)(outPath.Length > clippedPath.Length ? clippedPath.Length : outPath.Length);

            outPath = new NativeArray<Vector3>(length + startIndex, Allocator.Temp, NativeArrayOptions.ClearMemory);
            for (int i = 0; i < length; i++)
            {
                outPath[i + startIndex] = new Vector3(clippedPath[i].X / k_FloatMultipler, clippedPath[i].Y / k_FloatMultipler, 0);
            }
        }

        public void AddInputPath(NativeArray<Vector3> inPath)
        {
            Path input = new Path(1, Allocator.Temp, NativeArrayOptions.ClearMemory);

            for (int i = 0; i < inPath.Length; i++)
                input.Add(new (inPath[i].x * k_FloatMultipler, inPath[i].y * k_FloatMultipler));

            //m_ClipOffset.AddPath(ref input, JoinType.jtSquare, EndType.etClosedPolygon);
            m_ClipOffset.AddPath(ref input, JoinType.jtRound, EndType.etClosedPolygon);
        }
    }
}
