using System;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.Graphing;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    static class PassDescriptorExtension
    {
        public static bool IsDepthOrMV(this PassDescriptor pass)
        {
            return pass.lightMode == HDShaderPassNames.s_DepthForwardOnlyStr
                || pass.lightMode == HDShaderPassNames.s_DepthOnlyStr
                || pass.lightMode == HDShaderPassNames.s_MotionVectorsStr;
        }

        public static bool IsLightingOrMaterial(this PassDescriptor pass)
        {
            return pass.IsForward()
                || pass.lightMode == HDShaderPassNames.s_GBufferStr
                // DXR passes without visibility, prepass or path tracing
                || (pass.lightMode.Contains("DXR") && pass.lightMode != HDShaderPassNames.s_RayTracingVisibilityStr && pass.lightMode != HDShaderPassNames.s_PathTracingDXRStr);
        }

        public static bool IsDXR(this PassDescriptor pass)
        {
            return pass.lightMode.Contains("DXR")
                || pass.lightMode == HDShaderPassNames.s_RayTracingVisibilityStr
                || pass.lightMode == HDShaderPassNames.s_RayTracingPrepassStr;
        }

        public static bool IsForward(this PassDescriptor pass)
        {
            return pass.lightMode == HDShaderPassNames.s_ForwardOnlyStr
                || pass.lightMode == HDShaderPassNames.s_ForwardStr
                || pass.lightMode == HDShaderPassNames.s_TransparentBackfaceStr;
        }
    }
}
