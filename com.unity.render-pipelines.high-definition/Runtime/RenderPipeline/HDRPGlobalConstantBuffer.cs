using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct HDRPGlobalConstantBuffer
    {
//        public LightLoopShaderVariables m_LightLoopVars;
        public DecalShaderVariables m_DecalVars;
        public SubsurfaceScatteringShaderVariables m_SSSVars;
        public ShaderVariablesScreenSpaceLighting m_SSLVars;
    }
}
