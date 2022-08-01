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
            if (pass.lightMode == null)
                return false;

            return pass.IsForward()
                || pass.lightMode == HDShaderPassNames.s_GBufferStr
                // DXR passes without visibility, prepass or path tracing
                || (pass.lightMode.Contains("DXR") && pass.lightMode != HDShaderPassNames.s_RayTracingVisibilityStr && pass.lightMode != HDShaderPassNames.s_PathTracingDXRStr);
        }

        public static bool IsForward(this PassDescriptor pass)
        {
            return pass.lightMode == HDShaderPassNames.s_ForwardOnlyStr
                || pass.lightMode == HDShaderPassNames.s_ForwardStr
                || pass.lightMode == HDShaderPassNames.s_TransparentBackfaceStr;
        }

        public static bool NeedsDebugDisplay(this PassDescriptor pass)
        {
            return IsLightingOrMaterial(pass);
        }

        public static bool IsRaytracing(this PassDescriptor pass)
        {
            if (pass.pragmas == null)
                return false;

            foreach (var pragma in pass.pragmas)
            {
                if (pragma.value == "#pragma raytracing surface_shader")
                    return true;
            }

            return false;
        }

        // This function allow to know if a pass is used in context of raytracing rendering even if the pass is not a rayrtacing pass itself (like with RaytracingPrepass)
        public static bool IsRelatedToRaytracing(this PassDescriptor pass)
        {
            return pass.lightMode.Contains("DXR")
                || pass.lightMode == HDShaderPassNames.s_RayTracingVisibilityStr
                || pass.lightMode == HDShaderPassNames.s_RayTracingPrepassStr;
        }

        public static bool IsTessellation(this PassDescriptor pass)
        {
            foreach (var pragma in pass.pragmas)
            {
                if (pragma.value == "#pragma hull Hull")
                    return true;
            }

            return false;
        }
    }
}
