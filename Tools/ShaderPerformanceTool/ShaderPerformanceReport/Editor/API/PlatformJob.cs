using System;

namespace UnityEditor.Experimental.ShaderTools
{
    [Flags]
    public enum PlatformJob
    {
        None = 0,
        BuildShaderPerfReport = 1 << 0,
        BuildComputeShaderPerfReport = 1 << 1,
        BuildMaterialPerfReport = 1 << 2
    }
}
