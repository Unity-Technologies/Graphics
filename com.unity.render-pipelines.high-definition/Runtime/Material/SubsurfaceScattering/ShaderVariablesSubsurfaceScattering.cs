using UnityEngine.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct SubsurfaceScatteringShaderVariables
    {
        // Warning: Unity is not able to losslessly transfer integers larger than 2^24 to the shader system.
        // Therefore, we bitcast uint to float in C#, and bitcast back to uint in the shader.
        public uint m_EnableSubsurfaceScattering; // Globally toggles subsurface and transmission scattering on/off
        public float m_TexturingModeFlags;         // 1 bit/profile; 0 = PreAndPostScatter, 1 = PostScatter
        public float m_TransmissionFlags;          // 1 bit/profile; 0 = regular, 1 = thin
        public uint m_Pad;
                                                   // Use float4 to avoid any packing issue between compute and pixel shaders
        public fixed float m_ThicknessRemaps[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];   // R: start, G = end - start, BA unused
        public fixed float m_ShapeParams[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];        // RGB = S = 1 / D, A = filter radius
        public fixed float m_TransmissionTintsAndFresnel0[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];  // RGB = 1/4 * color, A = fresnel0
        public fixed float m_WorldScales[DiffusionProfileConstants.DIFFUSION_PROFILE_COUNT * 4];        // X = meters per world unit; Y = world units per meter
    }
}
