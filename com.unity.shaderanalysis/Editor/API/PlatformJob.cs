using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Describes kind of jobs that can be requested.</summary>
    [Flags]
    public enum PlatformJob
    {
        None = 0,
        /// <summary>Build a performance report for a shader.</summary>
        BuildShaderPerfReport = 1 << 0,
        /// <summary>Build a performance report for a compute shader.</summary>
        BuildComputeShaderPerfReport = 1 << 1,
        /// <summary>Build a performance report for a shader in a material.</summary>
        BuildMaterialPerfReport = 1 << 2
    }
}
