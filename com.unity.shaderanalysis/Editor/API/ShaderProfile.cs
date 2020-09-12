using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Common shader profiles.</summary>
    public enum ShaderProfile
    {
        /// <summary>The target entry point is a pixel shader.</summary>
        PixelProgram,
        /// <summary>The target entry point is a compute shader.</summary>
        ComputeProgram
    }

    /// <summary>Shader API targets</summary>
    // ReSharper disable InconsistentNaming
    public enum ShaderTarget
    {
        /// <summary>Vertex shader 5</summary>
        VS_5 = 0,
        /// <summary>Pixel shader 5</summary>
        PS_5,
        /// <summary>Compute shader 5</summary>
        CS_5
    }
    // ReSharper restore InconsistentNaming
}
