using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct DecalShaderVariables
    {
        public float m_DecalAtlasResolutionX;
        public float m_DecalAtlasResolutionY;
        public uint m_EnableDecals;
        private uint m_Pad;
        // DecalCount moved to light loop struct
    }
}
