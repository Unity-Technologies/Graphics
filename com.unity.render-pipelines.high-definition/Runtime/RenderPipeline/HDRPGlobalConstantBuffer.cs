using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct HDRPGlobalConstantBuffer
    {
        public DecalShaderVariables m_DecalVars;
        public SubsurfaceScatteringShaderVariables m_SSSVars;
    }
}
