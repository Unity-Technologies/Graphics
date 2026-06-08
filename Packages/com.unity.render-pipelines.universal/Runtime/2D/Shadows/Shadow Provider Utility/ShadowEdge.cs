using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ShadowEdge
    {
        public int v0;
        public int v1;

        public ShadowEdge(int indexA, int indexB)
        {
            v0 = indexA;
            v1 = indexB;
        }

        public void Reverse()
        {
            int tmp = v0;
            v0 = v1;
            v1 = tmp;
        }
    }
}
